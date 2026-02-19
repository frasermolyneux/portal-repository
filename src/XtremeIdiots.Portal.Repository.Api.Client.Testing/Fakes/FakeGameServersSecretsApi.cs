using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGameServersSecretsApi : IGameServersSecretsApi
{
    private readonly ConcurrentDictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);

    public FakeGameServersSecretsApi AddSecret(Guid gameServerId, string secretId, string secretValue) { _secrets[$"{gameServerId}:{secretId}"] = secretValue; return this; }
    public FakeGameServersSecretsApi Reset() { _secrets.Clear(); return this; }

    public Task<ApiResult<string>> GetGameServerSecret(Guid gameServerId, string secretId, CancellationToken cancellationToken = default)
    {
        if (_secrets.TryGetValue($"{gameServerId}:{secretId}", out var value))
            return Task.FromResult(new ApiResult<string>(HttpStatusCode.OK, new ApiResponse<string>(value)));
        return Task.FromResult(new ApiResult<string>(HttpStatusCode.NotFound, new ApiResponse<string>(new ApiError("NOT_FOUND", "Secret not found"))));
    }

    public Task<ApiResult<string>> SetGameServerSecret(Guid gameServerId, string secretId, string secretValue, CancellationToken cancellationToken = default)
    {
        _secrets[$"{gameServerId}:{secretId}"] = secretValue;
        return Task.FromResult(new ApiResult<string>(HttpStatusCode.OK, new ApiResponse<string>(secretValue)));
    }
}
