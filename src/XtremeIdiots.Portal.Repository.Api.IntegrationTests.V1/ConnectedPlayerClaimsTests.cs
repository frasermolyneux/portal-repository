using System.Net;
using System.Net.Http.Json;
using MX.Api.Abstractions;
using Newtonsoft.Json;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class ConnectedPlayerClaimsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Submit_HttpResponseIncludesServerAndDoesNotLink()
    {
        var user = Guid.NewGuid();
        var player = Guid.NewGuid();
        var server = Guid.NewGuid();
        var request = Guid.NewGuid();
        factory.SeedDatabase(db =>
        {
            db.UserProfiles.Add(new UserProfile { UserProfileId = user });
            db.Players.Add(new Player { PlayerId = player, Username = "Player" });
            db.GameServers.Add(new GameServer { GameServerId = server, Title = "Test server", Hostname = "localhost", BanFileRootPath = "/" });
            db.ConnectedPlayerActivationCodes.Add(new ConnectedPlayerActivationCode
            {
                ConnectedPlayerActivationCodeId = Guid.NewGuid(), UserProfileId = user, Code = "ABC123", CodeHash = "hash",
                ActivatedBy = "ApprovalRequired", IsActive = true, ActivatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(1), MaxAttempts = 5
            });
            db.SaveChanges();
        });
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/v1.0/connected-players/claims",
            new { requestId = request, playerId = player, gameServerId = server, code = "ABC123" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var claim = JsonConvert.DeserializeObject<ApiResponse<ConnectedPlayerClaimDto>>(await response.Content.ReadAsStringAsync())!.Data!;
        Assert.Equal(request, claim.ClaimId);
        Assert.Equal(server, claim.GameServerId);
        Assert.Equal("Test server", claim.GameServerName);
        Assert.Equal("Pending", claim.Status);
        factory.SeedDatabase(db => Assert.Empty(db.ConnectedPlayerProfiles));
    }
}
