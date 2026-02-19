using System.Net;
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
}
