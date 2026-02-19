using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Demos;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeDemosApi : IDemosApi
{
    private readonly ConcurrentDictionary<Guid, DemoDto> _demos = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeDemosApi AddDemo(DemoDto demo) { _demos[demo.DemoId] = demo; return this; }
    public FakeDemosApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeDemosApi Reset() { _demos.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<DemoDto>> GetDemo(Guid demoId, CancellationToken cancellationToken = default)
    {
        if (_demos.TryGetValue(demoId, out var demo))
            return Task.FromResult(new ApiResult<DemoDto>(HttpStatusCode.OK, new ApiResponse<DemoDto>(demo)));
        return Task.FromResult(new ApiResult<DemoDto>(HttpStatusCode.NotFound, new ApiResponse<DemoDto>(new ApiError("NOT_FOUND", "Demo not found"))));
    }

    public Task<ApiResult<CollectionModel<DemoDto>>> GetDemos(GameType[]? gameTypes, string? userId, string? filterString, int skipEntries, int takeEntries, DemoOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _demos.Values.AsEnumerable();
        if (gameTypes != null) items = items.Where(d => gameTypes.Contains(d.GameType));
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<DemoDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<DemoDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<DemoDto>>(collection)));
    }

    public Task<ApiResult<DemoDto>> CreateDemo(CreateDemoDto createDemoDto, CancellationToken cancellationToken = default)
    {
        var demo = new DemoDto();
        return Task.FromResult(new ApiResult<DemoDto>(HttpStatusCode.OK, new ApiResponse<DemoDto>(demo)));
    }

    public Task<ApiResult> SetDemoFile(Guid demoId, string fileName, string filePath, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteDemo(Guid demoId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
