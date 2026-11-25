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
    public IReadOnlyList<int> GameTypes { get; init; } = [];
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
    public IReadOnlyList<ActionExecutionProviderDefinition> ExecutionProviders { get; init; } = [];
    public IReadOnlyList<string> RequiredIntegrationCapabilities { get; init; } = [];
    public IReadOnlyList<string> RequiredRuntimeCapabilities { get; init; } = [];
    public IReadOnlyList<RuleParameterDefinition> Parameters { get; init; } = [];
    public bool ProducesModerationRecord { get; init; }
    public bool SupportsForumPublication { get; init; }
}

public sealed record ActionExecutionProviderDefinition
{
    public required string ExecutorId { get; init; }
    public IReadOnlyList<int> GameTypes { get; init; } = [];
    public IReadOnlyList<string> RequiredIntegrationCapabilities { get; init; } = [];
    public IReadOnlyList<string> RequiredRuntimeCapabilities { get; init; } = [];
    public IReadOnlyList<RuleParameterDefinition> ParameterConstraints { get; init; } = [];
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
    public string? AllowedCharacterSet { get; init; }
    public required string UiControl { get; init; }
    public string? Description { get; init; }
    public bool SupportsTemplates { get; init; }
    public bool SupportsMessageList { get; init; }
}

public sealed record FeatureDefinition
{
    public required string FeatureId { get; init; }
    public required string DisplayName { get; init; }
    public IReadOnlyList<int> GameTypes { get; init; } = [];
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
    public const string RconPlayerDropCapability = "rcon.player.drop";
    public const string RconPublicChatCapability = "rcon.chat.public";
    public const string RconPrivateChatCapability = "rcon.chat.private";
    public const string VpnProtectionFeature = "vpn-protection";
    public const string SiegeModeFeature = "siege-mode";
    public const string WelcomeMessagesFeature = "welcome-messages";
    public const string AlwaysCondition = "always";
    public const string PrivateTaggedMessageAction = "message.private-tagged";
    public const string Cod4xPrivateRecipientCapability = "cod4x.chat.private.recipient";
    public const string RconPrivateRecipientCapability = "rcon.chat.private.recipient";
    public const string Cod4xFragmentsCapability = "cod4x.chat.fragments";
    public static IReadOnlyList<int> SupportedGameTypes { get; } = Array.AsReadOnly([1, 2, 3, 24]);

