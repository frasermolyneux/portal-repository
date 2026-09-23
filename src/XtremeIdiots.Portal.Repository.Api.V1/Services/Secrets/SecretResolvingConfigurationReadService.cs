using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

internal sealed class SecretResolvingConfigurationReadService(
    IConfigurationReadService inner,
    IGameServerConfigurationSecretProtector secretProtector) : IConfigurationReadService
{
    public async Task<ApiResult<ConfigurationDto>> GetServerConfigurationAsync(
        Guid gameServerId,
        string ns,
        CancellationToken cancellationToken)
    {
        var result = await inner.GetServerConfigurationAsync(gameServerId, ns, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Result?.Data is null)
        {
            return result;
        }

        var configuration = result.Result.Data;
        var resolved = new ConfigurationDto
        {
            Namespace = configuration.Namespace,
            Configuration = await secretProtector
                .ResolveSecretsAsync(gameServerId, configuration.Namespace, configuration.Configuration, cancellationToken)
                .ConfigureAwait(false),
            LastModifiedUtc = configuration.LastModifiedUtc
        };

        return new ApiResponse<ConfigurationDto>(resolved).ToApiResult();
    }

    public Task<ApiResult<ConfigurationDto>> GetGlobalConfigurationAsync(
        string ns,
        CancellationToken cancellationToken) =>
        inner.GetGlobalConfigurationAsync(ns, cancellationToken);

    public async Task<ApiResult<CollectionModel<ConfigurationDto>>> GetServerConfigurationsAsync(
        Guid gameServerId,
        CancellationToken cancellationToken)
    {
        var result = await inner.GetServerConfigurationsAsync(gameServerId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Result?.Data?.Items is null)
        {
            return result;
        }

        var resolvedConfigurations = new List<ConfigurationDto>();
        foreach (var configuration in result.Result.Data.Items)
        {
            resolvedConfigurations.Add(new ConfigurationDto
            {
                Namespace = configuration.Namespace,
                Configuration = await secretProtector
                    .ResolveSecretsAsync(gameServerId, configuration.Namespace, configuration.Configuration, cancellationToken)
                    .ConfigureAwait(false),
                LastModifiedUtc = configuration.LastModifiedUtc
            });
        }

        return new ApiResponse<CollectionModel<ConfigurationDto>>(
            new CollectionModel<ConfigurationDto>(resolvedConfigurations)).ToApiResult();
    }

    public Task<ApiResult<CollectionModel<ConfigurationDto>>> GetGlobalConfigurationsAsync(
        CancellationToken cancellationToken) =>
        inner.GetGlobalConfigurationsAsync(cancellationToken);
}
