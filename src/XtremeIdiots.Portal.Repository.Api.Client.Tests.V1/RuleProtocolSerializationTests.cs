using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1;

public sealed class RuleProtocolSerializationTests
{
    [Fact]
    public void OccurrenceEffectIdentityUsesCanonicalInputs()
    {
        var server = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
        var occurrence = Guid.Parse("ffeeddcc-bbaa-9988-7766-554433221100");
        var id = RuleOccurrenceEffectIdentity.Create(server, occurrence, "chat-moderation", "new-player", "nudge");

        Assert.Equal("0bbdea94e2b9071310903237f5b610acb90ab486861c66594aaa6f2d3c78f81a", id);
        Assert.Equal(id, RuleOccurrenceEffectIdentity.Create(server, occurrence, "chat-moderation", "new-player", "nudge"));
        Assert.NotEqual(id, RuleOccurrenceEffectIdentity.Create(server, occurrence, "chat-moderation", "New-player", "nudge"));
        Assert.NotEqual(id, RuleOccurrenceEffectIdentity.Create(server, occurrence, "chat-moderation", "new-player", "other"));
        Assert.Throws<ArgumentException>(() => RuleOccurrenceEffectIdentity.Create(Guid.Empty, occurrence, "chat-moderation", "new-player", "nudge"));
        Assert.Throws<ArgumentException>(() => RuleOccurrenceEffectIdentity.Create(server, occurrence, "chat-moderation", "new-player ", "nudge"));
        Assert.Throws<ArgumentException>(() => RuleOccurrenceEffectIdentity.Create(server, occurrence, "chat-moderation", "\ud800", "nudge"));
    }

    [Fact]
    public void OccurrenceClaimUsesSafeFieldsAndForwardCompatibleDispositions()
    {
        var claim = new ClaimRuleOccurrenceEffectDto
        {
            LogicalEffectId = new string('a', 64),
            PlayerId = Guid.NewGuid(),
            FeatureId = "chat-moderation",
            RuleId = "rule",
            ActionId = "nudge",
            ActionTypeId = "message.private",
            RuleSetRevision = "revision-1",
            Mode = RuleExecutionMode.Enforce,
            Cooldown = new() { PolicyId = "nudge", DurationSeconds = 120 }
        };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        var serialized = new[]
        {
            System.Text.Json.JsonSerializer.Serialize(claim, options),
            JsonConvert.SerializeObject(claim, settings)
        };
        foreach (var json in serialized)
        {
            using var document = JsonDocument.Parse(json);
            Assert.Equal(claim.LogicalEffectId, document.RootElement.GetProperty("logicalEffectId").GetString());
            Assert.Equal("Enforce", document.RootElement.GetProperty("mode").GetString());
            Assert.Equal(120, document.RootElement.GetProperty("cooldown").GetProperty("durationSeconds").GetInt32());
            Assert.False(document.RootElement.TryGetProperty("adminActionId", out _));
            Assert.False(document.RootElement.TryGetProperty("message", out _));
            Assert.False(document.RootElement.TryGetProperty("reason", out _));
        }

        const string future = "\"FutureDisposition\"";
        Assert.Equal(RuleOccurrenceEffectDisposition.Unrecognized,
            System.Text.Json.JsonSerializer.Deserialize<RuleOccurrenceEffectDisposition>(future, options));
        Assert.Equal(RuleOccurrenceEffectDisposition.Unrecognized,
            JsonConvert.DeserializeObject<RuleOccurrenceEffectDisposition>(future, settings));
    }

    [Fact]
    public void EvaluationEvidenceUsesStringStatusesAndTreatsFutureValuesAsUnrecognized()
    {
        var evidence = new RuleEvaluationEvidence
        {
            Nodes = [new() { Kind = RuleEvidenceNodeKind.Check, Status = RuleEvaluationNodeStatus.Unknown }],
            ActionDecisions = [new() { Status = RuleActionDecisionStatus.NotRun }]
        };
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        var serialized = new[]
        {
            System.Text.Json.JsonSerializer.Serialize(evidence, options),
            JsonConvert.SerializeObject(evidence, settings)
        };
        foreach (var json in serialized)
        {
            using var document = JsonDocument.Parse(json);
            Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("Check", document.RootElement.GetProperty("nodes")[0].GetProperty("kind").GetString());
            Assert.Equal("Unknown", document.RootElement.GetProperty("nodes")[0].GetProperty("status").GetString());
            Assert.Equal("NotRun", document.RootElement.GetProperty("actionDecisions")[0].GetProperty("status").GetString());
            Assert.False(document.RootElement.TryGetProperty("hasSupportedSchema", out _));
        }

        const string futureStatus = """{"schemaVersion":1,"nodes":[{"kind":"Check","status":"FutureStatus"},{"kind":99,"status":99}]}""";
        var readers = new[]
        {
            System.Text.Json.JsonSerializer.Deserialize<RuleEvaluationEvidence>(futureStatus, options)!,
            JsonConvert.DeserializeObject<RuleEvaluationEvidence>(futureStatus, settings)!
        };
        foreach (var result in readers)
        {
            Assert.True(result.HasSupportedSchema);
            Assert.Equal(RuleEvaluationNodeStatus.Unrecognized, result.Nodes[0].Status);
            Assert.Equal(RuleEvidenceNodeKind.Unrecognized, result.Nodes[1].Kind);
            Assert.Equal(RuleEvaluationNodeStatus.Unrecognized, result.Nodes[1].Status);
        }
    }

