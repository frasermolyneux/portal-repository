using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using XtremeIdiots.Portal.Repository.Api.V1.Services.Caching;
using XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services
{
    /// <summary>
    /// DI wire-up for repository-side service seams and their caching decorators.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the repository-side read services and, when <paramref name="enableCaching"/>
        /// is true, their <see cref="MX.Caching.Abstractions.IMxCache"/>-backed decorators.
        /// The <see cref="RepositoryCacheMetrics"/> singleton is always registered so the
        /// invalidator and decorators can share a single <see cref="System.Diagnostics.Metrics.Meter"/>.
        /// </summary>
        public static IServiceCollection AddRepositoryReadServices(this IServiceCollection services, bool enableCaching)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<RepositoryCacheMetrics>();
            services.TryAddSingleton<IGameServerSecretStore>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var endpoint = configuration["GameServerCredentials:KeyVaultEndpoint"];
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    return new UnavailableGameServerSecretStore();
                }

                var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = configuration["AzureAppConfiguration:ManagedIdentityClientId"]
                });

                return new KeyVaultGameServerSecretStore(new SecretClient(new Uri(endpoint), credential));
            });
            services.TryAddSingleton<IGameServerConfigurationSecretProtector, GameServerConfigurationSecretProtector>();

            if (enableCaching)
            {
                // Concrete uncached implementations remain scoped (DbContext lifetime).
                services.AddScoped<GameServerReadService>();
                services.AddScoped<DashboardService>();
                services.AddScoped<ConfigurationReadService>();
                services.AddScoped<MapReadService>();
                services.AddScoped(sp =>
                    new CachingConfigurationReadService(
                        sp.GetRequiredService<ConfigurationReadService>(),
                        sp.GetRequiredService<MX.Caching.Abstractions.IMxCache>(),
                        sp.GetRequiredService<RepositoryCacheMetrics>(),
                        sp.GetRequiredService<ILogger<CachingConfigurationReadService>>()));

                // Public seams resolve to the caching decorator, which delegates into the
                // concrete implementations above on miss.
                services.AddScoped<IGameServerReadService>(sp =>
                    new CachingGameServerReadService(
                        sp.GetRequiredService<GameServerReadService>(),
                        sp.GetRequiredService<MX.Caching.Abstractions.IMxCache>(),
                        sp.GetRequiredService<RepositoryCacheMetrics>(),
                        sp.GetRequiredService<ILogger<CachingGameServerReadService>>()));

                services.AddScoped<IDashboardService>(sp =>
                    new CachingDashboardService(
                        sp.GetRequiredService<DashboardService>(),
                        sp.GetRequiredService<MX.Caching.Abstractions.IMxCache>(),
                        sp.GetRequiredService<RepositoryCacheMetrics>(),
                        sp.GetRequiredService<ILogger<CachingDashboardService>>()));

                services.AddScoped<IConfigurationReadService>(sp =>
                    new SecretResolvingConfigurationReadService(
                        sp.GetRequiredService<CachingConfigurationReadService>(),
                        sp.GetRequiredService<IGameServerConfigurationSecretProtector>()));

                services.AddScoped<IMapReadService>(sp =>
                    new CachingMapReadService(
                        sp.GetRequiredService<MapReadService>(),
                        sp.GetRequiredService<MX.Caching.Abstractions.IMxCache>(),
                        sp.GetRequiredService<RepositoryCacheMetrics>(),
                        sp.GetRequiredService<ILogger<CachingMapReadService>>()));

                services.AddScoped<IRepositoryCacheInvalidator>(sp =>
                    new RepositoryCacheInvalidator(
                        sp.GetRequiredService<MX.Caching.Abstractions.IMxCache>(),
                        sp.GetRequiredService<RepositoryCacheMetrics>(),
                        sp.GetRequiredService<ILogger<RepositoryCacheInvalidator>>()));
            }
            else
            {
                services.AddScoped<IGameServerReadService, GameServerReadService>();
                services.AddScoped<IDashboardService, DashboardService>();
                services.AddScoped<ConfigurationReadService>();
                services.AddScoped<IConfigurationReadService>(sp =>
                    new SecretResolvingConfigurationReadService(
                        sp.GetRequiredService<ConfigurationReadService>(),
                        sp.GetRequiredService<IGameServerConfigurationSecretProtector>()));
                services.AddScoped<IMapReadService, MapReadService>();
                services.AddScoped<IRepositoryCacheInvalidator, NoOpRepositoryCacheInvalidator>();
            }

            return services;
        }
    }
}
