namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

public static class BoundedPruner
{
    public const int BatchSize = 1000;
    public const int MaxBatches = 20;

    public static async Task<bool> RunAsync(
        IReadOnlyList<Func<CancellationToken, Task<int>>> deleteBatches,
        CancellationToken cancellationToken)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(20));
        var batches = 0;
        try
        {
            foreach (var deleteBatch in deleteBatches)
            {
                while (batches < MaxBatches)
                {
                    budget.Token.ThrowIfCancellationRequested();
                    var affected = await deleteBatch(budget.Token).ConfigureAwait(false);
                    batches++;
                    if (affected == 0)
                    {
                        break;
                    }
                }

                if (batches >= MaxBatches)
                {
                    return false;
                }
            }

            return true;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && budget.IsCancellationRequested)
        {
            // Earlier batches have committed; the next invocation resumes from remaining rows.
            return false;
        }
    }
}
