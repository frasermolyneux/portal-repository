using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.ChatCommands;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.RulesEngine;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

public sealed class ChatCommandPolicyTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PolicyAndRepliesRoundTripWithoutChangingLegacySettings()
    {
        const string input = """
            {
              "schemaVersion": 1,
              "commands": {
                "!fu": {
                  "requiredTags": [],
                  "settings": { "messages": ["Hello {name}"] },
                  "policy": {
                    "defaultDecision": "Deny",
                    "rules": [{
                      "id": "allowed", "enabled": true, "order": 10,
                      "condition": { "conditionId": "always", "parameters": {} },
                      "decision": "Allow", "futureField": 1
                    }],
                    "futurePolicy": true
                  },
                  "replies": {
                    "denied": ["Not allowed"], "targetAmbiguous": ["Try {suggestions}"],
                    "success": ["Hello {Name}"], "usage": null, "futureOptional": []
                  },
                  "futureCommand": 42
                }
              }
            }
            """;
        var document = JsonSerializer.Deserialize<ChatCommandSettingsDocument>(input, Options)!;
        var validation = new ChatCommandSettingsValidator().Validate(document);
        Assert.True(validation.IsValid, string.Join("; ", validation.Errors));
        var output = JsonSerializer.Serialize(document, Options);
        var roundTrip = JsonSerializer.Deserialize<ChatCommandSettingsDocument>(output, Options)!;
        var command = roundTrip.Commands["!fu"];
        Assert.Empty(command.RequiredTags!);
        Assert.Equal("Hello {name}", command.Settings!.Value.GetProperty("messages")[0].GetString());
        Assert.Equal(ChatCommandPolicyDecision.Deny, command.Policy!.DefaultDecision);
        Assert.True(command.Policy.ExtensionData!.ContainsKey("futurePolicy"));
        Assert.True(Assert.Single(command.Policy.Rules!).ExtensionData!.ContainsKey("futureField"));
        Assert.Null(command.Replies!["usage"]);
        Assert.Empty(command.Replies["futureOptional"]!);
        Assert.True(command.ExtensionData!.ContainsKey("futureCommand"));
    }

    [Fact]
    public void EmptyPolicyIsExplicitAndIncompleteOrInvalidRulesAreRejected()
    {
        var entry = new ChatCommandSettingsEntry
        {
            Policy = new ChatCommandPolicy { DefaultDecision = ChatCommandPolicyDecision.Deny, Rules = [] }
        };
        var document = new ChatCommandSettingsDocument { Commands = { ["!fu"] = entry } };
        var validator = new ChatCommandSettingsValidator();
        Assert.True(validator.Validate(document).IsValid);

        entry.Policy.Rules = null;
        Assert.False(validator.Validate(document).IsValid);
        entry.Policy.Rules =
        [
            new() { Id = "duplicate", Decision = ChatCommandPolicyDecision.Allow, Condition = new() { ConditionId = "always", Parameters = [] } },
            new() { Id = "duplicate", Decision = ChatCommandPolicyDecision.Unknown, Condition = new() }
        ];
        var invalid = validator.Validate(document);
        Assert.Contains(invalid.Errors, error => error.Contains(".id", StringComparison.Ordinal));
        Assert.Contains(invalid.Errors, error => error.Contains(".decision", StringComparison.Ordinal));
        Assert.Contains(invalid.Errors, error => error.Contains(".condition", StringComparison.Ordinal));
    }

    [Fact]
    public void FuRepliesCannotSuppressRequiredMessagesOrUseWrongTokens()
    {
        var entry = new ChatCommandSettingsEntry
        {
            Replies = new()
            {
                ["denied"] = [],
                ["targetAmbiguous"] = ["Target {name}"],
                ["success"] = ["Success {suggestions}"],
                ["usage"] = ["Bad {token"]
            }
        };
        var document = new ChatCommandSettingsDocument { Commands = { ["fu"] = entry } };
        var invalid = new ChatCommandSettingsValidator().Validate(document);
        Assert.Equal(4, invalid.Errors.Count);

        entry.Replies["denied"] = null;
        entry.Replies["targetAmbiguous"] = ["Try {Suggestions}"];
        entry.Replies["success"] = ["Hello {name}"];
        entry.Replies["usage"] = ["Specify a target"];
        Assert.True(new ChatCommandSettingsValidator().Validate(document).IsValid);
    }

    [Fact]
    public void CatalogueIdentifiesAliasesOwnershipAndConfirmedTransportWithoutDispatchRestriction()
    {
        var commands = ChatCommandCatalogueV1.Find("!help")!;
        Assert.Equal("commands", commands.Name);
        Assert.Equal("server-events", commands.ExecutionOwner);
        Assert.Equal(new[] { 1, 2, 3, 24 }, commands.ConfirmedTransportGameTypes);
        var fu = ChatCommandCatalogueV1.Find("!fu")!;
        Assert.Equal(ChatCommandReplyChannel.Private, fu.Replies.Single(item => item.Key == "denied").Channel);
        Assert.Equal(ChatCommandReplyChannel.Public, fu.Replies.Single(item => item.Key == "success").Channel);
        Assert.All(fu.Replies, reply => Assert.True(reply.Required));
    }
}
