using System.Net;
using System.Text.Json;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public sealed partial class RuleExecutionsControllerTests
{
    [Fact]
    public async Task FlowReturnsFrozenStructureAndSafeEvidenceWithoutPrivatePayloads()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid()))).Result!.Data!;
        var row = context.RuleFeatureExecutions.Single(item => item.ExecutionId == execution.ExecutionId);
        row.FrozenRulesJson = /*lang=json,strict*/ """
            {"rules":[{"id":"rule-1","enabled":true,"order":3,"afterMatch":"ContinueToNextRule",
              "condition":{"groupOperator":"And","conditions":[
                {"conditionId":"player.profile-age","parameters":{"operator":"LessThan","duration":"2.00:00:00","secret":"private-value"}},
                {"conditionId":"vpn.intelligence","parameters":{"signal":"ProxyCheckRiskScore","operator":"GreaterThan","expectedValue":"80"}},
                {"conditionId":"player.tags","parameters":{"operator":"ContainsAny","tags":["private-tag"]}},
                {"conditionId":"vpn.intelligence","parameters":{"signal":"ProxyCheckRiskScore","operator":"GreaterThan","expectedValue":"999"}}
              ]},"actions":[{"id":"kick-player","typeId":"player.kick","dependsOnActionIds":["previous"],"parameters":{"reason":"private-reason"}}]}]}
            """;
        await context.SaveChangesAsync();

        var result = await api.GetRuleExecutionFlow(execution.ExecutionId);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var flow = result.Result!.Data!;
        Assert.Equal(execution.ExecutionId, flow.Execution.ExecutionId);
        Assert.Equal(serverId, flow.Execution.GameServerId);
        Assert.Equal(RuleExecutionStructureStatus.Available, flow.StructureStatus);
        var rule = Assert.Single(flow.Rules);
        Assert.Equal("rule-1", rule.Id);
        Assert.Null(rule.DisplayName);
        Assert.True(rule.Enabled);
        Assert.Equal(3, rule.Order);
        Assert.Equal("ContinueToNextRule", rule.AfterMatch);
        Assert.Equal("And", rule.Condition.GroupOperator);
        Assert.Equal(4, rule.Condition.Conditions.Length);
        var age = rule.Condition.Conditions[0];
        Assert.Equal("Player profile age", age.DisplayName);
        Assert.Equal("LessThan", age.Operator);
        Assert.Equal("2.00:00:00", Assert.Single(age.SafeParameters).Value);
        Assert.True(age.HasUnavailableParameters);
        var vpn = rule.Condition.Conditions[1];
        Assert.Contains(vpn.SafeParameters, item => item.Name == "signal" && item.Value == "ProxyCheckRiskScore");
        Assert.Contains(vpn.SafeParameters, item => item.Name == "expectedValue" && item.Value == "80");
        Assert.True(rule.Condition.Conditions[2].HasUnavailableParameters);
        Assert.Empty(rule.Condition.Conditions[2].SafeParameters);
        Assert.True(rule.Condition.Conditions[3].HasUnavailableParameters);
        Assert.DoesNotContain(rule.Condition.Conditions[3].SafeParameters, item => item.Name == "expectedValue");
        var action = Assert.Single(rule.Actions);
        Assert.Equal("player.kick", action.TypeId);
        Assert.Equal(new[] { "previous" }, action.DependsOnActionIds);
        var json = JsonSerializer.Serialize(flow);
        Assert.DoesNotContain("private-value", json);
        Assert.DoesNotContain("private-tag", json);
        Assert.DoesNotContain("private-reason", json);
        Assert.DoesNotContain("DecisionSnapshotJson", json);
        Assert.DoesNotContain("FrozenRulesJson", json);
    }

    [Fact]
    public async Task FlowAcceptsInheritedSnapshotsLargerThanOneRuleDocument()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid()))).Result!.Data!;
        var row = context.RuleFeatureExecutions.Single(item => item.ExecutionId == execution.ExecutionId);
        row.FrozenRulesJson = JsonSerializer.Serialize(new
        {
            rules = Enumerable.Range(0, 120).Select(index => new
            {
                id = $"rule-{index}",
                condition = new { conditionId = "always" },
                actions = Array.Empty<object>(),
                afterMatch = index == 0 ? 1 : 2
            })
        });
        await context.SaveChangesAsync();
        var flow = (await api.GetRuleExecutionFlow(execution.ExecutionId)).Result!.Data!;
        Assert.Equal(RuleExecutionStructureStatus.Available, flow.StructureStatus);
        Assert.Equal(120, flow.Rules.Length);
        Assert.Equal("StopProcessingRules", flow.Rules[0].AfterMatch);
        Assert.Equal("ContinueToNextRule", flow.Rules[119].AfterMatch);
    }

    [Fact]
    public async Task FlowDistinguishesMissingMalformedAndEmptyStructure()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var serverId = await AddServer(context);
        var api = (IRuleExecutionsApi)new RuleExecutionsController(context);
        var execution = (await api.StartExecution(serverId, StartRequest(serverId, Guid.NewGuid()))).Result!.Data!;
        var row = context.RuleFeatureExecutions.Single(item => item.ExecutionId == execution.ExecutionId);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetRuleExecutionFlow(Guid.NewGuid())).StatusCode);
        foreach (var (json, expected) in new[]
        {
            (/*lang=json,strict*/ """{}""", RuleExecutionStructureStatus.Missing),
            (/*lang=json,strict*/ """{"rules":null}""", RuleExecutionStructureStatus.Missing),
            (/*lang=json,strict*/ """{"rules":[]}""", RuleExecutionStructureStatus.Available),
            (/*lang=json,strict*/ """{"rules":[{"id":"x","actions":[]}]}""", RuleExecutionStructureStatus.Malformed),
            (/*lang=json,strict*/ """{"rules":[{"id":"x","condition":{"conditionId":"always"},"actions":[],"order":"bad"}]}""", RuleExecutionStructureStatus.Malformed),
            (/*lang=json,strict*/ """{"rules":[{"id":"x","condition":{"conditionId":"always"},"actions":[],"afterMatch":"invalid"}]}""", RuleExecutionStructureStatus.Malformed),
            (/*lang=json,strict*/ """{"rules":[{"id":"x","condition":{"conditionId":"always"},"actions":[],"afterMatch":123}]}""", RuleExecutionStructureStatus.Malformed),
            ("{", RuleExecutionStructureStatus.Malformed)
        })
        {
            row.FrozenRulesJson = json;
            await context.SaveChangesAsync();
            var flow = (await api.GetRuleExecutionFlow(execution.ExecutionId)).Result!.Data!;
            Assert.Equal(expected, flow.StructureStatus);
            Assert.Empty(flow.Rules);
        }
    }
}
