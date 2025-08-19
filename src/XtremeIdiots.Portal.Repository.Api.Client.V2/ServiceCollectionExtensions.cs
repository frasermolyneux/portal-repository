using Microsoft.Extensions.DependencyInjection;
using MX.Api.Client.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

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
            // Register V2 API implementations using the new typed pattern
            serviceCollection.AddTypedApiClient<IRootApi, RootApi, RepositoryApiClientOptions, RepositoryApiOptionsBuilder>(configureOptions);

            // Register version selectors as scoped
            serviceCollection.AddScoped<IVersionedRootApi, VersionedRootApi>();

            // Register the unified client as scoped
            serviceCollection.AddScoped<IRepositoryApiClient, RepositoryApiClient>();

            return serviceCollection;
        }
    }
}
