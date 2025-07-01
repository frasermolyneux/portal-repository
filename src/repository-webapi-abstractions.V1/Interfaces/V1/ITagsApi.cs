using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MxIO.ApiClient.Abstractions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.V1.Tags;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces.V1
{
    /// <summary>
    /// API contract for managing Tags.
    /// </summary>
    /// <remarks>
    /// Player tag operations have been moved to IPlayersApi.
    /// </remarks>
    public interface ITagsApi
    {
        Task<ApiResponseDto<TagsCollectionDto>> GetTags(int skipEntries, int takeEntries);
        Task<ApiResponseDto<TagDto>> GetTag(Guid tagId);
        Task<ApiResponseDto> CreateTag(TagDto tagDto);
        Task<ApiResponseDto> UpdateTag(TagDto tagDto);
        Task<ApiResponseDto> DeleteTag(Guid tagId);
    }
}
