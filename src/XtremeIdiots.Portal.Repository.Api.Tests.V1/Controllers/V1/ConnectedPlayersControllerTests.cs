using System.Net;
using System.Security.Cryptography;
using System.Text;

using Moq;

using MX.Observability.ApplicationInsights.Auditing;

using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class ConnectedPlayersControllerTests
{
    private static (ConnectedPlayersController Controller, Mock<IAuditLogger> AuditLoggerMock) CreateControllerWithAuditMock(PortalDbContext context)
    {
        var auditLogger = new Mock<IAuditLogger>();
        return (new ConnectedPlayersController(context, auditLogger.Object), auditLogger);
    }

    private static ConnectedPlayersController CreateController(PortalDbContext context) => CreateControllerWithAuditMock(context).Controller;

    [Fact]
    public async Task CreateConnectedPlayerLink_WhenUnlinked_CreatesNewActiveLink()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        await context.SaveChangesAsync();

        var (controller, auditLoggerMock) = CreateControllerWithAuditMock(context);
        var api = (IConnectedPlayersApi)controller;

        var result = await api.CreateConnectedPlayerLink(new CreateConnectedPlayerLinkDto
        {
            PlayerId = playerId,
            UserProfileId = userProfileId
        });

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.ConnectedPlayerProfiles);
        Assert.True(context.ConnectedPlayerProfiles.Single().IsActive);
        Assert.Contains(auditLoggerMock.Invocations, invocation => invocation.Method.Name == "LogAudit");
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
    public async Task ActivateConnectedPlayerActivationCode_WhenValid_CreatesNewActiveCode()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        await context.SaveChangesAsync();

        var (controller, auditLoggerMock) = CreateControllerWithAuditMock(context);
        var api = (IConnectedPlayersApi)controller;

        var result = await api.ActivateConnectedPlayerActivationCode(new ActivateConnectedPlayerActivationCodeDto
        {
            UserProfileId = userProfileId
        });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.ConnectedPlayerActivationCodes);
        Assert.True(context.ConnectedPlayerActivationCodes.Single().IsActive);
        Assert.Equal("WebsiteActivation", context.ConnectedPlayerActivationCodes.Single().ActivatedBy);
        Assert.Equal(6, result.Result?.Data?.Code.Length);
        Assert.All(result.Result!.Data!.Code, character => Assert.Contains(character, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"));
        Assert.Contains(auditLoggerMock.Invocations, invocation => invocation.Method.Name == "LogAudit");
    }

    [Fact]
    public async Task ActivateConnectedPlayerActivationCode_InvalidatesPreviousActiveCode()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Code = "ABC123",
            CodeHash = HashToken("ABC123"),
            ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(4),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedBy = userProfileId.ToString()
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.ActivateConnectedPlayerActivationCode(new ActivateConnectedPlayerActivationCodeDto
        {
            UserProfileId = userProfileId
        });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, context.ConnectedPlayerActivationCodes.Count());
        Assert.Equal(1, context.ConnectedPlayerActivationCodes.Count(code => code.IsActive));
        Assert.NotNull(context.ConnectedPlayerActivationCodes.Single(code => !code.IsActive).InvalidatedAtUtc);
    }

    [Fact]
    public async Task ActivateConnectedPlayerActivationCode_WhenReplacingExistingCode_EmitsInvalidationAudit()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Code = "ABC123",
            CodeHash = HashToken("ABC123"),
            ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(4),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedBy = "WebsiteActivation"
        });
        await context.SaveChangesAsync();

        var (controller, auditLoggerMock) = CreateControllerWithAuditMock(context);
        var api = (IConnectedPlayersApi)controller;

        await api.ActivateConnectedPlayerActivationCode(new ActivateConnectedPlayerActivationCodeDto
        {
            UserProfileId = userProfileId
        });

        Assert.True(auditLoggerMock.Invocations.Count(invocation => invocation.Method.Name == "LogAudit") >= 2);
    }

    [Fact]
    public async Task GetActiveConnectedPlayerActivationCode_WhenActiveCodeExists_ReturnsIt()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Code = "XYZ789",
            CodeHash = HashToken("XYZ789"),
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedBy = userProfileId.ToString()
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.GetActiveConnectedPlayerActivationCode(userProfileId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("XYZ789", result.Result?.Data?.Code);
    }

    [Fact]
    public async Task GetActiveConnectedPlayerActivationCode_WhenExpired_InvalidatesAndReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Code = "EXPIRE",
            CodeHash = HashToken("EXPIRE"),
            ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedBy = userProfileId.ToString()
        });
        await context.SaveChangesAsync();

        var (controller, auditLoggerMock) = CreateControllerWithAuditMock(context);
        var api = (IConnectedPlayersApi)controller;
        var result = await api.GetActiveConnectedPlayerActivationCode(userProfileId);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.False(context.ConnectedPlayerActivationCodes.Single().IsActive);
        Assert.NotNull(context.ConnectedPlayerActivationCodes.Single().InvalidatedAtUtc);
        Assert.Contains(auditLoggerMock.Invocations, invocation => invocation.Method.Name == "LogAudit");
    }

    [Fact]
    public async Task ConsumeConnectedPlayerActivationCode_WithCorrectCode_CreatesLinkAndConsumesCode()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Code = "ABC123",
            CodeHash = HashToken("ABC123"),
            ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-2),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(3),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedBy = "WebsiteActivation"
        });
        await context.SaveChangesAsync();

        var (controller, auditLoggerMock) = CreateControllerWithAuditMock(context);
        var api = (IConnectedPlayersApi)controller;
        var result = await api.ConsumeConnectedPlayerActivationCode(new ConsumeConnectedPlayerActivationCodeDto
        {
            PlayerId = playerId,
            Code = "ABC123"
        });

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.ConnectedPlayerProfiles);
        Assert.Equal(ConnectedPlayerLinkMethod.ActivationCode.ToString(), context.ConnectedPlayerProfiles.Single().LinkMethod);
        Assert.False(context.ConnectedPlayerActivationCodes.Single().IsActive);
        Assert.NotNull(context.ConnectedPlayerActivationCodes.Single().ConsumedAtUtc);
        Assert.Contains(auditLoggerMock.Invocations, invocation => invocation.Method.Name == "LogAudit");
    }

    [Fact]
    public async Task ConsumeConnectedPlayerActivationCode_WhenAlreadyLinkedSameProfile_ReturnsOk()
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
            LinkMethod = ConnectedPlayerLinkMethod.ActivationCode.ToString(),
            LinkedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true
        });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Code = "ABC123",
            CodeHash = HashToken("ABC123"),
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedBy = "WebsiteActivation"
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.ConsumeConnectedPlayerActivationCode(new ConsumeConnectedPlayerActivationCodeDto
        {
            PlayerId = playerId,
            Code = "ABC123"
        });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.ConnectedPlayerProfiles);
        Assert.Equal(playerId, result.Result?.Data?.PlayerId);
        Assert.Equal(userProfileId, result.Result?.Data?.UserProfileId);
        Assert.Equal("Player", result.Result?.Data?.Username);
        Assert.False(context.ConnectedPlayerActivationCodes.Single().IsActive);
        Assert.NotNull(context.ConnectedPlayerActivationCodes.Single().ConsumedAtUtc);
    }

    [Fact]
    public async Task ConsumeConnectedPlayerActivationCode_WithWrongCode_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Code = "ABC123",
            CodeHash = HashToken("ABC123"),
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedBy = "WebsiteActivation"
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.ConsumeConnectedPlayerActivationCode(new ConsumeConnectedPlayerActivationCodeDto
        {
            PlayerId = playerId,
            Code = "WRONG1"
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Single(context.ConnectedPlayerActivationCodes);
        Assert.True(context.ConnectedPlayerActivationCodes.Single().IsActive);
        Assert.Empty(context.ConnectedPlayerProfiles);
    }

    [Fact]
    public async Task ConsumeConnectedPlayerActivationCode_WhenPlayerDoesNotExist_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Code = "ABC123",
            CodeHash = HashToken("ABC123"),
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedBy = "WebsiteActivation"
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.ConsumeConnectedPlayerActivationCode(new ConsumeConnectedPlayerActivationCodeDto
        {
            PlayerId = playerId,
            Code = "ABC123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Empty(context.ConnectedPlayerProfiles);
        Assert.True(context.ConnectedPlayerActivationCodes.Single().IsActive);
    }

    [Fact]
    public async Task ConsumeConnectedPlayerActivationCode_WhenCodeIsInactive_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var userProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId, DisplayName = "User" });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            Code = "ABC123",
            CodeHash = HashToken("ABC123"),
            ActivatedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = false,
            InvalidatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            ActivatedBy = "WebsiteActivation"
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.ConsumeConnectedPlayerActivationCode(new ConsumeConnectedPlayerActivationCodeDto
        {
            PlayerId = playerId,
            Code = "ABC123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(0, context.ConnectedPlayerProfiles.Count());
    }

    [Fact]
    public async Task ConsumeConnectedPlayerActivationCode_WhenExistingLinkBelongsToDifferentProfile_ReturnsConflict()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var playerId = Guid.NewGuid();
        var linkedUserProfileId = Guid.NewGuid();
        var activationUserProfileId = Guid.NewGuid();

        context.Players.Add(new Player { PlayerId = playerId, GameType = 1, Username = "Player", FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow });
        context.UserProfiles.Add(new UserProfile { UserProfileId = linkedUserProfileId, DisplayName = "Linked" });
        context.UserProfiles.Add(new UserProfile { UserProfileId = activationUserProfileId, DisplayName = "Activation" });
        context.ConnectedPlayerProfiles.Add(new ConnectedPlayerProfile
        {
            ConnectedPlayerProfileId = Guid.NewGuid(),
            PlayerId = playerId,
            UserProfileId = linkedUserProfileId,
            LinkMethod = ConnectedPlayerLinkMethod.TrustedWebsite.ToString(),
            LinkedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            IsActive = true
        });
        context.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
        {
            ConnectedPlayerActivationCodeId = Guid.NewGuid(),
            UserProfileId = activationUserProfileId,
            Code = "ABC123",
            CodeHash = HashToken("ABC123"),
            ActivatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5),
            AttemptCount = 0,
            MaxAttempts = 5,
            IsActive = true,
            ActivatedBy = "WebsiteActivation"
        });
        await context.SaveChangesAsync();

        var api = (IConnectedPlayersApi)CreateController(context);
        var result = await api.ConsumeConnectedPlayerActivationCode(new ConsumeConnectedPlayerActivationCodeDto
        {
            PlayerId = playerId,
            Code = "ABC123"
        });

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Single(context.ConnectedPlayerProfiles);
        Assert.True(context.ConnectedPlayerActivationCodes.Single().IsActive);
        Assert.Null(context.ConnectedPlayerActivationCodes.Single().ConsumedAtUtc);
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
            LinkMethod = ConnectedPlayerLinkMethod.ActivationCode
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

    [Fact]
    public async Task ForceUnlinkConnectedPlayer_WhenActive_UnlinksAndEmitsAudit()
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
            IsActive = true
        });
        await context.SaveChangesAsync();

        var (controller, auditLoggerMock) = CreateControllerWithAuditMock(context);
        var api = (IConnectedPlayersApi)controller;
        var result = await api.ForceUnlinkConnectedPlayer(connectedPlayerProfileId, new ForceUnlinkConnectedPlayerDto());

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(context.ConnectedPlayerProfiles.Single().IsActive);
        Assert.Contains(auditLoggerMock.Invocations, invocation => invocation.Method.Name == "LogAudit");
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
