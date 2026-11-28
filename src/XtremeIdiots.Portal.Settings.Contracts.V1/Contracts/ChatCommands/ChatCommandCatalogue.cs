using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Moderation;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.ChatCommands;

public enum ChatCommandReplyChannel
{
    Private,
    Public
}

public sealed record ChatCommandReplyDefinition(
    string Key, ChatCommandReplyChannel Channel, bool Required, IReadOnlyList<string> AllowedTokens);

public sealed record ChatCommandDefinition(
    string Name, string Prefix, IReadOnlyList<string> Aliases, string ExecutionOwner,
    IReadOnlyList<int> ConfirmedTransportGameTypes, IReadOnlyList<ChatCommandReplyDefinition> Replies);

/// <summary>Source-backed command metadata; policy and reply configuration remain staged until consumers support them.</summary>
public static class ChatCommandCatalogueV1
{
    private static readonly IReadOnlyList<int> TransportGames = RulesEngineCatalogueV1.SupportedGameTypes;

    /// <summary>Staged authoring capabilities; deployed consumers may support a smaller subset.</summary>
    public static IReadOnlyList<ConditionDefinition> PolicyConditions { get; } = Array.AsReadOnly(
    [
        RulesEngineCatalogueV1.Current.Conditions.Single(item => item.ConditionId == RulesEngineCatalogueV1.AlwaysCondition),
        RulesEngineCatalogueV1.Current.Conditions.Single(item => item.ConditionId == "player.tags"),
        ChatModerationCatalogueV1.Staged.Conditions.Single(item => item.ConditionId == ChatModerationRulesConstants.PlayerFirstSeen)
    ]);

    public static IReadOnlyList<ChatCommandDefinition> Commands { get; } = Array.AsReadOnly<ChatCommandDefinition>(
    [
        new("commands", "!commands", ["!help"], "server-events", TransportGames, []),
        new("register", "!register", [], "server-events", TransportGames, []),
        new("whoami", "!whoami", [], "server-events", TransportGames, []),
        new("like", "!like", [], "server-events", TransportGames, []),
        new("dislike", "!dislike", [], "server-events", TransportGames, []),
        new("fu", "!fu", [], "server-events", TransportGames,
        [
            new("denied", ChatCommandReplyChannel.Private, true, []),
            new("usage", ChatCommandReplyChannel.Private, true, []),
            new("targetUnavailable", ChatCommandReplyChannel.Private, true, []),
            new("targetNotFound", ChatCommandReplyChannel.Private, true, []),
            new("targetAmbiguous", ChatCommandReplyChannel.Private, true, ["suggestions"]),
            new("invalidMessage", ChatCommandReplyChannel.Private, true, []),
            new("sendFailed", ChatCommandReplyChannel.Private, true, []),
            new("success", ChatCommandReplyChannel.Public, true, ["name"])
        ])
    ]);

    public static ChatCommandDefinition? Find(string? name) =>
        Commands.FirstOrDefault(command =>
            string.Equals(command.Name, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command.Prefix, name, StringComparison.OrdinalIgnoreCase) ||
            command.Aliases.Any(alias => string.Equals(alias, name, StringComparison.OrdinalIgnoreCase)));
}
