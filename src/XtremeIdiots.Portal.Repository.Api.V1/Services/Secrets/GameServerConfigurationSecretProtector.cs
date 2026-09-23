using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.FileTransport;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

internal sealed class GameServerConfigurationSecretProtector(
    IGameServerSecretStore secretStore) : IGameServerConfigurationSecretProtector
{
    private const string ReferencePrefix = "@Portal.KeyVault(SecretId=";
    private const string VersionSeparator = ";Version=";
    private const string ReferenceSuffix = ")";

    private static readonly IReadOnlyDictionary<string, SecretField[]> SecretFields =
        new Dictionary<string, SecretField[]>(StringComparer.OrdinalIgnoreCase)
        {
            [FtpSettingsConstants.Namespace] =
            [
                new("password", "ftp-password")
            ],
            [SftpSettingsConstants.Namespace] =
            [
                new("password", "sftp-password"),
                new("privateKey", "sftp-private-key"),
                new("privateKeyPassphrase", "sftp-private-key-passphrase")
            ]
        };

    public async Task<string> ResolveSecretsAsync(
        Guid gameServerId,
        string configurationNamespace,
        string configuration,
        CancellationToken cancellationToken)
    {
        if (!SecretFields.TryGetValue(configurationNamespace, out var fields))
        {
            return configuration;
        }

        var document = JObject.Parse(configuration);
        var changed = false;

        foreach (var field in fields)
        {
            var property = GetSecretProperty(document, field);
            if (property?.Value.Type != JTokenType.String)
            {
                continue;
            }

            var value = property.Value.Value<string>();
            if (!TryParseReference(value, out var reference))
            {
                RejectMalformedReference(value);
                continue;
            }

            if (!string.Equals(reference.SecretId, field.SecretId, StringComparison.Ordinal))
            {
                throw new InvalidGameServerCredentialReferenceException(
                    $"The '{field.JsonPropertyName}' field references an unexpected secret ID.");
            }

            property.Value = await secretStore
                .GetSecretAsync(gameServerId, field.SecretId, reference.Version, cancellationToken)
                .ConfigureAwait(false);
            changed = true;
        }

        return changed ? document.ToString(Formatting.None) : configuration;
    }

    public async Task<string> ExternalizeSecretsAsync(
        Guid gameServerId,
        string configurationNamespace,
        string configuration,
        CancellationToken cancellationToken)
    {
        if (!SecretFields.TryGetValue(configurationNamespace, out var fields))
        {
            return configuration;
        }

        var document = JObject.Parse(configuration);
        var pendingSecrets = new List<PendingSecret>();

        foreach (var field in fields)
        {
            var property = GetSecretProperty(document, field);
            if (property?.Value.Type != JTokenType.String)
            {
                continue;
            }

            var value = property.Value.Value<string>();
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (TryParseReference(value, out var reference))
            {
                if (!string.Equals(reference.SecretId, field.SecretId, StringComparison.Ordinal))
                {
                    throw new InvalidGameServerCredentialReferenceException(
                        $"The '{field.JsonPropertyName}' field references an unexpected secret ID.");
                }

                continue;
            }

            RejectMalformedReference(value);

            pendingSecrets.Add(new PendingSecret(property, field.SecretId, value));
        }

        foreach (var pendingSecret in pendingSecrets)
        {
            var version = await secretStore
                .SetSecretAsync(gameServerId, pendingSecret.SecretId, pendingSecret.Value, cancellationToken)
                .ConfigureAwait(false);
            pendingSecret.Property.Value = CreateReference(pendingSecret.SecretId, version);
        }

        return pendingSecrets.Count > 0 ? document.ToString(Formatting.None) : configuration;
    }

    private static string CreateReference(string secretId, string version) =>
        $"{ReferencePrefix}{secretId}{VersionSeparator}{version}{ReferenceSuffix}";

    private static JProperty? GetSecretProperty(JObject document, SecretField field)
    {
        var matchingProperties = document
            .Properties()
            .Where(property => string.Equals(
                property.Name,
                field.JsonPropertyName,
                StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToArray();

        if (matchingProperties.Length > 1)
        {
            throw new InvalidGameServerCredentialReferenceException(
                $"The '{field.JsonPropertyName}' credential field appears more than once.");
        }

        return matchingProperties.SingleOrDefault();
    }

    private static bool TryParseReference(string? value, out SecretReference reference)
    {
        reference = new SecretReference(string.Empty, string.Empty);
        if (value is null
            || !value.StartsWith(ReferencePrefix, StringComparison.Ordinal)
            || !value.EndsWith(ReferenceSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        var referenceValue = value[ReferencePrefix.Length..^ReferenceSuffix.Length];
        var separatorIndex = referenceValue.IndexOf(VersionSeparator, StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex + VersionSeparator.Length >= referenceValue.Length)
        {
            return false;
        }

        reference = new SecretReference(
            referenceValue[..separatorIndex],
            referenceValue[(separatorIndex + VersionSeparator.Length)..]);
        return true;
    }

    private static void RejectMalformedReference(string? value)
    {
        if (value?.StartsWith("@Portal.KeyVault(", StringComparison.Ordinal) == true)
        {
            throw new InvalidGameServerCredentialReferenceException("The game-server credential reference is malformed.");
        }
    }

    private sealed record SecretField(string JsonPropertyName, string SecretId);

    private sealed record PendingSecret(JProperty Property, string SecretId, string Value);

    private sealed record SecretReference(string SecretId, string Version);
}
