namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

internal sealed class UnavailableGameServerSecretStore : IGameServerSecretStore
{
    private const string ErrorMessage = "GameServerCredentials:KeyVaultEndpoint is not configured.";

    public Task<string> GetSecretAsync(Guid gameServerId, string secretId, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(ErrorMessage);
    }

    public Task SetSecretAsync(Guid gameServerId, string secretId, string secretValue, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(ErrorMessage);
    }
}
