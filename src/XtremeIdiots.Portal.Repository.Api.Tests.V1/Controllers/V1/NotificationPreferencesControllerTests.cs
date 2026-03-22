using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class NotificationPreferencesControllerTests
{
    private NotificationPreferencesController CreateController(PortalDbContext context)
    {
        return new NotificationPreferencesController(context);
    }

    [Fact]
    public async Task GetNotificationPreferences_ReturnsPreferencesForUser()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId });
        context.NotificationTypes.Add(new NotificationType
        {
            NotificationTypeId = "test-type",
            DisplayName = "Test Type",
            Description = "Test description",
            Category = "General",
            DefaultChannels = "InSite",
            IsEnabled = true,
            SortOrder = 1,
            SupportsInSite = true,
            SupportsEmail = true
        });
        context.NotificationPreferences.Add(new NotificationPreference
        {
            NotificationPreferenceId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            InSiteEnabled = true,
            EmailEnabled = false,
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var controller = CreateController(context);
        var api = (INotificationPreferencesApi)controller;
        var result = await api.GetNotificationPreferences(userProfileId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result);
        Assert.Single(result.Result.Data!.Items!);
    }

    [Fact]
    public async Task GetNotificationPreferences_NoPreferences_ReturnsEmptyCollection()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId });
        await context.SaveChangesAsync();

        // Act
        var controller = CreateController(context);
        var api = (INotificationPreferencesApi)controller;
        var result = await api.GetNotificationPreferences(userProfileId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result.Data!.Items!);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_CreatesNewPreferences()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId });
        context.NotificationTypes.Add(new NotificationType
        {
            NotificationTypeId = "test-type",
            DisplayName = "Test Type",
            Description = "Test description",
            Category = "General",
            DefaultChannels = "InSite",
            IsEnabled = true,
            SortOrder = 1,
            SupportsInSite = true,
            SupportsEmail = true
        });
        await context.SaveChangesAsync();

        var editDtos = new List<EditNotificationPreferenceDto>
        {
            new("test-type") { InSiteEnabled = true, EmailEnabled = false }
        };

        // Act
        var controller = CreateController(context);
        var api = (INotificationPreferencesApi)controller;
        var result = await api.UpdateNotificationPreferences(userProfileId, editDtos);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.NotificationPreferences);
        var preference = context.NotificationPreferences.First();
        Assert.True(preference.InSiteEnabled);
        Assert.False(preference.EmailEnabled);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_UpdatesExistingPreferences()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId });
        context.NotificationTypes.Add(new NotificationType
        {
            NotificationTypeId = "test-type",
            DisplayName = "Test Type",
            Description = "Test description",
            Category = "General",
            DefaultChannels = "InSite",
            IsEnabled = true,
            SortOrder = 1,
            SupportsInSite = true,
            SupportsEmail = true
        });
        context.NotificationPreferences.Add(new NotificationPreference
        {
            NotificationPreferenceId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            InSiteEnabled = true,
            EmailEnabled = true,
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var editDtos = new List<EditNotificationPreferenceDto>
        {
            new("test-type") { InSiteEnabled = false, EmailEnabled = false }
        };

        // Act
        var controller = CreateController(context);
        var api = (INotificationPreferencesApi)controller;
        var result = await api.UpdateNotificationPreferences(userProfileId, editDtos);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.NotificationPreferences);
        var preference = context.NotificationPreferences.First();
        Assert.False(preference.InSiteEnabled);
        Assert.False(preference.EmailEnabled);
    }
}
