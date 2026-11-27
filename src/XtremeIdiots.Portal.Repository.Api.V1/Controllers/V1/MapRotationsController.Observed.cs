using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public partial class MapRotationsController
{
    [HttpGet("map-rotations/servers/{gameServerId:guid}/effective")]
    [ProducesResponseType<PublicServerRotationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEffectiveServerRotation(Guid gameServerId, CancellationToken cancellationToken = default)
        => (await ((IMapRotationsApi)this).GetEffectiveServerRotation(gameServerId, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<PublicServerRotationDto>> IMapRotationsApi.GetEffectiveServerRotation(Guid gameServerId, CancellationToken cancellationToken)
    {
        var server = await context.GameServers.AsNoTracking()
            .SingleOrDefaultAsync(s => s.GameServerId == gameServerId && !s.Deleted, cancellationToken).ConfigureAwait(false);
        if (server == null)
        {
            return new(HttpStatusCode.NotFound);
        }
        var rotations = await EffectiveRotationResolver.ResolveAsync(context, [server], cancellationToken).ConfigureAwait(false);
        return new ApiResponse<PublicServerRotationDto>(rotations.Single()).ToApiResult();
    }

    [HttpPut("map-rotations/servers/{gameServerId:guid}/observed")]
    [ProducesResponseType<MapRotationServerAssignmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReplaceObservedServerRotation(Guid gameServerId,
        [FromBody] ObserveServerRotationDto observation, CancellationToken cancellationToken = default)
        => (await ((IMapRotationsApi)this).ReplaceObservedServerRotation(gameServerId, observation, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    Task<ApiResult<MapRotationServerAssignmentDto>> IMapRotationsApi.ReplaceObservedServerRotation(
        Guid gameServerId, ObserveServerRotationDto observation, CancellationToken cancellationToken)
    {
        if (observation == null || observation.ObservedAt == default || observation.ObservedAt.Offset != TimeSpan.Zero
            || observation.ObservedAt > DateTimeOffset.UtcNow.AddMinutes(5) || observation.Entries == null
            || observation.Entries.Any(e => e == null || !ValidToken(e.MapName, 256)
                || (!string.IsNullOrEmpty(e.GameMode) && !ValidToken(e.GameMode, 50))))
        {
            return Task.FromResult(ObservationError(HttpStatusCode.BadRequest, "INVALID_OBSERVATION",
                "Supply a UTC observation time and ordered entries with console-safe map names and optional game modes. Empty entries explicitly represent an empty rotation."));
        }
        return ExecuteMutationAsync(() => ReplaceObservation(gameServerId, observation, cancellationToken), cancellationToken);
    }

    private async Task<ApiResult<MapRotationServerAssignmentDto>> ReplaceObservation(
        Guid gameServerId, ObserveServerRotationDto observation, CancellationToken cancellationToken)
    {
        await MapCatalogueWriteService.LockAsync(context, cancellationToken).ConfigureAwait(false);
        var server = await context.GameServers.AsNoTracking()
            .SingleOrDefaultAsync(s => s.GameServerId == gameServerId && !s.Deleted, cancellationToken).ConfigureAwait(false);
        if (server == null)
        {
            return ObservationError(HttpStatusCode.NotFound, "GAME_SERVER_NOT_FOUND", "The game server does not exist.");
        }
        var gameType = (GameType)server.GameType;
        if (gameType is not (GameType.CallOfDuty2 or GameType.CallOfDuty4 or GameType.CallOfDuty4x or GameType.CallOfDuty5))
        {
            return ObservationError(HttpStatusCode.BadRequest, "UNSUPPORTED_GAME_TYPE", "Server rotation observation supports only Call of Duty games.");
        }
        if (await context.MapRotationServerAssignments.AnyAsync(a => a.GameServerId == gameServerId
            && a.Source == (int)RotationSource.PortalManaged && a.UnassignedAt == null
            && a.DeploymentState != (int)DeploymentState.Removed, cancellationToken).ConfigureAwait(false))
        {
            return ObservationError(HttpStatusCode.Conflict, "PORTAL_MANAGED_ASSIGNMENT_EXISTS",
                "A portal-managed assignment owns this server. The observation was not applied.");
        }
        var current = await context.MapRotationServerAssignments
            .Include(a => a.MapRotation).ThenInclude(r => r.MapRotationMaps).ThenInclude(m => m.Map)
            .SingleOrDefaultAsync(a => a.GameServerId == gameServerId && a.Source == (int)RotationSource.ServerObserved
                && a.UnassignedAt == null, cancellationToken).ConfigureAwait(false);
        var observedAt = observation.ObservedAt.UtcDateTime;
        if (current?.ObservedAt > observedAt)
        {
            return ObservationError(HttpStatusCode.Conflict, "OBSERVATION_SUPERSEDED", "A newer observation has already been applied.");
        }
        var entries = observation.Entries.Select(e => new ObservedRotationEntryDto
        {
            MapName = e.MapName,
            GameMode = string.IsNullOrEmpty(e.GameMode) ? null : e.GameMode
        }).ToList();
        var unchanged = current != null && current.MapRotation.MapRotationMaps.OrderBy(m => m.SortOrder)
            .Select(m => new ObservedRotationEntryDto { MapName = m.MapName ?? m.Map.MapName!, GameMode = m.GameMode })
            .SequenceEqual(entries);
        if (current?.ObservedAt == observedAt && !unchanged)
        {
            return ObservationError(HttpStatusCode.Conflict, "OBSERVATION_CONFLICT", "The observation time already identifies different rotation content.");
        }
        if (unchanged)
        {
            if (current!.ObservedAt != observedAt)
            {
                current.ObservedAt = observedAt;
                current.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            return new ApiResponse<MapRotationServerAssignmentDto>(current!.ToDto()).ToApiResult();
        }
        var family = (int)gameType.GetMapGameType();
        var names = entries.Select(e => e.MapName.ToLowerInvariant()).Distinct().ToArray();
        var existingMaps = await context.Maps.Where(m => m.GameType == family
            && m.MapName != null && names.Contains(m.MapName.ToLower()))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var maps = existingMaps.GroupBy(m => m.MapName!.TrimEnd(' ').ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.GameType == (int)GameType.CallOfDuty4x).ThenBy(m => m.MapId).First());
        foreach (var name in names.Where(n => !maps.ContainsKey(n)))
        {
            var map = new Map { MapId = Guid.NewGuid(), GameType = (int)gameType.GetMapGameType(), MapName = name };
            maps.Add(name, map);
            context.Maps.Add(map);
        }
        var now = DateTime.UtcNow;
        var rotation = new MapRotation
        {
            MapRotationId = Guid.NewGuid(),
            GameType = (int)gameType.GetMapGameType(),
            Source = (int)RotationSource.ServerObserved,
            Title = "Server observed rotation",
            GameMode = "",
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now,
            MapRotationMaps = entries.Select((e, i) => new MapRotationMap
            {
                MapRotationMapId = Guid.NewGuid(),
                MapId = maps[e.MapName.ToLowerInvariant()].MapId,
                MapName = e.MapName,
                SortOrder = i,
                GameMode = e.GameMode
            }).ToList()
        };
        rotation.ContentHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(entries))));
        context.MapRotations.Add(rotation);
        if (current == null)
        {
            current = new MapRotationServerAssignment
            {
                MapRotationServerAssignmentId = Guid.NewGuid(),
                GameServerId = gameServerId,
                Source = (int)RotationSource.ServerObserved,
                DeploymentState = (int)DeploymentState.NotApplicable,
                ActivationState = (int)ActivationState.Inactive,
                ConfigVariableName = "sv_maprotation",
                CreatedAt = now
            };
            context.MapRotationServerAssignments.Add(current);
        }
        current.MapRotation = rotation;
        current.MapRotationId = rotation.MapRotationId;
        current.ObservedAt = observedAt;
        current.UpdatedAt = now;
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse<MapRotationServerAssignmentDto>(current.ToDto()).ToApiResult();
    }

    [HttpDelete("map-rotations/observed/unassigned")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupUnassignedObservedRotations(CancellationToken cancellationToken = default)
        => (await ((IMapRotationsApi)this).CleanupUnassignedObservedRotations(cancellationToken).ConfigureAwait(false)).ToHttpResult();

    Task<ApiResult> IMapRotationsApi.CleanupUnassignedObservedRotations(CancellationToken cancellationToken) =>
        ExecuteMutationAsync(async () =>
        {
            var rotations = await context.MapRotations
                .Include(r => r.MapRotationMaps)
                .Include(r => r.MapRotationServerAssignments).ThenInclude(a => a.MapRotationAssignmentOperations)
                .Where(r => r.Source == (int)RotationSource.ServerObserved
                    && !r.MapRotationServerAssignments.Any(a => a.UnassignedAt == null
                        || a.Source == (int)RotationSource.PortalManaged
                        || a.MapRotationAssignmentOperations.Any()))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            context.MapRotationMaps.RemoveRange(rotations.SelectMany(r => r.MapRotationMaps));
            context.MapRotationServerAssignments.RemoveRange(rotations.SelectMany(r => r.MapRotationServerAssignments));
            context.MapRotations.RemoveRange(rotations);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }, cancellationToken);

    private static ApiResult<MapRotationServerAssignmentDto> ObservationError(HttpStatusCode status, string code, string message)
        => new(status, new ApiResponse<MapRotationServerAssignmentDto>(new ApiError(code, message)));

    private static bool ValidToken(string? value, int maxLength) =>
        !string.IsNullOrEmpty(value) && value.Length <= maxLength
        && value.All(c => c > ' ' && c < '\u007f' && c is not ('"' or '\'' or ';' or '\\'));
}
