using Microsoft.EntityFrameworkCore;
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
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            if (!context.Database.IsRelational())
            {
                return await action().ConfigureAwait(false);
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await LockAsync(context, cancellationToken).ConfigureAwait(false);
            var result = await action().ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }).ConfigureAwait(false);
    }
}
