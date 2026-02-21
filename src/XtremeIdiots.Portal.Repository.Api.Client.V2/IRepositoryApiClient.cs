namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    public interface IRepositoryApiClient
    {
        IVersionedApiHealthApi ApiHealth { get; }
        IVersionedApiInfoApi ApiInfo { get; }
    }
}
