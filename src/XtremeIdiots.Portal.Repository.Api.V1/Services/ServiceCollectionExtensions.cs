using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using XtremeIdiots.Portal.Repository.Api.V1.Services.Caching;

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

            if (enableCaching)
            {
                // Concrete uncached implementations remain scoped (DbContext lifetime).
                services.AddScoped<GameServerReadService>();
                services.AddScoped<DashboardService>();
                services.AddScoped<ConfigurationReadService>();

                // Public seams resolve to the caching decorator, which delegates into the
                // concrete implementations above on miss.
                services.AddScoped<IGameServerReadService>(sp =>
                    new CachingGameServerReadService(
                        sp.GetRequiredService<GameServerReadService>(),
                        sp.GetRequiredService<MX.Caching.Abstractions.IMxCache>(),
                        sp.GetRequiredService<RepositoryCacheMetrics>()));

                services.AddScoped<IDashboardService>(sp =>
                    new CachingDashboardService(
                        sp.GetRequiredService<DashboardService>(),
                        sp.GetRequiredService<MX.Caching.Abstractions.IMxCache>(),
                        sp.GetRequiredService<RepositoryCacheMetrics>()));

                services.AddScoped<IConfigurationReadService>(sp =>
                    new CachingConfigurationReadService(
                        sp.GetRequiredService<ConfigurationReadService>(),
                        sp.GetRequiredService<MX.Caching.Abstractions.IMxCache>(),
                        sp.GetRequiredService<RepositoryCacheMetrics>()));

                services.AddScoped<IRepositoryCacheInvalidator, RepositoryCacheInvalidator>();
            }
            else
            {
                services.AddScoped<IGameServerReadService, GameServerReadService>();
                services.AddScoped<IDashboardService, DashboardService>();
                services.AddScoped<IConfigurationReadService, ConfigurationReadService>();
                services.AddScoped<IRepositoryCacheInvalidator, NoOpRepositoryCacheInvalidator>();
            }

            return services;
        }
    }
}
