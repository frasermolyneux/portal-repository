using System.Globalization;
using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using Newtonsoft.Json;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1;

public sealed class HistoryQuerySerializationTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task History_TransmitsFiltersAndDeserializesLookahead(bool events)
    {
        using var rest = new CapturingRestClientService();
        using var cancellation = new CancellationTokenSource();
        var options = new RepositoryApiClientOptions { BaseUrl = "https://example.test", MaxRetryCount = 1 };
        var serverId = Guid.NewGuid();
        var fromUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        if (events)
        {
            var api = new GameServersEventsApi(NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
                new FakeTokenProvider(), rest, options);
            var result = await api.GetGameServerEventsPage(GameType.CallOfDuty4, serverId, "MapChange",
                20, 20, GameServerEventOrder.TimestampAsc, fromUtc, cancellation.Token);
            Assert.True(result.IsSuccess);
            Assert.True(result.Result!.Data!.HasMore);
            Assert.Empty(result.Result.Data.Items);
        }
        else
        {
            var api = new ChatMessagesApi(NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
                new FakeTokenProvider(), rest, options);
            var result = await api.GetChatMessagesPage(GameType.CallOfDuty4, serverId, null, "signal",
                20, 20, ChatMessageOrder.TimestampAsc, true, fromUtc, cancellation.Token);
            Assert.True(result.IsSuccess);
            Assert.True(result.Result!.Data!.HasMore);
            Assert.Empty(result.Result.Data.Items);
            Assert.Equal("True", rest.Query["lockedOnly"]);
            Assert.Equal("signal", rest.Query["filterString"]);
        }

        Assert.Equal(events ? "v1/game-server-events/history" : "v1/chat-messages/history", rest.Resource);
        Assert.Equal("CallOfDuty4", rest.Query["gameType"]);
        Assert.Equal(serverId.ToString(), rest.Query["gameServerId"]);
        Assert.Equal("20", rest.Query["skipEntries"]);
        Assert.Equal("20", rest.Query["takeEntries"]);
        Assert.Equal("TimestampAsc", rest.Query["order"]);
        Assert.Equal(fromUtc.ToString("O", CultureInfo.InvariantCulture), rest.Query["fromUtc"]);
        Assert.Equal(cancellation.Token, rest.Token);
    }

    private sealed class CapturingRestClientService : IRestClientService
    {
        public string Resource { get; private set; } = "";
        public Dictionary<string, string> Query { get; private set; } = [];
        public CancellationToken Token { get; private set; }

        public Task<RestResponse> ExecuteAsync(string baseUrl, RestRequest request, CancellationToken cancellationToken = default)
        {
            Resource = request.Resource;
            Token = cancellationToken;
            Query = request.Parameters.Where(x => x.Type == ParameterType.QueryString)
                .ToDictionary(x => x.Name!, x => x.Value!.ToString()!);
            return Task.FromResult(new RestResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonConvert.SerializeObject(new { data = new { items = Array.Empty<object>(), hasMore = true } })
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
            return Task.FromResult("synthetic-token");
        }
    }
}
