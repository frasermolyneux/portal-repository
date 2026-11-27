using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1;

public sealed class RuleExecutionsClientTests
{
    [Fact]
    public async Task OccurrenceOperationsUseVersionedRoutesAndPreserveRequests()
    {
        var rest = new CapturingRestClientService();
        var api = new RuleExecutionsApi(
            NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
            new FakeTokenProvider(), rest, new RepositoryApiClientOptions { BaseUrl = "https://example.test", MaxRetryCount = 1 });
        var server = Guid.NewGuid();
        var occurrence = Guid.NewGuid();
        var effect = new string('a', 64);
        var root = $"v1/game-servers/{server}/chat-occurrences/{occurrence}/effects";

        var claim = new ClaimRuleOccurrenceEffectDto { LogicalEffectId = effect };
        await api.ClaimOccurrenceEffect(server, occurrence, claim);
        Assert.Equal(root, rest.LastResource);
        Assert.Equal(Method.Post, rest.LastMethod);
        Assert.Same(claim, rest.LastBody);

        var record = new EnsureRuleOccurrenceAdminActionDto();
        await api.EnsureOccurrenceAdminAction(server, occurrence, effect, record);
        Assert.Equal($"{root}/{effect}/admin-action", rest.LastResource);
        Assert.Equal(Method.Put, rest.LastMethod);
        Assert.Same(record, rest.LastBody);

        var outcome = new UpdateRuleOccurrenceEffectOutcomeDto { Outcome = RuleOccurrenceEffectOutcome.OutcomeUnknown };
        await api.UpdateOccurrenceEffectOutcome(server, occurrence, effect, outcome);
        Assert.Equal($"{root}/{effect}/outcome", rest.LastResource);
        Assert.Equal(Method.Put, rest.LastMethod);
        Assert.Same(outcome, rest.LastBody);

        await api.SearchRuleExecutionAudit(new RuleExecutionAuditQueryDto { GameServerId = server, TriggerOccurrenceId = occurrence });
        Assert.Equal("v1/rule-executions/audit", rest.LastResource);
        Assert.Equal(Method.Get, rest.LastMethod);
        Assert.Equal(occurrence.ToString(), rest.Query["triggerOccurrenceId"]);
    }

    private sealed class CapturingRestClientService : IRestClientService
    {
        public string LastResource { get; private set; } = string.Empty;
        public Method LastMethod { get; private set; }
        public object? LastBody { get; private set; }
        public Dictionary<string, string> Query { get; private set; } = [];

        public Task<RestResponse> ExecuteAsync(string baseUrl, RestRequest request, CancellationToken cancellationToken = default)
        {
            LastResource = request.Resource;
            LastMethod = request.Method;
            LastBody = request.Parameters.FirstOrDefault(parameter => parameter.Type == ParameterType.RequestBody)?.Value;
            Query = request.Parameters.Where(parameter => parameter.Type == ParameterType.QueryString)
                .ToDictionary(parameter => parameter.Name ?? string.Empty, parameter => parameter.Value?.ToString() ?? string.Empty);
            return Task.FromResult(new RestResponse { StatusCode = HttpStatusCode.OK, Content = "{}" });
        }

        public Task<RestResponse> ExecuteWithNamedOptionsAsync(string optionsName, RestRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Dispose() { }
    }

    private sealed class FakeTokenProvider : IApiTokenProvider
    {
        public Task<string> GetAccessTokenAsync(string audience, CancellationToken cancellationToken = default) =>
            Task.FromResult("token");
    }
}
