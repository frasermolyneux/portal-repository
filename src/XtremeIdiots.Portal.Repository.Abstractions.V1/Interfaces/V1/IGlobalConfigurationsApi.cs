using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1
{
    public interface IGlobalConfigurationsApi
    {
        Task<ApiResult<CollectionModel<ConfigurationDto>>> GetConfigurations(CancellationToken cancellationToken = default);
        Task<ApiResult<ConfigurationDto>> GetConfiguration(string ns, CancellationToken cancellationToken = default);
        Task<ApiResult> UpsertConfiguration(string ns, UpsertConfigurationDto dto, CancellationToken cancellationToken = default);
        Task<ApiResult> DeleteConfiguration(string ns, CancellationToken cancellationToken = default);
    }
}
