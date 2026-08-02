namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Caching
{
    /// <summary>
    /// No-op invalidator registered when the shared cache is not configured. Keeps controllers
    /// free of null checks by making every eviction call an inexpensive completed <see cref="Task"/>.
    /// </summary>
    public sealed class NoOpRepositoryCacheInvalidator : IRepositoryCacheInvalidator
    {
        public Task InvalidateGameServerAsync(Guid gameServerId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateDashboardAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateServerSettingsAsync(Guid gameServerId, string ns, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateGlobalNamespaceAsync(string ns, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateMapAsync(Guid mapId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateAllMapsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateTagPlayerCountsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
