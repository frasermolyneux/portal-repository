using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Services;

internal static class EffectiveRotationResolver
{
    public static async Task<List<PublicServerRotationDto>> ResolveAsync(
        PortalDbContext context, IReadOnlyList<GameServer> servers, CancellationToken cancellationToken)
    {
        var ids = servers.Select(s => s.GameServerId).ToArray();
        var assignments = await context.MapRotationServerAssignments.AsNoTracking()
            .Include(a => a.MapRotation).ThenInclude(r => r.MapRotationMaps).ThenInclude(m => m.Map)
            .Where(a => ids.Contains(a.GameServerId) && a.UnassignedAt == null)
            .AsSplitQuery().ToListAsync(cancellationToken).ConfigureAwait(false);
        var names = assignments.SelectMany(a => a.MapRotation.MapRotationMaps)
            .Select(m => m.Map.MapName).Where(n => n != null).Distinct().ToArray();
        var metadata = await MapReadService.CatalogueQuery(context)
            .Where(m => m.MapName != null && names.Contains(m.MapName))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var lookup = metadata.GroupBy(m => ((int)((GameType)m.GameType).GetMapGameType(), m.MapName!.ToUpperInvariant()))
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.GameType == (int)GameType.CallOfDuty4x).ThenBy(m => m.MapId).First());
        var votes = await context.MapVotes.AsNoTracking()
            .Where(v => v.GameServerId != null && ids.Contains(v.GameServerId.Value))
            .GroupBy(v => new
            {
                GameServerId = v.GameServerId!.Value,
                MapId = v.Map.GameType == (int)GameType.CallOfDuty4 || v.Map.GameType == (int)GameType.CallOfDuty4x ? Guid.Empty : v.MapId,
                GameType = v.Map.GameType == (int)GameType.CallOfDuty4x ? (int)GameType.CallOfDuty4 : v.Map.GameType,
                MapName = v.Map.MapName!.ToUpper()
            })
            .Select(g => new { g.Key, Likes = g.Count(v => v.Like), Dislikes = g.Count(v => !v.Like) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var voteLookup = votes.ToDictionary(v => (v.Key.GameServerId, v.Key.MapId, v.Key.GameType, v.Key.MapName));
        var results = new List<PublicServerRotationDto>();
        foreach (var server in servers)
        {
            var candidates = assignments.Where(a => a.GameServerId == server.GameServerId
                && a.Source == (int)RotationSource.PortalManaged
                && a.MapRotation.Source == (int)RotationSource.PortalManaged
                && a.ActivationState == (int)ActivationState.Active
                && string.Equals(a.ConfigVariableName, "sv_maprotation", StringComparison.OrdinalIgnoreCase)).ToList();
            var family = (int)((GameType)server.GameType).GetMapGameType();
            var assignment = candidates.Count == 1 && IsApplied(candidates[0]) && HasValidMaps(candidates[0], server) ? candidates[0] : null;
            assignment ??= assignments.SingleOrDefault(a => a.GameServerId == server.GameServerId
                && a.Source == (int)RotationSource.ServerObserved && a.MapRotation.Source == (int)RotationSource.ServerObserved);
            if (assignment != null && !HasValidMaps(assignment, server))
            {
                assignment = null;
            }
            var maps = new List<PublicRotationMapDto>();
            if (assignment != null)
            {
                foreach (var entry in assignment.MapRotation.MapRotationMaps.OrderBy(m => m.SortOrder))
                {
                    var name = entry.Map.MapName!;
                    var map = lookup.GetValueOrDefault((family, name.ToUpperInvariant())) ?? entry.Map;
                    var counts = voteLookup.GetValueOrDefault((server.GameServerId,
                        family == (int)GameType.CallOfDuty4 ? Guid.Empty : map.MapId, family, name.ToUpperInvariant()));
                    maps.Add(new()
                    {
                        MapId = map.MapId,
                        MapName = name,
                        GameMode = entry.GameMode ?? (assignment.MapRotation.GameMode.Length == 0 ? null : assignment.MapRotation.GameMode),
                        MapImageUri = map.MapImageUri,
                        Likes = map.TotalLikes,
                        Dislikes = map.TotalDislikes,
                        ServerLikes = counts?.Likes ?? 0,
                        ServerDislikes = counts?.Dislikes ?? 0
                    });
                }
            }
            results.Add(new()
            {
                GameServerId = server.GameServerId,
                Title = server.Title,
                GameType = (GameType)server.GameType,
                RotationTitle = assignment?.MapRotation.Title,
                MapRotationId = assignment?.MapRotationId,
                MapRotationServerAssignmentId = assignment?.MapRotationServerAssignmentId,
                Source = assignment == null ? null : (RotationSource)assignment.Source,
                ObservedAt = assignment?.ObservedAt,
                IsAvailable = assignment != null,
                IsObservedHistory = false,
                Maps = maps
            });
        }
        return results;
    }

    private static bool IsApplied(MapRotationServerAssignment assignment) =>
        assignment.DeploymentState == (int)DeploymentState.Synced
        && assignment.DeployedVersion == assignment.MapRotation.Version
        && assignment.ActivatedVersion == assignment.MapRotation.Version;

    private static bool HasValidMaps(MapRotationServerAssignment assignment, GameServer server) =>
        ((GameType)assignment.MapRotation.GameType).GetCompatibleMapGameTypes().Contains((GameType)server.GameType)
        && assignment.MapRotation.MapRotationMaps.Count > 0
        && assignment.MapRotation.MapRotationMaps.All(m => !string.IsNullOrEmpty(m.Map.MapName)
            && ((GameType)m.Map.GameType).GetMapGameType() == ((GameType)server.GameType).GetMapGameType());
}
