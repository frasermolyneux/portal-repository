using Microsoft.Extensions.DependencyInjection;
using MX.Api.Client.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;
using XtremeIdiots.Portal.Repository.Api.Client.V2.Caching;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the Repository API client services with custom configuration
        /// </summary>
        /// <param name="serviceCollection">The service collection</param>
        /// <param name="configureOptions">Action to configure the Repository API options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddRepositoryApiClient(
            this IServiceCollection serviceCollection,
            Action<RepositoryApiOptionsBuilder> configureOptions)
        {
            // Register library default cache policies per sub-API interface. Consumers opt in
            // per client with UseLibraryDefaults() on their CacheBuilder.
            serviceCollection.AddDefaultCachePolicies<IApiInfoApi>(RepositoryApiCacheDefaults.ConfigureApiInfo);
            serviceCollection.AddDefaultCachePolicies<IApiHealthApi>(RepositoryApiCacheDefaults.ConfigureApiHealth);

            // Register API info endpoint
            serviceCollection.AddTypedApiClient<IApiInfoApi, ApiInfoApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);

            // Register API health endpoint
            serviceCollection.AddTypedApiClient<IApiHealthApi, ApiHealthApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);

            // Register version selectors as scoped
            serviceCollection.AddScoped<IVersionedApiHealthApi, VersionedApiHealthApi>();
            serviceCollection.AddScoped<IVersionedApiInfoApi, VersionedApiInfoApi>();

            // Register the unified client as scoped
            serviceCollection.AddScoped<IRepositoryApiClient, RepositoryApiClient>();

            return serviceCollection;
        }
    }
}
