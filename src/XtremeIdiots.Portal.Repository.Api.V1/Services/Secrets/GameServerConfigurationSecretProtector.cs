using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.FileTransport;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

internal sealed class GameServerConfigurationSecretProtector(
    IGameServerSecretStore secretStore) : IGameServerConfigurationSecretProtector
{
    private const string ReferencePrefix = "@Portal.KeyVault(SecretId=";
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
            if (!TryParseReference(value, out var referencedSecretId))
            {
                RejectMalformedReference(value);
                continue;
            }

            if (!string.Equals(referencedSecretId, field.SecretId, StringComparison.Ordinal))
            {
                throw new InvalidGameServerCredentialReferenceException(
                    $"The '{field.JsonPropertyName}' field references an unexpected secret ID.");
            }

            property.Value = await secretStore
                .GetSecretAsync(gameServerId, field.SecretId, cancellationToken)
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

            if (TryParseReference(value, out var referencedSecretId))
            {
                if (!string.Equals(referencedSecretId, field.SecretId, StringComparison.Ordinal))
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
            await secretStore
                .SetSecretAsync(gameServerId, pendingSecret.SecretId, pendingSecret.Value, cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (var pendingSecret in pendingSecrets)
        {
            pendingSecret.Property.Value = CreateReference(pendingSecret.SecretId);
        }

        return pendingSecrets.Count > 0 ? document.ToString(Formatting.None) : configuration;
    }

    private static string CreateReference(string secretId) => $"{ReferencePrefix}{secretId}{ReferenceSuffix}";

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

    private static bool TryParseReference(string? value, out string secretId)
    {
        secretId = string.Empty;
        if (value is null
            || !value.StartsWith(ReferencePrefix, StringComparison.Ordinal)
            || !value.EndsWith(ReferenceSuffix, StringComparison.Ordinal))
        {
            return false;
        }

        secretId = value[ReferencePrefix.Length..^ReferenceSuffix.Length];
        return secretId.Length > 0;
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
}
