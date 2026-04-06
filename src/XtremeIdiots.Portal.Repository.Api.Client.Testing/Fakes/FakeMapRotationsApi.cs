using System.Collections.Concurrent;
using System.Net;
using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

public class FakeMapRotationsApi : IMapRotationsApi
{
    private readonly ConcurrentDictionary<Guid, MapRotationDto> _mapRotations = new();
    private readonly ConcurrentDictionary<Guid, MapRotationServerAssignmentDto> _serverAssignments = new();
    private readonly ConcurrentDictionary<Guid, MapRotationAssignmentOperationDto> _assignmentOperations = new();
    private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, ApiError Error)> _errorResponses = new(StringComparer.OrdinalIgnoreCase);

    public FakeMapRotationsApi AddMapRotation(MapRotationDto mapRotation) { _mapRotations[mapRotation.MapRotationId] = mapRotation; return this; }
    public FakeMapRotationsApi AddServerAssignment(MapRotationServerAssignmentDto assignment) { _serverAssignments[assignment.MapRotationServerAssignmentId] = assignment; return this; }
    public FakeMapRotationsApi AddAssignmentOperation(MapRotationAssignmentOperationDto operation) { _assignmentOperations[operation.MapRotationAssignmentOperationId] = operation; return this; }
    public FakeMapRotationsApi AddErrorResponse(string operationKey, HttpStatusCode statusCode, string errorCode, string errorMessage)
    {
        _errorResponses[operationKey] = (statusCode, new ApiError(errorCode, errorMessage));
        return this;
    }
    public FakeMapRotationsApi Reset() { _mapRotations.Clear(); _serverAssignments.Clear(); _assignmentOperations.Clear(); _errorResponses.Clear(); return this; }

    // ───────────────────────── Map Rotation CRUD ─────────────────────────

    public Task<ApiResult<MapRotationDto>> GetMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default)
    {
        if (_mapRotations.TryGetValue(mapRotationId, out var mr))
            return Task.FromResult(new ApiResult<MapRotationDto>(HttpStatusCode.OK, new ApiResponse<MapRotationDto>(mr)));
        return Task.FromResult(new ApiResult<MapRotationDto>(HttpStatusCode.NotFound, new ApiResponse<MapRotationDto>(new ApiError("NOT_FOUND", "Map rotation not found"))));
    }

    public Task<ApiResult<CollectionModel<MapRotationDto>>> GetMapRotations(GameType[]? gameTypes, string? gameMode, MapRotationsFilter? filter, int skipEntries, int takeEntries, MapRotationsOrder? order, CancellationToken cancellationToken = default)
    {
        var items = _mapRotations.Values.AsEnumerable();
        if (gameTypes != null) items = items.Where(mr => gameTypes.Contains(mr.GameType));
        if (gameMode != null) items = items.Where(mr => mr.GameMode == gameMode);
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapRotationDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<MapRotationDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapRotationDto>>(collection)));
    }

    public Task<ApiResult<MapRotationDto>> CreateMapRotation(CreateMapRotationDto createMapRotationDto, CancellationToken cancellationToken = default)
    {
        var dto = new MapRotationDto(Guid.NewGuid(), createMapRotationDto.GameType, createMapRotationDto.Title, createMapRotationDto.Description, createMapRotationDto.GameMode, 1, null, DateTime.UtcNow, DateTime.UtcNow, [], []);
        _mapRotations[dto.MapRotationId] = dto;
        return Task.FromResult(new ApiResult<MapRotationDto>(HttpStatusCode.Created, new ApiResponse<MapRotationDto>(dto)));
    }

    public Task<ApiResult> UpdateMapRotation(UpdateMapRotationDto updateMapRotationDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    // ───────────────────────── Server Assignment CRUD ─────────────────────────

    public Task<ApiResult<MapRotationServerAssignmentDto>> GetServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        if (_serverAssignments.TryGetValue(assignmentId, out var a))
            return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.OK, new ApiResponse<MapRotationServerAssignmentDto>(a)));
        return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.NotFound, new ApiResponse<MapRotationServerAssignmentDto>(new ApiError("NOT_FOUND", "Server assignment not found"))));
    }

    public Task<ApiResult<CollectionModel<MapRotationServerAssignmentDto>>> GetServerAssignments(Guid? mapRotationId, Guid? gameServerId, DeploymentState? deploymentState, int skipEntries, int takeEntries, CancellationToken cancellationToken = default)
    {
        var items = _serverAssignments.Values.AsEnumerable();
        if (mapRotationId.HasValue) items = items.Where(a => a.MapRotationId == mapRotationId.Value);
        if (gameServerId.HasValue) items = items.Where(a => a.GameServerId == gameServerId.Value);
        if (deploymentState.HasValue) items = items.Where(a => a.DeploymentState == deploymentState.Value);
        var list = items.Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapRotationServerAssignmentDto> { Items = list };
        return Task.FromResult(new ApiResult<CollectionModel<MapRotationServerAssignmentDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapRotationServerAssignmentDto>>(collection)));
    }

    public Task<ApiResult<MapRotationServerAssignmentDto>> CreateServerAssignment(CreateMapRotationServerAssignmentDto createDto, CancellationToken cancellationToken = default)
    {
        var dto = new MapRotationServerAssignmentDto(Guid.NewGuid(), createDto.MapRotationId, createDto.GameServerId, DeploymentState.Pending, ActivationState.Inactive, null, null, createDto.ConfigFilePath, createDto.ConfigVariableName, null, null, DateTime.UtcNow, DateTime.UtcNow, null);
        _serverAssignments[dto.MapRotationServerAssignmentId] = dto;
        return Task.FromResult(new ApiResult<MapRotationServerAssignmentDto>(HttpStatusCode.Created, new ApiResponse<MapRotationServerAssignmentDto>(dto)));
    }

    public Task<ApiResult> UpdateServerAssignment(UpdateMapRotationServerAssignmentDto updateDto, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
    public Task<ApiResult> DeleteServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));

    // ───────────────────────── Assignment Operations ─────────────────────────

    public Task<ApiResult<CollectionModel<MapRotationAssignmentOperationDto>>> GetAssignmentOperations(Guid assignmentId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default)
    {
        var items = _assignmentOperations.Values.Where(op => op.MapRotationServerAssignmentId == assignmentId).Skip(skipEntries).Take(takeEntries).ToList();
        var collection = new CollectionModel<MapRotationAssignmentOperationDto> { Items = items };
        return Task.FromResult(new ApiResult<CollectionModel<MapRotationAssignmentOperationDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<MapRotationAssignmentOperationDto>>(collection)));
    }

    public Task<ApiResult<MapRotationAssignmentOperationDto>> CreateAssignmentOperation(CreateMapRotationAssignmentOperationDto createDto, CancellationToken cancellationToken = default)
    {
        var dto = new MapRotationAssignmentOperationDto(Guid.NewGuid(), createDto.MapRotationServerAssignmentId, createDto.OperationType, AssignmentOperationStatus.InProgress, createDto.DurableFunctionInstanceId, DateTime.UtcNow, null, null);
        _assignmentOperations[dto.MapRotationAssignmentOperationId] = dto;
        return Task.FromResult(new ApiResult<MapRotationAssignmentOperationDto>(HttpStatusCode.Created, new ApiResponse<MapRotationAssignmentOperationDto>(dto)));
    }

    public Task<ApiResult> UpdateAssignmentOperation(Guid operationId, AssignmentOperationStatus status, string? error, CancellationToken cancellationToken = default) => Task.FromResult(new ApiResult(HttpStatusCode.OK, new ApiResponse()));
}
