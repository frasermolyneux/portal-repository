using System.Net;

using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Configurations;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.Api.V1.Validation;
using XtremeIdiots.Portal.Repository.DataLib;

using ServerListContractConstants = XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.ServerList.ServerListSettingsConstants;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// Default (uncached) implementation of <see cref="IConfigurationReadService"/>. Behaviour
    /// mirrors the pre-refactor <c>GameServerConfigurationsController.GetConfiguration</c> and
    /// <c>GlobalConfigurationsController.GetConfiguration</c> paths, including the canonical/
    /// legacy server-list namespace fallback.
    /// </summary>
    public sealed class ConfigurationReadService : IConfigurationReadService
    {
        private const string LegacyServerListNamespace = "serverList";
        private static readonly string CanonicalServerListNamespaceLower = ServerListContractConstants.Namespace.ToLowerInvariant();
        private static readonly string LegacyServerListNamespaceLower = LegacyServerListNamespace.ToLowerInvariant();

        private readonly PortalDbContext context;

        public ConfigurationReadService(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        public async Task<ApiResult<ConfigurationDto>> GetServerConfigurationAsync(Guid gameServerId, string ns, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            {
                return new ApiResult<ConfigurationDto>(HttpStatusCode.BadRequest);
            }

            var config = await context.GameServerConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.GameServerId == gameServerId && c.Namespace == ns, cancellationToken)
                .ConfigureAwait(false);

            if (config == null)
            {
                return new ApiResult<ConfigurationDto>(HttpStatusCode.NotFound);
            }

            return new ApiResponse<ConfigurationDto>(config.ToDto()).ToApiResult();
        }

        public async Task<ApiResult<ConfigurationDto>> GetGlobalConfigurationAsync(string ns, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(ns) || ns.Length > 128)
            {
                return new ApiResult<ConfigurationDto>(HttpStatusCode.BadRequest);
            }

            ns = NamespaceSchemaValidationRegistry.NormalizeNamespace(ns);

            var isServerListNamespace = string.Equals(ns, ServerListContractConstants.Namespace, StringComparison.OrdinalIgnoreCase);

            GlobalConfiguration? config;
            if (isServerListNamespace)
            {
                var configs = await context.GlobalConfigurations
                    .AsNoTracking()
                    .Where(c =>
                        c.Namespace.ToLower() == CanonicalServerListNamespaceLower ||
                        c.Namespace.ToLower() == LegacyServerListNamespaceLower)
                    .ToListAsync(cancellationToken).ConfigureAwait(false);

                config = configs.FirstOrDefault(c => string.Equals(c.Namespace, ServerListContractConstants.Namespace, StringComparison.Ordinal))
                    ?? configs.FirstOrDefault(c => string.Equals(c.Namespace, LegacyServerListNamespace, StringComparison.Ordinal))
                    ?? configs
                        .OrderByDescending(c => c.LastModifiedUtc)
                        .ThenBy(c => c.Namespace, StringComparer.Ordinal)
                        .FirstOrDefault();
            }
            else
            {
                config = await context.GlobalConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Namespace == ns, cancellationToken).ConfigureAwait(false);
            }

            if (config == null)
            {
                return new ApiResult<ConfigurationDto>(HttpStatusCode.NotFound);
            }

            return new ApiResponse<ConfigurationDto>(config.ToDto()).ToApiResult();
        }
    }
}
