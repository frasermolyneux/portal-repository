using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.VpnProtection;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

public sealed record RulesEngineCatalogue
{
    public int SchemaVersion { get; init; } = RuleSetConstants.SchemaVersion;
    public IReadOnlyList<TriggerDefinition> Triggers { get; init; } = [];
    public IReadOnlyList<ConditionDefinition> Conditions { get; init; } = [];
    public IReadOnlyList<ActionTypeDefinition> Actions { get; init; } = [];
    public IReadOnlyList<FeatureDefinition> Features { get; init; } = [];
}

public sealed record TriggerDefinition
{
    public required string TriggerId { get; init; }
    public required string DisplayName { get; init; }
    public IReadOnlyList<int> GameTypes { get; init; } = [];
    public IReadOnlyList<string> RequiredIntegrationCapabilities { get; init; } = [];
    public IReadOnlyList<string> RequiredRuntimeCapabilities { get; init; } = [];
    public IReadOnlyList<string> ContextFields { get; init; } = [];
    public IReadOnlyList<RuleParameterDefinition> Parameters { get; init; } = [];
    public required string CorrelationScope { get; init; }
}

public sealed record ConditionDefinition
{
    public required string ConditionId { get; init; }
    public required string DisplayName { get; init; }
    public required string ValueType { get; init; }
    public IReadOnlyList<string> Operators { get; init; } = [];
    public IReadOnlyList<string> GameCapabilities { get; init; } = [];
    public IReadOnlyList<string> RuntimeCapabilities { get; init; } = [];
    public IReadOnlyList<RuleParameterDefinition> Parameters { get; init; } = [];
}

public sealed record ActionTypeDefinition
{
    public required string ActionTypeId { get; init; }
    public required string DisplayName { get; init; }
    public required string ExecutorId { get; init; }
    public required string EffectClass { get; init; }
    public IReadOnlyList<int> GameTypes { get; init; } = [];
    public IReadOnlyList<string> RequiredIntegrationCapabilities { get; init; } = [];
    public IReadOnlyList<string> RequiredRuntimeCapabilities { get; init; } = [];
    public IReadOnlyList<RuleParameterDefinition> Parameters { get; init; } = [];
    public bool ProducesModerationRecord { get; init; }
    public bool SupportsForumPublication { get; init; }
}

public sealed record RuleParameterDefinition
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public bool Required { get; init; }
    public IReadOnlyList<string> AllowedValues { get; init; } = [];
    public double? Minimum { get; init; }
    public double? Maximum { get; init; }
    public int? MaxLength { get; init; }
    public int? MaxUtf8Bytes { get; init; }
    public bool RejectControlCharacters { get; init; }
    public required string UiControl { get; init; }
    public string? Description { get; init; }
}

public sealed record FeatureDefinition
{
    public required string FeatureId { get; init; }
    public required string DisplayName { get; init; }
    public IReadOnlyList<string> TriggerIds { get; init; } = [];
    public IReadOnlyList<string> ConditionIds { get; init; } = [];
    public IReadOnlyList<string> ActionTypeIds { get; init; } = [];
    public bool SupportsGlobalRules { get; init; }
    public bool SupportsServerOverrides { get; init; }
    public bool SupportsDryRun { get; init; }
}

public static class RulesEngineCatalogueV1
{
    public const string PlayerConnectedTrigger = "player.connected";
    public const string PlayerIpResolvedTrigger = "player.ip-resolved";
    public const string Cod4xPluginRuntime = "cod4x-plugin";
    public const string Cod4xPlayerDropCapability = "cod4x.player.drop";
    public const string Cod4xPublicChatCapability = "cod4x.chat.public";
    public const string Cod4xPrivateChatCapability = "cod4x.chat.private";
    public const string VpnProtectionFeature = "vpn-protection";
    public const string SiegeModeFeature = "siege-mode";

