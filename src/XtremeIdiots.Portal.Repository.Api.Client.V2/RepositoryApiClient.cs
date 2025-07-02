namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    /// <summary>
    /// Unified repository API client providing access to all V2 APIs.
    /// Use like: client.Root.V2
    /// </summary>
    public class RepositoryApiClient : IRepositoryApiClient
    {
        public RepositoryApiClient(IVersionedRootApi root)
        {
            Root = root;
        }

        public IVersionedRootApi Root { get; }
    }
}
