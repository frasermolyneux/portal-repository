using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class UserProfileControllerTests
{
    private UserProfileController CreateController(PortalDbContext context)
    {
        return new UserProfileController(context);
    }

    [Fact]
    public async Task GetUserProfile_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "TestUser"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfile(userProfileId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetUserProfile_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfile(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetUserProfileByIdentityId_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var identityOid = "test-identity-oid";
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = Guid.NewGuid(),
            IdentityOid = identityOid,
            DisplayName = "TestUser"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfileByIdentityId(identityOid);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetUserProfileByIdentityId_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfileByIdentityId("nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetUserProfileByXtremeIdiotsId_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var forumId = "12345";
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = Guid.NewGuid(),
            XtremeIdiotsForumId = forumId,
            DisplayName = "TestUser"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfileByXtremeIdiotsId(forumId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetUserProfileByDemoAuthKey_WithValidKey_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var demoKey = "auth-key-123";
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = Guid.NewGuid(),
            DemoAuthKey = demoKey,
            DisplayName = "TestUser"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfileByDemoAuthKey(demoKey);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetUserProfileByDemoAuthKey_WithInvalidKey_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfileByDemoAuthKey("nonexistent");

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetUserProfiles_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = Guid.NewGuid(),
            DisplayName = "User1"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfiles(null, null, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetUserProfiles_WithWebmastersFilter_ReturnsOnlyWebmasters()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var webmasterProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = webmasterProfileId,
            DisplayName = "Webmaster",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = webmasterProfileId,
                    ClaimType = UserProfileClaimType.Webmaster,
                    ClaimValue = GameType.Unknown.ToString(),
                    SystemGenerated = true
                }
            ]
        });

        var seniorAdminProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = seniorAdminProfileId,
            DisplayName = "SeniorAdmin",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = seniorAdminProfileId,
                    ClaimType = UserProfileClaimType.SeniorAdmin,
                    ClaimValue = GameType.Unknown.ToString(),
                    SystemGenerated = true
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfiles(null, UserProfileFilter.Webmasters, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal(webmasterProfileId, items[0].UserProfileId);
    }

    [Fact]
    public async Task GetUserProfiles_WithAnyAdminFilter_IncludesWebmasters()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var webmasterProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = webmasterProfileId,
            DisplayName = "Webmaster",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = webmasterProfileId,
                    ClaimType = UserProfileClaimType.Webmaster,
                    ClaimValue = GameType.Unknown.ToString(),
                    SystemGenerated = true
                }
            ]
        });

        var registeredUserProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = registeredUserProfileId,
            DisplayName = "RegisteredUser",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = registeredUserProfileId,
                    ClaimType = UserProfileClaimType.RegisteredUser,
                    ClaimValue = GameType.Unknown.ToString(),
                    SystemGenerated = true
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfiles(null, UserProfileFilter.AnyAdmin, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal(webmasterProfileId, items[0].UserProfileId);
    }

    [Fact]
    public async Task GetUserProfiles_WithModeratorsFilterAndGameType_ReturnsOnlyMatchingGameModerators()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var cod5ModeratorId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = cod5ModeratorId,
            DisplayName = "Cod5Moderator",
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

        var cod4ModeratorId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = cod4ModeratorId,
            DisplayName = "Cod4Moderator",
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
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.CallOfDuty5, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal(cod5ModeratorId, items[0].UserProfileId);
    }

    [Fact]
    public async Task GetUserProfiles_WithModeratorsFilterAndGameType_ExcludesUnrelatedGamePermissionOnSameProfile()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        // Profile is a COD4 moderator with an unrelated COD5 additional permission claim.
        // It must not be returned as a COD5 moderator - the role and game must match on the same claim.
        var profileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = profileId,
            DisplayName = "Cod4ModeratorWithCod5Permission",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = profileId,
                    ClaimType = UserProfileClaimType.Moderator,
                    ClaimValue = GameType.CallOfDuty4.ToString(),
                    SystemGenerated = true
                },
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = profileId,
                    ClaimType = AdditionalPermission.Maps_Read,
                    ClaimValue = GameType.CallOfDuty5.ToString(),
                    SystemGenerated = false
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.CallOfDuty5, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Empty(items);
    }

    [Fact]
    public async Task GetUserProfiles_WithModeratorsFilterAndGameType_ReturnsProfileForEachMatchingGame()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var profileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = profileId,
            DisplayName = "MultiGameModerator",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = profileId,
                    ClaimType = UserProfileClaimType.Moderator,
                    ClaimValue = GameType.CallOfDuty4.ToString(),
                    SystemGenerated = true
                },
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = profileId,
                    ClaimType = UserProfileClaimType.Moderator,
                    ClaimValue = GameType.CallOfDuty5.ToString(),
                    SystemGenerated = true
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;

        var cod4Result = await api.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.CallOfDuty4, 0, 50, null);
        var cod5Result = await api.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.CallOfDuty5, 0, 50, null);

        Assert.Single(cod4Result.Result!.Data!.Items!);
        Assert.Single(cod5Result.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GetUserProfiles_WithGameTypeFilter_ComposesWithSearchText()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var matchingProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = matchingProfileId,
            DisplayName = "SearchableCod5Moderator",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = matchingProfileId,
                    ClaimType = UserProfileClaimType.Moderator,
                    ClaimValue = GameType.CallOfDuty5.ToString(),
                    SystemGenerated = true
                }
            ]
        });

        var nonMatchingNameProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = nonMatchingNameProfileId,
            DisplayName = "OtherCod5Moderator",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = nonMatchingNameProfileId,
                    ClaimType = UserProfileClaimType.Moderator,
                    ClaimValue = GameType.CallOfDuty5.ToString(),
                    SystemGenerated = true
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfiles("Searchable", UserProfileFilter.Moderators, GameType.CallOfDuty5, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal(matchingProfileId, items[0].UserProfileId);
    }

    [Fact]
    public async Task GetUserProfiles_WithGameTypeFilter_CalculatesFilteredCountOrderingAndPaginationAfterGameFiltering()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        for (var i = 0; i < 3; i++)
        {
            var profileId = Guid.NewGuid();
            context.UserProfiles.Add(new UserProfile
            {
                UserProfileId = profileId,
                DisplayName = $"Cod5Moderator{i}",
                UserProfileClaims =
                [
                    new UserProfileClaim
                    {
                        UserProfileClaimId = Guid.NewGuid(),
                        UserProfileId = profileId,
                        ClaimType = UserProfileClaimType.Moderator,
                        ClaimValue = GameType.CallOfDuty5.ToString(),
                        SystemGenerated = true
                    }
                ]
            });
        }

        var cod4ModeratorId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = cod4ModeratorId,
            DisplayName = "Cod4Moderator",
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
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.CallOfDuty5, 0, 2, UserProfilesOrder.DisplayNameAsc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(4, result.Result!.Pagination!.TotalCount);
        Assert.Equal(3, result.Result!.Pagination!.FilteredCount);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("Cod5Moderator0", items[0].DisplayName);
        Assert.Equal("Cod5Moderator1", items[1].DisplayName);
    }

    [Fact]
    public async Task GetUserProfiles_WithoutGameType_RetainsExistingBehaviour()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var cod5ModeratorId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = cod5ModeratorId,
            DisplayName = "Cod5Moderator",
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

        var cod4ModeratorId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = cod4ModeratorId,
            DisplayName = "Cod4Moderator",
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
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;

        var viaOldOverload = await api.GetUserProfiles(null, UserProfileFilter.Moderators, 0, 50, null);
        var viaNewOverloadNoGameType = await api.GetUserProfiles(null, UserProfileFilter.Moderators, null, 0, 50, null);

        Assert.Equal(2, viaOldOverload.Result!.Data!.Items!.ToList().Count);
        Assert.Equal(2, viaNewOverloadNoGameType.Result!.Data!.Items!.ToList().Count);
    }

    [Fact]
    public async Task GetUserProfiles_WithUnknownGameType_IsTreatedAsNoGameFilter()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var cod5ModeratorId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = cod5ModeratorId,
            DisplayName = "Cod5Moderator",
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

        var cod4ModeratorId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = cod4ModeratorId,
            DisplayName = "Cod4Moderator",
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
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.Unknown, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data!.Items!.ToList().Count);
    }

    [Fact]
    public async Task GetUserProfiles_WithAnyAdminFilterAndGameType_MatchesGlobalAndGameScopedClaims()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var webmasterProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = webmasterProfileId,
            DisplayName = "Webmaster",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = webmasterProfileId,
                    ClaimType = UserProfileClaimType.Webmaster,
                    ClaimValue = GameType.Unknown.ToString(),
                    SystemGenerated = true
                }
            ]
        });

        var cod5ModeratorId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = cod5ModeratorId,
            DisplayName = "Cod5Moderator",
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

        var cod4ModeratorId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = cod4ModeratorId,
            DisplayName = "Cod4Moderator",
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
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetUserProfiles(null, UserProfileFilter.AnyAdmin, GameType.CallOfDuty5, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var ids = result.Result!.Data!.Items!.Select(i => i.UserProfileId).ToList();
        Assert.Equal(2, ids.Count);
        Assert.Contains(webmasterProfileId, ids);
        Assert.Contains(cod5ModeratorId, ids);
        Assert.DoesNotContain(cod4ModeratorId, ids);
    }

    [Fact]
    public async Task GetPermissionsReport_WithGameType_IncludesGameAndMatchingServerScopedClaimsExcludesOthers()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var cod5ServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = cod5ServerId,
            Title = "Cod5Server",
            GameType = (int)GameType.CallOfDuty5,
            Hostname = "localhost",
            QueryPort = 28960
        });

        var cod4ServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = cod4ServerId,
            Title = "Cod4Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28961
        });
        await context.SaveChangesAsync();

        var orphanedServerId = Guid.NewGuid();

        var profileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = profileId,
            DisplayName = "PermissionsUser",
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
                },
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = profileId,
                    ClaimType = AdditionalPermission.GameServers_Read,
                    ClaimValue = orphanedServerId.ToString(),
                    SystemGenerated = false
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetPermissionsReport(GameType.CallOfDuty5, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var claimValues = result.Result!.Data!.Items!.Select(i => i.ClaimValue).ToList();
        Assert.Equal(2, claimValues.Count);
        Assert.Contains(GameType.CallOfDuty5.ToString(), claimValues);
        Assert.Contains(cod5ServerId.ToString(), claimValues);
        Assert.DoesNotContain(cod4ServerId.ToString(), claimValues);
        Assert.DoesNotContain(orphanedServerId.ToString(), claimValues);
    }

    [Fact]
    public async Task GetPermissionsReport_WithGameTypeAndClaimType_ComposesBothFilters()
    {
        using var context = DbContextHelper.CreateInMemoryContext();

        var cod5ServerId = Guid.NewGuid();
        context.GameServers.Add(new GameServer
        {
            GameServerId = cod5ServerId,
            Title = "Cod5Server",
            GameType = (int)GameType.CallOfDuty5,
            Hostname = "localhost",
            QueryPort = 28960
        });
        await context.SaveChangesAsync();

        var profileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = profileId,
            DisplayName = "PermissionsUser",
            UserProfileClaims =
            [
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
                    ClaimType = AdditionalPermission.Maps_Read,
                    ClaimValue = GameType.CallOfDuty5.ToString(),
                    SystemGenerated = false
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;
        var result = await api.GetPermissionsReport(GameType.CallOfDuty5, AdditionalPermission.GameServers_Read);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal(AdditionalPermission.GameServers_Read, items[0].ClaimType);
        Assert.Equal(cod5ServerId.ToString(), items[0].ClaimValue);
    }

    [Fact]
    public async Task CreateUserProfile_CreatesEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;

        var dto = new CreateUserProfileDto("forum-123", "NewUser", "test@test.com")
        {
            IdentityOid = "new-oid"
        };

        var result = await api.CreateUserProfile(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.UserProfiles);
    }

    [Fact]
    public async Task SetUserProfileClaims_WithNullBody_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);

        var result = await controller.SetUserProfileClaims(Guid.NewGuid(), null!);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateUserProfileClaim_WithNullBody_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);

        var result = await controller.CreateUserProfileClaim(Guid.NewGuid(), null!);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task CreateUserProfileClaim_WithDuplicateClaimTypeAndValueInRequest_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var userProfileId = Guid.NewGuid();
        var serverId = Guid.NewGuid();

        var claims = new List<CreateUserProfileClaimDto>
        {
            new(userProfileId, AdditionalPermission.MapRotations_Deploy, serverId.ToString(), false),
            new(userProfileId, AdditionalPermission.MapRotations_Deploy, serverId.ToString(), false)
        };

        var result = await controller.CreateUserProfileClaim(userProfileId, claims);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task SetUserProfileClaims_WithDuplicateClaimTypeAndValue_ReturnsBadRequest()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var userProfileId = Guid.NewGuid();

        var claims = new List<CreateUserProfileClaimDto>
        {
            new(userProfileId, UserProfileClaimType.HeadAdmin, GameType.CallOfDuty2.ToString(), true),
            new(userProfileId, UserProfileClaimType.HeadAdmin, GameType.CallOfDuty2.ToString(), true)
        };

        var result = await controller.SetUserProfileClaims(userProfileId, claims);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task SetUserProfileClaims_WithSameClaimTypeDifferentValues_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "TestUser"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var claims = new List<CreateUserProfileClaimDto>
        {
            new(userProfileId, UserProfileClaimType.HeadAdmin, GameType.CallOfDuty2.ToString(), true),
            new(userProfileId, UserProfileClaimType.HeadAdmin, GameType.CallOfDuty4.ToString(), true),
            new(userProfileId, UserProfileClaimType.HeadAdmin, GameType.CallOfDuty5.ToString(), true)
        };

        var result = await controller.SetUserProfileClaims(userProfileId, claims);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task SetUserProfileClaims_WithNonExistentUserProfile_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var userProfileId = Guid.NewGuid();

        var claims = new List<CreateUserProfileClaimDto>
        {
            new(userProfileId, UserProfileClaimType.XtremeIdiotsId, "12345", true)
        };

        var result = await controller.SetUserProfileClaims(userProfileId, claims);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task SetUserProfileClaims_WithValidClaims_SavesClaimsToDatabase()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "TestUser"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var claims = new List<CreateUserProfileClaimDto>
        {
            new(userProfileId, UserProfileClaimType.XtremeIdiotsId, "12345", true),
            new(userProfileId, UserProfileClaimType.HeadAdmin, GameType.CallOfDuty2.ToString(), true)
        };

        await controller.SetUserProfileClaims(userProfileId, claims);

        var savedClaims = context.UserProfileClaims.Where(c => c.UserProfileId == userProfileId).ToList();
        Assert.Equal(2, savedClaims.Count);
    }

    [Fact]
    public async Task SetUserProfileClaims_ReplacesExistingClaims()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "TestUser",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = userProfileId,
                    ClaimType = UserProfileClaimType.SeniorAdmin,
                    ClaimValue = GameType.Unknown.ToString(),
                    SystemGenerated = true
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var newClaims = new List<CreateUserProfileClaimDto>
        {
            new(userProfileId, UserProfileClaimType.HeadAdmin, GameType.CallOfDuty2.ToString(), true)
        };

        await controller.SetUserProfileClaims(userProfileId, newClaims);

        var savedClaims = context.UserProfileClaims.Where(c => c.UserProfileId == userProfileId).ToList();
        Assert.Single(savedClaims);
        Assert.Equal(UserProfileClaimType.HeadAdmin, savedClaims[0].ClaimType);
    }

    [Fact]
    public async Task SetUserProfileClaims_WithEmptyList_ClearsAllClaims()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "TestUser",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = userProfileId,
                    ClaimType = UserProfileClaimType.SeniorAdmin,
                    ClaimValue = GameType.Unknown.ToString(),
                    SystemGenerated = true
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.SetUserProfileClaims(userProfileId, []);

        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusCodeResult.StatusCode);
        Assert.Empty(context.UserProfileClaims.Where(c => c.UserProfileId == userProfileId));
    }

    [Fact]
    public async Task CreateUserProfileClaim_WithSameClaimTypeDifferentValues_AddsAdditionalClaim()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        var existingServerId = Guid.NewGuid();
        var newServerId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "TestUser",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = userProfileId,
                    ClaimType = AdditionalPermission.MapRotations_Deploy,
                    ClaimValue = existingServerId.ToString(),
                    SystemGenerated = false
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;

        var result = await api.CreateUserProfileClaim(userProfileId,
        [
            new CreateUserProfileClaimDto(userProfileId, AdditionalPermission.MapRotations_Deploy, newServerId.ToString(), false)
        ]);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);

        var claims = context.UserProfileClaims
            .Where(c => c.UserProfileId == userProfileId && c.ClaimType == AdditionalPermission.MapRotations_Deploy)
            .ToList();

        Assert.Equal(2, claims.Count);
        Assert.Contains(claims, c => c.ClaimValue == existingServerId.ToString());
        Assert.Contains(claims, c => c.ClaimValue == newServerId.ToString());
    }

    [Fact]
    public async Task CreateUserProfileClaim_WithDuplicateClaimTypeAndValue_DoesNotAddDuplicate()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        var existingServerId = Guid.NewGuid();

        context.UserProfiles.Add(new UserProfile
        {
            UserProfileId = userProfileId,
            DisplayName = "TestUser",
            UserProfileClaims =
            [
                new UserProfileClaim
                {
                    UserProfileClaimId = Guid.NewGuid(),
                    UserProfileId = userProfileId,
                    ClaimType = AdditionalPermission.MapRotations_Deploy,
                    ClaimValue = existingServerId.ToString(),
                    SystemGenerated = false
                }
            ]
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IUserProfileApi)controller;

        var result = await api.CreateUserProfileClaim(userProfileId,
        [
            new CreateUserProfileClaimDto(userProfileId, AdditionalPermission.MapRotations_Deploy, existingServerId.ToString(), false)
        ]);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);

        var claims = context.UserProfileClaims
            .Where(c => c.UserProfileId == userProfileId && c.ClaimType == AdditionalPermission.MapRotations_Deploy)
            .ToList();

        Assert.Single(claims);
    }
}
