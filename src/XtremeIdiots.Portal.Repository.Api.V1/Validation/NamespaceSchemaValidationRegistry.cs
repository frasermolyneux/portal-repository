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
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Shared;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.WelcomeMessages;

namespace XtremeIdiots.Portal.Repository.Api.V1.Validation;

internal static class NamespaceSchemaValidationRegistry
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    private static readonly IReadOnlyDictionary<string, Func<string, SettingsValidationResult>> Validators =
        new Dictionary<string, Func<string, SettingsValidationResult>>(StringComparer.OrdinalIgnoreCase)
        {
            [AgentSettingsConstants.Namespace] = configuration =>
                new AgentSettingsValidator().Validate(Deserialize<AgentSettingsDocument>(configuration)),
            [FtpSettingsConstants.Namespace] = configuration =>
                new FtpSettingsValidator().Validate(Deserialize<FtpSettingsDocument>(configuration)),
            [SftpSettingsConstants.Namespace] = configuration =>
                new SftpSettingsValidator().Validate(Deserialize<SftpSettingsDocument>(configuration)),
            [RconSettingsConstants.Namespace] = configuration =>
                new RconSettingsValidator().Validate(Deserialize<RconSettingsDocument>(configuration)),
            [ScreenshotSettingsConstants.Namespace] = configuration =>
                new ScreenshotSettingsValidator().Validate(Deserialize<ScreenshotSettingsDocument>(configuration)),
            [BanFileSettingsConstants.Namespace] = configuration =>
                new BanFileSettingsValidator().Validate(Deserialize<BanFileSettingsDocument>(configuration)),
            [ServerListSettingsConstants.Namespace] = configuration =>
                new ServerListSettingsValidator().Validate(Deserialize<ServerListSettingsDocument>(configuration)),
            [ModerationSettingsConstants.Namespace] = configuration =>
                new ModerationSettingsValidator().Validate(Deserialize<ModerationSettingsDocument>(configuration)),
            [EventSettingsConstants.Namespace] = configuration =>
                new EventSettingsValidator().Validate(Deserialize<EventSettingsDocument>(configuration)),
            [BroadcastSettingsConstants.Namespace] = configuration =>
                new BroadcastSettingsValidator().Validate(Deserialize<BroadcastSettingsDocument>(configuration)),
            [ChatCommandSettingsConstants.Namespace] = configuration =>
                new ChatCommandSettingsValidator().Validate(Deserialize<ChatCommandSettingsDocument>(configuration)),
            [WelcomeMessageSettingsConstants.Namespace] = configuration =>
                new WelcomeMessageSettingsValidator().Validate(Deserialize<WelcomeMessageSettingsDocument>(configuration))
        };

    public static bool TryValidate(string ns, string configuration)
    {
        if (!Validators.TryGetValue(ns, out var validator))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(configuration);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var validation = validator(configuration);
            return validation.IsValid;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static TDocument? Deserialize<TDocument>(string configuration)
    {
        return JsonSerializer.Deserialize<TDocument>(configuration, JsonSerializerOptions);
    }
}