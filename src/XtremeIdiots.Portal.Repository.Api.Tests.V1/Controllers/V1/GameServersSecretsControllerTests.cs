using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.Services.Secrets;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class GameServersSecretsControllerTests
{
    private GameServersSecretsController CreateController(
        PortalDbContext context,
        InMemoryGameServerSecretStore? secretStore = null)
    {
        return new GameServersSecretsController(context, secretStore ?? new InMemoryGameServerSecretStore());
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new GameServersSecretsController(null!, new InMemoryGameServerSecretStore()));
    }

    [Fact]
    public void Constructor_WithNullSecretStore_ThrowsArgumentNullException()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        Assert.Throws<ArgumentNullException>(() => new GameServersSecretsController(context, null!));
    }

    [Fact]
    public async Task GetGameServerSecret_WithEmptySecretId_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.GetGameServerSecret(Guid.NewGuid(), "");

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetGameServerSecret_WithWhitespaceSecretId_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.GetGameServerSecret(Guid.NewGuid(), "   ");

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task GetGameServerSecret_WithNonExistentGameServer_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.GetGameServerSecret(Guid.NewGuid(), "rcon-password");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task SetGameServerSecret_WithEmptySecretId_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.SetGameServerSecret(Guid.NewGuid(), "", "secret-value");

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task SetGameServerSecret_WithWhitespaceSecretId_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.SetGameServerSecret(Guid.NewGuid(), "   ", "secret-value");

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task SetGameServerSecret_WithNonExistentGameServer_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.SetGameServerSecret(Guid.NewGuid(), "rcon-password", "secret-value");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetGameServerSecret_WithExistingGameServer_ReturnsStoredSecret()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Test Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960
        });
        await context.SaveChangesAsync();

        var secretStore = new InMemoryGameServerSecretStore();
        secretStore.Seed(gameServerId, "file-transport-password", "secret-value");
        var controller = CreateController(context, secretStore);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.GetGameServerSecret(gameServerId, "file-transport-password");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("secret-value", result.Result!.Data);
    }

    [Fact]
    public async Task SetGameServerSecret_WithExistingGameServer_StoresSecret()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gameServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = gameServerId,
            Title = "Test Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960
        });
        await context.SaveChangesAsync();

        var secretStore = new InMemoryGameServerSecretStore();
        var controller = CreateController(context, secretStore);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.SetGameServerSecret(gameServerId, "file-transport-password", "secret-value");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("secret-value", secretStore.Get(gameServerId, "file-transport-password"));
    }

    private sealed class InMemoryGameServerSecretStore : IGameServerSecretStore
    {
        private readonly Dictionary<(Guid GameServerId, string SecretId), string> secrets = [];

        public Task<string> GetSecretAsync(Guid gameServerId, string secretId, CancellationToken cancellationToken) =>
            Task.FromResult(secrets[(gameServerId, secretId)]);

        public Task SetSecretAsync(Guid gameServerId, string secretId, string secretValue, CancellationToken cancellationToken)
        {
            secrets[(gameServerId, secretId)] = secretValue;
            return Task.CompletedTask;
        }

        public void Seed(Guid gameServerId, string secretId, string value) =>
            secrets[(gameServerId, secretId)] = value;

        public string Get(Guid gameServerId, string secretId) => secrets[(gameServerId, secretId)];
    }
}
