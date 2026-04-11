using MX.Api.Abstractions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

namespace XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

public interface IMapRotationsApi
{
    // Map Rotation CRUD
    Task<ApiResult<MapRotationDto>> GetMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<MapRotationDto>>> GetMapRotations(GameType[]? gameTypes, string? gameMode, MapRotationsFilter? filter, int skipEntries, int takeEntries, MapRotationsOrder? order, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<MapRotationDto>>> GetMapRotations(GameType[]? gameTypes, string? gameMode, MapRotationStatus? status, string? filterString, MapRotationsFilter? filter, int skipEntries, int takeEntries, MapRotationsOrder? order, CancellationToken cancellationToken = default);
    Task<ApiResult<MapRotationDto>> CreateMapRotation(CreateMapRotationDto createMapRotationDto, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateMapRotation(UpdateMapRotationDto updateMapRotationDto, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default);

    // Server Assignment CRUD
    Task<ApiResult<MapRotationServerAssignmentDto>> GetServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<ApiResult<CollectionModel<MapRotationServerAssignmentDto>>> GetServerAssignments(Guid? mapRotationId, Guid? gameServerId, DeploymentState? deploymentState, int skipEntries, int takeEntries, CancellationToken cancellationToken = default);
    Task<ApiResult<MapRotationServerAssignmentDto>> CreateServerAssignment(CreateMapRotationServerAssignmentDto createDto, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateServerAssignment(UpdateMapRotationServerAssignmentDto updateDto, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default);

    // Assignment Operations
    Task<ApiResult<CollectionModel<MapRotationAssignmentOperationDto>>> GetAssignmentOperations(Guid assignmentId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default);
    Task<ApiResult<MapRotationAssignmentOperationDto>> CreateAssignmentOperation(CreateMapRotationAssignmentOperationDto createDto, CancellationToken cancellationToken = default);
    Task<ApiResult> UpdateAssignmentOperation(Guid operationId, AssignmentOperationStatus status, string? error, CancellationToken cancellationToken = default);
}
