using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class NotificationTypesControllerTests
{
    private NotificationTypesController CreateController(PortalDbContext context)
    {
        return new NotificationTypesController(context);
    }

    [Fact]
    public async Task GetNotificationTypes_ReturnsEnabledTypes()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();
        context.NotificationTypes.Add(new NotificationType
        {
            NotificationTypeId = "enabled-type-1",
            DisplayName = "Enabled Type 1",
            Description = "First enabled type",
            Category = "General",
            DefaultChannels = "InSite",
            IsEnabled = true,
            SortOrder = 1,
            SupportsInSite = true,
            SupportsEmail = true
        });
        context.NotificationTypes.Add(new NotificationType
        {
            NotificationTypeId = "enabled-type-2",
            DisplayName = "Enabled Type 2",
            Description = "Second enabled type",
            Category = "General",
            DefaultChannels = "InSite",
            IsEnabled = true,
            SortOrder = 2,
            SupportsInSite = true,
            SupportsEmail = false
        });
        context.NotificationTypes.Add(new NotificationType
        {
            NotificationTypeId = "disabled-type",
            DisplayName = "Disabled Type",
            Description = "A disabled type",
            Category = "General",
            DefaultChannels = "InSite",
            IsEnabled = false,
            SortOrder = 3,
            SupportsInSite = true,
            SupportsEmail = true
        });
        await context.SaveChangesAsync();

        // Act
        var controller = CreateController(context);
        var api = (INotificationTypesApi)controller;
        var result = await api.GetNotificationTypes();

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result);
        Assert.Equal(2, result.Result.Data!.Items!.Count());
    }

    [Fact]
    public async Task GetNotificationTypes_EmptyDatabase_ReturnsEmptyCollection()
    {
        // Arrange
        using var context = DbContextHelper.CreateInMemoryContext();

        // Act
        var controller = CreateController(context);
        var api = (INotificationTypesApi)controller;
        var result = await api.GetNotificationTypes();

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Result);
        Assert.Empty(result.Result.Data!.Items!);
    }
}
