using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MxIO.ApiClient.Abstractions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Tags;

namespace XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces
{
    /// <summary>
    /// API contract for managing Tags and PlayerTags.
    /// </summary>
    public interface ITagsApi
    {
        Task<ApiResponseDto<TagsCollectionDto>> GetTags(int skipEntries, int takeEntries);
        Task<ApiResponseDto<TagDto>> GetTag(Guid tagId);
        Task<ApiResponseDto> CreateTag(TagDto tagDto);
        Task<ApiResponseDto> UpdateTag(TagDto tagDto);
        Task<ApiResponseDto> DeleteTag(Guid tagId);

        Task<ApiResponseDto<PlayerTagsCollectionDto>> GetPlayerTags(Guid playerId);
        Task<ApiResponseDto> AddPlayerTag(PlayerTagDto playerTagDto);
        Task<ApiResponseDto> RemovePlayerTag(Guid playerTagId);
    }
}
