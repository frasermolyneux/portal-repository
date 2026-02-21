using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    public interface IVersionedApiHealthApi
    {
        IApiHealthApi V2 { get; }
    }

    public interface IVersionedApiInfoApi
    {
        IApiInfoApi V2 { get; }
    }
}
