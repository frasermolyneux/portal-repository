using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MxIO.ApiClient;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.Extensions;

using RestSharp;

using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.UserProfiles;

namespace XtremeIdiots.Portal.RepositoryApiClient.Api
{
    public class UserProfileApi : BaseApi, IUserProfileApi
    {
        public UserProfileApi(ILogger<UserProfileApi> logger, IApiTokenProvider apiTokenProvider, IOptions<RepositoryApiClientOptions> options, IRestClientSingleton restClientSingleton) : base(logger, apiTokenProvider, restClientSingleton, options)
        {
        }

        public async Task<ApiResponseDto<UserProfileDto>> GetUserProfile(Guid userProfileId)
        {
            var request = await CreateRequestAsync($"user-profile/{userProfileId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<UserProfileDto>();
        }

        public async Task<ApiResponseDto<UserProfileDto>> GetUserProfileByIdentityId(string identityId)
        {
            var request = await CreateRequestAsync($"user-profile/by-identity-id/{identityId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<UserProfileDto>();
        }

        public async Task<ApiResponseDto<UserProfileDto>> GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId)
        {
            var request = await CreateRequestAsync($"user-profile/by-xtremeidiots-id/{xtremeIdiotsId}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<UserProfileDto>();
        }

        public async Task<ApiResponseDto<UserProfileDto>> GetUserProfileByDemoAuthKey(string demoAuthKey)
        {
            var request = await CreateRequestAsync($"user-profile/by-demo-auth-key/{demoAuthKey}", Method.Get);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse<UserProfileDto>();
        }

        public async Task<ApiResponseDto<UserProfileCollectionDto>> GetUserProfiles(string? filterString, int skipEntries, int takeEntries, UserProfilesOrder? order)
        {
            var request = await CreateRequestAsync("user-profile", Method.Get);

            if (!string.IsNullOrWhiteSpace(filterString))
                request.AddQueryParameter("filterString", filterString.ToString());

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request);

            return response.ToApiResponse<UserProfileCollectionDto>();
        }

        public async Task<ApiResponseDto> CreateUserProfile(CreateUserProfileDto createUserProfileDto)
        {
            var request = await CreateRequestAsync("user-profile", Method.Post);
            request.AddJsonBody(new List<CreateUserProfileDto> { createUserProfileDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos)
        {
            var request = await CreateRequestAsync("user-profile", Method.Post);
            request.AddJsonBody(createUserProfileDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateUserProfile(EditUserProfileDto editUserProfileDto)
        {
            var request = await CreateRequestAsync("user-profile", Method.Put);
            request.AddJsonBody(new List<EditUserProfileDto> { editUserProfileDto });

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos)
        {
            var request = await CreateRequestAsync("user-profile", Method.Put);
            request.AddJsonBody(editUserProfileDtos);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto)
        {
            var request = await CreateRequestAsync($"user-profile/{userProfileId}/claims", Method.Patch);
            request.AddJsonBody(createUserProfileClaimDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto)
        {
            var request = await CreateRequestAsync($"user-profile/{userProfileId}/claims", Method.Post);
            request.AddJsonBody(createUserProfileClaimDto);

            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }

        public async Task<ApiResponseDto> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId)
        {
            var request = await CreateRequestAsync($"user-profile/{userProfileId}/claims/{userProfileClaimId}", Method.Delete);
            var response = await ExecuteAsync(request);

            return response.ToApiResponse();
        }
    }
}
