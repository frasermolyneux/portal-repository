using System.Net;
using System.Text;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class AdminActionsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminActionsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetAdminActions_ReturnsOk()
    {
        var response = await _client.GetAsync("/v1.0/admin-actions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAdminAction_ReturnsOk_WhenExists()
    {
        var adminActionId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        _factory.SeedDatabase(ctx =>
        {
            ctx.Players.Add(new Player
            {
                PlayerId = playerId,
                GameType = (int)GameType.CallOfDuty4,
                Username = "AdminActionPlayer",
                Guid = $"aa-guid-{Guid.NewGuid()}",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });
            ctx.AdminActions.Add(new AdminAction
            {
                AdminActionId = adminActionId,
                PlayerId = playerId,
                Type = (int)AdminActionType.Warning,
                Text = "Test warning",
                Created = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync($"/v1.0/admin-actions/{adminActionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test warning", content);
    }

    [Fact]
    public async Task GetAdminAction_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync($"/v1.0/admin-actions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAdminAction_ReturnsCreated()
    {
        var playerId = Guid.NewGuid();

        _factory.SeedDatabase(ctx =>
        {
            ctx.Players.Add(new Player
            {
                PlayerId = playerId,
                GameType = (int)GameType.CallOfDuty4,
                Username = "CreateAAPlayer",
                Guid = $"create-aa-guid-{Guid.NewGuid()}",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var createDto = new CreateAdminActionDto(playerId, AdminActionType.Ban, "Permanent ban for cheating");

        var json = JsonConvert.SerializeObject(createDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/admin-actions", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAdminAction_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.DeleteAsync($"/v1.0/admin-actions/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAdminAction_ReturnsOk_WhenExists()
    {
        var adminActionId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        _factory.SeedDatabase(ctx =>
        {
            ctx.Players.Add(new Player
            {
                PlayerId = playerId,
                GameType = (int)GameType.CallOfDuty2,
                Username = "DeleteAAPlayer",
                Guid = $"delete-aa-guid-{Guid.NewGuid()}",
                FirstSeen = DateTime.UtcNow,
                LastSeen = DateTime.UtcNow
            });
            ctx.AdminActions.Add(new AdminAction
            {
                AdminActionId = adminActionId,
                PlayerId = playerId,
                Type = (int)AdminActionType.Kick,
                Text = "Kicked for spam",
                Created = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.DeleteAsync($"/v1.0/admin-actions/{adminActionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
