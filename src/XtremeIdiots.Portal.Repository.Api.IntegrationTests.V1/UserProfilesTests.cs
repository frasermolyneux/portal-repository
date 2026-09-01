using System.Net;

using MX.Api.Abstractions;
using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class UserProfilesTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserProfilesTests(CustomWebApplicationFactory factory)
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
    public async Task GetUserProfiles_WithModeratorsFilterAndGameType_ReturnsOnlyMatchingGameModeratorsWithCorrectPagination()
    {
        var cod5ModeratorId = Guid.NewGuid();
        var cod4ModeratorId = Guid.NewGuid();

        _factory.SeedDatabase(ctx =>
        {
            ctx.UserProfiles.Add(new UserProfile
            {
                UserProfileId = cod5ModeratorId,
                DisplayName = "IntegrationCod5Moderator",
                UserProfileClaims =
                [
                    new UserProfileClaim
                    {
                        UserProfileClaimId = Guid.NewGuid(),
                        UserProfileId = cod5ModeratorId,
                        ClaimType = UserProfileClaimType.Moderator,
                        ClaimValue = GameType.CallOfDuty5.ToString(),
                        SystemGenerated = true
                    }
                ]
            });

            ctx.UserProfiles.Add(new UserProfile
            {
                UserProfileId = cod4ModeratorId,
                DisplayName = "IntegrationCod4Moderator",
                UserProfileClaims =
                [
                    new UserProfileClaim
                    {
                        UserProfileClaimId = Guid.NewGuid(),
                        UserProfileId = cod4ModeratorId,
                        ClaimType = UserProfileClaimType.Moderator,
                        ClaimValue = GameType.CallOfDuty4.ToString(),
                        SystemGenerated = true
                    }
                ]
            });

            ctx.SaveChanges();
        });

        var response = await _client.GetAsync("/v1.0/user-profiles?filter=Moderators&gameType=CallOfDuty5&filterString=IntegrationCod5&skipEntries=0&takeEntries=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<CollectionModel<UserProfileDto>>>(content);

        Assert.NotNull(result?.Data?.Items);
        var items = result.Data.Items.ToList();
        Assert.Single(items);
        Assert.Equal(cod5ModeratorId, items[0].UserProfileId);

        Assert.NotNull(result.Pagination);
        Assert.Equal(1, result.Pagination.FilteredCount);
        Assert.Equal(0, result.Pagination.Skip);
        Assert.Equal(50, result.Pagination.Top);
    }

    [Fact]
    public async Task GetUserProfiles_WithGameTypeFilter_ComposesWithPaginationQueryParameters()
    {
        for (var i = 0; i < 3; i++)
        {
            var profileId = Guid.NewGuid();
            _factory.SeedDatabase(ctx =>
            {
                ctx.UserProfiles.Add(new UserProfile
                {
                    UserProfileId = profileId,
                    DisplayName = $"IntegrationPaginationModerator{i}",
                    UserProfileClaims =
                    [
                        new UserProfileClaim
                        {
                            UserProfileClaimId = Guid.NewGuid(),
                            UserProfileId = profileId,
                            ClaimType = UserProfileClaimType.Moderator,
                            ClaimValue = GameType.Insurgency.ToString(),
                            SystemGenerated = true
                        }
                    ]
                });
                ctx.SaveChanges();
            });
        }

        var response = await _client.GetAsync("/v1.0/user-profiles?filter=Moderators&gameType=Insurgency&order=DisplayNameAsc&skipEntries=1&takeEntries=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<CollectionModel<UserProfileDto>>>(content);

        Assert.NotNull(result?.Data?.Items);
        var items = result.Data.Items.ToList();
        Assert.Single(items);
        Assert.Equal("IntegrationPaginationModerator1", items[0].DisplayName);

        Assert.NotNull(result.Pagination);
        Assert.Equal(3, result.Pagination.FilteredCount);
    }

    [Fact]
    public async Task GetPermissionsReport_WithGameType_ReturnsGameAndServerScopedClaimsExcludingOtherGames()
    {
        var cod5ServerId = Guid.NewGuid();
        var cod4ServerId = Guid.NewGuid();
        var profileId = Guid.NewGuid();

        _factory.SeedDatabase(ctx =>
        {
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = cod5ServerId,
                Title = "IntegrationCod5Server",
                GameType = (int)GameType.CallOfDuty5,
                Hostname = "127.0.0.1",
                QueryPort = 28960
            });

            ctx.GameServers.Add(new GameServer
            {
                GameServerId = cod4ServerId,
                Title = "IntegrationCod4Server",
                GameType = (int)GameType.CallOfDuty4,
                Hostname = "127.0.0.1",
                QueryPort = 28961
            });

            ctx.UserProfiles.Add(new UserProfile
            {
                UserProfileId = profileId,
                DisplayName = "IntegrationPermissionsUser",
                UserProfileClaims =
                [
                    new UserProfileClaim
                    {
                        UserProfileClaimId = Guid.NewGuid(),
                        UserProfileId = profileId,
                        ClaimType = AdditionalPermission.Maps_Read,
                        ClaimValue = GameType.CallOfDuty5.ToString(),
                        SystemGenerated = false
                    },
                    new UserProfileClaim
                    {
                        UserProfileClaimId = Guid.NewGuid(),
                        UserProfileId = profileId,
                        ClaimType = AdditionalPermission.GameServers_Read,
                        ClaimValue = cod5ServerId.ToString(),
                        SystemGenerated = false
                    },
                    new UserProfileClaim
                    {
                        UserProfileClaimId = Guid.NewGuid(),
                        UserProfileId = profileId,
                        ClaimType = AdditionalPermission.GameServers_Read,
                        ClaimValue = cod4ServerId.ToString(),
                        SystemGenerated = false
                    }
                ]
            });

            ctx.SaveChanges();
        });

        var response = await _client.GetAsync("/v1.0/user-profile/permissions-report?gameType=CallOfDuty5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ApiResponse<CollectionModel<PermissionReportEntryDto>>>(content);

        Assert.NotNull(result?.Data?.Items);
        var claimValues = result.Data.Items
            .Where(i => i.UserProfileId == profileId)
            .Select(i => i.ClaimValue)
            .ToList();

        Assert.Equal(2, claimValues.Count);
        Assert.Contains(GameType.CallOfDuty5.ToString(), claimValues);
        Assert.Contains(cod5ServerId.ToString(), claimValues);
        Assert.DoesNotContain(cod4ServerId.ToString(), claimValues);
    }
}
