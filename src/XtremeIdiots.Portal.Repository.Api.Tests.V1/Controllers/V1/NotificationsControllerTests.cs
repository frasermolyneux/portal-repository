using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class NotificationsControllerTests
{
    private NotificationsController CreateController(PortalDbContext context)
    {
        return new NotificationsController(context);
    }

    [Fact]
    public async Task GetNotifications_ReturnsNotificationsForUser()
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
        context.Notifications.Add(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            Title = "Test Notification",
            Message = "Test message",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var controller = CreateController(context);
        var api = (INotificationsApi)controller;
        var result = await api.GetNotifications(userProfileId, null, 0, 20, null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result);
        Assert.Single(result.Result.Data!.Items!);
    }

    [Fact]
    public async Task GetNotifications_UnreadOnly_FiltersCorrectly()
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
        context.Notifications.Add(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            Title = "Unread Notification",
            Message = "Unread message",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        context.Notifications.Add(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            Title = "Read Notification",
            Message = "Read message",
            IsRead = true,
            ReadAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var controller = CreateController(context);
        var api = (INotificationsApi)controller;
        var result = await api.GetNotifications(userProfileId, true, 0, 20, null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result);
        Assert.Single(result.Result.Data!.Items!);
        Assert.Equal("Unread Notification", result.Result.Data!.Items!.First().Title);
    }

    [Fact]
    public async Task GetUnreadNotificationCount_ReturnsCorrectCount()
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
        context.Notifications.Add(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            Title = "Unread 1",
            Message = "Message 1",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        context.Notifications.Add(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            Title = "Unread 2",
            Message = "Message 2",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        context.Notifications.Add(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            Title = "Read",
            Message = "Message 3",
            IsRead = true,
            ReadAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var controller = CreateController(context);
        var api = (INotificationsApi)controller;
        var result = await api.GetUnreadNotificationCount(userProfileId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(2, result.Result!.Data);
    }

    [Fact]
    public async Task CreateNotification_CreatesEntity()
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

        var dto = new CreateNotificationDto(userProfileId, "test-type", "New Notification", "New message");

        // Act
        var controller = CreateController(context);
        var api = (INotificationsApi)controller;
        var result = await api.CreateNotification(dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.Notifications);
    }

    [Fact]
    public async Task MarkNotificationAsRead_WithValidId_ReturnsOk()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
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
        context.Notifications.Add(new Notification
        {
            NotificationId = notificationId,
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            Title = "Test Notification",
            Message = "Test message",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var controller = CreateController(context);
        var api = (INotificationsApi)controller;
        var result = await api.MarkNotificationAsRead(notificationId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var notification = context.Notifications.First();
        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAt);
    }

    [Fact]
    public async Task MarkNotificationAsRead_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();

        // Act
        var controller = CreateController(context);
        var api = (INotificationsApi)controller;
        var result = await api.MarkNotificationAsRead(Guid.NewGuid());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task MarkAllNotificationsAsRead_MarksAllForUser()
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
        context.Notifications.Add(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            Title = "Notification 1",
            Message = "Message 1",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        context.Notifications.Add(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserProfileId = userProfileId,
            NotificationTypeId = "test-type",
            Title = "Notification 2",
            Message = "Message 2",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Act
        var controller = CreateController(context);
        var api = (INotificationsApi)controller;
        var result = await api.MarkAllNotificationsAsRead(userProfileId);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.All(context.Notifications, n =>
        {
            Assert.True(n.IsRead);
            Assert.NotNull(n.ReadAt);
        });
    }

    [Fact]
    public async Task CreateNotification_WithInvalidUserProfileId_ReturnsBadRequest()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();
        context.NotificationTypes.Add(new NotificationType
        {
            NotificationTypeId = "test-type",
            DisplayName = "Test Type",
            Description = "Test description",
            Category = "General",
            DefaultChannels = "[\"InSite\"]",
            IsEnabled = true,
            SupportsInSite = true,
            SupportsEmail = true,
            SortOrder = 1
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (INotificationsApi)controller;
        var dto = new CreateNotificationDto(Guid.NewGuid(), "test-type", "Title", "Message");

        // Act
        var result = await api.CreateNotification(dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreateNotification_WithInvalidNotificationTypeId_ReturnsBadRequest()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();
        var userProfileId = Guid.NewGuid();
        context.UserProfiles.Add(new UserProfile { UserProfileId = userProfileId });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (INotificationsApi)controller;
        var dto = new CreateNotificationDto(userProfileId, "nonexistent-type", "Title", "Message");

        // Act
        var result = await api.CreateNotification(dto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
    }
}
