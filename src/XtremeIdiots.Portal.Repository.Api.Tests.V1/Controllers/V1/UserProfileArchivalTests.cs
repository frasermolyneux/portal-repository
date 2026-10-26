using System.Net;
using Microsoft.EntityFrameworkCore;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.Cod4xPower;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class UserProfileArchivalTests
{
    [Fact]
    public async Task Archive_PreservesMetadataAndHistoryAndRevokesCapabilities()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var target = new UserProfile { IdentityOid = "identity", XtremeIdiotsForumId = "forum", DemoAuthKey = "demo" };
        var actor = new UserProfile();
        var player = new Player();
        var link = new ConnectedPlayerProfile
        {
            Player = player,
            UserProfile = target,
            LinkedByUserProfile = target,
            IsActive = true,
            LinkedAtUtc = DateTime.UtcNow.AddDays(-1),
            LinkMethod = "TrustedWebsite"
        };
        var code = new ConnectedPlayerActivationCode
        {
            UserProfile = target,
            Code = "ABC123",
            CodeHash = "hash",
            ActivatedBy = "WebsiteActivation",
            IsActive = true,
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        };
        var claim = new UserProfileClaim { UserProfile = target, ClaimType = UserProfileClaimType.Moderator, ClaimValue = "CallOfDuty4" };
        var action = new AdminAction { UserProfile = target, Player = player, Text = "Historical action" };
        context.AddRange(actor, link, code, claim, action);
        context.PlayerTags.Add(new PlayerTag { Player = player, Tag = new Tag { Name = "verified-player" } });
        context.PlayerTags.Add(new PlayerTag { Player = player, Tag = new Tag { Name = "Senior-Admin" } });
        await context.SaveChangesAsync();

        IUserProfileApi api = new UserProfileController(context);
        Assert.Equal(HttpStatusCode.OK, (await api.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, " Forum 404 "))).StatusCode);
        var archived = (await api.GetUserProfileIncludingArchived(target.UserProfileId)).Result!.Data!;
        Assert.NotNull(archived.ArchivedAtUtc);
        Assert.Equal(actor.UserProfileId, archived.ArchivedByUserProfileId);
        Assert.Equal("Forum 404", archived.ArchiveReason);
        Assert.Null(archived.DemoAuthKey);
        Assert.Single(archived.UserProfileClaims);

        Assert.Equal(HttpStatusCode.NoContent, (await api.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, "Different reason"))).StatusCode);
        var repeated = (await api.GetUserProfileIncludingArchived(target.UserProfileId)).Result!.Data!;
        Assert.Equal(archived.ArchivedAtUtc, repeated.ArchivedAtUtc);
        Assert.Equal(archived.ArchiveReason, repeated.ArchiveReason);
        var historicalAction = await context.AdminActions.Include(a => a.UserProfile).SingleAsync();
        Assert.Equal(target.UserProfileId, historicalAction.UserProfile!.UserProfileId);
        Assert.NotNull(historicalAction.UserProfile.ArchivedAtUtc);
        var historicalLink = await context.ConnectedPlayerProfiles.SingleAsync();
        Assert.False(historicalLink.IsActive);
        Assert.Equal(target.UserProfileId, historicalLink.LinkedByUserProfileId);
        Assert.Equal(actor.UserProfileId, historicalLink.UnlinkedByUserProfileId);
        Assert.NotNull(historicalLink.UnlinkedAtUtc);
        var invalidatedCode = await context.ConnectedPlayerActivationCodes.SingleAsync();
        Assert.False(invalidatedCode.IsActive);
        Assert.NotNull(invalidatedCode.InvalidatedAtUtc);
        Assert.Empty(context.PlayerTags);
        Assert.Single(context.UserProfileClaims);
    }

    [Fact]
    public async Task ArchivedProfiles_AreExcludedFromEveryDefaultLookupListCountAndPermissionReport()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var archived = new UserProfile
        {
            ArchivedAtUtc = DateTime.UtcNow,
            IdentityOid = "archived-id",
            XtremeIdiotsForumId = "archived-forum",
            DemoAuthKey = "stale-key",
            DisplayName = "Archived moderator",
            UserProfileClaims = [new() { ClaimType = UserProfileClaimType.Moderator, ClaimValue = "CallOfDuty4" }]
        };
        context.UserProfiles.AddRange(archived, new UserProfile { DisplayName = "Active" });
        await context.SaveChangesAsync();
        IUserProfileApi api = new UserProfileController(context);

        Assert.Equal(HttpStatusCode.NotFound, (await api.GetUserProfile(archived.UserProfileId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetUserProfileByIdentityId("archived-id")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetUserProfileByXtremeIdiotsId("archived-forum")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetUserProfileByDemoAuthKey("stale-key")).StatusCode);
        var all = await api.GetUserProfiles(null, null, 0, 50, null);
        Assert.Single(all.Result!.Data!.Items!);
        Assert.Equal(1, all.Result.Pagination!.TotalCount);
        var moderators = await api.GetUserProfiles("Archived", UserProfileFilter.Moderators, 0, 50, null);
        Assert.Empty(moderators.Result!.Data!.Items!);
        Assert.Equal(1, moderators.Result.Pagination!.TotalCount);
        Assert.Equal(0, moderators.Result.Pagination.FilteredCount);
        Assert.Empty((await api.GetPermissionsReport(null, null)).Result!.Data!.Items!);
        Assert.Equal(HttpStatusCode.OK, (await api.GetUserProfileIncludingArchived(archived.UserProfileId)).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task Archive_RejectsBlankReasons(string? reason)
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        IUserProfileApi api = new UserProfileController(context);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ArchiveUserProfile(new(Guid.NewGuid(), Guid.NewGuid(), reason!))).StatusCode);
    }

    [Fact]
    public async Task Archive_RequiresBoundedReasonActiveActorAndNonSelfTarget()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var target = new UserProfile();
        var actor = new UserProfile { ArchivedAtUtc = DateTime.UtcNow };
        context.AddRange(target, actor);
        await context.SaveChangesAsync();
        IUserProfileApi api = new UserProfileController(context);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, new string('a', 501)))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.ArchiveUserProfile(new(target.UserProfileId, target.UserProfileId, "Reason"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, "Reason"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ArchiveUserProfile(new(target.UserProfileId, Guid.NewGuid(), "Reason"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.ArchiveUserProfile(new(Guid.NewGuid(), target.UserProfileId, "Reason"))).StatusCode);
        Assert.Null((await api.GetUserProfile(target.UserProfileId)).Result!.Data!.ArchivedAtUtc);
    }

    [Fact]
    public async Task ArchivedProfiles_RejectMutationsAndReserveBothIdentifiers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var archived = new UserProfile
        {
            ArchivedAtUtc = DateTime.UtcNow,
            IdentityOid = "reserved",
            XtremeIdiotsForumId = "forum",
            UserProfileClaims = [new() { ClaimType = AdditionalPermission.Maps_Read, ClaimValue = "CallOfDuty4" }]
        };
        var active = new UserProfile();
        context.AddRange(archived, active);
        await context.SaveChangesAsync();
        var claimId = archived.UserProfileClaims.Single().UserProfileClaimId;
        IUserProfileApi api = new UserProfileController(context);
        var edit = new EditUserProfileDto(archived.UserProfileId) { DemoAuthKey = "new-key" };
        List<CreateUserProfileClaimDto> claims = [new(archived.UserProfileId, AdditionalPermission.Maps_Read, "CallOfDuty4", false)];
        Assert.Equal(HttpStatusCode.NotFound, (await api.UpdateUserProfile(edit)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.UpdateUserProfiles([edit])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.CreateUserProfileClaim(archived.UserProfileId, claims)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.SetUserProfileClaims(archived.UserProfileId, [])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.DeleteUserProfileClaim(archived.UserProfileId, claimId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.CreateUserProfile(new("other-forum", "Name", "email") { IdentityOid = "reserved" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.CreateUserProfiles([new("forum", "Name", "email") { IdentityOid = "new" }])).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.UpdateUserProfile(new(active.UserProfileId) { XtremeIdiotsForumId = "forum" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.UpdateUserProfiles([new(active.UserProfileId) { IdentityOid = "reserved" }])).StatusCode);
        Assert.Single(context.UserProfileClaims);
        Assert.Null((await api.GetUserProfileIncludingArchived(archived.UserProfileId)).Result!.Data!.DemoAuthKey);
    }

    [Fact]
    public async Task ArchivedProfiles_CannotActivateOrConsumeCodesRelinkOrReceiveNewNotifications()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var archived = new UserProfile { ArchivedAtUtc = DateTime.UtcNow };
        var active = new UserProfile();
        var player = new Player();
        context.AddRange(archived, active, player);
        context.ConnectedPlayerActivationCodes.Add(new()
        {
            UserProfile = archived,
            Code = "ABC123",
            CodeHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("ABC123"))),
            ActivatedBy = "WebsiteActivation",
            IsActive = true,
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
        });
        await context.SaveChangesAsync();
        IConnectedPlayersApi api = new ConnectedPlayersController(context, Mock.Of<IAuditLogger>());
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ActivateConnectedPlayerActivationCode(new() { UserProfileId = archived.UserProfileId })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.GetActiveConnectedPlayerActivationCode(archived.UserProfileId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ConsumeConnectedPlayerActivationCode(new() { PlayerId = player.PlayerId, Code = "ABC123" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateConnectedPlayerLink(new() { PlayerId = player.PlayerId, UserProfileId = archived.UserProfileId })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateConnectedPlayerLink(new()
        {
            PlayerId = player.PlayerId,
            UserProfileId = active.UserProfileId,
            LinkedByUserProfileId = archived.UserProfileId
        })).StatusCode);
        INotificationsApi notifications = new NotificationsController(context);
        Assert.Equal(HttpStatusCode.BadRequest, (await notifications.CreateNotification(new CreateNotificationDto(archived.UserProfileId, "type", "Title", "Message"))).StatusCode);
        INotificationPreferencesApi preferences = new NotificationPreferencesController(context);
        Assert.Equal(HttpStatusCode.NotFound, (await preferences.UpdateNotificationPreferences(archived.UserProfileId, [])).StatusCode);
        Assert.Empty(context.ConnectedPlayerProfiles);
    }

    [Fact]
    public async Task ArchivedProfile_WithStaleActiveLinkCannotAppearInAdminRoster()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var profile = new UserProfile { ArchivedAtUtc = DateTime.UtcNow };
        var player = new Player { GameType = (int)GameType.CallOfDuty4, Guid = "player-guid" };
        var server = new GameServer { GameType = (int)GameType.CallOfDuty4, Title = "Server", Hostname = "localhost" };
        context.AddRange(profile, player, server);
        context.ConnectedPlayerProfiles.Add(new()
        {
            UserProfile = profile,
            Player = player,
            IsActive = true,
            LinkMethod = "TrustedWebsite",
            LinkedAtUtc = DateTime.UtcNow
        });
        context.GlobalConfigurations.Add(new()
        {
            Namespace = Cod4xPowerSettingsConstants.Namespace,
            Configuration = System.Text.Json.JsonSerializer.Serialize(new { enabled = true, defaultPower = 10 }),
            LastModifiedUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        IConnectedPlayersApi api = new ConnectedPlayersController(context, Mock.Of<IAuditLogger>());
        var result = await api.GetCod4xAdminRoster(server.GameServerId);
        Assert.True(result.Result!.Data!.Enabled);
        Assert.Empty(result.Result.Data.Entries);
        Assert.Empty((await api.GetConnectedPlayersByUserProfile(profile.UserProfileId, 0, 50)).Result!.Data!.Items!);
    }
}
