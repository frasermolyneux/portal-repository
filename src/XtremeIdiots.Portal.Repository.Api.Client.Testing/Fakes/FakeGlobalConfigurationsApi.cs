using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeGlobalConfigurationsApi : IGlobalConfigurationsApi
{
    private readonly ConcurrentDictionary<string, ConfigurationDto> _configurations = new();

    public FakeGlobalConfigurationsApi AddConfiguration(string ns, ConfigurationDto config) { _configurations[ns] = config; return this; }
    public FakeGlobalConfigurationsApi Reset() { _configurations.Clear(); return this; }

    public Task<ApiResult<CollectionModel<ConfigurationDto>>> GetConfigurations(CancellationToken cancellationToken = default)
    {
        var list = _configurations.Values.ToList();
        var collection = new CollectionModel<ConfigurationDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<ConfigurationDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<ConfigurationDto>>(collection)));
    }

    public Task<ApiResult<ConfigurationDto>> GetConfiguration(string ns, CancellationToken cancellationToken = default)
    {
        if (_configurations.TryGetValue(ns, out var config))
            return Task.FromResult(new ApiResult<ConfigurationDto>(HttpStatusCode.OK, new ApiResponse<ConfigurationDto>(config)));
        return Task.FromResult(new ApiResult<ConfigurationDto>(HttpStatusCode.NotFound));
    }

    public Task<ApiResult> UpsertConfiguration(string ns, UpsertConfigurationDto dto, CancellationToken cancellationToken = default)
    {
        _configurations[ns] = new ConfigurationDto { Namespace = ns, Configuration = dto.Configuration, LastModifiedUtc = DateTime.UtcNow };
        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }

    public Task<ApiResult> DeleteConfiguration(string ns, CancellationToken cancellationToken = default)
    {
        if (_configurations.TryRemove(ns, out _))
            return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
        return Task.FromResult(new ApiResult(HttpStatusCode.NotFound));
    }
}
