using System.Net;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers;

public class MapRotationsControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GameModeEdit_IncrementsOnce_AndMetadataOnlyEditPreservesVersion(bool changeMaps)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var rotation = assignment.MapRotation;
        var first = new Map { MapId = Guid.NewGuid(), MapName = "mp_first", GameType = rotation.GameType };
        var second = new Map { MapId = Guid.NewGuid(), MapName = "mp_second", GameType = rotation.GameType };
        db.Maps.AddRange(first, second);
        rotation.MapRotationMaps.Add(new MapRotationMap { MapId = first.MapId, SortOrder = 0 });
        rotation.ContentHash = MapRotationsMappingExtensions.ComputeContentHash([first.MapId]);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var result = await api.UpdateMapRotation(new UpdateMapRotationDto(rotation.MapRotationId)
        {
            GameMode = "tdm",
            MapIds = changeMaps ? [second.MapId, first.MapId, second.MapId] : null
        });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, rotation.Version);
        Assert.Equal(2, assignment.ActivatedVersion);
        Assert.Equal((int)ActivationState.Inactive, assignment.ActivationState);
        Assert.Equal(changeMaps ? new[] { second.MapId, first.MapId } : new[] { first.MapId },
            rotation.MapRotationMaps.OrderBy(m => m.SortOrder).Select(m => m.MapId));
        await api.UpdateMapRotation(new UpdateMapRotationDto(rotation.MapRotationId) { Title = "Metadata", GameMode = "tdm" });
        Assert.Equal(3, rotation.Version);
    }

    [Theory]
    [InlineData(DeploymentState.Pending, 2, 2)]
    [InlineData(DeploymentState.Failed, 2, 2)]
    [InlineData(DeploymentState.Synced, 1, 2)]
    [InlineData(DeploymentState.Synced, 2, 1)]
    public async Task ActiveConfirmation_RejectsMissingCurrentEvidence(DeploymentState state, int deployed, int activated)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        assignment.DeploymentState = (int)state;
        assignment.DeployedVersion = deployed;
        assignment.ActivatedVersion = activated;
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var result = await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId) { ActivationState = ActivationState.Active });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal((int)ActivationState.Inactive, assignment.ActivationState);
    }

    [Fact]
    public async Task ActivationHandover_OnlyReplacesSameConfigTarget()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var next = Seed(db);
        var old = Clone(next, "small");
        var medium = Clone(next, "medium");
        old.ActivationState = medium.ActivationState = (int)ActivationState.Active;
        db.MapRotationServerAssignments.AddRange(old, medium);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var result = await api.UpdateServerAssignment(new(next.MapRotationServerAssignmentId) { ActivationState = ActivationState.Active });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal((int)ActivationState.Inactive, old.ActivationState);
        Assert.Null(old.ActivatedVersion);
        Assert.Equal((int)ActivationState.Active, medium.ActivationState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigTargetEdit_ClearsConfirmation_ButPreservesFileEvidence(bool editPath)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var result = await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            ConfigFilePath = editPath ? "other.cfg" : null,
            ConfigVariableName = editPath ? null : "large"
        });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(assignment.ActivatedVersion);
        Assert.Equal(2, assignment.DeployedVersion);
        Assert.Equal((int)DeploymentState.Synced, assignment.DeploymentState);
    }

    [Theory]
    [InlineData(ActivationState.Active, false)]
    [InlineData(ActivationState.Failed, false)]
    [InlineData(ActivationState.Inactive, true)]
    public async Task UnsafeTargetEditOrUnassign_IsRejected(ActivationState activation, bool inProgress)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        assignment.ActivationState = (int)activation;
        if (inProgress)
        {
            db.MapRotationAssignmentOperations.Add(Operation(assignment, AssignmentOperationStatus.InProgress));
        }
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId) { ConfigVariableName = "large" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId) { DeploymentState = DeploymentState.Removed })).StatusCode);
        Assert.Equal((int)activation, assignment.ActivationState);
        Assert.Equal("small", assignment.ConfigVariableName);
    }

    [Fact]
    public async Task Unassign_IsMetadataOnly_AndRotationDeleteCannotBypassRetention()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var operation = Operation(assignment, AssignmentOperationStatus.Succeeded);
        db.MapRotationAssignmentOperations.Add(operation);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.OK, (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);
        Assert.Equal((int)DeploymentState.Removed, assignment.DeploymentState);
        Assert.NotNull(assignment.UnassignedAt);
        Assert.Equal(2, assignment.DeployedVersion);
        Assert.Single(db.MapRotationAssignmentOperations);
        assignment.UnassignedAt = DateTime.UtcNow.AddDays(-89);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteMapRotation(assignment.MapRotationId)).StatusCode);
        assignment.UnassignedAt = operation.CompletedAt = operation.StartedAt = DateTime.UtcNow.AddDays(-91);
        await db.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.OK, (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);
        Assert.Empty(db.MapRotationAssignmentOperations);
        Assert.Empty(db.MapRotationServerAssignments);
    }

    [Fact]
    public async Task Completion_IsIdempotent_AndCancellationCannotBeOverwritten()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var operation = Operation(assignment, AssignmentOperationStatus.InProgress);
        db.MapRotationAssignmentOperations.Add(operation);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        await api.UpdateAssignmentOperation(operation.MapRotationAssignmentOperationId, AssignmentOperationStatus.Cancelled, "operator");
        var completedAt = operation.CompletedAt;
        Assert.Equal(HttpStatusCode.OK, (await api.UpdateAssignmentOperation(operation.MapRotationAssignmentOperationId, AssignmentOperationStatus.Cancelled, null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateAssignmentOperation(operation.MapRotationAssignmentOperationId, AssignmentOperationStatus.Succeeded, null)).StatusCode);
        Assert.Equal(completedAt, operation.CompletedAt);
        Assert.Equal("operator", operation.Error);
        Assert.Equal((int)AssignmentOperationStatus.Cancelled, operation.Status);
    }

    [Fact]
    public async Task EmptyLastError_ClearsTimestamp_EvenIfTimestampAlsoSupplied()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        assignment.LastError = "failure";
        assignment.LastErrorAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);
        await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId) { LastError = "", LastErrorAt = DateTime.UtcNow });
        Assert.Equal("", assignment.LastError);
        Assert.Null(assignment.LastErrorAt);
    }

    [Fact]
    public async Task RemovedAssignment_CannotBeResurrectedOrHaveRetentionBackdated()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var api = (IMapRotationsApi)new MapRotationsController(db);
        var before = DateTime.UtcNow;
        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            DeploymentState = DeploymentState.Removed,
            UnassignedAt = DateTime.UtcNow.AddDays(-100)
        })).StatusCode);
        Assert.True(assignment.UnassignedAt >= before);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            DeploymentState = DeploymentState.Synced
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateAssignmentOperation(new(assignment.MapRotationServerAssignmentId, AssignmentOperationType.Sync))).StatusCode);
    }

    [Fact]
    public async Task RecentOperationCompletion_ExtendsHistoryRetention()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        assignment.DeploymentState = (int)DeploymentState.Removed;
        assignment.UnassignedAt = DateTime.UtcNow.AddDays(-100);
        db.MapRotationAssignmentOperations.Add(Operation(assignment, AssignmentOperationStatus.Cancelled));
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteMapRotation(assignment.MapRotationId)).StatusCode);
        Assert.Single(db.MapRotationAssignmentOperations);
    }

    [Fact]
    public async Task SyncedConfirmation_RejectsStaleVersion_WithoutMarkingRuntimeInactive()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        assignment.ActivationState = (int)ActivationState.Active;
        assignment.DeploymentState = (int)DeploymentState.Syncing;
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            DeploymentState = DeploymentState.Synced,
            DeployedVersion = 1
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);
        Assert.Equal((int)ActivationState.Active, assignment.ActivationState);
        Assert.Equal((int)DeploymentState.Syncing, assignment.DeploymentState);
    }

    [Fact]
    public async Task FailedFileRepair_PreservesIndependentActiveConfigEvidence()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        assignment.ActivationState = (int)ActivationState.Active;
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            DeploymentState = DeploymentState.Failed,
            LastError = "Synthetic missing file",
            LastErrorAt = DateTime.UtcNow
        })).StatusCode);
        Assert.Equal((int)ActivationState.Active, assignment.ActivationState);
        Assert.Equal(2, assignment.ActivatedVersion);
        Assert.Equal((int)DeploymentState.Failed, assignment.DeploymentState);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);
    }

    [Fact]
    public async Task ConfigTargetEdit_RejectsOtherRunningServerOperation()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var other = Clone(assignment, "medium");
        db.MapRotationServerAssignments.Add(other);
        db.MapRotationAssignmentOperations.Add(Operation(other, AssignmentOperationStatus.InProgress));
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            ConfigFilePath = "other.cfg"
        })).StatusCode);
        Assert.Equal("server.cfg", assignment.ConfigFilePath);
        Assert.Equal(2, assignment.ActivatedVersion);
    }

    private static MapRotationServerAssignment Seed(PortalDbContext db)
    {
        var rotation = new MapRotation
        {
            MapRotationId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Title = "Synthetic",
            GameMode = "dm",
            Version = 2
        };
        var server = new GameServer
        {
            GameServerId = Guid.NewGuid(),
            GameType = rotation.GameType,
            Title = "Synthetic",
            Hostname = "127.0.0.1",
            QueryPort = 28960
        };
        var assignment = new MapRotationServerAssignment
        {
            MapRotationServerAssignmentId = Guid.NewGuid(),
            MapRotation = rotation,
            GameServer = server,
            MapRotationId = rotation.MapRotationId,
            GameServerId = server.GameServerId,
            ConfigFilePath = "server.cfg",
            ConfigVariableName = "small",
            DeploymentState = (int)DeploymentState.Synced,
            ActivationState = (int)ActivationState.Inactive,
            DeployedVersion = 2,
            ActivatedVersion = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.MapRotationServerAssignments.Add(assignment);
        db.SaveChanges();
        return assignment;
    }

    private static MapRotationServerAssignment Clone(MapRotationServerAssignment source, string target) => new()
    {
        MapRotationServerAssignmentId = Guid.NewGuid(),
        MapRotation = new MapRotation
        {
            MapRotationId = Guid.NewGuid(),
            GameType = source.MapRotation.GameType,
            Title = "Synthetic alternate",
            GameMode = "dm",
            Version = 2
        },
        GameServerId = source.GameServerId,
        ConfigFilePath = source.ConfigFilePath,
        ConfigVariableName = target,
        DeploymentState = source.DeploymentState,
        ActivationState = source.ActivationState,
        DeployedVersion = source.DeployedVersion,
        ActivatedVersion = source.ActivatedVersion
    };

    private static MapRotationAssignmentOperation Operation(MapRotationServerAssignment assignment, AssignmentOperationStatus status) => new()
    {
        MapRotationAssignmentOperationId = Guid.NewGuid(),
        MapRotationServerAssignmentId = assignment.MapRotationServerAssignmentId,
        OperationType = (int)AssignmentOperationType.Sync,
        Status = (int)status,
        StartedAt = DateTime.UtcNow,
        CompletedAt = status == AssignmentOperationStatus.InProgress ? null : DateTime.UtcNow
    };
}
