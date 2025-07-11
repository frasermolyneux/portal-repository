using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using Asp.Versioning;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Controller for managing data maintenance operations including pruning old data and resetting system-assigned player tags.
/// </summary>
[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}")]
public class DataMaintenanceController : ControllerBase, IDataMaintenanceApi
{
    private readonly PortalDbContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMaintenanceController"/> class.
    /// </summary>
    /// <param name="context">The database context for data operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public DataMaintenanceController(PortalDbContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Prunes chat messages older than 12 months to maintain database performance.
    /// Locked chat messages are preserved and will not be deleted.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpDelete("data-maintenance/prune-chat-messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PruneChatMessages(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PruneChatMessages();
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes chat messages older than 12 months to maintain database performance.
    /// Locked chat messages are preserved and will not be deleted.
    /// </summary>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneChatMessages()
    {
        // Execute pruning operation in batches to avoid locking issues
        // Only delete unlocked chat messages to preserve locked ones
        for (int i = 6; i <= 12; i++)
        {
            var batchCutoff = DateTime.UtcNow.AddMonths(-i);
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {batchCutoff} AND [Locked] = 0");
        }

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Prunes game server events older than 6 months to maintain database performance.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpDelete("data-maintenance/prune-game-server-events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PruneGameServerEvents(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PruneGameServerEvents();
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes game server events older than 6 months to maintain database performance.
    /// </summary>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneGameServerEvents()
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-6);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerEvents] WHERE [Timestamp] < {cutoffDate}");
        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Prunes game server statistics older than 6 months to maintain database performance.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpDelete("data-maintenance/prune-game-server-stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PruneGameServerStats(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PruneGameServerStats();
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes game server statistics older than 6 months to maintain database performance.
    /// </summary>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneGameServerStats()
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-6);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerStats] WHERE [Timestamp] < {cutoffDate}");
        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Prunes recent player records older than 7 days to maintain database performance.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpDelete("data-maintenance/prune-recent-players")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PruneRecentPlayers(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PruneRecentPlayers();
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes recent player records older than 7 days to maintain database performance.
    /// </summary>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneRecentPlayers()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[RecentPlayers] WHERE [Timestamp] < {cutoffDate}");
        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Resets system-assigned player tags based on player activity in the last 2 weeks.
    /// Active players get the "active-players" tag, inactive players get the "inactive-player" tag.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpPut("data-maintenance/reset-system-assigned-player-tags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetSystemAssignedPlayerTags(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).ResetSystemAssignedPlayerTags();
        return response.ToHttpResult();
    }

    /// <summary>
    /// Resets system-assigned player tags based on player activity in the last 2 weeks.
    /// Active players get the "active-players" tag, inactive players get the "inactive-player" tag.
    /// </summary>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.ResetSystemAssignedPlayerTags()
    {
        var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);

        // First, get the tag IDs by name using AsNoTracking for better performance
        var activeTag = await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == "active-players");

        var inactiveTag = await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == "inactive-player");

        if (activeTag == null || inactiveTag == null)
        {
            throw new InvalidOperationException("Required tags 'active-players' or 'inactive-player' do not exist.");
        }

        // Get list of active players (played in last 2 weeks)
        var activePlayers = await context.Players
            .AsNoTracking()
            .Where(rp => rp.LastSeen >= twoWeeksAgo)
            .Select(rp => rp.PlayerId)
            .ToListAsync();

        // Get all players
        var allPlayerIds = await context.Players
            .AsNoTracking()
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

        return new ApiResponse().ToApiResult();
    }
}
