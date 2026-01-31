using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        ArgumentNullException.ThrowIfNull(context);
            this.context = context;
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
        var response = await ((IDataMaintenanceApi)this).PruneChatMessages(cancellationToken);
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
            await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[ChatMessages] WHERE [Timestamp] < {batchCutoff} AND [Locked] = 0", cancellationToken);
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
        var response = await ((IDataMaintenanceApi)this).PruneGameServerEvents(cancellationToken);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes game server events older than 6 months to maintain database performance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneGameServerEvents(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-6);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerEvents] WHERE [Timestamp] < {cutoffDate}", cancellationToken);
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
        var response = await ((IDataMaintenanceApi)this).PruneGameServerStats(cancellationToken);
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
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[GameServerStats] WHERE [Timestamp] < {cutoffDate}", cancellationToken);
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
        var response = await ((IDataMaintenanceApi)this).PruneRecentPlayers(cancellationToken);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Prunes recent player records older than 7 days to maintain database performance.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.PruneRecentPlayers(CancellationToken cancellationToken)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [dbo].[RecentPlayers] WHERE [Timestamp] < {cutoffDate}", cancellationToken);
        return new ApiResponse().ToApiResult();
    }

    /// <summary>
    /// Resets system-assigned player tags based on player activity in the last 2 weeks.
    /// Active players get the "active-players" tag, inactive players get the "inactive-player" tag.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A success response indicating the operation completed.</returns>
    [HttpPost("data-maintenance/reset-system-assigned-player-tags")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResetSystemAssignedPlayerTags(CancellationToken cancellationToken = default)
    {
        var response = await ((IDataMaintenanceApi)this).ResetSystemAssignedPlayerTags(cancellationToken);
        return response.ToHttpResult();
    }

    /// <summary>
    /// Resets system-assigned player tags based on player activity in the last 2 weeks.
    /// Active players get the "active-players" tag, inactive players get the "inactive-player" tag.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An API result indicating the operation completed successfully.</returns>
    async Task<ApiResult> IDataMaintenanceApi.ResetSystemAssignedPlayerTags(CancellationToken cancellationToken)
    {
        var twoWeeksAgo = DateTime.UtcNow.AddDays(-14);

        // First, get the tag IDs by name using AsNoTracking for better performance
        var activeTag = await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == "active-players", cancellationToken);

        var inactiveTag = await context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == "inactive-player", cancellationToken);

        if (activeTag == null || inactiveTag == null)
        {
            throw new InvalidOperationException("Required tags 'active-players' or 'inactive-player' do not exist.");
        }

        // Get list of active players (played in last 2 weeks)
        var activePlayers = await context.Players
            .AsNoTracking()
            .Where(rp => rp.LastSeen >= twoWeeksAgo)
            .Select(rp => rp.PlayerId)
            .ToListAsync(cancellationToken);

        // Get all players
        var allPlayerIds = await context.Players
            .AsNoTracking()
            .Select(p => p.PlayerId)
            .ToListAsync(cancellationToken);

        // Determine which players are inactive (using except manually)
        var inactivePlayers = allPlayerIds
            .Where(p => !activePlayers.Contains(p))
            .ToList();

        // Get existing player tags
        var existingPlayerTags = await context.PlayerTags
            .Where(pt => pt.TagId == activeTag.TagId || pt.TagId == inactiveTag.TagId)
            .ToListAsync(cancellationToken);

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

        await context.SaveChangesAsync(cancellationToken);

        return new ApiResponse().ToApiResult();
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
        var response = await ((IDataMaintenanceApi)this).ValidateMapImages(cancellationToken);
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
            return new ApiResult(HttpStatusCode.InternalServerError);

        var blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
        var containerClient = blobServiceClient.GetBlobContainerClient("map-images");

        // Step 1: Remove any blobs (including orphans) that match any target MD5 hash
        var targetHashes = new[]
        {
            Convert.FromBase64String("aJ39V4B09SgLKGEgsVkteA=="),
            Convert.FromBase64String("MRMoR2jCIgtFK2zpmGOPRQ==")
        };
        var removedHashMatches = 0;
        await foreach (var blobItem in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, cancellationToken: cancellationToken))
        {
            var contentHash = blobItem.Properties.ContentHash;
            if (contentHash != null && targetHashes.Any(th => contentHash.SequenceEqual(th)))
            {
                await containerClient.DeleteBlobIfExistsAsync(blobItem.Name, cancellationToken: cancellationToken);
                removedHashMatches++;
            }
        }

        // Step 2: Validate DB-referenced images; clear MapImageUri if blob missing
        var mapsWithImages = await context.Maps
            .Where(m => m.MapImageUri != null)
            .ToListAsync(cancellationToken);

        var cleared = 0;
        foreach (var map in mapsWithImages)
        {
            var blobKey = $"{map.GameType.ToGameType()}_{map.MapName}.jpg";
            var blobClient = containerClient.GetBlobClient(blobKey);
            var exists = await blobClient.ExistsAsync(cancellationToken);
            if (!exists.Value)
            {
                map.MapImageUri = null;
                cleared++;
            }
        }

        if (cleared > 0)
            await context.SaveChangesAsync(cancellationToken);

        // Step 3: Normalize blobs with incorrect content-type (application/octet-stream) and missing metadata
        //   - If blob content appears to be HTML, delete it and clear any matching map reference
        //   - Else, set proper image content type and metadata (mapId, gameType, mapName) if resolvable
        var updatedMaps = 0;
        var normalizedBlobs = 0;
        await foreach (var blobItem in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, cancellationToken: cancellationToken))
        {
            if (!string.Equals(blobItem.Properties.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase))
                continue; // only process unknown content types

            var blobClient = containerClient.GetBlobClient(blobItem.Name);

            // Download a small portion to inspect content (first 1KB)
            try
            {
                var download = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
                using var mem = new MemoryStream();
                await download.Value.Content.CopyToAsync(mem, 1024, cancellationToken);
                var bytes = mem.ToArray();
                var head = Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 512));
                var looksHtml = head.TrimStart().StartsWith("<") && head.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0;

                if (looksHtml)
                {
                    // Delete blob and clear any map referencing it
                    await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

                    // Attempt to parse map filename to clear DB reference (pattern: GameType_MapName.jpg)
                    if (TryParseBlobName(blobItem.Name, out var gameTypeInt, out var mapName))
                    {
                        var mapsToClear = await context.Maps
                            .Where(m => m.GameType == gameTypeInt && m.MapName == mapName && m.MapImageUri != null)
                            .ToListAsync(cancellationToken);
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
                    var mapEntity = await context.Maps.FirstOrDefaultAsync(m => m.GameType == gti && m.MapName == mName, cancellationToken);
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

                await blobClient.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
                if (metadata != null)
                {
                    await blobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken);
                }
                normalizedBlobs++;
            }
            catch
            {
                // Swallow errors for individual blobs to allow processing to continue
            }
        }

        if (updatedMaps > 0)
            await context.SaveChangesAsync(cancellationToken);

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
                return false;

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
        if (string.IsNullOrWhiteSpace(extension)) return "image/jpeg";
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
