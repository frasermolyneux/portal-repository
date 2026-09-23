using Azure;
using Azure.Security.KeyVault.Secrets;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

internal sealed class KeyVaultGameServerSecretStore(SecretClient secretClient) : IGameServerSecretStore
{
    public async Task<string> GetSecretAsync(Guid gameServerId, string secretId, CancellationToken cancellationToken)
    {
        var response = await secretClient
            .GetSecretAsync(GetSecretName(gameServerId, secretId), null, cancellationToken)
            .ConfigureAwait(false);

        return response.Value.Value;
    }

    public async Task SetSecretAsync(Guid gameServerId, string secretId, string secretValue, CancellationToken cancellationToken)
    {
        var secretName = GetSecretName(gameServerId, secretId);

        try
        {
            var existing = await secretClient
                .GetSecretAsync(secretName, null, cancellationToken)
                .ConfigureAwait(false);

            if (string.Equals(existing.Value.Value, secretValue, StringComparison.Ordinal))
            {
                return;
            }
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }

        await secretClient.SetSecretAsync(secretName, secretValue, cancellationToken).ConfigureAwait(false);
    }

    private static string GetSecretName(Guid gameServerId, string secretId)
    {
        if (string.IsNullOrWhiteSpace(secretId)
            || secretId.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new ArgumentException("Secret IDs may contain only ASCII letters, digits, and hyphens.", nameof(secretId));
        }

        return $"{gameServerId:D}-{secretId}";
    }
}
