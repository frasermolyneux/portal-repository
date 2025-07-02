using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    public class VersionedRootApi : IVersionedRootApi
    {
        public VersionedRootApi(IRootApi v2Api)
        {
            V2 = v2Api;
        }

        public IRootApi V2 { get; }
    }
}
