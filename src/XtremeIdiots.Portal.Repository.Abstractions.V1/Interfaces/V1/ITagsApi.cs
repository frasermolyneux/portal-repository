using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    /// <summary>
    /// API contract for managing Tags.
    /// </summary>
    /// <remarks>
    /// Player tag operations have been moved to IPlayersApi.
    /// </remarks>
    public interface ITagsApi
    {
        Task<ApiResult<CollectionModel<TagDto>>> GetTags(int skipEntries, int takeEntries, CancellationToken cancellationToken = default);
        Task<ApiResult<TagDto>> GetTag(Guid tagId, CancellationToken cancellationToken = default);
        Task<ApiResult> CreateTag(TagDto tagDto, CancellationToken cancellationToken = default);
        Task<ApiResult> UpdateTag(TagDto tagDto, CancellationToken cancellationToken = default);
        Task<ApiResult> DeleteTag(Guid tagId, CancellationToken cancellationToken = default);
    }
}
