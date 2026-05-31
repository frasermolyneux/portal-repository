using System.Net;
using System.Security.Cryptography;
using System.Text;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class ConnectedPlayersControllerTests
{
    private static ConnectedPlayersController CreateController(PortalDbContext context) => new(context);

    [Fact]
    public async Task CreateConnectedPlayerLink_WhenUnlinked_CreatesNewActiveLink()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);

        var result = await api.CreateConnectedPlayerLink(new CreateConnectedPlayerLinkDto
        {
            PlayerId = playerId,
            UserProfileId = userProfileId
        });

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.ConnectedPlayerProfiles);
        Assert.True(context.ConnectedPlayerProfiles.Single().IsActive);
    }

    [Fact]
    public async Task CreateConnectedPlayerLink_WhenAlreadyLinked_ReturnsConflict()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerProfiles.Add(new ConnectedPlayerProfile
        {
            ConnectedPlayerProfileId = Guid.NewGuid(),
            PlayerId = playerId,
            UserProfileId = userProfileId,
            LinkMethod = ConnectedPlayerLinkMethod.TrustedWebsite.ToString(),
            LinkedAtUtc = DateTime.UtcNow,
            IsActive = true
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.CreateConnectedPlayerLink(new CreateConnectedPlayerLinkDto
        {
            PlayerId = playerId,
            UserProfileId = userProfileId
        });

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
    }

    [Fact]
    public async Task IssueConnectedPlayerRegistrationToken_InvalidatesPreviousActiveToken()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.ConnectedPlayerRegistrationTokens.Add(new ConnectedPlayerRegistrationToken
        {
            ConnectedPlayerRegistrationTokenId = Guid.NewGuid(),
            PlayerId = playerId,
            TokenHash = HashToken("000001"),
            IssuedAtUtc = DateTime.UtcNow.AddMinutes(-2),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(3),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            IssuedBy = "RegisterCommand"
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.IssueConnectedPlayerRegistrationToken(new IssueConnectedPlayerRegistrationTokenDto
        {
            PlayerId = playerId,
            ExpiryMinutes = 5,
            MaxAttempts = 5
        });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, context.ConnectedPlayerRegistrationTokens.Count());
        Assert.Equal(1, context.ConnectedPlayerRegistrationTokens.Count(t => t.IsActive));
    }

    [Fact]
    public async Task VerifyConnectedPlayerRegistrationToken_WithCorrectToken_CreatesLinkAndMarksTokenVerified()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerRegistrationTokens.Add(new ConnectedPlayerRegistrationToken
        {
            ConnectedPlayerRegistrationTokenId = Guid.NewGuid(),
            PlayerId = playerId,
            TokenHash = HashToken("123456"),
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            IssuedBy = "RegisterCommand"
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.VerifyConnectedPlayerRegistrationToken(new VerifyConnectedPlayerRegistrationTokenDto
        {
            PlayerId = playerId,
            UserProfileId = userProfileId,
            Token = "123456"
        });

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.ConnectedPlayerProfiles);
        Assert.False(context.ConnectedPlayerRegistrationTokens.Single().IsActive);
        Assert.NotNull(context.ConnectedPlayerRegistrationTokens.Single().VerifiedAtUtc);
    }

    [Fact]
    public async Task VerifyConnectedPlayerRegistrationToken_WithWrongToken_IncrementsAttemptCount()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerRegistrationTokens.Add(new ConnectedPlayerRegistrationToken
        {
            ConnectedPlayerRegistrationTokenId = Guid.NewGuid(),
            PlayerId = playerId,
            TokenHash = HashToken("654321"),
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            IssuedBy = "RegisterCommand"
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.VerifyConnectedPlayerRegistrationToken(new VerifyConnectedPlayerRegistrationTokenDto
        {
            PlayerId = playerId,
            UserProfileId = userProfileId,
            Token = "123456"
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(1, context.ConnectedPlayerRegistrationTokens.Single().AttemptCount);
    }

    [Fact]
    public async Task VerifyConnectedPlayerRegistrationToken_WhenMaxAttemptsReached_ReturnsBadRequestAndInvalidatesToken()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerRegistrationTokens.Add(new ConnectedPlayerRegistrationToken
        {
            ConnectedPlayerRegistrationTokenId = Guid.NewGuid(),
            PlayerId = playerId,
            TokenHash = HashToken("654321"),
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 1,
            IsActive = true,
            IssuedBy = "RegisterCommand"
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.VerifyConnectedPlayerRegistrationToken(new VerifyConnectedPlayerRegistrationTokenDto
        {
            PlayerId = playerId,
            UserProfileId = userProfileId,
            Token = "123456"
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(1, context.ConnectedPlayerRegistrationTokens.Single().AttemptCount);
        Assert.False(context.ConnectedPlayerRegistrationTokens.Single().IsActive);
        Assert.NotNull(context.ConnectedPlayerRegistrationTokens.Single().InvalidatedAtUtc);
    }

    [Fact]
    public async Task CreateConnectedPlayerLink_WhenLinkedByUserProfileDoesNotExist_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);

        var result = await api.CreateConnectedPlayerLink(new CreateConnectedPlayerLinkDto
        {
            PlayerId = playerId,
            UserProfileId = userProfileId,
            LinkedByUserProfileId = Guid.NewGuid(),
            LinkMethod = ConnectedPlayerLinkMethod.TokenVerified
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(context.ConnectedPlayerProfiles);
    }

    [Fact]
    public async Task ForceUnlinkConnectedPlayer_WhenAlreadyInactive_IsIdempotent()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var connectedPlayerProfileId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerProfiles.Add(new ConnectedPlayerProfile
        {
            ConnectedPlayerProfileId = connectedPlayerProfileId,
            PlayerId = playerId,
            UserProfileId = userProfileId,
            LinkMethod = ConnectedPlayerLinkMethod.TrustedWebsite.ToString(),
            LinkedAtUtc = DateTime.UtcNow.AddHours(-1),
            IsActive = false,
            UnlinkedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.ForceUnlinkConnectedPlayer(connectedPlayerProfileId, new ForceUnlinkConnectedPlayerDto());

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
