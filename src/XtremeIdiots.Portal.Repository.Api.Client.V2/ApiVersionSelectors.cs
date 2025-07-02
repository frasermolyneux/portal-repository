using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    public interface IVersionedRootApi
    {
        IRootApi V2 { get; }
    }
}
