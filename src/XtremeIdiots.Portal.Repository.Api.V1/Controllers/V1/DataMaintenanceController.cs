using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using Asp.Versioning;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

/// <summary>
/// Controller for managing data maintenance operations including pruning old data and resetting system-assigned player tags.
/// </summary>
[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public class DataMaintenanceController : ControllerBase, IDataMaintenanceApi
{
    private const string ConnectedPlayerVerifiedTagName = "verified-player";
    private const string SeniorAdminTagName = "senior-admin";
    private const string HeadAdminTagName = "head-admin";
    private const string GameAdminTagName = "game-admin";
    private const string ModeratorTagName = "moderator";
    private const string ClanMemberTagName = "clan-member";

    private readonly PortalDbContext context;
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataMaintenanceController"/> class.
    /// </summary>
    /// <param name="context">The database context for data operations.</param>
    /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
    public DataMaintenanceController(PortalDbContext context, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
        this.context = context;
        this.configuration = configuration;
    }

    /// <summary>
    /// Deletes a player and all associated player-linked data.
    /// </summary>
    /// <param name="playerId">The player id to delete.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpDelete("data-maintenance/players/{playerId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlayer(Guid playerId, CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).DeletePlayer(playerId, cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Deletes a player and all associated player-linked data.
    /// </summary>
    /// <param name="playerId">The player id to delete.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.DeletePlayer(Guid playerId, CancellationToken cancellationToken)
    {
        var executionStrategy = context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteInTransactionAsync(async operationCancellationToken =>
        {
            var player = await context.Players
                .FirstOrDefaultAsync(p => p.PlayerId == playerId, operationCancellationToken)
                .ConfigureAwait(false);

            if (player == null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            var adminActions = await context.AdminActions.Where(a => a.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var automationActionStates = await context.AutomationActionStates.Where(s => s.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var chatMessages = await context.ChatMessages.Where(c => c.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var connectedPlayerProfiles = await context.ConnectedPlayerProfiles.Where(c => c.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var mapVotes = await context.MapVotes.Where(v => v.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var playerAliases = await context.PlayerAliases.Where(a => a.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var playerIpAddresses = await context.PlayerIpAddresses.Where(a => a.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var playerTags = await context.PlayerTags.Where(t => t.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var protectedNames = await context.ProtectedNames.Where(n => n.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var recentPlayers = await context.RecentPlayers.Where(r => r.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);
            var reports = await context.Reports.Where(r => r.PlayerId == playerId).ToListAsync(operationCancellationToken).ConfigureAwait(false);

            context.AdminActions.RemoveRange(adminActions);
            context.AutomationActionStates.RemoveRange(automationActionStates);
            context.ChatMessages.RemoveRange(chatMessages);
            context.ConnectedPlayerProfiles.RemoveRange(connectedPlayerProfiles);
            context.MapVotes.RemoveRange(mapVotes);
            context.PlayerAliases.RemoveRange(playerAliases);
            context.PlayerIpAddresses.RemoveRange(playerIpAddresses);
            context.PlayerTags.RemoveRange(playerTags);
            context.ProtectedNames.RemoveRange(protectedNames);
            context.RecentPlayers.RemoveRange(recentPlayers);
            context.Reports.RemoveRange(reports);
            context.Players.Remove(player);

            await context.SaveChangesAsync(operationCancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }, async operationCancellationToken => !await context.Players
            .AnyAsync(p => p.PlayerId == playerId, operationCancellationToken)
            .ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
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
        var response = await ((IDataMaintenanceApi)this).PruneChatMessages(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes chat messages older than 12 months to maintain database performance.
    /// Locked chat messages are preserved and will not be deleted.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneChatMessages(CancellationToken cancellationToken)
    {
        // Execute pruning operation in batches to avoid locking issues
        // Only delete unlocked chat messages to preserve locked ones
        foreach (var i in Enumerable.Range(6, 7))
        {
            var batchCutoff = DateTime.UtcNow.AddMonths(-i);
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {batchCutoff} AND [Locked] = 0", cancellationToken).ConfigureAwait(false);
        }

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Prunes game server events older than 3 months to maintain database performance.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpDelete("data-maintenance/prune-game-server-events")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PruneGameServerEvents(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PruneGameServerEvents(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes game server events older than 3 months to maintain database performance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneGameServerEvents(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-3);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerEvents] WHERE [Timestamp] < {cutoffDate}", cancellationToken).ConfigureAwait(false);
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
        var response = await ((IDataMaintenanceApi)this).PruneGameServerStats(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes game server statistics older than 6 months to maintain database performance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneGameServerStats(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-6);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerStats] WHERE [Timestamp] < {cutoffDate}", cancellationToken).ConfigureAwait(false);
        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Prunes low-value player IP history rows to improve related-player accuracy.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpDelete("data-maintenance/prune-player-ip-addresses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PrunePlayerIpAddresses(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).PrunePlayerIpAddresses(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes low-value player IP history rows to improve related-player accuracy.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PrunePlayerIpAddresses(CancellationToken cancellationToken)
    {
        var hardDeleteDaysRaw = int.TryParse(configuration["DataRetention:PlayerIpAddressHardDeleteDays"], out var hdd) ? hdd : 365;
        var lowConfidenceDaysRaw = int.TryParse(configuration["DataRetention:PlayerIpAddressLowConfidenceDays"], out var lcd) ? lcd : 30;
        var lowConfidenceThresholdRaw = int.TryParse(configuration["DataRetention:PlayerIpAddressLowConfidenceThreshold"], out var lct) ? lct : 10;
        var batchSizeRaw = int.TryParse(configuration["DataRetention:PlayerIpAddressPruneBatchSize"], out var pbs) ? pbs : 5000;

        var hardDeleteDays = Math.Max(1, hardDeleteDaysRaw);
        var lowConfidenceDays = Math.Max(1, lowConfidenceDaysRaw);
        var lowConfidenceThreshold = Math.Max(0, lowConfidenceThresholdRaw);
        var batchSize = Math.Max(1, batchSizeRaw);

        var hardDeleteCutoff = DateTime.UtcNow.AddDays(-hardDeleteDays);
        var lowConfidenceCutoff = DateTime.UtcNow.AddDays(-lowConfidenceDays);

        // Step 1: Remove null/blank rows first.
        while (true)
        {
            var affected = await context.Database.ExecuteSqlInterpolatedAsync($@"
                DELETE TOP ({batchSize}) FROM [dbo].[PlayerIpAddresses]
                WHERE [Address] IS NULL OR LTRIM(RTRIM([Address])) = ''", cancellationToken).ConfigureAwait(false);

            if (affected == 0)
            {
                break;
            }
        }

        // Step 2: Remove old rows that are beyond hard retention.
        while (true)
        {
            var affected = await context.Database.ExecuteSqlInterpolatedAsync($@"
                DELETE TOP ({batchSize}) FROM [dbo].[PlayerIpAddresses]
                WHERE [LastUsed] < {hardDeleteCutoff}", cancellationToken).ConfigureAwait(false);

            if (affected == 0)
            {
                break;
            }
        }

        // Step 3: Remove stale low-confidence rows.
        while (true)
        {
            var affected = await context.Database.ExecuteSqlInterpolatedAsync($@"
                DELETE TOP ({batchSize}) FROM [dbo].[PlayerIpAddresses]
                WHERE [LastUsed] < {lowConfidenceCutoff}
                  AND [ConfidenceScore] < {lowConfidenceThreshold}", cancellationToken).ConfigureAwait(false);

            if (affected == 0)
            {
                break;
            }
        }

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
        var response = await ((IDataMaintenanceApi)this).PruneRecentPlayers(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes recent player records older than 7 days to maintain database performance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneRecentPlayers(CancellationToken cancellationToken)
    {
        var recentPlayersDays = int.TryParse(configuration["DataRetention:RecentPlayersDays"], out var rpd) ? rpd : 7;
        var cutoffDate = DateTime.UtcNow.AddDays(-recentPlayersDays);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[RecentPlayers] WHERE [Timestamp] < {cutoffDate}", cancellationToken).ConfigureAwait(false);
        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Resets system-assigned player tags based on player activity in the last 2 weeks.
    /// Active players get the "active-player" tag, inactive players get the "inactive-player" tag.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpPost("data-maintenance/reset-system-assigned-player-tags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetSystemAssignedPlayerTags(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).ResetSystemAssignedPlayerTags(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Resets system-assigned player tags based on player activity in the last 2 weeks.
    /// Active players get the "active-player" tag, inactive players get the "inactive-player" tag.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.ResetSystemAssignedPlayerTags(CancellationToken cancellationToken)
    {
        var inactivePlayerDays = int.TryParse(configuration["DataRetention:InactivePlayerDays"], out var ipd) ? ipd : 14;
        var twoWeeksAgo = DateTime.UtcNow.AddDays(-inactivePlayerDays);

        // Get the tag IDs by name
        var activeTag = await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == "active-player", cancellationToken).ConfigureAwait(false);

        var inactiveTag = await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == "inactive-player", cancellationToken).ConfigureAwait(false);

        if (activeTag == null || inactiveTag == null)
        {
            throw new InvalidOperationException("Required tags 'active-player' or 'inactive-player' do not exist.");
        }

        var activeTagId = activeTag.TagId;
        var inactiveTagId = inactiveTag.TagId;
        var now = DateTime.UtcNow;

        // Remove inactive tags from active players (played in last 2 weeks)
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE pt FROM [dbo].[PlayerTags] pt
            INNER JOIN [dbo].[Players] p ON pt.[PlayerId] = p.[PlayerId]
            WHERE pt.[TagId] = {inactiveTagId}
              AND p.[LastSeen] >= {twoWeeksAgo}", cancellationToken).ConfigureAwait(false);

        // Remove active tags from inactive players (not played in last 2 weeks)
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE pt FROM [dbo].[PlayerTags] pt
            INNER JOIN [dbo].[Players] p ON pt.[PlayerId] = p.[PlayerId]
            WHERE pt.[TagId] = {activeTagId}
              AND p.[LastSeen] < {twoWeeksAgo}", cancellationToken).ConfigureAwait(false);

        // Add active tag to active players who don't already have it
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO [dbo].[PlayerTags] ([PlayerTagId], [PlayerId], [TagId], [Assigned])
            SELECT NEWID(), p.[PlayerId], {activeTagId}, {now}
            FROM [dbo].[Players] p
            WHERE p.[LastSeen] >= {twoWeeksAgo}
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[PlayerTags] pt
                  WHERE pt.[PlayerId] = p.[PlayerId] AND pt.[TagId] = {activeTagId}
              )", cancellationToken).ConfigureAwait(false);

        // Add inactive tag to inactive players who don't already have it
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO [dbo].[PlayerTags] ([PlayerTagId], [PlayerId], [TagId], [Assigned])
            SELECT NEWID(), p.[PlayerId], {inactiveTagId}, {now}
            FROM [dbo].[Players] p
            WHERE p.[LastSeen] < {twoWeeksAgo}
              AND NOT EXISTS (
                  SELECT 1 FROM [dbo].[PlayerTags] pt
                  WHERE pt.[PlayerId] = p.[PlayerId] AND pt.[TagId] = {inactiveTagId}
              )", cancellationToken).ConfigureAwait(false);

        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Reconciles the system-managed connected-player tag projection from active ownership state.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpPost("data-maintenance/reconcile-connected-player-tags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReconcileConnectedPlayerTags(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).ReconcileConnectedPlayerTags(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Reconciles the system-managed connected-player tag projection from active ownership state.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.ReconcileConnectedPlayerTags(CancellationToken cancellationToken)
    {
        var requiredTagNames = new[]
        {
            ConnectedPlayerVerifiedTagName,
            SeniorAdminTagName,
            HeadAdminTagName,
            GameAdminTagName,
            ModeratorTagName,
            ClanMemberTagName,
        };

        var requiredTagNameSet = requiredTagNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allTags = await context.Tags
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var requiredTags = allTags
            .Where(t => requiredTagNameSet.Contains(t.Name))
            .ToList();

        var duplicateTagNames = requiredTags
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (duplicateTagNames.Count != 0)
        {
            throw new InvalidOperationException($"Duplicate required tags found: {string.Join(", ", duplicateTagNames)}.");
        }

        var tagsByName = requiredTags
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var missingTagNames = requiredTagNames
            .Where(tagName => !tagsByName.ContainsKey(tagName))
            .ToList();

        if (missingTagNames.Count != 0)
        {
            throw new InvalidOperationException($"Required tags are missing: {string.Join(", ", missingTagNames)}.");
        }

        var verifiedTagId = tagsByName[ConnectedPlayerVerifiedTagName].TagId;
        var seniorAdminTagId = tagsByName[SeniorAdminTagName].TagId;
        var headAdminTagId = tagsByName[HeadAdminTagName].TagId;
        var gameAdminTagId = tagsByName[GameAdminTagName].TagId;
        var moderatorTagId = tagsByName[ModeratorTagName].TagId;
        var clanMemberTagId = tagsByName[ClanMemberTagName].TagId;

        var now = DateTime.UtcNow;

        var activeOwnerships = await context.ConnectedPlayerProfiles
            .AsNoTracking()
            .Where(cp => cp.IsActive)
            .Join(
                context.Players.AsNoTracking(),
                cp => cp.PlayerId,
                player => player.PlayerId,
                (cp, player) => new
                {
                    cp.PlayerId,
                    cp.UserProfileId,
                    PlayerGameType = ((GameType)player.GameType).ToString(),
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var linkedPlayerIds = activeOwnerships
            .Select(o => o.PlayerId)
            .Distinct()
            .ToList();

        var linkedUserProfileIds = activeOwnerships
            .Select(o => o.UserProfileId)
            .Distinct()
            .ToList();

        var roleClaimTypes = new[]
        {
            UserProfileClaimType.SeniorAdmin,
            UserProfileClaimType.HeadAdmin,
            UserProfileClaimType.GameAdmin,
            UserProfileClaimType.Moderator,
            UserProfileClaimType.ClanMember,
        };

        var roleClaims = await context.UserProfileClaims
            .AsNoTracking()
            .Where(claim =>
                linkedUserProfileIds.Contains(claim.UserProfileId)
                && claim.SystemGenerated
                && roleClaimTypes.Contains(claim.ClaimType))
            .Select(claim => new
            {
                claim.UserProfileId,
                claim.ClaimType,
                claim.ClaimValue,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var claimsByUserProfileId = roleClaims
            .GroupBy(claim => claim.UserProfileId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var seniorAdminPlayerIds = new HashSet<Guid>();
        var headAdminPlayerIds = new HashSet<Guid>();
        var gameAdminPlayerIds = new HashSet<Guid>();
        var moderatorPlayerIds = new HashSet<Guid>();
        var clanMemberPlayerIds = new HashSet<Guid>();

        foreach (var ownership in activeOwnerships)
        {
            if (!claimsByUserProfileId.TryGetValue(ownership.UserProfileId, out var claims))
            {
                continue;
            }

            if (claims.Any(c => c.ClaimType == UserProfileClaimType.SeniorAdmin))
            {
                seniorAdminPlayerIds.Add(ownership.PlayerId);
            }

            if (claims.Any(c =>
                c.ClaimType == UserProfileClaimType.HeadAdmin
                && string.Equals(c.ClaimValue, ownership.PlayerGameType, StringComparison.OrdinalIgnoreCase)))
            {
                headAdminPlayerIds.Add(ownership.PlayerId);
            }

            if (claims.Any(c =>
                c.ClaimType == UserProfileClaimType.GameAdmin
                && string.Equals(c.ClaimValue, ownership.PlayerGameType, StringComparison.OrdinalIgnoreCase)))
            {
                gameAdminPlayerIds.Add(ownership.PlayerId);
            }

            if (claims.Any(c =>
                c.ClaimType == UserProfileClaimType.Moderator
                && string.Equals(c.ClaimValue, ownership.PlayerGameType, StringComparison.OrdinalIgnoreCase)))
            {
                moderatorPlayerIds.Add(ownership.PlayerId);
            }

            if (claims.Any(c => c.ClaimType == UserProfileClaimType.ClanMember))
            {
                clanMemberPlayerIds.Add(ownership.PlayerId);
            }
        }

        var hasChanges = false;

        hasChanges |= await ReconcileProjectedTag(
            verifiedTagId,
            linkedPlayerIds,
            now,
            removeDuplicatesForProjectedPlayers: true,
            cancellationToken).ConfigureAwait(false);

        hasChanges |= await ReconcileProjectedTag(
            seniorAdminTagId,
            seniorAdminPlayerIds,
            now,
            removeDuplicatesForProjectedPlayers: true,
            cancellationToken).ConfigureAwait(false);

        hasChanges |= await ReconcileProjectedTag(
            headAdminTagId,
            headAdminPlayerIds,
            now,
            removeDuplicatesForProjectedPlayers: true,
            cancellationToken).ConfigureAwait(false);

        hasChanges |= await ReconcileProjectedTag(
            gameAdminTagId,
            gameAdminPlayerIds,
            now,
            removeDuplicatesForProjectedPlayers: true,
            cancellationToken).ConfigureAwait(false);

        hasChanges |= await ReconcileProjectedTag(
            moderatorTagId,
            moderatorPlayerIds,
            now,
            removeDuplicatesForProjectedPlayers: true,
            cancellationToken).ConfigureAwait(false);

        hasChanges |= await ReconcileProjectedTag(
            clanMemberTagId,
            clanMemberPlayerIds,
            now,
            removeDuplicatesForProjectedPlayers: true,
            cancellationToken).ConfigureAwait(false);

        if (hasChanges)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new ApiResponse().ToApiResult();
    }

    private async Task<bool> ReconcileProjectedTag(
        Guid tagId,
        IEnumerable<Guid> projectedPlayerIds,
        DateTime now,
        bool removeDuplicatesForProjectedPlayers,
        CancellationToken cancellationToken)
    {
        var existingTaggedEntries = await context.PlayerTags
            .Where(pt => pt.TagId == tagId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var projectedPlayerIdSet = projectedPlayerIds.ToHashSet();

        var entriesToRemove = existingTaggedEntries
            .Where(pt => pt.PlayerId == null || !projectedPlayerIdSet.Contains(pt.PlayerId.Value))
            .ToList();

        if (removeDuplicatesForProjectedPlayers)
        {
            var duplicateEntriesToRemove = existingTaggedEntries
                .Where(pt => pt.PlayerId.HasValue && projectedPlayerIdSet.Contains(pt.PlayerId.Value))
                .GroupBy(pt => pt.PlayerId!.Value)
                .SelectMany(group => group
                    .OrderBy(entry => entry.Assigned)
                    .ThenBy(entry => entry.PlayerTagId)
                    .Skip(1))
                .ToList();

            if (duplicateEntriesToRemove.Count != 0)
            {
                entriesToRemove.AddRange(duplicateEntriesToRemove);
            }
        }

        if (entriesToRemove.Count != 0)
        {
            context.PlayerTags.RemoveRange(entriesToRemove);
        }

        var alreadyTaggedPlayerIds = existingTaggedEntries
            .Where(pt => pt.PlayerId.HasValue)
            .Select(pt => pt.PlayerId!.Value)
            .ToHashSet();

        var newEntries = projectedPlayerIdSet
            .Where(playerId => !alreadyTaggedPlayerIds.Contains(playerId))
            .Select(playerId => new PlayerTag
            {
                PlayerTagId = Guid.NewGuid(),
                PlayerId = playerId,
                TagId = tagId,
                Assigned = now,
                UserProfileId = null,
            })
            .ToList();

        if (newEntries.Count != 0)
        {
            await context.PlayerTags.AddRangeAsync(newEntries, cancellationToken).ConfigureAwait(false);
        }

        return entriesToRemove.Count != 0 || newEntries.Count != 0;
    }

    /// <summary>
    /// Validates map images: for each map with a stored image URI, checks if the backing blob exists; if missing, clears the URI.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response after validation completes.</returns>
    [HttpPost("data-maintenance/validate-map-images")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateMapImages(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).ValidateMapImages(cancellationToken).ConfigureAwait(false);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Validates map images: for each map with a stored image URI, checks if the backing blob exists; if missing, clears the URI.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An API result indicating completion.</returns>
    async Task<ApiResult> IDataMaintenanceApi.ValidateMapImages(CancellationToken cancellationToken)
    {
        var blobEndpoint = Environment.GetEnvironmentVariable("appdata_storage_blob_endpoint");
        if (string.IsNullOrEmpty(blobEndpoint))
        {
            return new ApiResult(HttpStatusCode.InternalServerError);
        }

        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
        var containerClient = blobServiceClient.GetBlobContainerClient("map-images");

        // Step 1: Remove any blobs (including orphans) that match any target MD5 hash
        byte[][] targetHashes =
        [
            Convert.FromBase64String("aJ39V4B09SgLKGEgsVkteA=="),
            Convert.FromBase64String("MRMoR2jCIgtFK2zpmGOPRQ==")
        ];
        var removedHashMatches = 0;
        await foreach (var blobItem in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix: null, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            var contentHash = blobItem.Properties.ContentHash;
            if (contentHash != null && targetHashes.Any(th => contentHash.SequenceEqual(th)))
            {
                await containerClient.DeleteBlobIfExistsAsync(blobItem.Name, cancellationToken: cancellationToken).ConfigureAwait(false);
                removedHashMatches++;
            }
        }

        // Step 2: Validate DB-referenced images; clear MapImageUri if blob missing
        var mapsWithImages = await context.Maps
            .Where(m => m.MapImageUri != null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var cleared = 0;
        foreach (var map in mapsWithImages)
        {
            var blobKey = $"{map.GameType.ToGameType()}_{map.MapName}.jpg";
            var blobClient = containerClient.GetBlobClient(blobKey);
            var exists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if (!exists.Value)
            {
                map.MapImageUri = null;
                cleared++;
            }
        }

        if (cleared > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        // Step 3: Normalize blobs with incorrect content-type (application/octet-stream) and missing metadata
        //   - If blob content appears to be HTML, delete it and clear any matching map reference
        //   - Else, set proper image content type and metadata (mapId, gameType, mapName) if resolvable
        var updatedMaps = 0;
        var normalizedBlobs = 0;
        await foreach (var blobItem in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix: null, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (!string.Equals(blobItem.Properties.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
            {
                continue; // only process unknown content types
            }

            var blobClient = containerClient.GetBlobClient(blobItem.Name);

            // Download a small portion to inspect content (first 1KB)
            try
            {
                var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                using var mem = new MemoryStream();
                await download.Value.Content.CopyToAsync(mem, 1024, cancellationToken).ConfigureAwait(false);
                var bytes = mem.ToArray();
                var head = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 512));
                var looksHtml = head.TrimStart().StartsWith("<") && head.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0;

                if (looksHtml)
                {
                    // Delete blob and clear any map referencing it
                    await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    // Attempt to parse map filename to clear DB reference (pattern: GameType_MapName.jpg)
                    if (TryParseBlobName(blobItem.Name, out var gameTypeInt, out var mapName))
                    {
                        var mapsToClear = await context.Maps
                            .Where(m => m.GameType == gameTypeInt && m.MapName == mapName && m.MapImageUri != null)
                            .ToListAsync(cancellationToken).ConfigureAwait(false);
                        foreach (var m in mapsToClear)
                        {
                            m.MapImageUri = null;
                            updatedMaps++;
                        }
                    }
                    continue;
                }

                // Not HTML; treat as image and normalize headers/metadata
                string contentType = GetContentTypeFromExtension(Path.GetExtension(blobItem.Name));

                // Attempt to resolve map for metadata
                Dictionary<string, string>? metadata = null;
                if (TryParseBlobName(blobItem.Name, out var gti, out var mName))
                {
                    var mapEntity = await context.Maps.FirstOrDefaultAsync(m => m.GameType == gti && m.MapName == mName, cancellationToken).ConfigureAwait(false);
                    if (mapEntity != null)
                    {
                        metadata = new Dictionary<string, string>
                        {
                            ["mapId"] = mapEntity.MapId.ToString(),
                            ["gameType"] = mapEntity.GameType.ToString(),
                            ["mapName"] = mapEntity.MapName ?? string.Empty
                        };

                        // Ensure DB MapImageUri is set
                        if (string.IsNullOrEmpty(mapEntity.MapImageUri))
                        {
                            mapEntity.MapImageUri = blobClient.Uri.ToString();
                            updatedMaps++;
                        }
                    }
                }

                await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (metadata != null)
                {
                    await blobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                normalizedBlobs++;
            }
            catch
            {
                // Swallow errors for individual blobs to allow processing to continue
            }
        }

        if (updatedMaps > 0)
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new ApiResponse().ToApiResult();
    }

    // Helpers
    private static bool TryParseBlobName(string blobName, out int gameTypeInt, out string mapName)
    {
        gameTypeInt = 0;
        mapName = string.Empty;
        try
        {
            var noExt = Path.GetFileNameWithoutExtension(blobName);
            var underscoreIndex = noExt.IndexOf('_');
            if (underscoreIndex <= 0 || underscoreIndex >= noExt.Length - 1)
            {
                return false;
            }

            var gameTypeStr = noExt[..underscoreIndex];
            mapName = noExt[(underscoreIndex + 1)..];

            // Try parse enum by name (case-insensitive) using V1 GameType enum
            if (Enum.TryParse(typeof(GameType), gameTypeStr, true, out var enumVal) && enumVal is GameType gt)
            {
                gameTypeInt = gt.ToGameTypeInt();
                return true;
            }
        }
        catch { }
        return false;
    }

    private static string GetContentTypeFromExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "image/jpeg";
        }

        return extension.ToLowerInvariant() switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
