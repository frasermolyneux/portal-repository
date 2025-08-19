using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class TagsApi : BaseApi<RepositoryApiClientOptions>, ITagsApi
    {
        public TagsApi(
            ILogger<BaseApi<RepositoryApiClientOptions>> logger,
            IApiTokenProvider? apiTokenProvider,
            IRestClientService restClientService,
            RepositoryApiClientOptions options)
            : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<CollectionModel<TagDto>>> GetTags(int skipEntries, int takeEntries, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = await CreateRequestAsync($"v1/tags", Method.Get, cancellationToken);
                request.AddQueryParameter("skipEntries", skipEntries);
                request.AddQueryParameter("takeEntries", takeEntries);
                var response = await ExecuteAsync(request, cancellationToken);
                return response.ToApiResult<CollectionModel<TagDto>>();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var errorResponse = new ApiResponse<CollectionModel<TagDto>>(
                    new ApiError("CLIENT_ERROR", "Failed to retrieve tags"));
                return new ApiResult<CollectionModel<TagDto>>(System.Net.HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        public async Task<ApiResult<TagDto>> GetTag(Guid tagId, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = await CreateRequestAsync($"v1/tags/{tagId}", Method.Get, cancellationToken);
                var response = await ExecuteAsync(request, cancellationToken);
                return response.ToApiResult<TagDto>();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var errorResponse = new ApiResponse<TagDto>(
                    new ApiError("CLIENT_ERROR", "Failed to retrieve tag"));
                return new ApiResult<TagDto>(System.Net.HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        public async Task<ApiResult> CreateTag(TagDto tagDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = await CreateRequestAsync($"v1/tags", Method.Post, cancellationToken);
                request.AddJsonBody(tagDto);
                var response = await ExecuteAsync(request, cancellationToken);

                var result = response.ToApiResult();
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var errorResponse = new ApiResponse(
                    new ApiError("CLIENT_ERROR", "Failed to create tag"));
                return new ApiResult(System.Net.HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        public async Task<ApiResult> UpdateTag(TagDto tagDto, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = await CreateRequestAsync($"v1/tags/{tagDto.TagId}", Method.Put, cancellationToken);
                request.AddJsonBody(tagDto);
                var response = await ExecuteAsync(request, cancellationToken);

                var result = response.ToApiResult();
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var errorResponse = new ApiResponse(
                    new ApiError("CLIENT_ERROR", "Failed to update tag"));
                return new ApiResult(System.Net.HttpStatusCode.InternalServerError, errorResponse);
            }
        }

        public async Task<ApiResult> DeleteTag(Guid tagId, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = await CreateRequestAsync($"v1/tags/{tagId}", Method.Delete, cancellationToken);
                var response = await ExecuteAsync(request, cancellationToken);

                var result = response.ToApiResult();
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var errorResponse = new ApiResponse(
                    new ApiError("CLIENT_ERROR", "Failed to delete tag"));
                return new ApiResult(System.Net.HttpStatusCode.InternalServerError, errorResponse);
            }
        }
    }
}


