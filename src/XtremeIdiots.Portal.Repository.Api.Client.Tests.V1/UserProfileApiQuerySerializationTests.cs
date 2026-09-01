using System.Net;
using System.Security.Authentication;
using Microsoft.Extensions.Logging.Abstractions;

using MX.Api.Client;
using MX.Api.Client.Auth;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1;

public class UserProfileApiQuerySerializationTests
{
    [Fact]
    public async Task GetUserProfiles_WithGameType_EmitsGameTypeQueryParameter()
    {
        var rest = new CapturingRestClientService();
        var api = new UserProfileApi(
            NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
            new FakeTokenProvider(),
            rest,
            CreateOptions());

        await api.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.CallOfDuty5, 0, 50, null, CancellationToken.None);

        Assert.Equal("v1/user-profiles", rest.LastResource);
        Assert.Equal("Moderators", rest.Query["filter"]);
        Assert.Equal("CallOfDuty5", rest.Query["gameType"]);
    }

    [Fact]
    public async Task GetUserProfiles_WithoutGameType_DoesNotEmitGameTypeQueryParameter()
    {
        var rest = new CapturingRestClientService();
        var api = new UserProfileApi(
            NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
            new FakeTokenProvider(),
            rest,
            CreateOptions());

        await api.GetUserProfiles(null, UserProfileFilter.Moderators, null, 0, 50, null, CancellationToken.None);

        Assert.Equal("v1/user-profiles", rest.LastResource);
        Assert.False(rest.Query.ContainsKey("gameType"));
    }

    [Fact]
    public async Task GetUserProfiles_OldOverload_RemainsFunctionalAndDoesNotEmitGameTypeQueryParameter()
    {
        var rest = new CapturingRestClientService();
        var api = new UserProfileApi(
            NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
            new FakeTokenProvider(),
            rest,
            CreateOptions());

        var result = await api.GetUserProfiles(null, UserProfileFilter.Moderators, 0, 50, null, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("v1/user-profiles", rest.LastResource);
        Assert.Equal("Moderators", rest.Query["filter"]);
        Assert.False(rest.Query.ContainsKey("gameType"));
    }

    private static RepositoryApiClientOptions CreateOptions()
    {
        return new RepositoryApiClientOptions
        {
            BaseUrl = "https://example.test",
            MaxRetryCount = 1
        };
    }

    private sealed class CapturingRestClientService : IRestClientService
    {
        public string LastResource { get; private set; } = string.Empty;

        public Dictionary<string, string> Query { get; private set; } = [];

        public Task<RestResponse> ExecuteAsync(string baseUrl, RestRequest request, CancellationToken cancellationToken = default)
        {
            LastResource = request.Resource;
            Query = request.Parameters
                .Where(p => p.Type == ParameterType.QueryString)
                .ToDictionary(p => p.Name ?? string.Empty, p => p.Value?.ToString() ?? string.Empty);

            return Task.FromResult(new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = "{}"
            });
        }

        public Task<RestResponse> ExecuteWithNamedOptionsAsync(string optionsName, RestRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeTokenProvider : IApiTokenProvider
    {
        public Task<string> GetAccessTokenAsync(string audience, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new AuthenticationException("Invalid audience");
            }

            return Task.FromResult("token");
        }
    }
}
