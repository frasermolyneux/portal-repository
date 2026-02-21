namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    /// <summary>
    /// Unified repository API client providing access to all V2 APIs.
    /// </summary>
    public class RepositoryApiClient : IRepositoryApiClient
    {
        public RepositoryApiClient(IVersionedApiHealthApi apiHealth, IVersionedApiInfoApi apiInfo)
        {
            ApiHealth = apiHealth;
            ApiInfo = apiInfo;
        }

        public IVersionedApiHealthApi ApiHealth { get; }
        public IVersionedApiInfoApi ApiInfo { get; }
    }
}
