using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;
using RestSharp;
using System;
using System.Threading.Tasks;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Tags;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class TagsApi : BaseApi, ITagsApi
    {
        public TagsApi(ILogger<TagsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton)
            : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<TagsCollectionDto>> GetTags(int skipEntries, int takeEntries)
        {
            var request = await CreateRequestAsync($"tags", Method.Get);
            request.AddQueryParameter("skipEntries", skipEntries);
            request.AddQueryParameter("takeEntries", takeEntries);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse<TagsCollectionDto>();
        }

        public async Task<ApiResponseDto<TagDto>> GetTag(Guid tagId)
        {
            var request = await CreateRequestAsync($"tags/{tagId}", Method.Get);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse<TagDto>();
        }

        public async Task<ApiResponseDto> CreateTag(TagDto tagDto)
        {
            var request = await CreateRequestAsync($"tags", Method.Post);
            request.AddJsonBody(tagDto);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateTag(TagDto tagDto)
        {
            var request = await CreateRequestAsync($"tags/{tagDto.TagId}", Method.Put);
            request.AddJsonBody(tagDto);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeleteTag(Guid tagId)
        {
            var request = await CreateRequestAsync($"tags/{tagId}", Method.Delete);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto<PlayerTagsCollectionDto>> GetPlayerTags(Guid playerId)
        {
            var request = await CreateRequestAsync($"tags/player/{playerId}", Method.Get);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse<PlayerTagsCollectionDto>();
        }

        public async Task<ApiResponseDto> AddPlayerTag(PlayerTagDto playerTagDto)
        {
            var request = await CreateRequestAsync($"tags/player", Method.Post);
            request.AddJsonBody(playerTagDto);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> RemovePlayerTag(Guid playerTagId)
        {
            var request = await CreateRequestAsync($"tags/player/{playerTagId}", Method.Delete);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse();
        }
    }
}
