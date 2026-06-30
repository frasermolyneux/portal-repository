using Microsoft.Extensions.DependencyInjection;
using XtremeIdiots.Portal.Repository.Api.Client.Testing;
using XtremeIdiots.Portal.Repository.Api.Client.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

[Trait("Category", "Unit")]
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFakeRepositoryApiClient_RegistersIRepositoryApiClient()
    {
        var services = new ServiceCollection();

        services.AddFakeRepositoryApiClient();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IRepositoryApiClient>());
    }

    [Fact]
    public void CanResolveVersionedApis()
    {
        var services = new ServiceCollection();
        services.AddFakeRepositoryApiClient();
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IVersionedPlayersApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedGameServersApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedApiHealthApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedApiInfoApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedAdminActionsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedTagsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedReportsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedUserProfileApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedMapRotationsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedDashboardApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedConnectedPlayersApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedGlobalAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedGameAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedServerAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedDashboardAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedMapAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedPlayerAnalyticsV2Api>());
    }

    [Fact]
    public void CanResolveIndividualApis()
    {
        var services = new ServiceCollection();
        services.AddFakeRepositoryApiClient();
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IPlayersApi>());
        Assert.NotNull(provider.GetRequiredService<IGameServersApi>());
        Assert.NotNull(provider.GetRequiredService<IApiHealthApi>());
        Assert.NotNull(provider.GetRequiredService<IApiInfoApi>());
        Assert.NotNull(provider.GetRequiredService<ITagsApi>());
        Assert.NotNull(provider.GetRequiredService<IAdminActionsApi>());
        Assert.NotNull(provider.GetRequiredService<IMapRotationsApi>());
        Assert.NotNull(provider.GetRequiredService<IDashboardApi>());
        Assert.NotNull(provider.GetRequiredService<IConnectedPlayersApi>());
        Assert.NotNull(provider.GetRequiredService<IGlobalAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IGameAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IServerAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IDashboardAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IMapAnalyticsApi>());
        Assert.NotNull(provider.GetRequiredService<IPlayerAnalyticsV2Api>());
    }

    [Fact]
    public async Task ConfigureCallback_IsInvokedAndModifiesFake()
    {
        var services = new ServiceCollection();
        services.AddFakeRepositoryApiClient(client =>
        {
            client.PlayersApi.AddPlayer(
                RepositoryDtoFactory.CreatePlayer(username: "Configured"));
        });

        var provider = services.BuildServiceProvider();
        var apiClient = provider.GetRequiredService<IRepositoryApiClient>();
        var players = await apiClient.Players.V1.GetPlayers(null, null, null, 0, 100, null, default);

        var items = players.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal("Configured", items[0].Username);
    }

    [Fact]
    public void Fake_IsRegisteredAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddFakeRepositoryApiClient();
        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IRepositoryApiClient>();
        var second = provider.GetRequiredService<IRepositoryApiClient>();

        Assert.Same(first, second);
    }

    [Fact]
    public void WithoutConfigure_RegistersWithDefaults()
    {
        var services = new ServiceCollection();

        services.AddFakeRepositoryApiClient();

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IRepositoryApiClient>());
        Assert.NotNull(provider.GetRequiredService<IVersionedPlayersApi>());
    }
}
