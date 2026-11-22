using System.Net;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class ConnectedPlayerClaimsTests
{
    [Fact]
    public async Task Replacement_IsScopedAndPreservesClaimsAndIssueRetries()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        var player = Guid.NewGuid();
        var server = Guid.NewGuid();
        db.UserProfiles.AddRange(new UserProfile { UserProfileId = user }, new UserProfile { UserProfileId = other });
        db.Players.Add(new Player { PlayerId = player });
        foreach (var name in new[] { "verified-player", "senior-admin", "head-admin", "game-admin", "moderator", "clan-member" })
        {
            db.Tags.Add(new Tag { TagId = Guid.NewGuid(), Name = name });
        }
        await db.SaveChangesAsync();
        var api = (IConnectedPlayersApi)new ConnectedPlayersController(db, Mock.Of<IAuditLogger>());
        var first = (await api.IssueConnectedPlayerClaimCode(new() { UserProfileId = user })).Result!.Data!;
        var request = new SubmitConnectedPlayerClaimDto { RequestId = Guid.NewGuid(), PlayerId = player, GameServerId = server, Code = first.Code };
        Assert.Equal(HttpStatusCode.Created, (await api.SubmitConnectedPlayerClaim(request)).StatusCode);
        var current = (await api.IssueConnectedPlayerClaimCode(new() { UserProfileId = user })).Result!.Data!;
        Assert.Equal(HttpStatusCode.Conflict, (await api.ReplaceConnectedPlayerClaimCode(other, current.ConnectedPlayerActivationCodeId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.ReplaceConnectedPlayerClaimCode(user, first.ConnectedPlayerActivationCodeId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.ReplaceConnectedPlayerClaimCode(user, Guid.Empty)).StatusCode);
        var replacement = (await api.ReplaceConnectedPlayerClaimCode(user, current.ConnectedPlayerActivationCodeId)).Result!.Data!;
        Assert.NotEqual(current.Code, replacement.Code);
        Assert.Equal(TimeSpan.FromHours(1), replacement.ExpiresAtUtc - replacement.ActivatedAtUtc);
        Assert.Equal(HttpStatusCode.Conflict, (await api.ReplaceConnectedPlayerClaimCode(user, current.ConnectedPlayerActivationCodeId)).StatusCode);
        Assert.Equal(replacement.Code, (await api.IssueConnectedPlayerClaimCode(new() { UserProfileId = user })).Result!.Data!.Code);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SubmitConnectedPlayerClaim(request with { RequestId = Guid.NewGuid(), Code = current.Code })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.SubmitConnectedPlayerClaim(request)).StatusCode);
        Assert.Equal("Pending", db.ConnectedPlayerClaims.Single().Status);
        Assert.Equal(HttpStatusCode.OK, (await api.ApproveConnectedPlayerClaim(user, request.RequestId)).StatusCode);
        Assert.NotNull(db.ConnectedPlayerActivationCodes.Single(c => c.ConnectedPlayerActivationCodeId == current.ConnectedPlayerActivationCodeId).InvalidatedAtUtc);
    }

    [Fact]
    public async Task Replacement_DoesNotInvalidateLegacyOrOtherProfilesCodes()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        db.UserProfiles.AddRange(new UserProfile { UserProfileId = user }, new UserProfile { UserProfileId = other });
        await db.SaveChangesAsync();
        var api = (IConnectedPlayersApi)new ConnectedPlayersController(db, Mock.Of<IAuditLogger>());
        var legacy = (await api.ActivateConnectedPlayerActivationCode(new() { UserProfileId = user })).Result!.Data!;
        var otherCode = (await api.IssueConnectedPlayerClaimCode(new() { UserProfileId = other })).Result!.Data!;
        Assert.Equal(HttpStatusCode.Conflict, (await api.ReplaceConnectedPlayerClaimCode(user, legacy.ConnectedPlayerActivationCodeId)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.ReplaceConnectedPlayerClaimCode(user, otherCode.ConnectedPlayerActivationCodeId)).StatusCode);
        Assert.All(db.ConnectedPlayerActivationCodes, c => Assert.True(c.IsActive));
    }

    [Fact]
    public async Task ApprovalCodes_AreOneHourAndCannotBeConsumedByLegacyEndpoint()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var user = Guid.NewGuid();
        var player = Guid.NewGuid();
        var server = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfile { UserProfileId = user });
        db.Players.Add(new Player { PlayerId = player });
        db.GameServers.Add(new GameServer { GameServerId = server, Title = "Test server", Hostname = "localhost", BanFileRootPath = "/" });
        await db.SaveChangesAsync();
        var api = (IConnectedPlayersApi)new ConnectedPlayersController(db, Mock.Of<IAuditLogger>());

        var issued = await api.IssueConnectedPlayerClaimCode(new() { UserProfileId = user });
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        var code = Assert.IsType<ConnectedPlayerActivationCodeDto>(issued.Result!.Data);
        Assert.Equal(TimeSpan.FromHours(1), code.ExpiresAtUtc - code.ActivatedAtUtc);
        Assert.Equal(HttpStatusCode.Conflict, (await api.ConsumeConnectedPlayerActivationCode(new() { PlayerId = player, Code = code.Code })).StatusCode);
        Assert.Empty(db.ConnectedPlayerProfiles);
        Assert.True(db.ConnectedPlayerActivationCodes.Single().IsActive);
        var claim = await api.SubmitConnectedPlayerClaim(new() { RequestId = Guid.NewGuid(), PlayerId = player, GameServerId = server, Code = code.Code });
        Assert.Equal(HttpStatusCode.Created, claim.StatusCode);
        Assert.Equal(TimeSpan.FromHours(24), claim.Result!.Data!.ExpiresAtUtc - claim.Result.Data.CreatedAtUtc);
        Assert.Equal(server, claim.Result.Data.GameServerId);
        Assert.Equal("Test server", claim.Result.Data.GameServerName);
        Assert.Empty(db.ConnectedPlayerProfiles);
    }

    [Fact]
    public async Task LegacyAndExpiredApprovalCodes_CannotCreateClaims()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var user = Guid.NewGuid();
        var player = Guid.NewGuid();
        var server = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfile { UserProfileId = user });
        db.Players.Add(new Player { PlayerId = player });
        db.ConnectedPlayerActivationCodes.AddRange(
            new ConnectedPlayerActivationCode
            {
                ConnectedPlayerActivationCodeId = Guid.NewGuid(),
                UserProfileId = user,
                Code = "LEGACY",
                CodeHash = "legacy",
                IsActive = true,
                ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(4),
                ActivatedBy = "WebsiteActivation",
                MaxAttempts = 5
            },
            new ConnectedPlayerActivationCode
            {
                ConnectedPlayerActivationCodeId = Guid.NewGuid(),
                UserProfileId = user,
                Code = "EXPIRE",
                CodeHash = "expired",
                IsActive = true,
                ActivatedAtUtc = DateTime.UtcNow.AddHours(-2),
                ExpiresAtUtc = DateTime.UtcNow.AddHours(-1),
                ActivatedBy = "ApprovalRequired",
                MaxAttempts = 5
            });
        await db.SaveChangesAsync();
        var api = (IConnectedPlayersApi)new ConnectedPlayersController(db, Mock.Of<IAuditLogger>());

        Assert.Equal(HttpStatusCode.BadRequest, (await api.SubmitConnectedPlayerClaim(new() { RequestId = Guid.NewGuid(), PlayerId = player, GameServerId = server, Code = "LEGACY" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SubmitConnectedPlayerClaim(new() { RequestId = Guid.NewGuid(), PlayerId = player, GameServerId = server, Code = "EXPIRE" })).StatusCode);
        Assert.Empty(db.ConnectedPlayerClaims);
        Assert.Empty(db.ConnectedPlayerProfiles);
    }

    [Fact]
    public async Task SubmitApproveAndUnlink_AreScopedAndReplaySafe()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        var player = Guid.NewGuid();
        var request = Guid.NewGuid();
        var server = Guid.NewGuid();
        db.UserProfiles.AddRange(new UserProfile { UserProfileId = user }, new UserProfile { UserProfileId = other });
        db.Players.Add(new Player { PlayerId = player, Username = "Player", GameType = (int)GameType.CallOfDuty4 });
        foreach (var name in new[] { "verified-player", "senior-admin", "head-admin", "game-admin", "moderator", "clan-member", "unrelated" })
        {
            db.Tags.Add(new Tag { TagId = Guid.NewGuid(), Name = name });
        }
        db.UserProfileClaims.Add(new UserProfileClaim
        {
            UserProfileClaimId = Guid.NewGuid(),
            UserProfileId = user,
            SystemGenerated = true,
            ClaimType = UserProfileClaimType.ClanMember,
            ClaimValue = ""
        });
        foreach (var role in new[] { UserProfileClaimType.SeniorAdmin, UserProfileClaimType.HeadAdmin,
            UserProfileClaimType.GameAdmin, UserProfileClaimType.Moderator })
        {
            db.UserProfileClaims.Add(new UserProfileClaim
            {
                UserProfileClaimId = Guid.NewGuid(),
                UserProfileId = user,
                SystemGenerated = true,
                ClaimType = role,
                ClaimValue = GameType.CallOfDuty4.ToString()
            });
        }
        var unrelated = db.Tags.Local.Single(t => t.Name == "unrelated");
        db.PlayerTags.Add(new PlayerTag { PlayerTagId = Guid.NewGuid(), PlayerId = player, TagId = unrelated.TagId, Assigned = DateTime.UtcNow });
        db.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = user,
            Code = "ABC123",
            CodeHash = "hash",
            IsActive = true,
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            ActivatedBy = "ApprovalRequired",
            MaxAttempts = 5
        });
        await db.SaveChangesAsync();
        var api = (IConnectedPlayersApi)new ConnectedPlayersController(db, Mock.Of<IAuditLogger>());
        var submission = new SubmitConnectedPlayerClaimDto { RequestId = request, PlayerId = player, GameServerId = server, Code = "ABC123" };

        Assert.Equal(HttpStatusCode.Created, (await api.SubmitConnectedPlayerClaim(submission)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.SubmitConnectedPlayerClaim(submission)).StatusCode);
        Assert.Null(db.ConnectedPlayerClaims.Single().GameServerName);
        Assert.Equal(HttpStatusCode.Conflict, (await api.SubmitConnectedPlayerClaim(submission with { GameServerId = Guid.NewGuid() })).StatusCode);
        Assert.Empty(db.ConnectedPlayerProfiles);
        Assert.Single(db.PlayerTags);
        Assert.Equal(HttpStatusCode.NotFound, (await api.ApproveConnectedPlayerClaim(other, request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.ApproveConnectedPlayerClaim(user, request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.ApproveConnectedPlayerClaim(user, request)).StatusCode);
        var link = Assert.Single(db.ConnectedPlayerProfiles);
        Assert.Equal(7, db.PlayerTags.Count());
        Assert.Equal(HttpStatusCode.NotFound, (await api.UnlinkOwnConnectedPlayer(other, link.ConnectedPlayerProfileId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.UnlinkOwnConnectedPlayer(user, link.ConnectedPlayerProfileId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.UnlinkOwnConnectedPlayer(user, link.ConnectedPlayerProfileId)).StatusCode);
        Assert.Equal(unrelated.TagId, Assert.Single(db.PlayerTags).TagId);
        Assert.False(db.ConnectedPlayerProfiles.Single().IsActive);
        Assert.Equal(HttpStatusCode.OK, (await api.ApproveConnectedPlayerClaim(user, request)).StatusCode);
        Assert.False(db.ConnectedPlayerProfiles.Single().IsActive);
    }

    [Fact]
    public async Task Claim_RejectsReusedCodeAndConflictingRequestIdentity()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var user = Guid.NewGuid();
        var player = Guid.NewGuid();
        var other = Guid.NewGuid();
        var request = Guid.NewGuid();
        var server = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfile { UserProfileId = user });
        db.Players.AddRange(new Player { PlayerId = player }, new Player { PlayerId = other });
        db.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = user,
            Code = "ABC123",
            CodeHash = "hash",
            IsActive = true,
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
            ActivatedBy = "ApprovalRequired",
            MaxAttempts = 5
        });
        await db.SaveChangesAsync();
        var api = (IConnectedPlayersApi)new ConnectedPlayersController(db, Mock.Of<IAuditLogger>());
        Assert.Equal(HttpStatusCode.Created, (await api.SubmitConnectedPlayerClaim(new() { RequestId = request, PlayerId = player, GameServerId = server, Code = "ABC123" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.SubmitConnectedPlayerClaim(new() { RequestId = request, PlayerId = other, GameServerId = server, Code = "ABC123" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.SubmitConnectedPlayerClaim(new() { RequestId = Guid.NewGuid(), PlayerId = other, GameServerId = server, Code = "ABC123" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.DeclineConnectedPlayerClaim(user, request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.DeclineConnectedPlayerClaim(user, request)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await api.ApproveConnectedPlayerClaim(user, request)).StatusCode);
        Assert.Empty(db.ConnectedPlayerProfiles);
    }

    [Fact]
    public async Task ExpiredClaim_CannotBeApproved()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var user = Guid.NewGuid();
        var player = Guid.NewGuid();
        var request = Guid.NewGuid();
        db.UserProfiles.Add(new UserProfile { UserProfileId = user });
        db.Players.Add(new Player { PlayerId = player });
        db.ConnectedPlayerClaims.Add(new ConnectedPlayerClaim
        {
            ConnectedPlayerClaimId = request,
            UserProfileId = user,
            PlayerId = player,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();
        var api = (IConnectedPlayersApi)new ConnectedPlayersController(db, Mock.Of<IAuditLogger>());
        Assert.Equal(HttpStatusCode.Conflict, (await api.ApproveConnectedPlayerClaim(user, request)).StatusCode);
        Assert.Equal("Expired", db.ConnectedPlayerClaims.Single().Status);
        Assert.Empty(db.ConnectedPlayerProfiles);
    }
}
