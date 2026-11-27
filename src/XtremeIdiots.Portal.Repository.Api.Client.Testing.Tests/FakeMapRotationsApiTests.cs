using System.Net;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;
using XtremeIdiots.Portal.Repository.Api.Client.Testing.Fakes;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

public class FakeMapRotationsApiTests
{
    [Fact]
    public async Task ObservationFake_PreservesOwnershipIdempotencyAndSourceScopedReads()
    {
        var api = new FakeMapRotationsApi();
        var serverId = Guid.NewGuid();
        var observation = new ObserveServerRotationDto { ObservedAt = DateTimeOffset.UtcNow, Entries = [new() { MapName = "mp_map" }] };
        var created = (await api.ReplaceObservedServerRotation(serverId, observation)).Result!.Data!;
        var rotationId = created.MapRotationId;
        observation.ObservedAt = observation.ObservedAt.AddSeconds(1);
        Assert.Equal(rotationId, (await api.ReplaceObservedServerRotation(serverId, observation)).Result!.Data!.MapRotationId);
        Assert.Empty((await api.GetServerAssignments(null, serverId, null, 0, 20)).Result!.Data!.Items!);
        Assert.Single((await api.GetServerAssignments(null, serverId, null, RotationSource.ServerObserved, 0, 20)).Result!.Data!.Items!);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateAssignmentOperation(new(created.MapRotationServerAssignmentId, AssignmentOperationType.Sync))).StatusCode);
        observation.Entries = [];
        Assert.Equal(HttpStatusCode.Conflict, (await api.ReplaceObservedServerRotation(serverId, observation)).StatusCode);
        observation.ObservedAt = observation.ObservedAt.AddSeconds(1);
        Assert.NotEqual(rotationId, (await api.ReplaceObservedServerRotation(serverId, observation)).Result!.Data!.MapRotationId);
        await api.CreateServerAssignment(new(Guid.NewGuid(), serverId));
        observation.ObservedAt = observation.ObservedAt.AddSeconds(1);
        var managed = await api.ReplaceObservedServerRotation(serverId, observation);
        Assert.Equal(HttpStatusCode.Conflict, managed.StatusCode);
        Assert.Equal("PORTAL_MANAGED_ASSIGNMENT_EXISTS", Assert.Single(managed.Result!.Errors!).Code);
    }

    [Fact]
    public async Task ObservationFake_StoresReadOnlySnapshotsUntilCleanup()
    {
        var api = new FakeMapRotationsApi();
        var serverId = Guid.NewGuid();
        var observation = new ObserveServerRotationDto
        {
            ObservedAt = DateTimeOffset.UtcNow,
            Entries = [new() { MapName = "MP_Test" }, new() { MapName = "mp_test", GameMode = "SD" }]
        };
        var assignment = (await api.ReplaceObservedServerRotation(serverId, observation)).Result!.Data!;
        var firstId = assignment.MapRotationId;
        var snapshot = (await api.GetMapRotation(firstId)).Result!.Data!;
        Assert.Equal(RotationSource.ServerObserved, snapshot.Source);
        Assert.Equal(new[] { "MP_Test", "mp_test" }, snapshot.MapRotationMaps.Select(m => m.MapName));
        Assert.Equal(new[] { 0, 1 }, snapshot.MapRotationMaps.Select(m => m.SortOrder));
        Assert.Equal(snapshot.MapRotationMaps[0].MapId, snapshot.MapRotationMaps[1].MapId);
        Assert.Equal("SD", snapshot.MapRotationMaps[1].GameMode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.UpdateMapRotation(new(firstId))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.DeleteMapRotation(firstId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.CreateServerAssignment(new(firstId, serverId))).StatusCode);

        observation.ObservedAt = observation.ObservedAt.AddSeconds(1);
        observation.Entries = [];
        var currentId = (await api.ReplaceObservedServerRotation(serverId, observation)).Result!.Data!.MapRotationId;
        Assert.Equal(HttpStatusCode.OK, (await api.GetMapRotation(firstId)).StatusCode);
        await api.CleanupUnassignedObservedRotations();
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetMapRotation(firstId)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.GetMapRotation(currentId)).StatusCode);
    }

    [Fact]
    public async Task ObservationCleanup_RemovesOnlyUnassignedSnapshotsWithoutProtectedReferences()
    {
        var api = new FakeMapRotationsApi();
        var now = DateTime.UtcNow;
        MapRotationDto Rotation() => new(Guid.NewGuid(), GameType.CallOfDuty4, "Observed", null, "", 1, null, now, now, [], [])
        { Source = RotationSource.ServerObserved };
        MapRotationServerAssignmentDto Assignment(MapRotationDto rotation, RotationSource source, DateTime? unassignedAt) =>
            new(Guid.NewGuid(), rotation.MapRotationId, Guid.NewGuid(), DeploymentState.NotApplicable,
                ActivationState.Inactive, null, null, null, null, null, null, now, now, unassignedAt) { Source = source };
        var removable = Rotation();
        var managed = Rotation();
        var active = Rotation();
        var operation = Rotation();
        var old = Assignment(removable, RotationSource.ServerObserved, now);
        var managedReference = Assignment(managed, RotationSource.PortalManaged, now);
        var activeReference = Assignment(active, RotationSource.ServerObserved, null);
        var operationReference = Assignment(operation, RotationSource.ServerObserved, now);
        api.AddMapRotation(removable).AddMapRotation(managed).AddMapRotation(active).AddMapRotation(operation)
            .AddServerAssignment(old).AddServerAssignment(managedReference).AddServerAssignment(activeReference)
            .AddServerAssignment(operationReference)
            .AddAssignmentOperation(new(Guid.NewGuid(), operationReference.MapRotationServerAssignmentId,
                AssignmentOperationType.Sync, AssignmentOperationStatus.InProgress, null, now, null, null));

        await api.CleanupUnassignedObservedRotations();

        Assert.Equal(HttpStatusCode.NotFound, (await api.GetMapRotation(removable.MapRotationId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await api.GetServerAssignment(old.MapRotationServerAssignmentId)).StatusCode);
        foreach (var retained in new[] { managed, active, operation })
        {
            Assert.Equal(HttpStatusCode.OK, (await api.GetMapRotation(retained.MapRotationId)).StatusCode);
        }
    }
}
