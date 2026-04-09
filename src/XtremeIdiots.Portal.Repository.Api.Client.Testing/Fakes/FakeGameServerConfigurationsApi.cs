using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGameServerConfigurationsApi : IGameServerConfigurationsApi
{
    private readonly ConcurrentDictionary<(Guid, string), ConfigurationDto> _configurations = new();

    public FakeGameServerConfigurationsApi AddConfiguration(Guid gameServerId, string ns, ConfigurationDto config) { _configurations[(gameServerId, ns)] = config; return this; }
    public FakeGameServerConfigurationsApi Reset() { _configurations.Clear(); return this; }

    public Task<ApiResult<CollectionModel<ConfigurationDto>>> GetConfigurations(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var list = _configurations.Where(kv => kv.Key.Item1 == gameServerId).Select(kv => kv.Value).ToList();
        var collection = new CollectionModel<ConfigurationDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<ConfigurationDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<ConfigurationDto>>(collection)));
    }

    public Task<ApiResult<ConfigurationDto>> GetConfiguration(Guid gameServerId, string ns, CancellationToken cancellationToken = default)
    {
        if (_configurations.TryGetValue((gameServerId, ns), out var config))
            return Task.FromResult(new ApiResult<ConfigurationDto>(HttpStatusCode.OK, new ApiResponse<ConfigurationDto>(config)));
        return Task.FromResult(new ApiResult<ConfigurationDto>(HttpStatusCode.NotFound));
    }

    public Task<ApiResult> UpsertConfiguration(Guid gameServerId, string ns, UpsertConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        _configurations[(gameServerId, ns)] = new ConfigurationDto { Namespace = ns, Configuration = dto.Configuration, LastModifiedUtc = DateTime.UtcNow };
        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }

    public Task<ApiResult> DeleteConfiguration(Guid gameServerId, string ns, CancellationToken cancellationToken = default)
    {
        if (_configurations.TryRemove((gameServerId, ns), out _))
            return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
        return Task.FromResult(new ApiResult(HttpStatusCode.NotFound));
    }
}
