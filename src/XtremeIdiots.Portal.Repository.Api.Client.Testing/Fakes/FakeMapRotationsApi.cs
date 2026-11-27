using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Extensions.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeMapRotationsApi : IMapRotationsApi
{
    private readonly ConcurrentDictionary<Guid, MapRotationDto> _mapRotations = new();
    private readonly ConcurrentDictionary<Guid, MapRotationServerAssignmentDto> _serverAssignments = new();
    private readonly ConcurrentDictionary<Guid, MapRotationAssignmentOperationDto> _assignmentOperations = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);
    private PublicServerRotationsDto _publicRotations = new();
    private readonly ConcurrentDictionary<Guid, PublicServerRotationDto> _effectiveRotations = new();
    private readonly ConcurrentDictionary<Guid, ObserveServerRotationDto> _observations = new();

    public FakeMapRotationsApi SetPublicServerRotations(PublicServerRotationsDto rotations) { _publicRotations = rotations; return this; }
    public FakeMapRotationsApi SetEffectiveServerRotation(PublicServerRotationDto rotation)
    {
        _effectiveRotations[rotation.GameServerId] = rotation;
        return this;
    }

    public Task<ApiResult<PublicServerRotationDto>> GetEffectiveServerRotation(Guid gameServerId, CancellationToken cancellationToken = default)
        => Task.FromResult(_effectiveRotations.TryGetValue(gameServerId, out var rotation)
            ? new ApiResult<PublicServerRotationDto>(HttpStatusCode.OK, new ApiResponse<PublicServerRotationDto>(rotation))
            : new ApiResult<PublicServerRotationDto>(HttpStatusCode.NotFound, new ApiResponse<PublicServerRotationDto>(
                new ApiError("NOT_FOUND", "Set the effective rotation for this server before reading it."))));

    public Task<ApiResult<MapRotationServerAssignmentDto>> ReplaceObservedServerRotation(
        Guid gameServerId, ObserveServerRotationDto observation, CancellationToken cancellationToken = default)
    {
        if (_errorResponses.TryGetValue(nameof(ReplaceObservedServerRotation), out var configured))
        {
            return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(configured.StatusCode,
                new ApiResponse<MapRotationServerAssignmentDto>(configured.Error)));
        }
        if (observation == null || observation.ObservedAt == default || observation.ObservedAt.Offset != TimeSpan.Zero
            || observation.Entries == null || observation.Entries.Any(e => e == null || string.IsNullOrWhiteSpace(e.MapName)))
        {
            return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("INVALID_OBSERVATION", "Supply a UTC observation time and explicit entries."))));
        }
        if (_serverAssignments.Values.Any(a => a.GameServerId == gameServerId && a.Source == RotationSource.PortalManaged
            && a.UnassignedAt == null && a.DeploymentState != DeploymentState.Removed))
        {
            return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.Conflict,
                new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("PORTAL_MANAGED_ASSIGNMENT_EXISTS", "A portal-managed assignment owns this server."))));
        }
        var current = _serverAssignments.Values.SingleOrDefault(a => a.GameServerId == gameServerId
            && a.Source == RotationSource.ServerObserved && a.UnassignedAt == null);
        if (current?.ObservedAt > observation.ObservedAt.UtcDateTime)
        {
            return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.Conflict,
                new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("OBSERVATION_SUPERSEDED", "A newer observation has already been applied."))));
        }
        var saved = observation with
        {
            Entries = observation.Entries.Select(e => e with
            { MapName = e.MapName, GameMode = string.IsNullOrEmpty(e.GameMode) ? null : e.GameMode }).ToList()
        };
        var unchanged = _observations.TryGetValue(gameServerId, out var previous)
            && previous.Entries.SequenceEqual(saved.Entries);
        if (current?.ObservedAt == observation.ObservedAt.UtcDateTime && !unchanged)
        {
            return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.Conflict,
                new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("OBSERVATION_CONFLICT", "The observation time already identifies different content."))));
        }
        var dto = current ?? new MapRotationServerAssignmentDto(Guid.NewGuid(), Guid.NewGuid(), gameServerId,
            DeploymentState.NotApplicable, ActivationState.Inactive, null, null, null, "sv_maprotation", null, null,
            DateTime.UtcNow, DateTime.UtcNow, null)
        { Source = RotationSource.ServerObserved };
        if (current != null && !unchanged)
        {
            dto.MapRotationId = Guid.NewGuid();
        }
        dto.ObservedAt = observation.ObservedAt.UtcDateTime;
        _observations[gameServerId] = saved;
        _serverAssignments[dto.MapRotationServerAssignmentId] = dto;
        return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.OK, new ApiResponse<MapRotationServerAssignmentDto>(dto)));
    }

    public Task<ApiResult> CleanupUnassignedObservedRotations(CancellationToken cancellationToken = default)
    {
        if (_errorResponses.TryGetValue(nameof(CleanupUnassignedObservedRotations), out var configured))
        {
            return Task.FromResult(new ApiResult(configured.StatusCode, new ApiResponse(configured.Error)));
        }
        foreach (var rotation in _mapRotations.Values.Where(r => r.Source == RotationSource.ServerObserved
            && !_serverAssignments.Values.Any(a => a.MapRotationId == r.MapRotationId)))
        {
            _mapRotations.TryRemove(rotation.MapRotationId, out _);
        }
        return Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    }

    public Task<ApiResult<PublicServerRotationsDto>> GetPublicServerRotations(CancellationToken cancellationToken = default)
        => Task.FromResult(new ApiResult<PublicServerRotationsDto>(HttpStatusCode.OK, new ApiResponse<PublicServerRotationsDto>(_publicRotations)));

    public FakeMapRotationsApi AddMapRotation(MapRotationDto mapRotation) { _mapRotations[mapRotation.MapRotationId] = mapRotation; return this; }
    public FakeMapRotationsApi AddServerAssignment(MapRotationServerAssignmentDto assignment) { _serverAssignments[assignment.MapRotationServerAssignmentId] = assignment; return this; }
    public FakeMapRotationsApi AddAssignmentOperation(MapRotationAssignmentOperationDto operation) { _assignmentOperations[operation.MapRotationAssignmentOperationId] = operation; return this; }
    public FakeMapRotationsApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeMapRotationsApi Reset() { _mapRotations.Clear(); _serverAssignments.Clear(); _assignmentOperations.Clear(); _errorResponses.Clear(); _effectiveRotations.Clear(); _observations.Clear(); _publicRotations = new(); return this; }

    // ───────────────────────── Map Rotation CRUD ─────────────────────────

    public Task<ApiResult<MapRotationDto>> GetMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default)
    {
        if (_mapRotations.TryGetValue(mapRotationId, out var mr))
        {
            return Task.FromResult(new ApiResult<MapRotationDto>(HttpStatusCode.OK, new ApiResponse<MapRotationDto>(mr)));
        }

        return Task.FromResult(new ApiResult<MapRotationDto>(HttpStatusCode.NotFound, new ApiResponse<MapRotationDto>(new ApiError("NOT_FOUND", "Map rotation not found"))));
    }

    public Task<ApiResult<CollectionModel<MapRotationDto>>> GetMapRotations(GameType[]? gameTypes, string? gameMode, MapRotationsFilter? filter, int skipEntries, int takeEntries, MapRotationsOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _mapRotations.Values.Where(r => r.Source == RotationSource.PortalManaged);
        if (gameTypes?.Length > 0)
        {
            var compatibleGameTypes = gameTypes.SelectMany(type => type.GetCompatibleMapGameTypes()).ToHashSet();
            items = items.Where(mr => compatibleGameTypes.Contains(mr.GameType));
        }

        if (gameMode != null)
        {
            items = items.Where(mr => mr.GameMode == gameMode);
        }

        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapRotationDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<MapRotationDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapRotationDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<MapRotationDto>>> GetMapRotations(GameType[]? gameTypes, string? gameMode, MapRotationStatus? status, string? filterString, MapRotationsFilter? filter, int skipEntries, int takeEntries, MapRotationsOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _mapRotations.Values.Where(r => r.Source == RotationSource.PortalManaged);
        if (gameTypes?.Length > 0)
        {
            var compatibleGameTypes = gameTypes.SelectMany(type => type.GetCompatibleMapGameTypes()).ToHashSet();
            items = items.Where(mr => compatibleGameTypes.Contains(mr.GameType));
        }

        if (gameMode != null)
        {
            items = items.Where(mr => mr.GameMode == gameMode);
        }

        if (status.HasValue)
        {
            items = items.Where(mr => mr.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filterString))
        {
            items = items.Where(mr => mr.Title.Contains(filterString, StringComparison.OrdinalIgnoreCase));
        }

        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapRotationDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<MapRotationDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapRotationDto>>(collection)));
    }

    public Task<ApiResult<CollectionModel<MapRotationDto>>> GetMapRotations(GameType[]? gameTypes, string? gameMode, MapRotationStatus? status, string? filterString, Guid? createdByUserId, MapRotationsFilter? filter, int skipEntries, int takeEntries, MapRotationsOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _mapRotations.Values.Where(r => r.Source == RotationSource.PortalManaged);
        if (gameTypes?.Length > 0)
        {
            var compatibleGameTypes = gameTypes.SelectMany(type => type.GetCompatibleMapGameTypes()).ToHashSet();
            items = items.Where(mr => compatibleGameTypes.Contains(mr.GameType));
        }

        if (gameMode != null)
        {
            items = items.Where(mr => mr.GameMode == gameMode);
        }

        if (status.HasValue)
        {
            items = items.Where(mr => mr.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filterString))
        {
            items = items.Where(mr => mr.Title.Contains(filterString, StringComparison.OrdinalIgnoreCase));
        }

        if (createdByUserId.HasValue)
        {
            items = items.Where(mr => mr.CreatedByUserId == createdByUserId.Value);
        }

        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapRotationDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<MapRotationDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapRotationDto>>(collection)));
    }

    public Task<ApiResult<MapRotationDto>> CreateMapRotation(CreateMapRotationDto createMapRotationDto, CancellationToken cancellationToken = default)
    {
        var entries = createMapRotationDto.Entries ?? createMapRotationDto.MapIds.Select(id => new MapRotationEntryDto { MapId = id }).ToList();
        var dto = new MapRotationDto(Guid.NewGuid(), createMapRotationDto.GameType, createMapRotationDto.Title, createMapRotationDto.Description, createMapRotationDto.GameMode, 1, null, DateTime.UtcNow, DateTime.UtcNow,
            entries.Select((entry, index) => new MapRotationMapDto { MapRotationMapId = Guid.NewGuid(), MapId = entry.MapId, GameMode = entry.GameMode, SortOrder = index }).ToList(), []);
        _mapRotations[dto.MapRotationId] = dto;
        return Task.FromResult(new ApiResult<MapRotationDto>(HttpStatusCode.Created, new ApiResponse<MapRotationDto>(dto)));
    }

    public Task<ApiResult> UpdateMapRotation(UpdateMapRotationDto updateMapRotationDto, CancellationToken cancellationToken = default)
        => Task.FromResult(RotationIsObserved(updateMapRotationDto.MapRotationId) ? ReadOnlyObservation() : new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default)
        => Task.FromResult(RotationIsObserved(mapRotationId) ? ReadOnlyObservation() : new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    // ───────────────────────── Server Assignment CRUD ─────────────────────────

    public Task<ApiResult<MapRotationServerAssignmentDto>> GetServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        if (_serverAssignments.TryGetValue(assignmentId, out var a))
        {
            return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.OK, new ApiResponse<MapRotationServerAssignmentDto>(a)));
        }

        return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.NotFound, new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("NOT_FOUND", "Server assignment not found"))));
    }

    public Task<ApiResult<CollectionModel<MapRotationServerAssignmentDto>>> GetServerAssignments(Guid? mapRotationId, Guid? gameServerId, DeploymentState? deploymentState, int skipEntries, int takeEntries, CancellationToken cancellationToken = default)
        => GetServerAssignments(mapRotationId, gameServerId, deploymentState, RotationSource.PortalManaged, skipEntries, takeEntries, cancellationToken);

    public Task<ApiResult<CollectionModel<MapRotationServerAssignmentDto>>> GetServerAssignments(
        Guid? mapRotationId, Guid? gameServerId, DeploymentState? deploymentState,
        RotationSource source, int skipEntries, int takeEntries, CancellationToken cancellationToken = default)
    {
        var items = _serverAssignments.Values.Where(a => a.Source == source);
        if (mapRotationId.HasValue)
        {
            items = items.Where(a => a.MapRotationId == mapRotationId.Value);
        }

        if (gameServerId.HasValue)
        {
            items = items.Where(a => a.GameServerId == gameServerId.Value);
        }

        if (deploymentState.HasValue)
        {
            items = items.Where(a => a.DeploymentState == deploymentState.Value);
        }

        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapRotationServerAssignmentDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<MapRotationServerAssignmentDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapRotationServerAssignmentDto>>(collection)));
    }

    public Task<ApiResult<MapRotationServerAssignmentDto>> CreateServerAssignment(CreateMapRotationServerAssignmentDto createDto, CancellationToken cancellationToken = default)
    {
        if (RotationIsObserved(createDto.MapRotationId))
        {
            return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.BadRequest,
                new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("OBSERVED_ROTATION_READ_ONLY", "Observed rotations cannot be manually assigned."))));
        }
        var dto = new MapRotationServerAssignmentDto(Guid.NewGuid(), createDto.MapRotationId, createDto.GameServerId, DeploymentState.Pending, ActivationState.Inactive, null, null, createDto.ConfigFilePath, createDto.ConfigVariableName, null, null, DateTime.UtcNow, DateTime.UtcNow, null);
        _serverAssignments[dto.MapRotationServerAssignmentId] = dto;
        if (createDto.AacpConfigFilePath != null)
        {
            var picker = dto with
            {
                MapRotationServerAssignmentId = Guid.NewGuid(),
                ConfigFilePath = createDto.AacpConfigFilePath,
                ConfigVariableName = "scr_aacp_maps_1"
            };
            _serverAssignments[picker.MapRotationServerAssignmentId] = picker;
        }
        return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.Created, new ApiResponse<MapRotationServerAssignmentDto>(dto)));
    }

    public Task<ApiResult> UpdateServerAssignment(UpdateMapRotationServerAssignmentDto updateDto, CancellationToken cancellationToken = default)
        => Task.FromResult(AssignmentIsObserved(updateDto.MapRotationServerAssignmentId) || updateDto.DeploymentState == DeploymentState.NotApplicable
            ? ReadOnlyObservation() : new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default)
        => Task.FromResult(AssignmentIsObserved(assignmentId) ? ReadOnlyObservation() : new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    // ───────────────────────── Assignment Operations ─────────────────────────

    public Task<ApiResult<CollectionModel<MapRotationAssignmentOperationDto>>> GetAssignmentOperations(Guid assignmentId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default)
    {
        var items = _assignmentOperations.Values.Where(op => op.MapRotationServerAssignmentId == assignmentId).Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapRotationAssignmentOperationDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<MapRotationAssignmentOperationDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapRotationAssignmentOperationDto>>(collection)));
    }

    public Task<ApiResult<MapRotationAssignmentOperationDto>> CreateAssignmentOperation(CreateMapRotationAssignmentOperationDto createDto, CancellationToken cancellationToken = default)
    {
        if (AssignmentIsObserved(createDto.MapRotationServerAssignmentId))
        {
            return Task.FromResult(new ApiResult<MapRotationAssignmentOperationDto>(HttpStatusCode.BadRequest,
                new ApiResponse<MapRotationAssignmentOperationDto>(new ApiError("ASSIGNMENT_IN_USE", "Observed assignments cannot run deployment operations."))));
        }
        var dto = new MapRotationAssignmentOperationDto(Guid.NewGuid(), createDto.MapRotationServerAssignmentId, createDto.OperationType, AssignmentOperationStatus.InProgress, createDto.DurableFunctionInstanceId, DateTime.UtcNow, null, null);
        _assignmentOperations[dto.MapRotationAssignmentOperationId] = dto;
        return Task.FromResult(new ApiResult<MapRotationAssignmentOperationDto>(HttpStatusCode.Created, new ApiResponse<MapRotationAssignmentOperationDto>(dto)));
    }

    public Task<ApiResult> UpdateAssignmentOperation(Guid operationId, AssignmentOperationStatus status, string? error, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    private bool RotationIsObserved(Guid id) => _mapRotations.TryGetValue(id, out var rotation) && rotation.Source == RotationSource.ServerObserved;
    private bool AssignmentIsObserved(Guid id) => _serverAssignments.TryGetValue(id, out var assignment) && assignment.Source == RotationSource.ServerObserved;
    private static ApiResult ReadOnlyObservation() => new(HttpStatusCode.BadRequest,
        new ApiResponse(new ApiError("OBSERVED_ASSIGNMENT_READ_ONLY", "Observed data has no managed lifecycle.")));
}
