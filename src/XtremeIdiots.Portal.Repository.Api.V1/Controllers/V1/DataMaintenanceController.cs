using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using Asp.Versioning;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class DataMaintenanceController : ControllerBase, IDataMaintenanceApi
{
    private readonly PortalDbContext context;

    public DataMaintenanceController(PortalDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    [HttpDelete]
    [Route("data-maintenance/prune-chat-messages")]
    public async Task<IActionResult> PruneChatMessages(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PruneChatMessages();

        return response.ToHttpResult();
    }

    async Task<ApiResult> IDataMaintenanceApi.PruneChatMessages()
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {DateTime.UtcNow.AddMonths(-12)}");
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {DateTime.UtcNow.AddMonths(-11)}");
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {DateTime.UtcNow.AddMonths(-10)}");
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {DateTime.UtcNow.AddMonths(-9)}");
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {DateTime.UtcNow.AddMonths(-8)}");
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {DateTime.UtcNow.AddMonths(-7)}");
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {DateTime.UtcNow.AddMonths(-6)}");
        return new ApiResult();
    }

    [HttpDelete]
    [Route("data-maintenance/prune-game-server-events")]
    public async Task<IActionResult> PruneGameServerEvents(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PruneGameServerEvents();

        return response.ToHttpResult();
    }

    async Task<ApiResult> IDataMaintenanceApi.PruneGameServerEvents()
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerEvents] WHERE [Timestamp] < {DateTime.UtcNow.AddMonths(-6)}");
        return new ApiResult();
    }

    [HttpDelete]
    [Route("data-maintenance/prune-game-server-stats")]
    public async Task<IActionResult> PruneGameServerStats(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PruneGameServerStats();

        return response.ToHttpResult();
    }

    async Task<ApiResult> IDataMaintenanceApi.PruneGameServerStats()
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerStats] WHERE [Timestamp] < {DateTime.UtcNow.AddMonths(-6)}");
        return new ApiResult();
    }

    [HttpDelete]
    [Route("data-maintenance/prune-recent-players")]
    public async Task<IActionResult> PruneRecentPlayers(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PruneRecentPlayers();

        return response.ToHttpResult();
    }

    async Task<ApiResult> IDataMaintenanceApi.PruneRecentPlayers()
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[RecentPlayers] WHERE [Timestamp] < {DateTime.UtcNow.AddDays(-7)}");
        return new ApiResult();
    }

    [HttpPut]
    [Route("data-maintenance/reset-system-assigned-player-tags")]
    public async Task<IActionResult> ResetSystemAssignedPlayerTags(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).ResetSystemAssignedPlayerTags();

        return response.ToHttpResult();
    }

    async Task<ApiResult> IDataMaintenanceApi.ResetSystemAssignedPlayerTags()
    {
        var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);

        // First, get the tag IDs by name
        var activeTag = await context.Tags.FirstOrDefaultAsync(t => t.Name == "active-players");
        var inactiveTag = await context.Tags.FirstOrDefaultAsync(t => t.Name == "inactive-player");

        if (activeTag == null || inactiveTag == null)
        {
            throw new InvalidOperationException("Required tags 'active-players' or 'inactive-player' do not exist.");
        }

        // Get list of active players (played in last 2 weeks)
        var activePlayers = await context.Players
            .Where(rp => rp.LastSeen >= twoWeeksAgo)
            .Select(rp => rp.PlayerId)
            .ToListAsync();

        // Get all players
        var allPlayerIds = await context.Players
            .Select(p => p.PlayerId)
            .ToListAsync();

        // Determine which players are inactive (using except manually)
        var inactivePlayers = allPlayerIds
            .Where(p => !activePlayers.Contains(p))
            .ToList();

        // Get existing player tags
        var existingPlayerTags = await context.PlayerTags
            .Where(pt => pt.TagId == activeTag.TagId || pt.TagId == inactiveTag.TagId)
            .ToListAsync();

        // Process active players
        foreach (var playerId in activePlayers)
        {
            // Remove inactive tag if exists
            var existingInactiveTag = existingPlayerTags
                .FirstOrDefault(pt => pt.PlayerId == playerId && pt.TagId == inactiveTag.TagId);

            if (existingInactiveTag != null)
            {
                context.PlayerTags.Remove(existingInactiveTag);
            }

            // Add active tag if not exists
            var hasActiveTag = existingPlayerTags
                .Any(pt => pt.PlayerId == playerId && pt.TagId == activeTag.TagId);

            if (!hasActiveTag)
            {
                context.PlayerTags.Add(new PlayerTag
                {
                    PlayerTagId = Guid.NewGuid(),
                    PlayerId = playerId,
                    TagId = activeTag.TagId,
                    Assigned = DateTime.UtcNow
                });
            }
        }

        // Process inactive players
        foreach (var playerId in inactivePlayers)
        {
            // Remove active tag if exists
            var existingActiveTag = existingPlayerTags
                .FirstOrDefault(pt => pt.PlayerId == playerId && pt.TagId == activeTag.TagId);

            if (existingActiveTag != null)
            {
                context.PlayerTags.Remove(existingActiveTag);
            }

            // Add inactive tag if not exists
            var hasInactiveTag = existingPlayerTags
                .Any(pt => pt.PlayerId == playerId && pt.TagId == inactiveTag.TagId);

            if (!hasInactiveTag)
            {
                context.PlayerTags.Add(new PlayerTag
                {
                    PlayerTagId = Guid.NewGuid(),
                    PlayerId = playerId,
                    TagId = inactiveTag.TagId,
                    Assigned = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();

        return new ApiResult();
    }
}
