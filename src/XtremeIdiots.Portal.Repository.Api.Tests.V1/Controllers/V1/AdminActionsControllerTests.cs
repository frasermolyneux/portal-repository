using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.AdminActions;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class AdminActionsControllerTests
{
    private AdminActionsController CreateController(PortalDbContext context)
    {
        return new AdminActionsController(context);
    }

    [Fact]
    public async Task GetAdminAction_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var adminActionId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.AdminActions.Add(new AdminAction
        {
            AdminActionId = adminActionId,
            PlayerId = playerId,
            Type = (int)AdminActionType.Warning,
            Text = "Test warning",
            Created = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;
        var result = await api.GetAdminAction(adminActionId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetAdminAction_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;
        var result = await api.GetAdminAction(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetAdminActions_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.AdminActions.Add(new AdminAction
        {
            AdminActionId = Guid.NewGuid(),
            PlayerId = playerId,
            Type = (int)AdminActionType.Warning,
            Text = "Warning 1",
            Created = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;
        var result = await api.GetAdminActions(null, null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateAdminAction_CreatesEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;

        var dto = new CreateAdminActionDto(playerId, AdminActionType.Warning, "Test warning");

        var result = await api.CreateAdminAction(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.AdminActions);
    }

    [Fact]
    public async Task UpdateAdminAction_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var adminActionId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.AdminActions.Add(new AdminAction
        {
            AdminActionId = adminActionId,
            PlayerId = playerId,
            Type = (int)AdminActionType.Warning,
            Text = "Original",
            Created = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;

        var editDto = new EditAdminActionDto(adminActionId)
        {
            Text = "Updated text"
        };

        var result = await api.UpdateAdminAction(editDto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task UpdateAdminAction_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;

        var editDto = new EditAdminActionDto(Guid.NewGuid())
        {
            Text = "Updated"
        };

        var result = await api.UpdateAdminAction(editDto);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task DeleteAdminAction_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var adminActionId = Guid.NewGuid();
        context.Players.Add(new Player
        {
            PlayerId = playerId,
            GameType = (int)GameType.CallOfDuty4,
            Username = "TestPlayer",
            FirstSeen = DateTime.UtcNow.AddDays(-10),
            LastSeen = DateTime.UtcNow
        });
        context.AdminActions.Add(new AdminAction
        {
            AdminActionId = adminActionId,
            PlayerId = playerId,
            Type = (int)AdminActionType.Warning,
            Text = "To delete",
            Created = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;
        var result = await api.DeleteAdminAction(adminActionId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(context.AdminActions);
    }

    [Fact]
    public async Task DeleteAdminAction_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;
        var result = await api.DeleteAdminAction(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
