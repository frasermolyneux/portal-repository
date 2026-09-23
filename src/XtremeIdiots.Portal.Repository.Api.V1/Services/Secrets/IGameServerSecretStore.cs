namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

public interface IGameServerSecretStore
{
    Task<string> GetSecretAsync(
        Guid gameServerId,
        string secretId,
        string? secretVersion,
        CancellationToken cancellationToken);

    Task<string> SetSecretAsync(
        Guid gameServerId,
        string secretId,
        string secretValue,
        CancellationToken cancellationToken);
}
