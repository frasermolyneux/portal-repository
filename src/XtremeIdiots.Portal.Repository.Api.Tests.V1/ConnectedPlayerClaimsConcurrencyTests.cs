using System.Net;
using Microsoft.EntityFrameworkCore;
using Moq;
using MX.Observability.ApplicationInsights.Auditing;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1;

[Trait("Category", "SqlIntegration")]
[Collection("SqlServer")]
public class ConnectedPlayerClaimsConcurrencyTests
{
    [Fact]
    public Task ConcurrentApprovals_OnlyOneMemberOwnsPlayer()
        => SqlServerTestDatabase.RunAsync(async connectionString =>
        {
            var options = new DbContextOptionsBuilder<PortalDbContext>().UseSqlServer(connectionString).Options;
            await using var setup = new PortalDbContext(options);
            var firstUser = new UserProfile();
            var secondUser = new UserProfile();
            var player = new Player { FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow };
            setup.AddRange(firstUser, secondUser, player);
            foreach (var name in new[] { "verified-player", "senior-admin", "head-admin", "game-admin", "moderator", "clan-member" })
            {
                setup.Tags.Add(new Tag { Name = name, UserDefined = false });
            }
            await setup.SaveChangesAsync();

            var firstCode = new ConnectedPlayerActivationCode
            {
                UserProfileId = firstUser.UserProfileId,
                Code = "ABC123",
                CodeHash = "A",
                ActivatedBy = "ApprovalRequired",
                IsActive = true,
                ActivatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                MaxAttempts = 5
            };
            var secondCode = new ConnectedPlayerActivationCode
            {
                UserProfileId = secondUser.UserProfileId,
                Code = "DEF456",
                CodeHash = "B",
                ActivatedBy = "ApprovalRequired",
                IsActive = true,
                ActivatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1),
                MaxAttempts = 5
            };
            setup.AddRange(firstCode, secondCode);
            await setup.SaveChangesAsync();
            var firstId = Guid.NewGuid();
            var secondId = Guid.NewGuid();
            var serverId = Guid.NewGuid();
            await using var firstDb = new PortalDbContext(options);
            await using var secondDb = new PortalDbContext(options);
            IConnectedPlayersApi first = new ConnectedPlayersController(firstDb, Mock.Of<IAuditLogger>());
            IConnectedPlayersApi second = new ConnectedPlayersController(secondDb, Mock.Of<IAuditLogger>());
            Assert.Equal(HttpStatusCode.Created, (await first.SubmitConnectedPlayerClaim(new SubmitConnectedPlayerClaimDto
            { RequestId = firstId, PlayerId = player.PlayerId, GameServerId = serverId, Code = firstCode.Code })).StatusCode);
            Assert.Equal(HttpStatusCode.Created, (await second.SubmitConnectedPlayerClaim(new SubmitConnectedPlayerClaimDto
            { RequestId = secondId, PlayerId = player.PlayerId, GameServerId = serverId, Code = secondCode.Code })).StatusCode);
            var outcomes = await Task.WhenAll(first.ApproveConnectedPlayerClaim(firstUser.UserProfileId, firstId),
                second.ApproveConnectedPlayerClaim(secondUser.UserProfileId, secondId));
            Assert.Single(outcomes, outcome => outcome.StatusCode == HttpStatusCode.OK);
            Assert.Single(outcomes, outcome => outcome.StatusCode == HttpStatusCode.Conflict);
            await using var verify = new PortalDbContext(options);
            Assert.Equal(1, await verify.ConnectedPlayerProfiles.CountAsync(p => p.PlayerId == player.PlayerId && p.IsActive));
        });
}
