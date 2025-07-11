using Microsoft.Extensions.Logging;



using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class UserProfileApi : BaseApi<RepositoryApiClientOptions>, IUserProfileApi
    {
        public UserProfileApi(ILogger<UserProfileApi> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        public async Task<ApiResult<UserProfileDto>> GetUserProfile(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/user-profile/{userProfileId}", Method.Get);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<UserProfileDto>();
        }

        public async Task<ApiResult<UserProfileDto>> GetUserProfileByIdentityId(string identityId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/user-profile/by-identity-id/{identityId}", Method.Get);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<UserProfileDto>();
        }

        public async Task<ApiResult<UserProfileDto>> GetUserProfileByXtremeIdiotsId(string xtremeIdiotsId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/user-profile/by-xtremeidiots-id/{xtremeIdiotsId}", Method.Get);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<UserProfileDto>();
        }

        public async Task<ApiResult<UserProfileDto>> GetUserProfileByDemoAuthKey(string demoAuthKey, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/user-profile/by-demo-auth-key/{demoAuthKey}", Method.Get);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<UserProfileDto>();
        }

        public async Task<ApiResult<CollectionModel<UserProfileDto>>> GetUserProfiles(string? filterString, int skipEntries, int takeEntries, UserProfilesOrder? order, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/user-profile", Method.Get);

            if (!string.IsNullOrWhiteSpace(filterString))
                request.AddQueryParameter("filterString", filterString.ToString());

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult<CollectionModel<UserProfileDto>>();
        }

        public async Task<ApiResult> CreateUserProfile(CreateUserProfileDto createUserProfileDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/user-profile", Method.Post);
            request.AddJsonBody(new List<CreateUserProfileDto> { createUserProfileDto });

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> CreateUserProfiles(List<CreateUserProfileDto> createUserProfileDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/user-profile", Method.Post);
            request.AddJsonBody(createUserProfileDtos);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateUserProfile(EditUserProfileDto editUserProfileDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/user-profile", Method.Put);
            request.AddJsonBody(new List<EditUserProfileDto> { editUserProfileDto });

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> UpdateUserProfiles(List<EditUserProfileDto> editUserProfileDtos, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/user-profile", Method.Put);
            request.AddJsonBody(editUserProfileDtos);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> CreateUserProfileClaim(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/user-profile/{userProfileId}/claims", Method.Patch);
            request.AddJsonBody(createUserProfileClaimDto);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> SetUserProfileClaims(Guid userProfileId, List<CreateUserProfileClaimDto> createUserProfileClaimDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/user-profile/{userProfileId}/claims", Method.Post);
            request.AddJsonBody(createUserProfileClaimDto);

            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteUserProfileClaim(Guid userProfileId, Guid userProfileClaimId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/user-profile/{userProfileId}/claims/{userProfileClaimId}", Method.Delete);
            var response = await ExecuteAsync(request, cancellationToken);

            return response.ToApiResult();
        }
    }
}