    [Theory]
    [InlineData(/*lang=json,strict*/ """{"schemaVersion":2,"nodes":{"newSchema":true}}""", 2)]
    [InlineData(/*lang=json,strict*/ """{"nodes":{"newSchema":true}}""", 0)]
    [InlineData(/*lang=json,strict*/ """{"schemaVersion":"invalid","nodes":{"newSchema":true}}""", 0)]
    [InlineData(/*lang=json,strict*/ """{"schemaVersion":2147483648,"nodes":{"newSchema":true}}""", 0)]
    [InlineData(/*lang=json,strict*/ """{"schemaVersion":{},"nodes":{"newSchema":true}}""", 0)]
    public void UnknownEvidenceSchemasDoNotRequireUnderstandingTheirPayload(string json, int version)
    {
        var results = new[]
        {
            System.Text.Json.JsonSerializer.Deserialize<RuleEvaluationEvidence>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!,
            JsonConvert.DeserializeObject<RuleEvaluationEvidence>(json)!
        };
        foreach (var result in results)
        {
            Assert.False(result.HasSupportedSchema);
            Assert.Equal(version, result.SchemaVersion);
            Assert.Empty(result.Nodes);
        }
    }

    [Fact]
    public void EvidenceSchemaUsesSystemTextJsonNamingAndCaseOptions()
    {
        var web = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Assert.True(System.Text.Json.JsonSerializer.Deserialize<RuleEvaluationEvidence>(
            """{"SCHEMAVERSION":1,"nodes":[]}""", web)!.HasSupportedSchema);
        Assert.True(System.Text.Json.JsonSerializer.Deserialize<RuleEvaluationEvidence>(
            """{"schemaVersion":"1","nodes":[]}""", web)!.HasSupportedSchema);

        var strict = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
        Assert.True(System.Text.Json.JsonSerializer.Deserialize<RuleEvaluationEvidence>(
            """{"schema_version":1,"nodes":[]}""", strict)!.HasSupportedSchema);
        Assert.False(System.Text.Json.JsonSerializer.Deserialize<RuleEvaluationEvidence>(
            """{"SCHEMA_VERSION":1,"nodes":[]}""", strict)!.HasSupportedSchema);

        var future = System.Text.Json.JsonSerializer.Deserialize<RuleEvaluationEvidence>(
            """{"schemaVersion":1,"SCHEMAVERSION":2,"nodes":{"future":true}}""", web)!;
        Assert.Equal(2, future.SchemaVersion);
        Assert.Empty(future.Nodes);
        Assert.True(System.Text.Json.JsonSerializer.Deserialize<RuleEvaluationEvidence>(
            """{"schemaVersion":2,"SCHEMAVERSION":1,"nodes":[]}""", web)!.HasSupportedSchema);
    }

