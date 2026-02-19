using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

using V1RootDto = XtremeIdiots.Portal.Repository.Abstractions.V1.Models.Root.RootDto;
using V1_1RootDto = XtremeIdiots.Portal.Repository.Abstractions.V1_1.Models.Root.RootDto;
using V1_1Interfaces = XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1_1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeRootApi : IRootApi, V1_1Interfaces.IRootApi
{
    private V1RootDto _v1Root = new();
    private V1_1RootDto _v1_1Root = new();

    public FakeRootApi WithV1Root(V1RootDto root) { _v1Root = root; return this; }
    public FakeRootApi WithV1_1Root(V1_1RootDto root) { _v1_1Root = root; return this; }
    public FakeRootApi Reset() { _v1Root = new(); _v1_1Root = new(); return this; }

    Task<ApiResult<V1RootDto>> IRootApi.GetRoot(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ApiResult<V1RootDto>(HttpStatusCode.OK, new ApiResponse<V1RootDto>(_v1Root)));
    }

    Task<ApiResult<V1_1RootDto>> V1_1Interfaces.IRootApi.GetRoot(CancellationToken cancellationToken)
    {
        return Task.FromResult(new ApiResult<V1_1RootDto>(HttpStatusCode.OK, new ApiResponse<V1_1RootDto>(_v1_1Root)));
    }
}