    public static RulesEngineCatalogue Current { get; } = new()
    {
        Triggers =
        [
            new()
            {
                TriggerId = PlayerConnectedTrigger,
                DisplayName = "Player connected",
                RequiredIntegrationCapabilities = ["player.connection"],
                ContextFields = ["gameServerId", "gameType", "connectionId", "playerGuid", "slotId"],
                CorrelationScope = "connection"
            },
            new()
            {
                TriggerId = PlayerIpResolvedTrigger,
                DisplayName = "Player IP resolved",
                RequiredIntegrationCapabilities = ["player.ip"],
                ContextFields = ["gameServerId", "gameType", "connectionId", "playerGuid", "playerIpAddress"],
                CorrelationScope = "connection"
            }
        ],
        Conditions =
        [
            new() { ConditionId = "vpn.intelligence", DisplayName = "VPN intelligence", ValueType = "typed", Operators = ["Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Contains"], Parameters = [new() { Name = "signal", Type = "string", Required = true, AllowedValues = Enum.GetNames<VpnProtectionSignal>().Where(static name => name != nameof(VpnProtectionSignal.Unknown)).ToArray(), UiControl = "select" }, new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Contains"], UiControl = "select" }, new() { Name = "expectedValue", Type = "string", Required = true, MaxLength = 256, UiControl = "text" }] },
            new() { ConditionId = "player.tags", DisplayName = "Player tags", ValueType = "string-array", Operators = ["ContainsAny", "ContainsAll", "ContainsNone"], Parameters = [new() { Name = "tags", Type = "string[]", Required = true, MaxLength = 128, UiControl = "multi-select" }, new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["ContainsAny", "ContainsAll", "ContainsNone"], UiControl = "select" }] },
            new() { ConditionId = "player.first-seen-guid", DisplayName = "GUID first seen", ValueType = "date-time", Operators = ["IsNew", "IsKnown", "Before", "After", "WithinAge"], Parameters = [new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["IsNew", "IsKnown", "Before", "After", "WithinAge"], UiControl = "select" }, new() { Name = "value", Type = "date-time-or-duration", UiControl = "input" }, new() { Name = "referenceScope", Type = "string", Required = true, AllowedValues = ["ObservedHistory", "SiegeBaseline"], UiControl = "select" }] },
            new() { ConditionId = "player.first-seen-ip", DisplayName = "IP first seen", ValueType = "date-time", Operators = ["IsNew", "IsKnown", "Before", "After", "WithinAge"], Parameters = [new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["IsNew", "IsKnown", "Before", "After", "WithinAge"], UiControl = "select" }, new() { Name = "value", Type = "date-time-or-duration", UiControl = "input" }, new() { Name = "referenceScope", Type = "string", Required = true, AllowedValues = ["ObservedHistory", "SiegeBaseline"], UiControl = "select" }] },
            new() { ConditionId = "player.profile-age", DisplayName = "Player profile age", ValueType = "duration", Operators = ["LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual"], Parameters = [new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual"], UiControl = "select" }, new() { Name = "duration", Type = "duration", Required = true, UiControl = "duration-input" }] }
        ],
        Actions =
        [
            new() { ActionTypeId = "moderation.observation", DisplayName = "Observation", ExecutorId = "server-events", EffectClass = "record", ProducesModerationRecord = true, SupportsForumPublication = true, Parameters = [new() { Name = "reason", Type = "string", Required = true, MaxLength = 512, UiControl = "text" }, new() { Name = "postToForums", Type = "boolean", UiControl = "toggle" }] },
            new() { ActionTypeId = "player.kick", DisplayName = "Kick", ExecutorId = "game-plugin", EffectClass = "enforcement", RequiredRuntimeCapabilities = [Cod4xPlayerDropCapability], Parameters = [new() { Name = "reason", Type = "string", Required = true, MaxLength = 256, MaxUtf8Bytes = 256, RejectControlCharacters = true, UiControl = "text" }] },
            new() { ActionTypeId = "moderation.kick-with-record", DisplayName = "Kick with record", ExecutorId = "server-events", EffectClass = "record+enforcement", RequiredRuntimeCapabilities = [Cod4xPlayerDropCapability], ProducesModerationRecord = true, SupportsForumPublication = true, Parameters = [new() { Name = "reason", Type = "string", Required = true, MaxLength = 512, MaxUtf8Bytes = 256, RejectControlCharacters = true, UiControl = "text" }, new() { Name = "postToForums", Type = "boolean", UiControl = "toggle" }] },
            new() { ActionTypeId = "moderation.temporary-ban", DisplayName = "Temporary ban", ExecutorId = "server-events", EffectClass = "record+enforcement", ProducesModerationRecord = true, SupportsForumPublication = true, Parameters = [new() { Name = "durationMinutes", Type = "integer", Required = true, Minimum = 1, Maximum = 525600, UiControl = "number" }, new() { Name = "reason", Type = "string", Required = true, MaxLength = 512, UiControl = "text" }, new() { Name = "postToForums", Type = "boolean", UiControl = "toggle" }] },
            new() { ActionTypeId = "moderation.ban", DisplayName = "Ban", ExecutorId = "server-events", EffectClass = "record+enforcement", ProducesModerationRecord = true, SupportsForumPublication = true, Parameters = [new() { Name = "reason", Type = "string", Required = true, MaxLength = 512, UiControl = "text" }, new() { Name = "postToForums", Type = "boolean", UiControl = "toggle" }] },
            new() { ActionTypeId = "message.public", DisplayName = "Public message", ExecutorId = "game-plugin", EffectClass = "communication", RequiredRuntimeCapabilities = [Cod4xPublicChatCapability], Parameters = [new() { Name = "message", Type = "string", Required = true, MaxLength = 256, MaxUtf8Bytes = 256, RejectControlCharacters = true, UiControl = "text" }] },
            new() { ActionTypeId = "message.private", DisplayName = "Private message", ExecutorId = "game-plugin", EffectClass = "communication", RequiredRuntimeCapabilities = [Cod4xPrivateChatCapability], Parameters = [new() { Name = "message", Type = "string", Required = true, MaxLength = 256, MaxUtf8Bytes = 256, RejectControlCharacters = true, UiControl = "text" }] }
        ],
        Features =
        [
            new()
            {
                FeatureId = VpnProtectionFeature,
                DisplayName = "VPN protection",
                TriggerIds = [PlayerConnectedTrigger, PlayerIpResolvedTrigger],
                ConditionIds = ["vpn.intelligence", "player.tags", "player.first-seen-guid", "player.first-seen-ip", "player.profile-age"],
                ActionTypeIds = ["moderation.observation", "player.kick", "moderation.kick-with-record", "moderation.temporary-ban", "moderation.ban", "message.public", "message.private"],
                SupportsGlobalRules = true,
                SupportsServerOverrides = true
            },
            new()
            {
                FeatureId = SiegeModeFeature,
                DisplayName = "Siege mode",
                TriggerIds = [PlayerConnectedTrigger, PlayerIpResolvedTrigger],
                ConditionIds = ["vpn.intelligence", "player.tags", "player.first-seen-guid", "player.first-seen-ip", "player.profile-age"],
                ActionTypeIds = ["moderation.observation", "player.kick", "moderation.kick-with-record", "moderation.temporary-ban", "moderation.ban", "message.public", "message.private"],
                SupportsGlobalRules = true,
                SupportsServerOverrides = true,
                SupportsDryRun = true
            }
        ]
    };
}
