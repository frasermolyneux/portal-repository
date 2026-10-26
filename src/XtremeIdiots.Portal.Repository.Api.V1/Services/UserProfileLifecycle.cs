using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

internal static class UserProfileLifecycle
{
    // Share this transaction-owned lock with connected-player writes. Infrequent profile
    // mutations are serialized across hosts without filtering historical relationships.
    public static async Task LockAsync(PortalDbContext context, CancellationToken cancellationToken)
    {
        if (context.Database.IsSqlServer())
        {
            await context.Database.ExecuteSqlRawAsync("""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = N'Portal.UserProfileLifecycle',
                    @LockMode = N'Exclusive',
                    @LockOwner = N'Transaction',
                    @LockTimeout = 30000;
                IF @result < 0 THROW 51000, 'Unable to lock user profile lifecycle.', 1;
                """, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task<T> ExecuteAsync<T>(PortalDbContext context, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        if (!context.Database.IsRelational())
        {
            context.ChangeTracker.Clear();
            return await action().ConfigureAwait(false);
        }

        var invocation = new Invocation<T>();
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(invocation, async (_, state, token) =>
            {
                context.ChangeTracker.Clear();
                await using var transaction = await context.Database.BeginTransactionAsync(token).ConfigureAwait(false);
                await LockAsync(context, token).ConfigureAwait(false);

                if (state.HasResult && await IsCommitted(context, state.OperationId, token).ConfigureAwait(false))
                {
                    return state.Result;
                }

                state.HasResult = false;
                state.Result = await action().ConfigureAwait(false);
                state.HasResult = true;
                // Rejected batches may leave unsaved changes; only the core operation
                // may persist domain changes, not the commit-verification marker.
                context.ChangeTracker.Clear();
                var now = DateTime.UtcNow;
                context.UserProfileLifecycleOperations.Add(new UserProfileLifecycleOperation { OperationId = state.OperationId, CompletedAtUtc = now });
                await context.SaveChangesAsync(token).ConfigureAwait(false);
                // These are bounded retry-verification markers, not historical audit records.
                await context.UserProfileLifecycleOperations.Where(operation => operation.CompletedAtUtc < now.AddDays(-7))
                    .ExecuteDeleteAsync(token).ConfigureAwait(false);
                await transaction.CommitAsync(token).ConfigureAwait(false);
                return state.Result;
            },
            async (_, state, token) =>
            {
                var committed = state.HasResult && await IsCommitted(context, state.OperationId, token).ConfigureAwait(false);
                return new ExecutionResult<T>(committed, state.Result);
            }, cancellationToken).ConfigureAwait(false);
    }

    private static Task<bool> IsCommitted(PortalDbContext context, Guid operationId, CancellationToken cancellationToken)
        => context.UserProfileLifecycleOperations.AsNoTracking()
            .AnyAsync(operation => operation.OperationId == operationId, cancellationToken);

    private sealed class Invocation<T>
    {
        public Guid OperationId { get; } = Guid.NewGuid();
        public T Result { get; set; } = default!;
        public bool HasResult { get; set; }
    }
}