    private static RulesEngineCatalogue CreateCatalogue() => new()
    {
        Triggers =
        [
            new()
            {
                TriggerId = PlayerConnectedTrigger,
                DisplayName = "Player connected",
                GameTypes = SupportedGameTypes,
                ContextFields = ["gameServerId", "gameType", "connectionId", "playerGuid", "slotId"],
                CorrelationScope = "connection"
            },
            new()
            {
                TriggerId = PlayerIpResolvedTrigger,
                DisplayName = "Player IP resolved",
                GameTypes = SupportedGameTypes,
                ContextFields = ["gameServerId", "gameType", "connectionId", "playerGuid", "playerIpAddress"],
                CorrelationScope = "connection"
            }
        ],
        Conditions =
        [
            new() { ConditionId = AlwaysCondition, DisplayName = "Always / Everyone", ValueType = "boolean", GameTypes = SupportedGameTypes },
            new() { ConditionId = "vpn.intelligence", DisplayName = "VPN intelligence", ValueType = "typed", GameTypes = SupportedGameTypes, Operators = ["Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Contains"], Parameters = [new() { Name = "signal", Type = "string", Required = true, AllowedValues = Enum.GetNames<VpnProtectionSignal>().Where(static name => name != nameof(VpnProtectionSignal.Unknown)).ToArray(), UiControl = "select" }, new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["Equal", "NotEqual", "GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Contains"], UiControl = "select" }, new() { Name = "expectedValue", Type = "string", Required = true, MaxLength = 256, UiControl = "text" }] },
            new() { ConditionId = "player.tags", DisplayName = "Player tags", ValueType = "string-array", GameTypes = SupportedGameTypes, Operators = ["ContainsAny", "ContainsAll", "ContainsNone"], Parameters = [new() { Name = "tags", Type = "string[]", Required = true, MaxLength = 128, UiControl = "multi-select" }, new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["ContainsAny", "ContainsAll", "ContainsNone"], UiControl = "select" }] },
            new() { ConditionId = "player.first-seen-guid", DisplayName = "GUID first seen", ValueType = "date-time", GameTypes = SupportedGameTypes, Operators = ["IsNew", "IsKnown", "Before", "After", "WithinAge"], Parameters = [new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["IsNew", "IsKnown", "Before", "After", "WithinAge"], UiControl = "select" }, new() { Name = "value", Type = "date-time-or-duration", UiControl = "input" }, new() { Name = "referenceScope", Type = "string", Required = true, AllowedValues = ["ObservedHistory", "SiegeBaseline"], UiControl = "select" }] },
            new() { ConditionId = "player.first-seen-ip", DisplayName = "IP first seen", ValueType = "date-time", GameTypes = SupportedGameTypes, Operators = ["IsNew", "IsKnown", "Before", "After", "WithinAge"], Parameters = [new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["IsNew", "IsKnown", "Before", "After", "WithinAge"], UiControl = "select" }, new() { Name = "value", Type = "date-time-or-duration", UiControl = "input" }, new() { Name = "referenceScope", Type = "string", Required = true, AllowedValues = ["ObservedHistory", "SiegeBaseline"], UiControl = "select" }] },
            new() { ConditionId = "player.profile-age", DisplayName = "Player profile age", ValueType = "duration", GameTypes = SupportedGameTypes, Operators = ["LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual"], Parameters = [new() { Name = "operator", Type = "string", Required = true, AllowedValues = ["LessThan", "LessThanOrEqual", "GreaterThan", "GreaterThanOrEqual"], UiControl = "select" }, new() { Name = "duration", Type = "duration", Required = true, UiControl = "duration-input" }] }
        ],
        Actions =
        [
            new() { ActionTypeId = "moderation.observation", DisplayName = "Observation", ExecutorId = "server-events", EffectClass = "record", GameTypes = SupportedGameTypes, ProducesModerationRecord = true, SupportsForumPublication = true, Parameters = [new() { Name = "reason", Type = "string", Required = true, MaxLength = 512, SupportsTemplates = true, UiControl = "text" }, new() { Name = "postToForums", Type = "boolean", UiControl = "toggle" }] },
            new() { ActionTypeId = "player.kick", DisplayName = "Kick", ExecutorId = "game-runtime", EffectClass = "enforcement", GameTypes = SupportedGameTypes, ExecutionProviders = DropProviders, Parameters = [new() { Name = "reason", Type = "string", Required = true, MaxLength = 256, MaxUtf8Bytes = 256, RejectControlCharacters = true, SupportsTemplates = true, UiControl = "text" }] },
            new() { ActionTypeId = "moderation.kick-with-record", DisplayName = "Kick with record", ExecutorId = "server-events", EffectClass = "record+enforcement", GameTypes = SupportedGameTypes, ExecutionProviders = DropProviders, ProducesModerationRecord = true, SupportsForumPublication = true, Parameters = [new() { Name = "reason", Type = "string", Required = true, MaxLength = 512, MaxUtf8Bytes = 256, RejectControlCharacters = true, SupportsTemplates = true, UiControl = "text" }, new() { Name = "postToForums", Type = "boolean", UiControl = "toggle" }] },
            new() { ActionTypeId = "moderation.temporary-ban", DisplayName = "Temporary ban", ExecutorId = "server-events", EffectClass = "record+enforcement", GameTypes = SupportedGameTypes, ProducesModerationRecord = true, SupportsForumPublication = true, Parameters = [new() { Name = "durationMinutes", Type = "integer", Required = true, Minimum = 1, Maximum = 525600, UiControl = "number" }, new() { Name = "reason", Type = "string", Required = true, MaxLength = 512, SupportsTemplates = true, UiControl = "text" }, new() { Name = "postToForums", Type = "boolean", UiControl = "toggle" }] },
            new() { ActionTypeId = "moderation.ban", DisplayName = "Ban", ExecutorId = "server-events", EffectClass = "record+enforcement", GameTypes = SupportedGameTypes, ProducesModerationRecord = true, SupportsForumPublication = true, Parameters = [new() { Name = "reason", Type = "string", Required = true, MaxLength = 512, SupportsTemplates = true, UiControl = "text" }, new() { Name = "postToForums", Type = "boolean", UiControl = "toggle" }] },
            new() { ActionTypeId = "message.public", DisplayName = "Public message", ExecutorId = "game-runtime", EffectClass = "communication", GameTypes = SupportedGameTypes, ExecutionProviders = PublicMessageProviders, Parameters = MessageParameters },
            new() { ActionTypeId = "message.private", DisplayName = "Private message", ExecutorId = "game-runtime", EffectClass = "communication", GameTypes = SupportedGameTypes, ExecutionProviders = PrivateMessageProviders, Parameters = MessageParameters },
            new() { ActionTypeId = PrivateTaggedMessageAction, DisplayName = "Private message tagged players", ExecutorId = "game-runtime", EffectClass = "communication", GameTypes = SupportedGameTypes, ExecutionProviders = RecipientMessageProviders, Parameters = [.. MessageParameters, new() { Name = "recipientTags", Type = "string[]", Required = true, MaxLength = 128, UiControl = "multi-select" }, new() { Name = "recipientTagMatch", Type = "string", Required = true, AllowedValues = ["Any", "All"], UiControl = "select" }] }
        ],
        Features =
        [
            new()
            {
                FeatureId = VpnProtectionFeature,
                DisplayName = "VPN protection",
                GameTypes = SupportedGameTypes,
                TriggerIds = [PlayerConnectedTrigger, PlayerIpResolvedTrigger],
                ConditionIds = [AlwaysCondition, "vpn.intelligence", "player.tags", "player.first-seen-guid", "player.first-seen-ip", "player.profile-age"],
                ActionTypeIds = ["moderation.observation", "player.kick", "moderation.kick-with-record", "moderation.temporary-ban", "moderation.ban", "message.public", "message.private", PrivateTaggedMessageAction],
                SupportsGlobalRules = true,
                SupportsServerOverrides = true
            },
            new()
            {
                FeatureId = SiegeModeFeature,
                DisplayName = "Siege mode",
                GameTypes = SupportedGameTypes,
                TriggerIds = [PlayerConnectedTrigger, PlayerIpResolvedTrigger],
                ConditionIds = [AlwaysCondition, "vpn.intelligence", "player.tags", "player.first-seen-guid", "player.first-seen-ip", "player.profile-age"],
                ActionTypeIds = ["moderation.observation", "player.kick", "moderation.kick-with-record", "moderation.temporary-ban", "moderation.ban", "message.public", "message.private", PrivateTaggedMessageAction],
                SupportsGlobalRules = true,
                SupportsServerOverrides = true,
                SupportsDryRun = true
            },
            new()
            {
                FeatureId = WelcomeMessagesFeature,
                DisplayName = "Welcome messages",
                GameTypes = SupportedGameTypes,
                TriggerIds = [PlayerConnectedTrigger],
                ConditionIds = [AlwaysCondition, "player.tags", "player.first-seen-guid", "player.first-seen-ip", "player.profile-age", "vpn.intelligence"],
                ActionTypeIds = ["message.public", "message.private", PrivateTaggedMessageAction],
                SupportsGlobalRules = true,
                SupportsServerOverrides = true
            }
        ]
    };

    private static IReadOnlyList<RuleParameterDefinition> MessageParameters { get; } =
    [
        new() { Name = "message", Type = "string", MaxLength = 16384, RejectControlCharacters = true, SupportsTemplates = true, SupportsMessageList = true, UiControl = "text" },
        new() { Name = "messages", Type = "string[]", MaxLength = 16384, RejectControlCharacters = true, SupportsTemplates = true, SupportsMessageList = true, UiControl = "message-list" }
    ];

    private static IReadOnlyList<ActionExecutionProviderDefinition> RecipientMessageProviders { get; } =
    [
        new() { ExecutorId = Cod4xPluginRuntime, GameTypes = [24], RequiredRuntimeCapabilities = [Cod4xPrivateChatCapability, Cod4xPrivateRecipientCapability, Cod4xFragmentsCapability] },
        new() { ExecutorId = "rcon", GameTypes = SupportedGameTypes, RequiredIntegrationCapabilities = [RconPrivateChatCapability, RconPrivateRecipientCapability] }
    ];

    private static IReadOnlyList<ActionExecutionProviderDefinition> DropProviders { get; } =
    [
        new()
        {
            ExecutorId = Cod4xPluginRuntime,
            GameTypes = [24],
            RequiredRuntimeCapabilities = [Cod4xPlayerDropCapability]
        },
        new()
        {
            ExecutorId = "rcon",
            GameTypes = SupportedGameTypes,
            RequiredIntegrationCapabilities = [RconPlayerDropCapability]
        }
    ];

    private static IReadOnlyList<ActionExecutionProviderDefinition> PublicMessageProviders { get; } =
    [
        new()
        {
            ExecutorId = Cod4xPluginRuntime,
            GameTypes = [24],
            RequiredRuntimeCapabilities = [Cod4xPublicChatCapability]
        },
        new()
        {
            ExecutorId = "rcon",
            GameTypes = SupportedGameTypes,
            RequiredIntegrationCapabilities = [RconPublicChatCapability],
            ParameterConstraints =
            [
                new()
                {
                    Name = "message",
                    Type = "string",
                    Required = true,
                    MaxLength = 256,
                    MaxUtf8Bytes = 256,
                    RejectControlCharacters = true,
                    AllowedCharacterSet = "RconSafePrintableAscii",
                    UiControl = "text"
                }
            ]
        }
    ];

    private static IReadOnlyList<ActionExecutionProviderDefinition> PrivateMessageProviders { get; } =
    [
        new()
        {
            ExecutorId = Cod4xPluginRuntime,
            GameTypes = [24],
            RequiredRuntimeCapabilities = [Cod4xPrivateChatCapability]
        },
        new()
        {
            ExecutorId = "rcon",
            GameTypes = SupportedGameTypes,
            RequiredIntegrationCapabilities = [RconPrivateChatCapability],
            ParameterConstraints =
            [
                new()
                {
                    Name = "message",
                    Type = "string",
                    Required = true,
                    MaxLength = 256,
                    MaxUtf8Bytes = 256,
                    RejectControlCharacters = true,
                    AllowedCharacterSet = "RconSafePrintableAscii",
                    UiControl = "text"
                }
            ]
        }
    ];

    public static RulesEngineCatalogue Current { get; } = CreateCatalogue();
}
