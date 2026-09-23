using Azure;
using Azure.Security.KeyVault.Secrets;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

internal sealed class KeyVaultGameServerSecretStore(SecretClient secretClient) : IGameServerSecretStore
{
    public async Task<string> GetSecretAsync(
        Guid gameServerId,
        string secretId,
        string? secretVersion,
        CancellationToken cancellationToken)
    {
        var response = await secretClient
            .GetSecretAsync(GetSecretName(gameServerId, secretId), secretVersion, cancellationToken)
            .ConfigureAwait(false);

        return response.Value.Value;
    }

    public async Task<string> SetSecretAsync(
        Guid gameServerId,
        string secretId,
        string secretValue,
        CancellationToken cancellationToken)
    {
        var secretName = GetSecretName(gameServerId, secretId);

        try
        {
            var existing = await secretClient
                .GetSecretAsync(secretName, null, cancellationToken)
                .ConfigureAwait(false);

            if (string.Equals(existing.Value.Value, secretValue, StringComparison.Ordinal))
            {
                return GetRequiredVersion(existing.Value);
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }

        var response = await secretClient
            .SetSecretAsync(secretName, secretValue, cancellationToken)
            .ConfigureAwait(false);

        return GetRequiredVersion(response.Value);
    }

    private static string GetSecretName(Guid gameServerId, string secretId)
    {
        if (!GameServerSecretId.IsValid(secretId))
        {
            throw new ArgumentException("Secret IDs may contain only ASCII letters, digits, and hyphens.", nameof(secretId));
        }

        return $"{gameServerId:D}-{secretId}";
    }

    private static string GetRequiredVersion(KeyVaultSecret secret) =>
        !string.IsNullOrWhiteSpace(secret.Properties.Version)
            ? secret.Properties.Version
            : throw new InvalidOperationException("Azure Key Vault did not return a secret version.");
}
