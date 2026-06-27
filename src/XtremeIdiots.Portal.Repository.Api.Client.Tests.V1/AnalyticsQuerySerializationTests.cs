using System.Net;
using System.Security.Authentication;
using Microsoft.Extensions.Logging.Abstractions;

using MX.Api.Client;
using MX.Api.Client.Auth;

using RestSharp;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1.Analytics;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1;

public class AnalyticsQuerySerializationTests
{
    [Fact]
    public async Task GameAnalytics_GetTimeseries_UsesExpectedCompareQueryValues()
    {
        var rest = new CapturingRestClientService();
        var api = new GameAnalyticsApi(
            NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
            new FakeTokenProvider(),
            rest,
            CreateOptions());

        await api.GetTimeseries(
            GameType.CallOfDuty4,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            AnalyticsBucket.OneHour,
            AnalyticsCompareMode.PreviousPeriod,
            comparePeriods: 3,
            alignMode: AnalyticsAlignMode.Month,
            timezone: "Europe/London",
            normalize: true,
            cancellationToken: CancellationToken.None);

        Assert.Equal("v1/analytics/games/timeseries", rest.LastResource);
        Assert.Equal("previous_period", rest.Query["compareMode"]);
        Assert.Equal("month", rest.Query["alignMode"]);
        Assert.Equal("3", rest.Query["comparePeriods"]);
        Assert.Equal("Europe/London", rest.Query["timezone"]);
        Assert.Equal("true", rest.Query["normalize"]);
    }

    [Fact]
    public async Task ServerAnalytics_GetTimeseries_UsesExpectedCompareQueryValues()
    {
        var rest = new CapturingRestClientService();
        var api = new ServerAnalyticsApi(
            NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
            new FakeTokenProvider(),
            rest,
            CreateOptions());

        var serverId = Guid.NewGuid();
        await api.GetTimeseries(
            serverId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            AnalyticsBucket.FifteenMinutes,
            AnalyticsCompareMode.RollingPeriods,
            comparePeriods: 4,
            alignMode: AnalyticsAlignMode.Week,
            timezone: "UTC",
            normalize: false,
            cancellationToken: CancellationToken.None);

        Assert.Equal($"v1/analytics/servers/{serverId}/timeseries", rest.LastResource);
        Assert.Equal("rolling_periods", rest.Query["compareMode"]);
        Assert.Equal("week", rest.Query["alignMode"]);
        Assert.Equal("4", rest.Query["comparePeriods"]);
        Assert.Equal("UTC", rest.Query["timezone"]);
        Assert.Equal("false", rest.Query["normalize"]);
    }

    [Fact]
    public async Task PlayerAnalyticsV2_GetTrends_UsesExpectedCompareQueryValues()
    {
        var rest = new CapturingRestClientService();
        var api = new PlayerAnalyticsV2Api(
            NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
            new FakeTokenProvider(),
            rest,
            CreateOptions());

        var playerId = Guid.NewGuid();
        await api.GetTrends(
            playerId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            AnalyticsBucket.OneDay,
            AnalyticsCompareMode.None,
            comparePeriods: 1,
            alignMode: AnalyticsAlignMode.None,
            timezone: "UTC",
            normalize: true,
            cancellationToken: CancellationToken.None);

        Assert.Equal($"v1/analytics/players/{playerId}/trends", rest.LastResource);
        Assert.Equal("none", rest.Query["compareMode"]);
        Assert.Equal("none", rest.Query["alignMode"]);
        Assert.Equal("1", rest.Query["comparePeriods"]);
        Assert.Equal("UTC", rest.Query["timezone"]);
        Assert.Equal("true", rest.Query["normalize"]);
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
