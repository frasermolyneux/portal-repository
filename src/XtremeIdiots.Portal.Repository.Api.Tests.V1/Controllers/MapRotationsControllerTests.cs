using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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
    [Fact]
    public async Task UnassignAmbiguousNormalTargets_DoesNotRemoveIndependentConfigurations()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var owner = Seed(db);
        owner.ConfigVariableName = "sv_maprotation";
        var duplicate = Clone(owner, "SV_MAPROTATION");
        duplicate.MapRotation = owner.MapRotation;
        duplicate.MapRotationId = owner.MapRotationId;
        duplicate.ConfigFilePath = "other.cfg";
        db.MapRotationServerAssignments.Add(duplicate);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteServerAssignment(owner.MapRotationServerAssignmentId)).StatusCode);
        Assert.All(await db.MapRotationServerAssignments.ToListAsync(), target =>
            Assert.Equal((int)DeploymentState.Synced, target.DeploymentState));
    }

    [Fact]
    public async Task EnablingAacpRequiresANormalConfigurationPathWithoutChangingEitherTarget()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var owner = Seed(db);
        owner.ConfigVariableName = "sv_maprotation";
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var result = await api.UpdateServerAssignment(new(owner.MapRotationServerAssignmentId)
        {
            ConfigFilePath = "",
            AacpEnabled = true,
            AacpConfigFilePath = "picker.cfg"
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("server.cfg", owner.ConfigFilePath);
        Assert.Equal(1, await db.MapRotationServerAssignments.CountAsync());
    }

    [Theory]
    [InlineData(ActivationState.Active, HttpStatusCode.BadRequest, false)]
    [InlineData(ActivationState.Inactive, HttpStatusCode.OK, false)]
    [InlineData(ActivationState.Inactive, HttpStatusCode.OK, true)]
    public async Task UnassignCombinedAssignment_IsAtomicAndPreservesOperations(ActivationState pickerState, HttpStatusCode expected, bool uppercase)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var owner = Seed(db);
        owner.ConfigVariableName = uppercase ? "SV_MAPROTATION" : "sv_maprotation";
        var picker = new MapRotationServerAssignment
        {
            MapRotationServerAssignmentId = Guid.NewGuid(),
            MapRotationId = owner.MapRotationId,
            GameServerId = owner.GameServerId,
            ConfigFilePath = "picker.cfg",
            ConfigVariableName = uppercase ? "SCR_AACP_MAPS_1" : "scr_aacp_maps_1",
            DeploymentState = (int)DeploymentState.Synced,
            ActivationState = (int)pickerState
        };
        db.MapRotationServerAssignments.Add(picker);
        db.MapRotationAssignmentOperations.Add(Operation(picker, AssignmentOperationStatus.Succeeded));
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var result = await api.DeleteServerAssignment(owner.MapRotationServerAssignmentId);

        Assert.Equal(expected, result.StatusCode);
        Assert.Equal((int)(expected == HttpStatusCode.OK ? DeploymentState.Removed : DeploymentState.Synced), owner.DeploymentState);
        Assert.Equal(owner.DeploymentState, picker.DeploymentState);
        Assert.Equal(1, await db.MapRotationAssignmentOperations.CountAsync());
        Assert.Equal(2, await db.MapRotationServerAssignments.CountAsync());
    }

    [Theory]
    [InlineData("sv_maprotation", "picker.cfg", HttpStatusCode.Created, 2)]
    [InlineData("sv_maprotation", "", HttpStatusCode.BadRequest, 0)]
    [InlineData("scr_small_rotation", "picker.cfg", HttpStatusCode.BadRequest, 0)]
    public async Task CreateCombinedAssignment_ValidatesBeforeSavingBothTargets(string variable, string path,
        HttpStatusCode status, int count)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var seeded = Seed(db);
        db.MapRotationServerAssignments.Remove(seeded);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var result = await api.CreateServerAssignment(new(seeded.MapRotationId, seeded.GameServerId)
        {
            ConfigVariableName = variable,
            ConfigFilePath = "normal.cfg",
            AacpConfigFilePath = path
        });

        Assert.Equal(status, result.StatusCode);
        var assignments = await db.MapRotationServerAssignments.ToListAsync();
        Assert.Equal(count, assignments.Count);
        if (count == 2)
        {
            Assert.Equal("sv_maprotation", result.Result!.Data!.ConfigVariableName);
            Assert.Equal(new[] { "scr_aacp_maps_1", "sv_maprotation" }, assignments.Select(a => a.ConfigVariableName).Order());
            Assert.All(assignments, a => Assert.Null(a.ActivatedVersion));
            Assert.All(assignments, a => Assert.Equal((int)ActivationState.Inactive, a.ActivationState));
        }
    }

    [Theory]
    [InlineData("sv_maprotation")]
    [InlineData("SV_MAPROTATION")]
    public async Task CombinedAssignment_EnablesAndDisablesOnlyInactivePickerWithoutDiscardingHistory(string normalVariable)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var owner = Seed(db);
        owner.ConfigVariableName = normalVariable;
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(owner.MapRotationServerAssignmentId)
        {
            AacpEnabled = true,
            AacpConfigFilePath = "picker.cfg"
        })).StatusCode);
        var picker = await db.MapRotationServerAssignments.SingleAsync(a => a.ConfigVariableName == "scr_aacp_maps_1");
        var operation = Operation(picker, AssignmentOperationStatus.Succeeded);
        db.MapRotationAssignmentOperations.Add(operation);
        await db.SaveChangesAsync();
        Assert.Null(picker.DeployedVersion);
        Assert.Equal(2, owner.DeployedVersion);

        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(owner.MapRotationServerAssignmentId)
        {
            AacpEnabled = false
        })).StatusCode);
        Assert.Equal((int)DeploymentState.Removed, picker.DeploymentState);
        Assert.NotNull(picker.UnassignedAt);
        Assert.Equal(1, await db.MapRotationAssignmentOperations.CountAsync());
        Assert.Equal((int)DeploymentState.Synced, owner.DeploymentState);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CombinedAssignment_RejectsActiveTargetChangesAndBusyGroup(bool changePath, bool busy)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var owner = Seed(db);
        owner.ConfigVariableName = "sv_maprotation";
        var picker = new MapRotationServerAssignment
        {
            MapRotationServerAssignmentId = Guid.NewGuid(),
            MapRotationId = owner.MapRotationId,
            GameServerId = owner.GameServerId,
            ConfigVariableName = "scr_aacp_maps_1",
            ConfigFilePath = "picker.cfg",
            ActivationState = (int)ActivationState.Active,
            DeploymentState = (int)DeploymentState.Synced
        };
        db.MapRotationServerAssignments.Add(picker);
        if (busy)
        {
            db.MapRotationAssignmentOperations.Add(Operation(owner, AssignmentOperationStatus.InProgress));
        }
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var result = await api.UpdateServerAssignment(new(owner.MapRotationServerAssignmentId)
        {
            AacpEnabled = changePath || busy,
            AacpConfigFilePath = changePath ? "different.cfg" : "picker.cfg"
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("picker.cfg", picker.ConfigFilePath);
        Assert.Equal((int)ActivationState.Active, picker.ActivationState);
        Assert.Equal((int)DeploymentState.Synced, picker.DeploymentState);
        Assert.Null(picker.UnassignedAt);
    }

    [Theory]
    [InlineData(MapRotationsOrder.ServerCountDesc)]
    [InlineData(MapRotationsOrder.ServerCountAsc)]
    public async Task ServerCountOrder_CountsDistinctAssignedServersBeforePagination(MapRotationsOrder order)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        static MapRotationServerAssignment Slot(MapRotationServerAssignment source, string target)
        {
            return new()
            {
                MapRotationServerAssignmentId = Guid.NewGuid(),
                MapRotation = source.MapRotation,
                GameServerId = source.GameServerId,
                GameServer = source.GameServer,
                ConfigFilePath = source.ConfigFilePath,
                ConfigVariableName = target,
                DeploymentState = source.DeploymentState,
                ActivationState = source.ActivationState
            };
        }

        var one = Seed(db);
        one.MapRotation.Title = "One server";
        db.MapRotationServerAssignments.Add(Slot(one, "picker"));
        db.MapRotationServerAssignments.Add(Slot(one, "medium"));
        for (var i = 0; i < 3; i++)
        {
            var retired = Slot(one, $"retired_{i}");
            retired.DeploymentState = (int)DeploymentState.Removed;
            db.MapRotationServerAssignments.Add(retired);
        }

        var two = Seed(db);
        two.MapRotation.Title = "Two servers";
        var secondServer = Slot(two, "second");
        secondServer.GameServerId = one.GameServerId;
        secondServer.GameServer = one.GameServer;
        db.MapRotationServerAssignments.Add(secondServer);
        var removed = Seed(db);
        removed.MapRotation.Title = "Retired";
        removed.DeploymentState = (int)DeploymentState.Removed;
        var empty = new MapRotation
        {
            MapRotationId = Guid.NewGuid(),
            Title = "Empty",
            GameMode = "dm",
            GameType = one.MapRotation.GameType
        };
        db.MapRotations.Add(empty);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var first = await api.GetMapRotations(null, null, null, 0, 2, order);
        var second = await api.GetMapRotations(null, null, null, 2, 2, order);

        Guid[] expected = order == MapRotationsOrder.ServerCountDesc
            ? [two.MapRotationId, one.MapRotationId, empty.MapRotationId, removed.MapRotationId]
            : [empty.MapRotationId, removed.MapRotationId, one.MapRotationId, two.MapRotationId];
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(expected, first.Result!.Data!.Items!.Concat(second.Result!.Data!.Items!).Select(r => r.MapRotationId));
    }

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigTargetEdit_RejectsOtherRunningServerOperation(bool aacp)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var other = Clone(assignment, "medium");
        var picker = new MapRotationServerAssignment
        {
            MapRotationServerAssignmentId = Guid.NewGuid(),
            MapRotationId = assignment.MapRotationId,
            GameServerId = assignment.GameServerId,
            ConfigFilePath = "picker.cfg",
            ConfigVariableName = "scr_aacp_maps_1",
            DeploymentState = (int)DeploymentState.Synced,
            ActivationState = (int)ActivationState.Inactive
        };
        if (aacp)
        {
            assignment.ConfigVariableName = "sv_maprotation";
            db.MapRotationServerAssignments.Add(picker);
        }
        db.MapRotationServerAssignments.Add(other);
        db.MapRotationAssignmentOperations.Add(Operation(other, AssignmentOperationStatus.InProgress));
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            ConfigFilePath = aacp ? null : "other.cfg",
            AacpEnabled = aacp ? true : null,
            AacpConfigFilePath = aacp ? "other-picker.cfg" : null
        })).StatusCode);
        Assert.Equal("server.cfg", assignment.ConfigFilePath);
        Assert.Equal(2, assignment.ActivatedVersion);
        Assert.Equal("picker.cfg", picker.ConfigFilePath);
    }

    [Fact]
    public async Task ReplayedDurableOperationCreate_ReusesInProgress_ButAllowsTerminalInstanceRetry()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var api = (IMapRotationsApi)new MapRotationsController(db);
        var request = new CreateMapRotationAssignmentOperationDto(assignment.MapRotationServerAssignmentId, AssignmentOperationType.Sync)
        {
            DurableFunctionInstanceId = "synthetic-retry"
        };

        Assert.Equal(HttpStatusCode.Created, (await api.CreateAssignmentOperation(request)).StatusCode);
        var original = Assert.Single(db.MapRotationAssignmentOperations);
        Assert.Equal(HttpStatusCode.Created, (await api.CreateAssignmentOperation(request)).StatusCode);
        Assert.Same(original, Assert.Single(db.MapRotationAssignmentOperations));
        await api.UpdateAssignmentOperation(original.MapRotationAssignmentOperationId, AssignmentOperationStatus.Cancelled, "operator");
        Assert.Equal(HttpStatusCode.Created, (await api.CreateAssignmentOperation(request)).StatusCode);
        Assert.Equal(2, db.MapRotationAssignmentOperations.Count());
        Assert.Single(db.MapRotationAssignmentOperations.Where(op => op.Status == (int)AssignmentOperationStatus.InProgress));
        Assert.Equal((int)AssignmentOperationStatus.Cancelled, original.Status);
        Assert.Equal("operator", original.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DurableOperationIdentity_DistinguishesOperationTypeAndAssignment(bool useLinkedAssignment)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var linked = Clone(assignment, "medium");
        db.MapRotationServerAssignments.Add(linked);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);
        var first = new CreateMapRotationAssignmentOperationDto(assignment.MapRotationServerAssignmentId, AssignmentOperationType.Sync)
        {
            DurableFunctionInstanceId = "synthetic-apply"
        };
        var second = new CreateMapRotationAssignmentOperationDto(
            useLinkedAssignment ? linked.MapRotationServerAssignmentId : assignment.MapRotationServerAssignmentId,
            useLinkedAssignment ? AssignmentOperationType.Sync : AssignmentOperationType.Activate)
        {
            DurableFunctionInstanceId = first.DurableFunctionInstanceId
        };

        await api.CreateAssignmentOperation(first);
        await api.CreateAssignmentOperation(second);
        await api.CreateAssignmentOperation(first);
        await api.CreateAssignmentOperation(second);
        Assert.Equal(2, db.MapRotationAssignmentOperations.Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LostCommitAcknowledgement_DeletionReplayReturnsSuccess_SeparateRequestKeepsChecks(bool purge)
    {
        using var db = CreateReplayContext();
        var assignment = Seed(db);
        if (purge)
        {
            assignment.DeploymentState = (int)DeploymentState.Removed;
            assignment.UnassignedAt = DateTime.UtcNow.AddDays(-91);
            await db.SaveChangesAsync();
        }
        var replay = (ReplayExecutionStrategyFactory)db.GetService<IExecutionStrategyFactory>();
        replay.ReplayNext = true;
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.OK, (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);
        Assert.Equal(purge ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest,
            (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);
        Assert.Equal(purge ? 0 : 1, db.MapRotationServerAssignments.Count());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LostCommitAcknowledgement_OperationReplayPrioritizesOriginalIdOverNewDurableAttempt(bool reuseExisting)
    {
        using var db = CreateReplayContext();
        var assignment = Seed(db);
        if (reuseExisting)
        {
            var existing = Operation(assignment, AssignmentOperationStatus.InProgress);
            existing.DurableFunctionInstanceId = "synthetic-lost-ack";
            db.MapRotationAssignmentOperations.Add(existing);
            await db.SaveChangesAsync();
        }
        var replay = (ReplayExecutionStrategyFactory)db.GetService<IExecutionStrategyFactory>();
        var originalId = Guid.Empty;
        replay.AfterCommit = () =>
        {
            var original = db.MapRotationAssignmentOperations.Single();
            originalId = original.MapRotationAssignmentOperationId;
            original.Status = (int)AssignmentOperationStatus.Cancelled;
            original.CompletedAt = DateTime.UtcNow;
            var newer = Operation(assignment, AssignmentOperationStatus.InProgress);
            newer.DurableFunctionInstanceId = original.DurableFunctionInstanceId;
            db.MapRotationAssignmentOperations.Add(newer);
            db.SaveChanges();
        };
        replay.ReplayNext = true;
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var response = await api.CreateAssignmentOperation(new(assignment.MapRotationServerAssignmentId, AssignmentOperationType.Sync)
        {
            DurableFunctionInstanceId = "synthetic-lost-ack"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(originalId, response.Result!.Data!.MapRotationAssignmentOperationId);
        Assert.Equal(AssignmentOperationStatus.Cancelled, response.Result.Data.Status);
        Assert.Equal(2, db.MapRotationAssignmentOperations.Count());
    }

    [Fact]
    public async Task LostCommitAcknowledgement_RemovedPatchReplaySucceeds_ButSeparateHistoryEditIsRejected()
    {
        using var db = CreateReplayContext();
        var assignment = Seed(db);
        var replay = (ReplayExecutionStrategyFactory)db.GetService<IExecutionStrategyFactory>();
        replay.ReplayNext = true;
        var api = (IMapRotationsApi)new MapRotationsController(db);
        var request = new UpdateMapRotationServerAssignmentDto(assignment.MapRotationServerAssignmentId)
        {
            DeploymentState = DeploymentState.Removed,
            UnassignedAt = DateTime.UtcNow.AddDays(-100)
        };

        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(request)).StatusCode);
        var removed = db.MapRotationServerAssignments.Single();
        Assert.Equal((int)DeploymentState.Removed, removed.DeploymentState);
        Assert.True(removed.UnassignedAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateServerAssignment(request)).StatusCode);
    }

    [Fact]
    public async Task LostCommitAcknowledgement_RotationDeletionReplaySucceeds_IndependentRequestReturnsNotFound()
    {
        using var db = CreateReplayContext();
        var rotation = new MapRotation
        {
            MapRotationId = Guid.NewGuid(),
            GameType = (int)GameType.CallOfDuty4,
            Title = "Synthetic empty rotation",
            GameMode = "dm",
            Version = 1
        };
        db.MapRotations.Add(rotation);
        await db.SaveChangesAsync();
        ((ReplayExecutionStrategyFactory)db.GetService<IExecutionStrategyFactory>()).ReplayNext = true;
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.OK, (await api.DeleteMapRotation(rotation.MapRotationId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.DeleteMapRotation(rotation.MapRotationId)).StatusCode);
        Assert.Empty(db.MapRotations);
    }

    [Theory]
    [InlineData(DeploymentState.Removed, ActivationState.Active)]
    [InlineData(DeploymentState.Removed, ActivationState.Failed)]
    [InlineData(DeploymentState.Removing, ActivationState.Active)]
    [InlineData(DeploymentState.Removing, ActivationState.Failed)]
    public async Task HistoricalRemovalState_AllowsConfirmedDeactivationWithoutResettingRetention(DeploymentState deployment, ActivationState activation)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        assignment.DeploymentState = (int)deployment;
        assignment.ActivationState = (int)activation;
        assignment.UnassignedAt = DateTime.UtcNow.AddDays(-10);
        var unassignedAt = assignment.UnassignedAt;
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        var created = await api.CreateAssignmentOperation(new(assignment.MapRotationServerAssignmentId, AssignmentOperationType.Deactivate));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            ActivationState = ActivationState.Deactivating,
            DeploymentState = deployment
        })).StatusCode);
        await api.UpdateAssignmentOperation(created.Result!.Data!.MapRotationAssignmentOperationId, AssignmentOperationStatus.Succeeded, null);
        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            ActivationState = ActivationState.Inactive,
            DeploymentState = deployment
        })).StatusCode);
        Assert.Equal((int)deployment, assignment.DeploymentState);
        Assert.Equal(unassignedAt, assignment.UnassignedAt);
        Assert.Equal((int)ActivationState.Inactive, assignment.ActivationState);
    }

    [Fact]
    public async Task UnassignWorkflow_ConfirmsRemovedBeforeCompletingItsOwnRemoveOperation()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var api = (IMapRotationsApi)new MapRotationsController(db);
        var operation = await api.CreateAssignmentOperation(new(assignment.MapRotationServerAssignmentId, AssignmentOperationType.Remove)
        {
            DurableFunctionInstanceId = "synthetic-unassign-v2"
        });
        Assert.Equal(HttpStatusCode.Created, operation.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteServerAssignment(assignment.MapRotationServerAssignmentId)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            DeploymentState = DeploymentState.Removed,
            UnassignedAt = DateTime.UtcNow
        })).StatusCode);
        Assert.Equal((int)DeploymentState.Removed, assignment.DeploymentState);
        Assert.Equal((int)ActivationState.Inactive, assignment.ActivationState);
        Assert.Equal(2, assignment.DeployedVersion);
        Assert.Equal(HttpStatusCode.OK, (await api.UpdateAssignmentOperation(operation.Result!.Data!.MapRotationAssignmentOperationId,
            AssignmentOperationStatus.Succeeded, null)).StatusCode);
        Assert.Equal((int)AssignmentOperationStatus.Succeeded, db.MapRotationAssignmentOperations.Single().Status);
    }

    [Theory]
    [InlineData(AssignmentOperationType.Sync)]
    [InlineData(AssignmentOperationType.Activate)]
    [InlineData(AssignmentOperationType.Deactivate)]
    [InlineData(AssignmentOperationType.Verify)]
    public async Task UnassignConfirmation_RejectsOtherInProgressOperations(AssignmentOperationType type)
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        var operation = Operation(assignment, AssignmentOperationStatus.InProgress);
        operation.OperationType = (int)type;
        db.MapRotationAssignmentOperations.Add(operation);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            DeploymentState = DeploymentState.Removed
        })).StatusCode);
        Assert.Equal((int)DeploymentState.Synced, assignment.DeploymentState);
    }

    [Fact]
    public async Task RepairWorkflow_CanStartSyncingWhileExistingConfigurationRemainsActive()
    {
        using var db = DbContextHelper.CreateInMemoryContext();
        var assignment = Seed(db);
        assignment.ActivationState = (int)ActivationState.Active;
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);
        await api.CreateAssignmentOperation(new(assignment.MapRotationServerAssignmentId, AssignmentOperationType.Sync));

        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            DeploymentState = DeploymentState.Syncing
        })).StatusCode);
        Assert.Equal((int)DeploymentState.Syncing, assignment.DeploymentState);
        Assert.Equal((int)ActivationState.Active, assignment.ActivationState);
        Assert.Equal(2, assignment.ActivatedVersion);
    }

    [Fact]
    public async Task LostCommitAcknowledgement_ConfigTargetEditNeverRestoresSuppliedOldActivationEvidence()
    {
        using var db = CreateReplayContext();
        var assignment = Seed(db);
        ((ReplayExecutionStrategyFactory)db.GetService<IExecutionStrategyFactory>()).ReplayNext = true;
        var api = (IMapRotationsApi)new MapRotationsController(db);

        Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            ConfigVariableName = "large",
            ActivatedVersion = 2
        })).StatusCode);
        var edited = db.MapRotationServerAssignments.Single();
        Assert.Equal("large", edited.ConfigVariableName);
        Assert.Null(edited.ActivatedVersion);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateServerAssignment(new(assignment.MapRotationServerAssignmentId)
        {
            ActivationState = ActivationState.Active
        })).StatusCode);
    }

    [Fact]
    public async Task LostCommitAcknowledgement_ActivationReplayCannotOverwriteNewerTargetHandover()
    {
        using var db = CreateReplayContext();
        var first = Seed(db);
        var newer = Clone(first, "small");
        db.MapRotationServerAssignments.Add(newer);
        await db.SaveChangesAsync();
        var api = (IMapRotationsApi)new MapRotationsController(db);
        var replay = (ReplayExecutionStrategyFactory)db.GetService<IExecutionStrategyFactory>();
        replay.AfterCommitAsync = async () =>
        {
            Assert.Equal(HttpStatusCode.OK, (await api.UpdateServerAssignment(new(newer.MapRotationServerAssignmentId)
            {
                ActivationState = ActivationState.Active,
                DeployedVersion = 2,
                ActivatedVersion = 2
            })).StatusCode);
        };
        replay.ReplayNext = true;

        var result = await api.UpdateServerAssignment(new(first.MapRotationServerAssignmentId)
        {
            ActivationState = ActivationState.Active,
            DeployedVersion = 2,
            ActivatedVersion = 2
        });

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal((int)ActivationState.Inactive, db.MapRotationServerAssignments.Single(a => a.MapRotationServerAssignmentId == first.MapRotationServerAssignmentId).ActivationState);
        Assert.Equal((int)ActivationState.Active, db.MapRotationServerAssignments.Single(a => a.MapRotationServerAssignmentId == newer.MapRotationServerAssignmentId).ActivationState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LostCommitAcknowledgement_RotationEditDoesNotOverwriteNewerContentOrIncrementTwice(bool interveningEdit)
    {
        using var db = CreateReplayContext();
        var assignment = Seed(db);
        var api = (IMapRotationsApi)new MapRotationsController(db);
        var replay = (ReplayExecutionStrategyFactory)db.GetService<IExecutionStrategyFactory>();
        if (interveningEdit)
        {
            replay.AfterCommitAsync = async () =>
            {
                Assert.Equal(HttpStatusCode.OK, (await api.UpdateMapRotation(new(assignment.MapRotationId)
                {
                    GameMode = "sd"
                })).StatusCode);
            };
        }
        replay.ReplayNext = true;

        var result = await api.UpdateMapRotation(new(assignment.MapRotationId) { GameMode = "tdm" });

        Assert.Equal(interveningEdit ? HttpStatusCode.BadRequest : HttpStatusCode.OK, result.StatusCode);
        var rotation = db.MapRotations.Single();
        Assert.Equal(interveningEdit ? "sd" : "tdm", rotation.GameMode);
        Assert.Equal(interveningEdit ? 4 : 3, rotation.Version);
    }

    private static PortalDbContext CreateReplayContext() => new(new DbContextOptionsBuilder<PortalDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ReplaceService<IExecutionStrategyFactory, ReplayExecutionStrategyFactory>().Options);

    public sealed class ReplayExecutionStrategyFactory(ExecutionStrategyDependencies dependencies) : IExecutionStrategyFactory
    {
        public bool ReplayNext { get; set; }
        public Action? AfterCommit { get; set; }
        public Func<Task>? AfterCommitAsync { get; set; }
        public IExecutionStrategy Create() => new ReplayExecutionStrategy(this, dependencies.CurrentContext.Context);
    }

    private sealed class ReplayExecutionStrategy(ReplayExecutionStrategyFactory factory, DbContext db) : IExecutionStrategy
    {
        public bool RetriesOnFailure => true;

        public TResult Execute<TState, TResult>(TState state, Func<DbContext, TState, TResult> operation,
            Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded)
            => operation(db, state);

        public async Task<TResult> ExecuteAsync<TState, TResult>(TState state,
            Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
            Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
            CancellationToken cancellationToken = default)
        {
            var replay = factory.ReplayNext;
            factory.ReplayNext = false;
            var result = await operation(db, state, cancellationToken);
            if (!replay)
            {
                return result;
            }

            factory.AfterCommit?.Invoke();
            if (factory.AfterCommitAsync != null)
            {
                await factory.AfterCommitAsync();
            }
            db.ChangeTracker.Clear();
            return await operation(db, state, cancellationToken);
        }
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
