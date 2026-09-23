namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

public interface IGameServerConfigurationSecretProtector
{
    Task<string> ResolveSecretsAsync(
        Guid gameServerId,
        string configurationNamespace,
        string configuration,
        CancellationToken cancellationToken);

    Task<string> ExternalizeSecretsAsync(
        Guid gameServerId,
        string configurationNamespace,
        string configuration,
        CancellationToken cancellationToken);
}
