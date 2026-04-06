using System.Net;
using System.Text;

using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class MapRotationsTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MapRotationsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetMapRotations_ReturnsOk()
    {
        var rotationId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.MapRotations.Add(new MapRotation
            {
                MapRotationId = rotationId,
                GameType = (int)GameType.CallOfDuty4,
                Title = "Test Rotation",
                GameMode = "dm",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync("/v1.0/map-rotations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Rotation", content);
    }

    [Fact]
    public async Task GetMapRotation_ReturnsOk_WhenExists()
    {
        var rotationId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.MapRotations.Add(new MapRotation
            {
                MapRotationId = rotationId,
                GameType = (int)GameType.CallOfDuty5,
                Title = "Sniper Rotation",
                GameMode = "tdm",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync($"/v1.0/map-rotations/{rotationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Sniper Rotation", content);
    }

    [Fact]
    public async Task GetMapRotation_ReturnsNotFound_WhenMissing()
    {
        var response = await _client.GetAsync($"/v1.0/map-rotations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateMapRotation_ReturnsCreated()
    {
        var mapId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.Maps.Add(new Map
            {
                MapId = mapId,
                GameType = (int)GameType.CallOfDuty4,
                MapName = "mp_crash"
            });
            ctx.SaveChanges();
        });

        var dto = new CreateMapRotationDto(GameType.CallOfDuty4, "DM Small Rotation", "dm")
        {
            Description = "Small maps for DM",
            MapIds = [mapId]
        };

        var json = JsonConvert.SerializeObject(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/map-rotations", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("DM Small Rotation", responseContent);
    }

    [Fact]
    public async Task UpdateMapRotation_ReturnsOk()
    {
        var rotationId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.MapRotations.Add(new MapRotation
            {
                MapRotationId = rotationId,
                GameType = (int)GameType.CallOfDuty4,
                Title = "Original Title",
                GameMode = "dm",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var dto = new UpdateMapRotationDto(rotationId) { Title = "Updated Title" };
        var json = JsonConvert.SerializeObject(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/v1.0/map-rotations/{rotationId}")
        {
            Content = content
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMapRotation_ReturnsOk()
    {
        var rotationId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.MapRotations.Add(new MapRotation
            {
                MapRotationId = rotationId,
                GameType = (int)GameType.CallOfDuty4,
                Title = "To Delete",
                GameMode = "dm",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.DeleteAsync($"/v1.0/map-rotations/{rotationId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify it's gone
        var getResponse = await _client.GetAsync($"/v1.0/map-rotations/{rotationId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateServerAssignment_ReturnsCreated()
    {
        var rotationId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.MapRotations.Add(new MapRotation
            {
                MapRotationId = rotationId,
                GameType = (int)GameType.CallOfDuty4,
                Title = "DM Rotation",
                GameMode = "dm",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "DM Server",
                GameType = (int)GameType.CallOfDuty4,
                Hostname = "127.0.0.1",
                QueryPort = 28960
            });
            ctx.SaveChanges();
        });

        var dto = new CreateMapRotationServerAssignmentDto(rotationId, serverId)
        {
            ConfigFilePath = "server.cfg",
            ConfigVariableName = "sv_maprotation"
        };

        var json = JsonConvert.SerializeObject(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/map-rotations/assignments", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateServerAssignment_ReturnsBadRequest_WhenGameTypeMismatch()
    {
        var rotationId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.MapRotations.Add(new MapRotation
            {
                MapRotationId = rotationId,
                GameType = (int)GameType.CallOfDuty4,
                Title = "COD4 Rotation",
                GameMode = "dm",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "COD5 Server",
                GameType = (int)GameType.CallOfDuty5,
                Hostname = "127.0.0.1",
                QueryPort = 28960
            });
            ctx.SaveChanges();
        });

        var dto = new CreateMapRotationServerAssignmentDto(rotationId, serverId);

        var json = JsonConvert.SerializeObject(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/map-rotations/assignments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetServerAssignment_ReturnsOk_WhenExists()
    {
        var rotationId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.MapRotations.Add(new MapRotation
            {
                MapRotationId = rotationId,
                GameType = (int)GameType.CallOfDuty4,
                Title = "Rotation",
                GameMode = "dm",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "Server",
                GameType = (int)GameType.CallOfDuty4,
                Hostname = "127.0.0.1",
                QueryPort = 28960
            });
            ctx.MapRotationServerAssignments.Add(new MapRotationServerAssignment
            {
                MapRotationServerAssignmentId = assignmentId,
                MapRotationId = rotationId,
                GameServerId = serverId,
                DeploymentState = (int)DeploymentState.Pending,
                ActivationState = (int)ActivationState.Inactive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync($"/v1.0/map-rotations/assignments/{assignmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAssignmentOperations_ReturnsOk()
    {
        var rotationId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.MapRotations.Add(new MapRotation
            {
                MapRotationId = rotationId,
                GameType = (int)GameType.CallOfDuty4,
                Title = "Rotation",
                GameMode = "dm",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "Server",
                GameType = (int)GameType.CallOfDuty4,
                Hostname = "127.0.0.1",
                QueryPort = 28960
            });
            ctx.MapRotationServerAssignments.Add(new MapRotationServerAssignment
            {
                MapRotationServerAssignmentId = assignmentId,
                MapRotationId = rotationId,
                GameServerId = serverId,
                DeploymentState = (int)DeploymentState.Syncing,
                ActivationState = (int)ActivationState.Inactive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.MapRotationAssignmentOperations.Add(new MapRotationAssignmentOperation
            {
                MapRotationAssignmentOperationId = operationId,
                MapRotationServerAssignmentId = assignmentId,
                OperationType = (int)AssignmentOperationType.Sync,
                Status = (int)AssignmentOperationStatus.InProgress,
                StartedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.GetAsync($"/v1.0/map-rotations/assignments/{assignmentId}/operations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateMapRotation_ReturnsBadRequest_WhenMapIdsInvalid()
    {
        var dto = new CreateMapRotationDto(GameType.CallOfDuty4, "Bad Maps Rotation", "dm")
        {
            MapIds = [Guid.NewGuid()]
        };

        var json = JsonConvert.SerializeObject(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/map-rotations", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("INVALID_MAP_IDS", body);
    }

    [Fact]
    public async Task CreateMapRotation_ReturnsBadRequest_WhenMapGameTypeMismatch()
    {
        var mapId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.Maps.Add(new Map
            {
                MapId = mapId,
                GameType = (int)GameType.CallOfDuty5,
                MapName = "mp_cod5map"
            });
            ctx.SaveChanges();
        });

        var dto = new CreateMapRotationDto(GameType.CallOfDuty4, "Mismatched Rotation", "dm")
        {
            MapIds = [mapId]
        };

        var json = JsonConvert.SerializeObject(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/map-rotations", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("MAP_GAME_TYPE_MISMATCH", body);
    }

    [Fact]
    public async Task DeleteMapRotation_ReturnsNonSuccess_WhenActiveAssignmentsExist()
    {
        var rotationId = Guid.NewGuid();
        var serverId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        _factory.SeedDatabase(ctx =>
        {
            ctx.GameServers.Add(new GameServer
            {
                GameServerId = serverId,
                Title = "Server",
                GameType = (int)GameType.CallOfDuty4,
                Hostname = "127.0.0.1",
                QueryPort = 28960
            });
            ctx.MapRotations.Add(new MapRotation
            {
                MapRotationId = rotationId,
                GameType = (int)GameType.CallOfDuty4,
                Title = "Active Rotation",
                GameMode = "dm",
                Version = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();

            ctx.MapRotationServerAssignments.Add(new MapRotationServerAssignment
            {
                MapRotationServerAssignmentId = assignmentId,
                MapRotationId = rotationId,
                GameServerId = serverId,
                DeploymentState = (int)DeploymentState.Synced,
                ActivationState = (int)ActivationState.Inactive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            ctx.SaveChanges();
        });

        var response = await _client.DeleteAsync($"/v1.0/map-rotations/{rotationId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAssignmentOperation_ReturnsBadRequest_WhenAssignmentMissing()
    {
        var dto = new CreateMapRotationAssignmentOperationDto(Guid.NewGuid(), AssignmentOperationType.Sync);

        var json = JsonConvert.SerializeObject(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/v1.0/map-rotations/assignments/operations", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("ASSIGNMENT_NOT_FOUND", body);
    }
}
