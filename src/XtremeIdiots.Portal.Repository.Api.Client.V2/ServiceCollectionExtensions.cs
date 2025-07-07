using Microsoft.Extensions.DependencyInjection;

using MxIO.ApiClient.Extensions;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.V2
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the repository API client V2 with version selectors.
        /// This provides access to V2 APIs like: client.Root.V2
        /// </summary>
        public static void AddRepositoryApiClientV2(this IServiceCollection serviceCollection, Action<RepositoryApiClientOptions> configure)
        {
            serviceCollection.AddApiClient();
            serviceCollection.Configure(configure);

            // Register V2 API implementations as scoped to match IRestClientService lifetime
            serviceCollection.AddScoped<RootApi>();

            // Register V2 API interfaces using concrete implementations as scoped
            serviceCollection.AddScoped<IRootApi>(provider => provider.GetRequiredService<RootApi>());

            // Register version selectors as scoped to match V2 API lifetime
            serviceCollection.AddScoped<IVersionedRootApi, VersionedRootApi>();

            // Register the unified client as scoped to match versioned API lifetime
            serviceCollection.AddScoped<IRepositoryApiClient, RepositoryApiClient>();
        }
    }
}
