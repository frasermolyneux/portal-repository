using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeTagsApi : ITagsApi
{
    private readonly ConcurrentDictionary<Guid, TagDto> _tags = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeTagsApi AddTag(TagDto tag) { _tags[tag.TagId] = tag; return this; }
    public FakeTagsApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeTagsApi Reset() { _tags.Clear(); _errorResponses.Clear(); return this; }

    public Task<ApiResult<CollectionModel<TagDto>>> GetTags(int skipEntries, int takeEntries, CancellationToken cancellationToken = default)
    {
        var items = _tags.Values.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<TagDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<TagDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<TagDto>>(collection)));
    }

    public Task<ApiResult<TagDto>> GetTag(Guid tagId, CancellationToken cancellationToken = default)
    {
        if (_tags.TryGetValue(tagId, out var tag))
            return Task.FromResult(new ApiResult<TagDto>(HttpStatusCode.OK, new ApiResponse<TagDto>(tag)));
        return Task.FromResult(new ApiResult<TagDto>(HttpStatusCode.NotFound, new ApiResponse<TagDto>(new ApiError("NOT_FOUND", "Tag not found"))));
    }

    public Task<ApiResult> CreateTag(TagDto tagDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> UpdateTag(TagDto tagDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteTag(Guid tagId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
