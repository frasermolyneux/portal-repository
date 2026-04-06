using System.Security.Cryptography;
using System.Text;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.V1.Mapping
{
    /// <summary>
    /// Mapping extensions for MapRotation entities and DTOs.
    /// </summary>
    public static class MapRotationsMappingExtensions
    {
        public static MapRotationDto ToDto(this MapRotation entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new MapRotationDto(
                entity.MapRotationId,
                (GameType)entity.GameType,
                entity.Title,
                entity.Description,
                entity.GameMode,
                entity.Version,
                entity.ContentHash,
                entity.CreatedAt,
                entity.UpdatedAt,
                (expand && entity.MapRotationMaps is not null)
                    ? entity.MapRotationMaps.OrderBy(m => m.SortOrder).Select(m => m.ToDto()).ToList()
                    : [],
                (expand && entity.MapRotationServerAssignments is not null)
                    ? entity.MapRotationServerAssignments.Select(a => a.ToDto(false)).ToList()
                    : []
            );
        }

        public static MapRotation ToEntity(this CreateMapRotationDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new MapRotation
            {
                GameType = (int)dto.GameType,
                Title = dto.Title,
                Description = dto.Description,
                GameMode = dto.GameMode,
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static void ApplyTo(this UpdateMapRotationDto dto, MapRotation entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.Title is not null) entity.Title = dto.Title;
            if (dto.Description is not null) entity.Description = dto.Description;
            if (dto.GameMode is not null) entity.GameMode = dto.GameMode;

            entity.UpdatedAt = DateTime.UtcNow;
        }

        public static MapRotationMapDto ToDto(this MapRotationMap entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new MapRotationMapDto
            {
                MapRotationMapId = entity.MapRotationMapId,
                MapId = entity.MapId,
                SortOrder = entity.SortOrder
            };
        }

        public static MapRotationServerAssignmentDto ToDto(this MapRotationServerAssignment entity, bool expand = true)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new MapRotationServerAssignmentDto(
                entity.MapRotationServerAssignmentId,
                entity.MapRotationId,
                entity.GameServerId,
                (DeploymentState)entity.DeploymentState,
                (ActivationState)entity.ActivationState,
                entity.DeployedVersion,
                entity.ActivatedVersion,
                entity.ConfigFilePath,
                entity.ConfigVariableName,
                entity.LastError,
                entity.LastErrorAt,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.UnassignedAt
            );
        }

        public static MapRotationServerAssignment ToEntity(this CreateMapRotationServerAssignmentDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new MapRotationServerAssignment
            {
                MapRotationId = dto.MapRotationId,
                GameServerId = dto.GameServerId,
                DeploymentState = (int)DeploymentState.Pending,
                ActivationState = (int)ActivationState.Inactive,
                ConfigFilePath = dto.ConfigFilePath,
                ConfigVariableName = dto.ConfigVariableName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static void ApplyTo(this UpdateMapRotationServerAssignmentDto dto, MapRotationServerAssignment entity)
        {
            ArgumentNullException.ThrowIfNull(dto);
            ArgumentNullException.ThrowIfNull(entity);

            if (dto.DeploymentState.HasValue) entity.DeploymentState = (int)dto.DeploymentState.Value;
            if (dto.ActivationState.HasValue) entity.ActivationState = (int)dto.ActivationState.Value;
            if (dto.DeployedVersion.HasValue) entity.DeployedVersion = dto.DeployedVersion.Value;
            if (dto.ActivatedVersion.HasValue) entity.ActivatedVersion = dto.ActivatedVersion.Value;
            if (dto.ConfigFilePath is not null) entity.ConfigFilePath = dto.ConfigFilePath;
            if (dto.ConfigVariableName is not null) entity.ConfigVariableName = dto.ConfigVariableName;
            if (dto.LastError is not null) entity.LastError = dto.LastError;
            if (dto.LastErrorAt.HasValue) entity.LastErrorAt = dto.LastErrorAt.Value;
            if (dto.UnassignedAt.HasValue) entity.UnassignedAt = dto.UnassignedAt.Value;

            entity.UpdatedAt = DateTime.UtcNow;
        }

        public static MapRotationAssignmentOperationDto ToDto(this MapRotationAssignmentOperation entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return new MapRotationAssignmentOperationDto(
                entity.MapRotationAssignmentOperationId,
                entity.MapRotationServerAssignmentId,
                (AssignmentOperationType)entity.OperationType,
                (AssignmentOperationStatus)entity.Status,
                entity.DurableFunctionInstanceId,
                entity.StartedAt,
                entity.CompletedAt,
                entity.Error
            );
        }

        public static MapRotationAssignmentOperation ToEntity(this CreateMapRotationAssignmentOperationDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new MapRotationAssignmentOperation
            {
                MapRotationServerAssignmentId = dto.MapRotationServerAssignmentId,
                OperationType = (int)dto.OperationType,
                Status = (int)AssignmentOperationStatus.InProgress,
                DurableFunctionInstanceId = dto.DurableFunctionInstanceId,
                StartedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Computes a SHA256 content hash from the ordered list of map IDs in a rotation.
        /// Used for stale detection on assignments.
        /// </summary>
        public static string ComputeContentHash(IEnumerable<Guid> orderedMapIds)
        {
            var input = string.Join(",", orderedMapIds);
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexStringLower(hashBytes);
        }
    }
}
