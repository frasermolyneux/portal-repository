using MX.Api.Abstractions;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IGameServersSecretsApi
{
    Task<ApiResult<string>> GetGameServerSecret(Guid gameServerId, string secretId, CancellationToken cancellationToken = default);
    Task<ApiResult<string>> SetGameServerSecret(Guid gameServerId, string secretId, string secretValue, CancellationToken cancellationToken = default);
}
