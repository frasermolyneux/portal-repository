using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IGameServerConfigurationsApi
    {
        Task<ApiResult<CollectionModel<ConfigurationDto>>> GetConfigurations(Guid gameServerId, CancellationToken cancellationToken = default);
        Task<ApiResult<ConfigurationDto>> GetConfiguration(Guid gameServerId, string ns, CancellationToken cancellationToken = default);
        Task<ApiResult> UpsertConfiguration(Guid gameServerId, string ns, UpsertConfigurationDto dto, CancellationToken cancellationToken = default);
        Task<ApiResult> DeleteConfiguration(Guid gameServerId, string ns, CancellationToken cancellationToken = default);
    }
}
