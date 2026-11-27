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
}
