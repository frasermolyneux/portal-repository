using System.Net;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class GameServersSecretsControllerTests
{
    private GameServersSecretsController CreateController(PortalDbContext context, IConfiguration? configuration = null)
    {
        configuration ??= new Mock<IConfiguration>().Object;
        return new GameServersSecretsController(context, configuration);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var mockConfig = new Mock<IConfiguration>();
        Assert.Throws<ArgumentNullException>(() => new GameServersSecretsController(null!, mockConfig.Object));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
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

    [Fact(Skip = "Requires Azure Key Vault")]
    public async Task GetGameServerSecret_WithExistingGameServer_RequiresKeyVault()
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

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["gameservers-keyvault-endpoint"]).Returns("https://fake-vault.vault.azure.net/");
        var controller = CreateController(context, mockConfig.Object);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.GetGameServerSecret(gameServerId, "rcon-password");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact(Skip = "Requires Azure Key Vault")]
    public async Task SetGameServerSecret_WithExistingGameServer_RequiresKeyVault()
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

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["gameservers-keyvault-endpoint"]).Returns("https://fake-vault.vault.azure.net/");
        var controller = CreateController(context, mockConfig.Object);
        var api = (IGameServersSecretsApi)controller;

        var result = await api.SetGameServerSecret(gameServerId, "rcon-password", "secret-value");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
