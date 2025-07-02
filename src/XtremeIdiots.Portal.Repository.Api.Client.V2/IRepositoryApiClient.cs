using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    public interface IRepositoryApiClient
    {
        IVersionedRootApi Root { get; }
    }
}
