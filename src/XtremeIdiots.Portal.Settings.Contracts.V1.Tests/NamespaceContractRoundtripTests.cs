using System.Text.Json;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Agent;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.BanFiles;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Broadcasts;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.ChatCommands;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Events;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.FileTransport;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Moderation;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Rcon;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Screenshots;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.ServerList;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.WelcomeMessages;

namespace XtremeIdiots.Portal.Settings.Contracts.V1.Tests;

[Trait("Category", "Unit")]
public sealed class NamespaceContractRoundtripTests
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
  };

  [Fact]
  public void Agent_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "pollIntervalMs": 5000,
              "statusPublishIntervalSeconds": 60,
              "rconSyncIntervalSeconds": 60,
              "offsetSaveIntervalSeconds": 10,
              "rconSyncEnabled": true,
              "agentName": "portal-agent",
              "logFilePath": "logs/server.log",
              "unknownProperty": "kept"
            }
            """;

    var document = JsonSerializer.Deserialize<AgentSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new AgentSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<AgentSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Equal("portal-agent", roundtripped.AgentName);
  }

  [Fact]
  public void Ftp_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "hostname": "ftp.example.com",
              "port": 21,
              "username": "user",
              "password": "secret",
              "mapsRootPath": "/maps",
              "legacyField": true
            }
            """;

    var document = JsonSerializer.Deserialize<FtpSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new FtpSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<FtpSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Equal("ftp.example.com", roundtripped.Hostname);
  }

  [Fact]
  public void Sftp_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "hostname": "sftp.example.com",
              "port": 22,
              "username": "user",
              "password": "secret",
              "mapsRootPath": "/maps",
              "hostKeyFingerprint": "SHA256:abc"
            }
            """;

    var document = JsonSerializer.Deserialize<SftpSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new SftpSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<SftpSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Equal("SHA256:abc", roundtripped.HostKeyFingerprint);
  }

  [Fact]
  public void Rcon_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "password": "rcon-secret",
              "maxMessageLength": 130,
              "messagePrefixLength": 24
            }
            """;

    var document = JsonSerializer.Deserialize<RconSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new RconSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<RconSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Equal("rcon-secret", roundtripped.Password);
    Assert.Equal(130, roundtripped.MaxMessageLength);
    Assert.Equal(24, roundtripped.MessagePrefixLength);
  }

  [Fact]
  public void Rcon_RejectsInvalidMessageLengthSettings()
  {
    var validator = new RconSettingsValidator();

    var invalidMin = validator.Validate(new RconSettingsDocument
    {
      SchemaVersion = 1,
      MaxMessageLength = 12
    });

    var invalidPrefix = validator.Validate(new RconSettingsDocument
    {
      SchemaVersion = 1,
      MaxMessageLength = 100,
      MessagePrefixLength = 100
    });

    Assert.False(invalidMin.IsValid);
    Assert.Contains(invalidMin.Errors, error => error.Contains("MaxMessageLength", StringComparison.Ordinal));

    Assert.False(invalidPrefix.IsValid);
    Assert.Contains(invalidPrefix.Errors, error => error.Contains("MessagePrefixLength", StringComparison.Ordinal));
  }

  [Fact]
  public void Screenshots_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "enabled": true,
              "directoryPath": "/screenshots",
              "filePattern": "*.jpg",
              "pollIntervalSeconds": 60
            }
            """;

    var document = JsonSerializer.Deserialize<ScreenshotSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new ScreenshotSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<ScreenshotSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Equal("*.jpg", roundtripped.FilePattern);
  }

  [Fact]
  public void Banfiles_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "checkIntervalSeconds": 120
            }
            """;

    var document = JsonSerializer.Deserialize<BanFileSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new BanFileSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<BanFileSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Equal(120, roundtripped.CheckIntervalSeconds);
  }

  [Fact]
  public void Serverlist_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "htmlBanner": "<b>Live</b>"
            }
            """;

    var document = JsonSerializer.Deserialize<ServerListSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new ServerListSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<ServerListSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Equal("<b>Live</b>", roundtripped.HtmlBanner);
  }

  [Fact]
  public void Moderation_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "contentSafetyHateSeverityThreshold": 2,
              "contentSafetyViolenceSeverityThreshold": 2,
              "contentSafetySexualSeverityThreshold": 2,
              "contentSafetySelfHarmSeverityThreshold": 2,
              "minMessageLength": 5,
              "protectedNameEnforcementEnabled": true
            }
            """;

    var document = JsonSerializer.Deserialize<ModerationSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new ModerationSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<ModerationSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Equal(5, roundtripped.MinMessageLength);
  }

  [Fact]
  public void Events_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "staleThresholdSeconds": 120,
              "playerCacheExpirationSeconds": 900
            }
            """;

    var document = JsonSerializer.Deserialize<EventSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new EventSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<EventSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Equal(900, roundtripped.PlayerCacheExpirationSeconds);
  }

  [Fact]
  public void Broadcasts_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "enabled": true,
              "intervalSeconds": 500,
              "messages": [
                { "message": "Welcome", "enabled": true },
                { "message": "Rules", "enabled": false }
              ]
            }
            """;

    var document = JsonSerializer.Deserialize<BroadcastSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new BroadcastSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<BroadcastSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.NotNull(roundtripped.Messages);
    Assert.Equal(2, roundtripped.Messages.Count);
  }

  [Fact]
  public void ChatCommands_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "defaults": {
                "enabled": true,
                "freshnessSeconds": { "default": 5, "readOnly": 5, "mutating": 3 },
                "requiredTags": ["admin"]
              },
              "commands": {
                "!register": {
                  "enabled": true,
                  "freshnessSeconds": 3,
                  "requiredTags": ["member"],
                  "settings": {
                    "cooldownSeconds": 5
                  }
                }
              }
            }
            """;

    var document = JsonSerializer.Deserialize<ChatCommandSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new ChatCommandSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<ChatCommandSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.True(roundtripped.Commands.ContainsKey("!register"));
  }

  [Fact]
  public void WelcomeMessages_RoundtripAndValidate_Succeeds()
  {
    var input = """
            {
              "schemaVersion": 1,
              "enabled": true,
              "inheritGlobalRules": true,
              "defaults": {
                "countryFallback": "Unknown",
                "connectionDelaySeconds": 2,
                "staleThresholdSeconds": 120
              },
              "rules": [
                {
                  "id": "default",
                  "enabled": true,
                  "priority": 10,
                  "visibility": "Public",
                  "messageTemplate": "Welcome {playerName}",
                  "requiredTags": ["member"],
                  "connectionDelaySeconds": 1
                }
              ],
              "ruleOverrides": [
                {
                  "id": "default",
                  "enabled": true,
                  "priority": 11,
                  "visibility": "Public",
                  "messageTemplate": "Welcome back {playerName}",
                  "requiredTags": ["member"],
                  "connectionDelaySeconds": 2
                }
              ]
            }
            """;

    var document = JsonSerializer.Deserialize<WelcomeMessageSettingsDocument>(input, JsonOptions);
    Assert.NotNull(document);

    var validation = new WelcomeMessageSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);

    var output = JsonSerializer.Serialize(document, JsonOptions);
    var roundtripped = JsonSerializer.Deserialize<WelcomeMessageSettingsDocument>(output, JsonOptions);
    Assert.NotNull(roundtripped);
    Assert.Single(roundtripped.Rules);
    Assert.Single(roundtripped.RuleOverrides);
  }

  [Fact]
  public void LegacySchemaVersion_IsAcceptedForRolloutTolerance()
  {
    var legacyJson = """
            {
              "schemaVersion": 0,
              "checkIntervalSeconds": 60
            }
            """;

    var document = JsonSerializer.Deserialize<BanFileSettingsDocument>(legacyJson, JsonOptions);
    Assert.NotNull(document);

    var validation = new BanFileSettingsValidator().Validate(document);
    Assert.True(validation.IsValid);
  }

  [Fact]
  public void UnsupportedSchemaVersion_ReturnsErrors_ForAllNamespaces()
  {
    Assert.False(new AgentSettingsValidator().Validate(new AgentSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new FtpSettingsValidator().Validate(new FtpSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new SftpSettingsValidator().Validate(new SftpSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new RconSettingsValidator().Validate(new RconSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new ScreenshotSettingsValidator().Validate(new ScreenshotSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new BanFileSettingsValidator().Validate(new BanFileSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new ServerListSettingsValidator().Validate(new ServerListSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new ModerationSettingsValidator().Validate(new ModerationSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new EventSettingsValidator().Validate(new EventSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new BroadcastSettingsValidator().Validate(new BroadcastSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new ChatCommandSettingsValidator().Validate(new ChatCommandSettingsDocument { SchemaVersion = 99 }).IsValid);
    Assert.False(new WelcomeMessageSettingsValidator().Validate(new WelcomeMessageSettingsDocument { SchemaVersion = 99 }).IsValid);
  }

  [Fact]
  public void Validators_DoNotThrow_OnMalformedNullableCollections()
  {
    var malformedBroadcasts = new BroadcastSettingsDocument
    {
      Messages = [null, new BroadcastSettingsMessage { Message = null }]
    };
    var malformedChatCommands = new ChatCommandSettingsDocument
    {
      Commands = null!
    };
    var malformedWelcomeMessages = new WelcomeMessageSettingsDocument
    {
      Rules = [null!],
      RuleOverrides = [null!]
    };

    var broadcastException = Record.Exception(() => new BroadcastSettingsValidator().Validate(malformedBroadcasts));
    var chatException = Record.Exception(() => new ChatCommandSettingsValidator().Validate(malformedChatCommands));
    var welcomeException = Record.Exception(() => new WelcomeMessageSettingsValidator().Validate(malformedWelcomeMessages));

    Assert.Null(broadcastException);
    Assert.Null(chatException);
    Assert.Null(welcomeException);
  }

  [Fact]
  public void Validators_RejectInvalidRangeValues()
  {
    var ftpValidation = new FtpSettingsValidator().Validate(new FtpSettingsDocument { Port = 0 });
    var screenshotValidation = new ScreenshotSettingsValidator().Validate(new ScreenshotSettingsDocument { Enabled = true, DirectoryPath = null, PollIntervalSeconds = 5 });
    var moderationValidation = new ModerationSettingsValidator().Validate(new ModerationSettingsDocument { ContentSafetyHateSeverityThreshold = 9 });
    var eventsValidation = new EventSettingsValidator().Validate(new EventSettingsDocument { StaleThresholdSeconds = 0, PlayerCacheExpirationSeconds = 0 });
    var broadcastsValidation = new BroadcastSettingsValidator().Validate(new BroadcastSettingsDocument { IntervalSeconds = 0, Messages = [new BroadcastSettingsMessage { Message = string.Empty }] });
    var chatValidation = new ChatCommandSettingsValidator().Validate(new ChatCommandSettingsDocument
    {
      Commands = new Dictionary<string, ChatCommandSettingsEntry>(StringComparer.OrdinalIgnoreCase)
      {
        ["!bad"] = new() { FreshnessSeconds = -1 }
      }
    });
    var welcomeValidation = new WelcomeMessageSettingsValidator().Validate(new WelcomeMessageSettingsDocument
    {
      Rules =
      [
        new WelcomeMessageRule
            {
              Id = "r1",
              MessageTemplate = "ok",
              RequiredTags = [string.Empty],
              ConnectionDelaySeconds = 999
            }
      ]
    });

    Assert.False(ftpValidation.IsValid);
    Assert.False(screenshotValidation.IsValid);
    Assert.False(moderationValidation.IsValid);
    Assert.False(eventsValidation.IsValid);
    Assert.False(broadcastsValidation.IsValid);
    Assert.False(chatValidation.IsValid);
    Assert.False(welcomeValidation.IsValid);
  }
}
