using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    public class VersionedApiHealthApi : IVersionedApiHealthApi
    {
        public VersionedApiHealthApi(IApiHealthApi v2Api)
        {
            V2 = v2Api;
        }

        public IApiHealthApi V2 { get; }
    }

    public class VersionedApiInfoApi : IVersionedApiInfoApi
    {
        public VersionedApiInfoApi(IApiInfoApi v2Api)
        {
            V2 = v2Api;
        }

        public IApiInfoApi V2 { get; }
    }
}
