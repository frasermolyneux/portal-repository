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
    private static BanFileMonitorsController CreateController(PortalDbContext context)
    {
        return new BanFileMonitorsController(context);
    }

    private static GameServer CreateGameServer(PortalDbContext context, Guid? id = null)
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
            GameServerId = gs.GameServerId
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
            GameServerId = gs.GameServerId
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;
        var result = await api.GetBanFileMonitors(null, null, null, 0, 20, null);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task UpsertBanFileMonitorStatus_WithNoExistingRow_CreatesRow()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateGameServer(context);
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;

        var dto = new UpsertBanFileMonitorStatusDto(gs.GameServerId)
        {
            LastCheckUtc = DateTime.UtcNow,
            LastCheckResult = "Success",
            RemoteFilePath = "/mods/xi_sniper/ban.txt",
            ResolvedForMod = "xi_sniper",
            RemoteFileSize = 1024,
            ConsecutiveFailureCount = 0
        };

        var result = await api.UpsertBanFileMonitorStatus(dto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.BanFileMonitors);
        var saved = context.BanFileMonitors.Single();
        Assert.Equal("/mods/xi_sniper/ban.txt", saved.RemoteFilePath);
        Assert.Equal("xi_sniper", saved.ResolvedForMod);
        Assert.Equal(1024, saved.RemoteFileSize);
    }

    [Fact]
    public async Task UpsertBanFileMonitorStatus_WithExistingRow_UpdatesInPlace()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateGameServer(context);
        var bfmId = Guid.NewGuid();
        context.BanFileMonitors.Add(new BanFileMonitor
        {
            BanFileMonitorId = bfmId,
            GameServerId = gs.GameServerId,
            LastCheckUtc = DateTime.UtcNow.AddHours(-1),
            LastCheckResult = "FtpError"
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;

        var dto = new UpsertBanFileMonitorStatusDto(gs.GameServerId)
        {
            LastCheckUtc = DateTime.UtcNow,
            LastCheckResult = "Success",
            RemoteFileSize = 2048
        };

        var result = await api.UpsertBanFileMonitorStatus(dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Single(context.BanFileMonitors);
        var saved = context.BanFileMonitors.Single();
        Assert.Equal(bfmId, saved.BanFileMonitorId);
        Assert.Equal("Success", saved.LastCheckResult);
        Assert.Equal(2048, saved.RemoteFileSize);
    }

    [Fact]
    public async Task UpsertBanFileMonitorStatus_PartialUpdate_PreservesUnsetFields()
    {
        // A check-only cycle (no push, no import) should not blank out the previously
        // recorded push fields — the partial-update contract.
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateGameServer(context);
        var bfmId = Guid.NewGuid();
        var pastPush = DateTime.UtcNow.AddHours(-2);
        context.BanFileMonitors.Add(new BanFileMonitor
        {
            BanFileMonitorId = bfmId,
            GameServerId = gs.GameServerId,
            LastPushUtc = pastPush,
            LastPushedEtag = "etag-1",
            LastPushedSize = 4096
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;

        var dto = new UpsertBanFileMonitorStatusDto(gs.GameServerId)
        {
            LastCheckUtc = DateTime.UtcNow,
            LastCheckResult = "Success",
            RemoteFileSize = 4096
            // Deliberately omit any push fields — they should be preserved.
        };

        var result = await api.UpsertBanFileMonitorStatus(dto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var saved = context.BanFileMonitors.Single();
        Assert.Equal(pastPush, saved.LastPushUtc);
        Assert.Equal("etag-1", saved.LastPushedEtag);
        Assert.Equal(4096, saved.LastPushedSize);
    }

    [Fact]
    public async Task UpsertBanFileMonitorStatus_WithUnknownGameServer_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;

        var dto = new UpsertBanFileMonitorStatusDto(Guid.NewGuid())
        {
            LastCheckUtc = DateTime.UtcNow,
            LastCheckResult = "Success"
        };

        var result = await api.UpsertBanFileMonitorStatus(dto);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task UpsertBanFileMonitorStatus_WithDeletedGameServer_ReturnsNotFound()
    {
        // Sanity: the agent should not be able to upsert for a soft-deleted game server.
        using var context = DbContextHelper.CreateInMemoryContext();
        var gs = CreateGameServer(context);
        gs.Deleted = true;
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (IBanFileMonitorsApi)controller;

        var dto = new UpsertBanFileMonitorStatusDto(gs.GameServerId)
        {
            LastCheckUtc = DateTime.UtcNow,
            LastCheckResult = "Success"
        };

        var result = await api.UpsertBanFileMonitorStatus(dto);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
