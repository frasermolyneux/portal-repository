using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.BanFileMonitors;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class BanFileMonitorsControllerTests
{
    private BanFileMonitorsController CreateController(PortalDbContext context)
    {
        return new BanFileMonitorsController(context);
    }

    private GameServer CreateGameServer(PortalDbContext context, Guid? id = null)
    {
        var gs = new GameServer
        {
            GameServerId = id ?? Guid.NewGuid(),
            Title = "Test Server",
            GameType = (int)GameType.CallOfDuty4,
            Hostname = "localhost",
            QueryPort = 28960,
            Deleted = false
        };
        context.GameServers.Add(gs);
        return gs;
    }

    [Fact]
    public async Task GetBanFileMonitor_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateGameServer(context);
        var bfmId = Guid.NewGuid();
        context.BanFileMonitors.Add(new BanFileMonitor
        {
            BanFileMonitorId = bfmId,
            GameServerId = gs.GameServerId,
            FilePath = "/path/to/file"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;
        var result = await api.GetBanFileMonitor(bfmId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetBanFileMonitor_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;
        var result = await api.GetBanFileMonitor(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetBanFileMonitors_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateGameServer(context);
        context.BanFileMonitors.Add(new BanFileMonitor
        {
            BanFileMonitorId = Guid.NewGuid(),
            GameServerId = gs.GameServerId,
            FilePath = "/path1"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;
        var result = await api.GetBanFileMonitors(null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateBanFileMonitor_CreatesEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateGameServer(context);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;

        var dto = new CreateBanFileMonitorDto(gs.GameServerId, "/new/path", GameType.CallOfDuty4);

        var result = await api.CreateBanFileMonitor(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.BanFileMonitors);
    }

    [Fact]
    public async Task UpdateBanFileMonitor_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateGameServer(context);
        var bfmId = Guid.NewGuid();
        context.BanFileMonitors.Add(new BanFileMonitor
        {
            BanFileMonitorId = bfmId,
            GameServerId = gs.GameServerId,
            FilePath = "/original"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;

        var editDto = new EditBanFileMonitorDto(bfmId, "/updated");

        var result = await api.UpdateBanFileMonitor(editDto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task UpdateBanFileMonitor_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;

        var editDto = new EditBanFileMonitorDto(Guid.NewGuid(), "/updated");

        var result = await api.UpdateBanFileMonitor(editDto);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task DeleteBanFileMonitor_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateGameServer(context);
        var bfmId = Guid.NewGuid();
        context.BanFileMonitors.Add(new BanFileMonitor
        {
            BanFileMonitorId = bfmId,
            GameServerId = gs.GameServerId,
            FilePath = "/delete"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;
        var result = await api.DeleteBanFileMonitor(bfmId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(context.BanFileMonitors);
    }

    [Fact]
    public async Task DeleteBanFileMonitor_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;
        var result = await api.DeleteBanFileMonitor(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
