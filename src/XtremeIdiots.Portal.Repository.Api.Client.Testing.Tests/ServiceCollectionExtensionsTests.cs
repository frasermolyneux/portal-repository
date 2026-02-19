using Microsoft.Extensions.DependencyInjection;
using XtremeIdiots.Portal.Repository.Api.Client.Testing;
using XtremeIdiots.Portal.Repository.Api.Client.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

using V1_1Interfaces = XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1_1;

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
        Assert.NotNull(provider.GetRequiredService<IVersionedRootApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedAdminActionsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedTagsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedReportsApi>());
        Assert.NotNull(provider.GetRequiredService<IVersionedUserProfileApi>());
    }

    [Fact]
    public void CanResolveIndividualApis()
    {
        var services = new ServiceCollection();
        services.AddFakeRepositoryApiClient();
        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IPlayersApi>());
        Assert.NotNull(provider.GetRequiredService<IGameServersApi>());
        Assert.NotNull(provider.GetRequiredService<IRootApi>());
        Assert.NotNull(provider.GetRequiredService<V1_1Interfaces.IRootApi>());
        Assert.NotNull(provider.GetRequiredService<ITagsApi>());
        Assert.NotNull(provider.GetRequiredService<IAdminActionsApi>());
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
