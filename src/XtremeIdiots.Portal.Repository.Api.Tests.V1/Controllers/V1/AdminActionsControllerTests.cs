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
    public async Task EnsureAutomatedAction_FirstObservation_CreatesAction()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;

        var result = await api.EnsureAutomatedAction(CreateAutomationRequest(playerId, AdminActionType.Observation));

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.True(result.Result?.Data?.Created);
        var action = Assert.Single(context.AdminActions);
        Assert.Equal((byte)ActionSource.Automation, action.Source);
        Assert.Equal((int)AutomationFeature.VpnProtection, action.AutomationFeature);
        Assert.Equal("vpn", action.AutomationRuleId);
    }

    [Fact]
    public async Task EnsureAutomatedAction_RepeatedObservation_ReturnsExistingAction()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;

        var first = await api.EnsureAutomatedAction(CreateAutomationRequest(playerId, AdminActionType.Observation));
        var second = await api.EnsureAutomatedAction(CreateAutomationRequest(playerId, AdminActionType.Observation));

        Assert.True(first.Result?.Data?.Created);
        Assert.False(second.Result?.Data?.Created);
        Assert.Single(context.AdminActions);
        Assert.Equal(first.Result?.Data?.AdminAction.AdminActionId, second.Result?.Data?.AdminAction.AdminActionId);
    }

    [Fact]
    public async Task EnsureAutomatedAction_StrongerAction_CreatesAdditionalAction()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;

        await api.EnsureAutomatedAction(CreateAutomationRequest(playerId, AdminActionType.Observation));
        var result = await api.EnsureAutomatedAction(CreateAutomationRequest(playerId, AdminActionType.Ban));

        Assert.True(result.Result?.Data?.Created);
        Assert.Equal(2, context.AdminActions.Count());
        Assert.True(context.AdminActions.Single(action => action.Type == (int)AdminActionType.Ban).AutomationIsActive);
    }

    [Fact]
    public async Task EnsureAutomatedAction_LiftedBan_CreatesNewBanCycle()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;

        var first = await api.EnsureAutomatedAction(CreateAutomationRequest(playerId, AdminActionType.Ban));
        await api.UpdateAdminAction(new EditAdminActionDto(first.Result!.Data!.AdminAction.AdminActionId)
        {
            Expires = DateTime.UtcNow
        });

        var second = await api.EnsureAutomatedAction(CreateAutomationRequest(playerId, AdminActionType.Ban));

        Assert.True(second.Result?.Data?.Created);
        Assert.Equal(2, context.AdminActions.Count(action => action.Type == (int)AdminActionType.Ban));
        Assert.Single(context.AdminActions.Where(action => action.AutomationIsActive));
    }

    [Fact]
    public async Task EnsureAutomatedAction_RepeatedRconBanImport_ReusesAction()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;
        var request = new EnsureAutomatedActionDto(
            playerId,
            AdminActionType.Ban,
            "VPN Protection: matched rule 'proxycheck-risk-score-dangerous'",
            AutomationFeature.RconBanImport,
            "cod4x:server:canonical-puid:permanent");

        var first = await api.EnsureAutomatedAction(request);
        var second = await api.EnsureAutomatedAction(request);

        Assert.True(first.Result?.Data?.Created);
        Assert.False(second.Result?.Data?.Created);
        Assert.Single(context.AdminActions);
        Assert.Equal((int)AutomationFeature.RconBanImport, context.AdminActions.Single().AutomationFeature);
    }

    [Fact]
    public async Task ClaimForumTopicPublication_RepeatedClaimRequiresManualRecovery()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        var action = AddAutomatedRconImportAction(context, playerId);
        await context.SaveChangesAsync();
        var api = (IAdminActionsApi)CreateController(context);

        var first = await api.ClaimForumTopicPublication(action.AdminActionId);
        var second = await api.ClaimForumTopicPublication(action.AdminActionId);

        Assert.True(first.IsSuccess);
        Assert.NotNull(first.Result?.Data?.ClaimId);
        Assert.False(first.Result?.Data?.RequiresManualRecovery);
        Assert.True(second.IsSuccess);
        Assert.Null(second.Result?.Data?.ClaimId);
        Assert.True(second.Result?.Data?.RequiresManualRecovery);
        Assert.Equal(first.Result?.Data?.ClaimId, context.AdminActions.Single().ForumTopicPublicationClaimId);
    }

    [Fact]
    public async Task CompleteForumTopicPublication_ValidClaimLinksTopicAndMakesFutureClaimsNoOp()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        var action = AddAutomatedRconImportAction(context, playerId);
        await context.SaveChangesAsync();
        var api = (IAdminActionsApi)CreateController(context);
        var claim = await api.ClaimForumTopicPublication(action.AdminActionId);
        var claimId = Assert.IsType<Guid>(claim.Result?.Data?.ClaimId);

        var complete = await api.CompleteForumTopicPublication(action.AdminActionId, new CompleteForumTopicPublicationDto(claimId, 12345));
        var repeatedClaim = await api.ClaimForumTopicPublication(action.AdminActionId);

        Assert.True(complete.IsSuccess);
        Assert.Equal(12345, context.AdminActions.Single().ForumTopicId);
        Assert.True(repeatedClaim.IsSuccess);
        Assert.Equal(12345, repeatedClaim.Result?.Data?.ForumTopicId);
        Assert.Null(repeatedClaim.Result?.Data?.ClaimId);
        Assert.False(repeatedClaim.Result?.Data?.RequiresManualRecovery);
    }

    [Fact]
    public async Task CompleteForumTopicPublication_WrongClaimReturnsConflict()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        var action = AddAutomatedRconImportAction(context, playerId);
        await context.SaveChangesAsync();
        var api = (IAdminActionsApi)CreateController(context);
        await api.ClaimForumTopicPublication(action.AdminActionId);

        var complete = await api.CompleteForumTopicPublication(action.AdminActionId, new CompleteForumTopicPublicationDto(Guid.NewGuid(), 12345));

        Assert.Equal(HttpStatusCode.Conflict, complete.StatusCode);
        Assert.Null(context.AdminActions.Single().ForumTopicId);
    }

    [Fact]
    public async Task UpdateAdminAction_RconBanImportForumTopicLinkReturnsConflict()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        var action = AddAutomatedRconImportAction(context, playerId);
        await context.SaveChangesAsync();
        var api = (IAdminActionsApi)CreateController(context);

        var result = await api.UpdateAdminAction(new EditAdminActionDto(action.AdminActionId)
        {
            ForumTopicId = 12345
        });

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Null(context.AdminActions.Single().ForumTopicId);
    }

    [Fact]
    public async Task GetAdminActions_AutomationSourceFilter_ReturnsOnlyAutomatedActions()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = await AddPlayerAsync(context);
        context.AdminActions.AddRange(
            new AdminAction
            {
                AdminActionId = Guid.NewGuid(),
                PlayerId = playerId,
                Type = (int)AdminActionType.Warning,
                Text = "Manual",
                Created = DateTime.UtcNow,
                Source = (byte)ActionSource.Manual
            },
            new AdminAction
            {
                AdminActionId = Guid.NewGuid(),
                PlayerId = playerId,
                Type = (int)AdminActionType.Observation,
                Text = "Automated",
                Created = DateTime.UtcNow,
                Source = (byte)ActionSource.Automation,
                AutomationFeature = (int)AutomationFeature.VpnProtection,
                AutomationRuleId = "vpn"
            });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IAdminActionsApi)controller;
        var result = await api.GetAdminActions(null, playerId, null, null, 0, 20, null, ActionSource.Automation, null, null);

        var action = Assert.Single(result.Result!.Data!.Items!);
        Assert.Equal("Automated", action.Text);
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

    private static async Task<Guid> AddPlayerAsync(PortalDbContext context)
    {
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
        return playerId;
    }

    private static EnsureAutomatedActionDto CreateAutomationRequest(Guid playerId, AdminActionType actionType) => new(
        playerId,
        actionType,
        "VPN Protection: vpn",
        AutomationFeature.VpnProtection,
        "vpn");

    private static AdminAction AddAutomatedRconImportAction(PortalDbContext context, Guid playerId)
    {
        var action = new AdminAction
        {
            AdminActionId = Guid.NewGuid(),
            PlayerId = playerId,
            Type = (int)AdminActionType.Ban,
            Text = "Imported from server RCON dumpbanlist",
            Created = DateTime.UtcNow,
            Source = (byte)ActionSource.Automation,
            AutomationFeature = (int)AutomationFeature.RconBanImport,
            AutomationRuleId = "cod4x:server:canonical-puid:permanent",
            AutomationIsActive = true
        };
        context.AdminActions.Add(action);
        return action;
    }
}
