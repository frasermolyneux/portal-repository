using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Repository-side seam for reading resolved configuration documents. Extracted so the
    /// per-server and global reads can be layered with caching decorators; mutation-side
    /// controllers still write directly to <see cref="DataLib.PortalDbContext"/> and evict
    /// via <see cref="Caching.IRepositoryCacheInvalidator"/>.
    /// </summary>
    public interface IConfigurationReadService
    {
        /// <summary>
        /// Returns the per-server configuration document for <paramref name="ns"/>, or
        /// <c>NotFound</c> when no row exists. Also returns <c>NotFound</c> when the game
        /// server itself does not exist.
        /// </summary>
        Task<ApiResult<ConfigurationDto>> GetServerConfigurationAsync(Guid gameServerId, string ns, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the global configuration document for <paramref name="ns"/> preserving the
        /// canonical/legacy server-list namespace compatibility behaviour.
        /// </summary>
        Task<ApiResult<ConfigurationDto>> GetGlobalConfigurationAsync(string ns, CancellationToken cancellationToken);

        /// <summary>
        /// Returns all per-server configuration documents for a single game server.
        /// Returns <c>NotFound</c> when the game server does not exist.
        /// </summary>
        Task<ApiResult<CollectionModel<ConfigurationDto>>> GetServerConfigurationsAsync(Guid gameServerId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns all global configuration documents.
        /// </summary>
        Task<ApiResult<CollectionModel<ConfigurationDto>>> GetGlobalConfigurationsAsync(CancellationToken cancellationToken);
    }
}