    [Fact]
    public void EvidenceSchemaUsesNewtonsoftContractNaming()
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        };
        Assert.True(JsonConvert.DeserializeObject<RuleEvaluationEvidence>(
            /*lang=json,strict*/ """{"SCHEMA_VERSION":1,"nodes":[]}""", settings)!.HasSupportedSchema);
        var future = JsonConvert.DeserializeObject<RuleEvaluationEvidence>(
            /*lang=json,strict*/ """{"schema_version":1,"SCHEMA_VERSION":2,"nodes":{"future":true}}""", settings)!;
        Assert.Equal(2, future.SchemaVersion);
        Assert.Empty(future.Nodes);
        Assert.True(JsonConvert.DeserializeObject<RuleEvaluationEvidence>(
            /*lang=json,strict*/ """{"schema_version":2,"SCHEMA_VERSION":1,"nodes":[]}""", settings)!.HasSupportedSchema);
    }

    [Fact]
    public void ExecutionDiagnosticsUseRendererCompatibleStringCodesWithBothSerializers()
    {
        var progress = new UpdateRuleFeatureExecutionProgressDto
        {
            Diagnostics = [new()
            {
                Code = RuleExecutionDiagnosticCode.FallbackUsed, TokenKey = "context.server.Name",
                Source = "ServerMetadata", Detail = "Display value unavailable."
            }]
        };
        var serialized = new[]
        {
            System.Text.Json.JsonSerializer.Serialize(progress, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            JsonConvert.SerializeObject(progress, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() })
        };
        foreach (var json in serialized)
        {
            using var document = JsonDocument.Parse(json);
            var diagnostic = document.RootElement.GetProperty("diagnostics")[0];
            Assert.Equal("FallbackUsed", diagnostic.GetProperty("code").GetString());
            Assert.Equal("ServerMetadata", diagnostic.GetProperty("source").GetString());
            Assert.Equal(progress.Diagnostics[0].TokenKey, diagnostic.GetProperty("tokenKey").GetString());
        }
    }

    [Fact]
    public void RecipientAndFragmentContractsPreserveOriginalTriggerWithBothSerializers()
    {
        var trigger = Guid.NewGuid();
        var recipient = Guid.NewGuid();
        var delivery = new RuleDeliveryIdentity
        {
            ParentLogicalEffectId = new string('a', 64),
            RecipientConnectionId = recipient,
            MessageIndex = 1,
            FragmentIndex = 2
        };
        var command = new RuleActionCommand
        {
            ConnectionId = trigger,
            Correlation = new() { ConnectionId = trigger },
            RecipientTarget = new() { ConnectionId = recipient, ConnectionGeneration = 3, SlotId = 4, PlayerGuid = "recipient" },
            DeliveryIdentity = delivery,
            LogicalEffectId = delivery.ToLogicalEffectId()
        };
        var serialized = new[]
        {
            System.Text.Json.JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            JsonConvert.SerializeObject(command, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() })
        };
        foreach (var json in serialized)
        {
            using var document = JsonDocument.Parse(json);
            Assert.Equal(trigger, document.RootElement.GetProperty("connectionId").GetGuid());
            Assert.Equal(trigger, document.RootElement.GetProperty("correlation").GetProperty("connectionId").GetGuid());
            Assert.Equal(recipient, document.RootElement.GetProperty("recipientTarget").GetProperty("connectionId").GetGuid());
            Assert.Equal(2, document.RootElement.GetProperty("deliveryIdentity").GetProperty("fragmentIndex").GetInt32());
        }
        Assert.NotEqual(delivery.ToLogicalEffectId(), (delivery with { FragmentIndex = 3 }).ToLogicalEffectId());
        Assert.NotEqual(delivery.ToLogicalEffectId(), (delivery with { RecipientConnectionId = Guid.NewGuid() }).ToLogicalEffectId());
    }

    [Fact]
    public void EvaluationRetryAndActionProgressFieldsUseStableCamelCaseDefaults()
    {
        var defaultResponseJson = System.Text.Json.JsonSerializer.Serialize(
            new RuleEvaluationResponse(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var defaultResponse = JsonDocument.Parse(defaultResponseJson);
        Assert.False(defaultResponse.RootElement.GetProperty("retryTrigger").GetBoolean());

        var retryResponseJson = System.Text.Json.JsonSerializer.Serialize(
            new RuleEvaluationResponse { RetryTrigger = true },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var retryResponse = JsonDocument.Parse(retryResponseJson);
        Assert.True(retryResponse.RootElement.GetProperty("retryTrigger").GetBoolean());

        var newtonsoftDefaultResponseJson = JsonConvert.SerializeObject(
            new RuleEvaluationResponse(),
            new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
        using var newtonsoftDefaultResponse = JsonDocument.Parse(newtonsoftDefaultResponseJson);
        Assert.False(newtonsoftDefaultResponse.RootElement.GetProperty("retryTrigger").GetBoolean());

        var adminActionId = Guid.NewGuid();
        var result = new RuleActionResult
        {
            GameServerId = Guid.NewGuid(),
            Outcome = RuleActionExecutionOutcome.Applied,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };
        var progressJson = System.Text.Json.JsonSerializer.Serialize(
            new UpdateRuleActionOutcomeDto
            {
                Result = result,
                AdminActionId = adminActionId,
                NativeEvidence = result
            },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var progress = JsonDocument.Parse(progressJson);
        Assert.Equal(adminActionId, progress.RootElement.GetProperty("adminActionId").GetGuid());
        Assert.Equal("Applied", progress.RootElement.GetProperty("nativeEvidence").GetProperty("outcome").GetString());
    }

    [Fact]
    public void AtomicRuleAdminActionRequestUsesStableCamelCaseFields()
    {
        var request = new EnsureRuleActionAdminActionDto
        {
            ExpectedVersion = 4,
            LogicalEffectId = new string('a', 64),
            Type = AdminActionType.TempBan,
            Text = "Rule decision",
            Expires = DateTime.UtcNow.AddHours(1)
        };

        var json = System.Text.Json.JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        Assert.Equal(4, document.RootElement.GetProperty("expectedVersion").GetInt64());
        Assert.Equal(new string('a', 64), document.RootElement.GetProperty("logicalEffectId").GetString());
        Assert.Equal("TempBan", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("Rule decision", document.RootElement.GetProperty("text").GetString());
        Assert.True(document.RootElement.TryGetProperty("expires", out _));
    }

    [Fact]
    public void BindingSnapshotUsesAgentIdentityWithNullableGeneration()
    {
        var snapshot = new RuleConnectionStatusSnapshotDto
        {
            GameType = GameType.CallOfDuty4,
            Runtime = new RuleRuntimeDescriptor
            {
                Id = "server-agent",
                IdentityModel = RuleRuntimeIdentityModel.AgentObservedConnection,
                InstanceId = Guid.NewGuid(),
                StartedAtUtc = DateTimeOffset.UtcNow
            },
            Players =
            [
                new RuleConnectionStatusPlayerDto
                {
                    SlotId = 3,
                    PlayerGuid = "agent-player",
                    State = RuleConnectionState.Connected,
                    ConnectionId = Guid.NewGuid(),
                    ConnectionGeneration = null
                }
            ]
        };

        var json = System.Text.Json.JsonSerializer.Serialize(
            snapshot,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        var runtime = document.RootElement.GetProperty("runtime");
        var player = document.RootElement.GetProperty("players")[0];

        Assert.Equal("AgentObservedConnection", runtime.GetProperty("identityModel").GetString());
        Assert.Equal("Connected", player.GetProperty("state").GetString());
        Assert.True(player.GetProperty("connectionGeneration").ValueKind == JsonValueKind.Null);
        Assert.Contains(typeof(RuleConnectionBindingDto).Assembly.GetExportedTypes(),
            type => type == typeof(RuleServerCapabilitiesDto));
    }

    [Fact]
    public void TriggerUsesEstablishedGameTypeNamesWithBothSerializers()
    {
        var trigger = new RuleTriggerEnvelope
        {
            ProtocolVersion = 1,
            GameType = GameType.CallOfDuty4x,
            Runtime = new RuleRuntimeDescriptor
            {
                Id = "cod4x-plugin",
                ProtocolVersion = 1
            }
        };

        var systemTextJson = System.Text.Json.JsonSerializer.Serialize(
            trigger,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var systemTextDocument = JsonDocument.Parse(systemTextJson);
        Assert.Equal("CallOfDuty4x", systemTextDocument.RootElement.GetProperty("gameType").GetString());
        Assert.Equal(1, systemTextDocument.RootElement.GetProperty("runtime").GetProperty("protocolVersion").GetInt32());

        var newtonsoftJson = JsonConvert.SerializeObject(trigger, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        using var newtonsoftDocument = JsonDocument.Parse(newtonsoftJson);
        Assert.Equal("CallOfDuty4x", newtonsoftDocument.RootElement.GetProperty("gameType").GetString());
        Assert.Equal(1, newtonsoftDocument.RootElement.GetProperty("runtime").GetProperty("protocolVersion").GetInt32());

        var result = new RuleActionResult { GameServerId = Guid.NewGuid() };
        var resultJson = System.Text.Json.JsonSerializer.Serialize(
            result,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var resultDocument = JsonDocument.Parse(resultJson);
        Assert.Equal(result.GameServerId, resultDocument.RootElement.GetProperty("gameServerId").GetGuid());

        var newtonsoftResult = JsonConvert.SerializeObject(result, new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });
        using var newtonsoftResultDocument = JsonDocument.Parse(newtonsoftResult);
        Assert.Equal(result.GameServerId, newtonsoftResultDocument.RootElement.GetProperty("gameServerId").GetGuid());

        var command = new RuleActionCommand
        {
            Runtime = new RuleRuntimeDescriptor
            {
                Id = "server-agent",
                IdentityModel = RuleRuntimeIdentityModel.AgentObservedConnection,
                InstanceId = Guid.NewGuid(),
                StartedAtUtc = DateTimeOffset.UtcNow
            }
        };
        var commandJson = System.Text.Json.JsonSerializer.Serialize(
            command,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var commandDocument = JsonDocument.Parse(commandJson);
        var runtime = commandDocument.RootElement.GetProperty("runtime");
        Assert.Equal("server-agent", runtime.GetProperty("id").GetString());
        Assert.Equal("AgentObservedConnection", runtime.GetProperty("identityModel").GetString());
        Assert.Equal(command.Runtime.InstanceId, runtime.GetProperty("instanceId").GetGuid());
        Assert.True(runtime.TryGetProperty("startedAtUtc", out _));
    }
}
