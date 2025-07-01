using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class TagsApi : BaseApi, ITagsApi
    {
        public TagsApi(ILogger<TagsApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton)
            : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<TagsCollectionDto>> GetTags(int skipEntries, int takeEntries)
        {
            var request = await CreateRequestAsync($"v1/tags", Method.Get);
            request.AddQueryParameter("skipEntries", skipEntries);
            request.AddQueryParameter("takeEntries", takeEntries);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse<TagsCollectionDto>();
        }

        public async Task<ApiResponseDto<TagDto>> GetTag(Guid tagId)
        {
            var request = await CreateRequestAsync($"v1/tags/{tagId}", Method.Get);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse<TagDto>();
        }

        public async Task<ApiResponseDto> CreateTag(TagDto tagDto)
        {
            var request = await CreateRequestAsync($"v1/tags", Method.Post);
            request.AddJsonBody(tagDto);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateTag(TagDto tagDto)
        {
            var request = await CreateRequestAsync($"v1/tags/{tagDto.TagId}", Method.Put);
            request.AddJsonBody(tagDto);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse();
        }
        public async Task<ApiResponseDto> DeleteTag(Guid tagId)
        {
            var request = await CreateRequestAsync($"v1/tags/{tagId}", Method.Delete);
            var response = await ExecuteAsync(request);
            return response.ToApiResponse();
        }
    }
}
