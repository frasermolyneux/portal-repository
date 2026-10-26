using System.Net;
using System.Security.Authentication;
using Microsoft.Extensions.Logging.Abstractions;

using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Configuration;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1;

public class UserProfileApiQuerySerializationTests
{
    [Fact]
    public async Task ArchiveAndHistoryRead_UseExpectedRoutesBodyAndCancellation()
    {
        var rest = new CapturingRestClientService();
        var tokenProvider = new FakeTokenProvider();
        var api = new UserProfileApi(NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance, tokenProvider, rest, CreateOptions());
        var dto = new ArchiveUserProfileDto(Guid.NewGuid(), Guid.NewGuid(), "Upstream 404");
        using var cancellation = new CancellationTokenSource();
        var result = await api.ArchiveUserProfile(dto, cancellation.Token);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal($"v1/user-profile/{dto.UserProfileId}/archive", rest.LastResource);
        Assert.Equal(Method.Post, rest.LastRequest!.Method);
        Assert.Same(dto, rest.LastRequest.Parameters.Single(parameter => parameter.Type == ParameterType.RequestBody).Value);
        Assert.Equal(cancellation.Token, rest.LastCancellationToken);
        await api.GetUserProfileIncludingArchived(dto.UserProfileId, cancellation.Token);
        Assert.Equal($"v1/user-profile/{dto.UserProfileId}/including-archived", rest.LastResource);
        Assert.Equal(Method.Get, rest.LastRequest.Method);
        Assert.Equal(cancellation.Token, rest.LastCancellationToken);
        rest.ResponseStatus = HttpStatusCode.NoContent;
        Assert.Equal(HttpStatusCode.NoContent, (await api.ArchiveUserProfile(dto)).StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ArchiveAndHistoryRead_CancelDuringTokenAcquisition(bool archive)
    {
        using var cancellation = new CancellationTokenSource();
        var tokenProvider = new FakeTokenProvider
        {
            OnRequest = token =>
            {
                cancellation.Cancel();
                Assert.True(token.IsCancellationRequested);
                token.ThrowIfCancellationRequested();
            }
        };
        var options = CreateOptions();
        options.AuthenticationOptions = [new AzureCredentialAuthenticationOptions { ApiAudience = "api://test" }];
        var api = new UserProfileApi(NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
            tokenProvider, new CapturingRestClientService(), options);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            if (archive)
            {
                await api.ArchiveUserProfile(new(Guid.NewGuid(), Guid.NewGuid(), "Reason"), cancellation.Token);
            }
            else
            {
                await api.GetUserProfileIncludingArchived(Guid.NewGuid(), cancellation.Token);
            }
        });
    }

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
        public RestRequest? LastRequest { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }
        public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;

        public Task<RestResponse> ExecuteAsync(string baseUrl, RestRequest request, CancellationToken cancellationToken = default)
        {
            LastResource = request.Resource;
            LastRequest = request;
            LastCancellationToken = cancellationToken;
            Query = request.Parameters
                .Where(p => p.Type == ParameterType.QueryString)
                .ToDictionary(p => p.Name ?? string.Empty, p => p.Value?.ToString() ?? string.Empty);

            return Task.FromResult(new RestResponse
            {
                StatusCode = ResponseStatus,
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
        public Action<CancellationToken>? OnRequest { get; init; }

        public Task<string> GetAccessTokenAsync(string audience, CancellationToken cancellationToken = default)
        {
            OnRequest?.Invoke(cancellationToken);
            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new AuthenticationException("Invalid audience");
            }

            return Task.FromResult("token");
        }
    }
}
