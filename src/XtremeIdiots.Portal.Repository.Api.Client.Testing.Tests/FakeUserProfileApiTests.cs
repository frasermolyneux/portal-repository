using System.Net;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.UserProfiles;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

[Trait("Category", "Unit")]
public class FakeUserProfileApiTests
{
    [Fact]
    public async Task Archive_IsIdempotentExcludesDefaultsAndPreservesReservedIdentifiers()
    {
        var actor = RepositoryDtoFactory.CreateUserProfile();
        var target = RepositoryDtoFactory.CreateUserProfile(identityOid: "identity", xtremeIdiotsForumId: "forum", demoAuthKey: "key",
            claims: [RepositoryDtoFactory.CreateUserProfileClaim()]);
        var fake = new FakeUserProfileApi().AddUserProfile(actor).AddUserProfile(target);
        Assert.Equal(HttpStatusCode.BadRequest, (await fake.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, "  "))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fake.ArchiveUserProfile(new(actor.UserProfileId, actor.UserProfileId, "Self"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fake.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, "Original"))).StatusCode);
        var historical = (await fake.GetUserProfileIncludingArchived(target.UserProfileId)).Result!.Data!;
        Assert.Equal(HttpStatusCode.NoContent, (await fake.ArchiveUserProfile(new(target.UserProfileId, actor.UserProfileId, "Changed"))).StatusCode);
        Assert.Equal(historical, (await fake.GetUserProfileIncludingArchived(target.UserProfileId)).Result!.Data);
        Assert.Single(historical.UserProfileClaims);
        Assert.Null(historical.DemoAuthKey);
        Assert.Equal(HttpStatusCode.NotFound, (await fake.GetUserProfile(target.UserProfileId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fake.GetUserProfileByIdentityId("identity")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fake.GetUserProfileByXtremeIdiotsId("forum")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fake.GetUserProfileByDemoAuthKey("key")).StatusCode);
        var profiles = await fake.GetUserProfiles(null, null, 0, 50, null);
        Assert.Single(profiles.Result!.Data!.Items!);
        Assert.Equal(1, profiles.Result.Pagination!.TotalCount);
        Assert.Equal(HttpStatusCode.Conflict, (await fake.CreateUserProfile(new("forum", "Name", "email"))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fake.CreateUserProfiles([new("other-forum", "Name", "email") { IdentityOid = "identity" }])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fake.UpdateUserProfile(new(target.UserProfileId))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fake.UpdateUserProfiles([new(target.UserProfileId)])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fake.CreateUserProfileClaim(target.UserProfileId, [])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fake.SetUserProfileClaims(target.UserProfileId, [])).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await fake.DeleteUserProfileClaim(target.UserProfileId, Guid.NewGuid())).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fake.ArchiveUserProfile(new(actor.UserProfileId, target.UserProfileId, "Archived actor"))).StatusCode);
    }

    [Fact]
    public async Task GetUserProfiles_WithModeratorsFilterAndGameType_ReturnsOnlyMatchingGameModerators()
    {
        var fake = new FakeUserProfileApi();

        var cod5Moderator = RepositoryDtoFactory.CreateUserProfile(displayName: "Cod5Moderator", claims:
        [
            RepositoryDtoFactory.CreateUserProfileClaim(systemGenerated: true, claimType: UserProfileClaimType.Moderator, claimValue: GameType.CallOfDuty5.ToString())
        ]);
        var cod4Moderator = RepositoryDtoFactory.CreateUserProfile(displayName: "Cod4Moderator", claims:
        [
            RepositoryDtoFactory.CreateUserProfileClaim(systemGenerated: true, claimType: UserProfileClaimType.Moderator, claimValue: GameType.CallOfDuty4.ToString())
        ]);

        fake.AddUserProfile(cod5Moderator).AddUserProfile(cod4Moderator);

        var result = await fake.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.CallOfDuty5, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Single(items);
        Assert.Equal(cod5Moderator.UserProfileId, items[0].UserProfileId);
    }

    [Fact]
    public async Task GetUserProfiles_WithModeratorsFilterAndGameType_ExcludesUnrelatedGamePermissionOnSameProfile()
    {
        var fake = new FakeUserProfileApi();

        var profile = RepositoryDtoFactory.CreateUserProfile(displayName: "Cod4ModeratorWithCod5Permission", claims:
        [
            RepositoryDtoFactory.CreateUserProfileClaim(systemGenerated: true, claimType: UserProfileClaimType.Moderator, claimValue: GameType.CallOfDuty4.ToString()),
            RepositoryDtoFactory.CreateUserProfileClaim(systemGenerated: false, claimType: AdditionalPermission.Maps_Read, claimValue: GameType.CallOfDuty5.ToString())
        ]);

        fake.AddUserProfile(profile);

        var result = await fake.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.CallOfDuty5, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(result.Result!.Data!.Items!);
    }

    [Fact]
    public async Task GetUserProfiles_WithoutGameType_RetainsExistingBehaviour()
    {
        var fake = new FakeUserProfileApi();

        var cod5Moderator = RepositoryDtoFactory.CreateUserProfile(displayName: "Cod5Moderator", claims:
        [
            RepositoryDtoFactory.CreateUserProfileClaim(systemGenerated: true, claimType: UserProfileClaimType.Moderator, claimValue: GameType.CallOfDuty5.ToString())
        ]);
        var cod4Moderator = RepositoryDtoFactory.CreateUserProfile(displayName: "Cod4Moderator", claims:
        [
            RepositoryDtoFactory.CreateUserProfileClaim(systemGenerated: true, claimType: UserProfileClaimType.Moderator, claimValue: GameType.CallOfDuty4.ToString())
        ]);

        fake.AddUserProfile(cod5Moderator).AddUserProfile(cod4Moderator);

        var viaOldOverload = await fake.GetUserProfiles(null, UserProfileFilter.Moderators, 0, 50, null);

        Assert.Equal(HttpStatusCode.OK, viaOldOverload.StatusCode);
        Assert.Equal(2, viaOldOverload.Result!.Data!.Items!.ToList().Count);
    }

    [Fact]
    public async Task GetUserProfiles_SupportsSearchOrderingAndPaginationSemantics()
    {
        var fake = new FakeUserProfileApi();

        for (var i = 0; i < 3; i++)
        {
            fake.AddUserProfile(RepositoryDtoFactory.CreateUserProfile(displayName: $"Cod5Moderator{i}", claims:
            [
                RepositoryDtoFactory.CreateUserProfileClaim(systemGenerated: true, claimType: UserProfileClaimType.Moderator, claimValue: GameType.CallOfDuty5.ToString())
            ]));
        }

        fake.AddUserProfile(RepositoryDtoFactory.CreateUserProfile(displayName: "Cod4Moderator", claims:
        [
            RepositoryDtoFactory.CreateUserProfileClaim(systemGenerated: true, claimType: UserProfileClaimType.Moderator, claimValue: GameType.CallOfDuty4.ToString())
        ]));

        var result = await fake.GetUserProfiles(null, UserProfileFilter.Moderators, GameType.CallOfDuty5, 0, 2, UserProfilesOrder.DisplayNameAsc);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(4, result.Result!.Pagination!.TotalCount);
        Assert.Equal(3, result.Result!.Pagination!.FilteredCount);
        var items = result.Result!.Data!.Items!.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("Cod5Moderator0", items[0].DisplayName);
        Assert.Equal("Cod5Moderator1", items[1].DisplayName);
    }

    [Fact]
    public async Task Reset_ClearsAllState()
    {
        var fake = new FakeUserProfileApi();
        fake.AddUserProfile(RepositoryDtoFactory.CreateUserProfile());

        fake.Reset();

        var result = await fake.GetUserProfiles(null, null, 0, 50, null);
        Assert.Empty(result.Result!.Data!.Items!);
    }
}
