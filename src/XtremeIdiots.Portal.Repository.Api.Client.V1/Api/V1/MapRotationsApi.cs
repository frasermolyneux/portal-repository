using Microsoft.Extensions.Logging;
using MX.Api.Abstractions;
using MX.Api.Client;
using MX.Api.Client.Auth;
using MX.Api.Client.Extensions;
using RestSharp;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.MapRotations;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public class MapRotationsApi : BaseApi<RepositoryApiClientOptions>, IMapRotationsApi
    {
        public MapRotationsApi(ILogger<BaseApi<RepositoryApiClientOptions>> logger, IApiTokenProvider apiTokenProvider, IRestClientService restClientService, RepositoryApiClientOptions options) : base(logger, apiTokenProvider, restClientService, options)
        {
        }

        // ───────────────────────── Map Rotation CRUD ─────────────────────────

        public async Task<ApiResult<MapRotationDto>> GetMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/map-rotations/{mapRotationId}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<MapRotationDto>();
        }

        public async Task<ApiResult<CollectionModel<MapRotationDto>>> GetMapRotations(GameType[]? gameTypes, string? gameMode, MapRotationsFilter? filter, int skipEntries, int takeEntries, MapRotationsOrder? order, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/map-rotations", Method.Get).ConfigureAwait(false);

            if (gameTypes != null)
                request.AddQueryParameter("gameTypes", string.Join(",", gameTypes));

            if (gameMode != null)
                request.AddQueryParameter("gameMode", gameMode);

            if (filter.HasValue)
                request.AddQueryParameter("filter", filter.ToString());

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            if (order.HasValue)
                request.AddQueryParameter("order", order.ToString());

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<MapRotationDto>>();
        }

        public async Task<ApiResult<MapRotationDto>> CreateMapRotation(CreateMapRotationDto createMapRotationDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/map-rotations", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(createMapRotationDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<MapRotationDto>();
        }

        public async Task<ApiResult> UpdateMapRotation(UpdateMapRotationDto updateMapRotationDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/map-rotations/{updateMapRotationDto.MapRotationId}", Method.Patch).ConfigureAwait(false);
            request.AddJsonBody(updateMapRotationDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteMapRotation(Guid mapRotationId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/map-rotations/{mapRotationId}", Method.Delete).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        // ───────────────────────── Server Assignment CRUD ─────────────────────────

        public async Task<ApiResult<MapRotationServerAssignmentDto>> GetServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/map-rotations/assignments/{assignmentId}", Method.Get).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<MapRotationServerAssignmentDto>();
        }

        public async Task<ApiResult<CollectionModel<MapRotationServerAssignmentDto>>> GetServerAssignments(Guid? mapRotationId, Guid? gameServerId, DeploymentState? deploymentState, int skipEntries, int takeEntries, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/map-rotations/assignments", Method.Get).ConfigureAwait(false);

            if (mapRotationId.HasValue)
                request.AddQueryParameter("mapRotationId", mapRotationId.Value.ToString());

            if (gameServerId.HasValue)
                request.AddQueryParameter("gameServerId", gameServerId.Value.ToString());

            if (deploymentState.HasValue)
                request.AddQueryParameter("deploymentState", deploymentState.ToString());

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<MapRotationServerAssignmentDto>>();
        }

        public async Task<ApiResult<MapRotationServerAssignmentDto>> CreateServerAssignment(CreateMapRotationServerAssignmentDto createDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/map-rotations/assignments", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(createDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<MapRotationServerAssignmentDto>();
        }

        public async Task<ApiResult> UpdateServerAssignment(UpdateMapRotationServerAssignmentDto updateDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/map-rotations/assignments/{updateDto.MapRotationServerAssignmentId}", Method.Patch).ConfigureAwait(false);
            request.AddJsonBody(updateDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        public async Task<ApiResult> DeleteServerAssignment(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/map-rotations/assignments/{assignmentId}", Method.Delete).ConfigureAwait(false);
            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }

        // ───────────────────────── Assignment Operations ─────────────────────────

        public async Task<ApiResult<CollectionModel<MapRotationAssignmentOperationDto>>> GetAssignmentOperations(Guid assignmentId, int skipEntries, int takeEntries, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/map-rotations/assignments/{assignmentId}/operations", Method.Get).ConfigureAwait(false);

            request.AddQueryParameter("skipEntries", skipEntries.ToString());
            request.AddQueryParameter("takeEntries", takeEntries.ToString());

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<CollectionModel<MapRotationAssignmentOperationDto>>();
        }

        public async Task<ApiResult<MapRotationAssignmentOperationDto>> CreateAssignmentOperation(CreateMapRotationAssignmentOperationDto createDto, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync("v1/map-rotations/assignments/operations", Method.Post).ConfigureAwait(false);
            request.AddJsonBody(createDto);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult<MapRotationAssignmentOperationDto>();
        }

        public async Task<ApiResult> UpdateAssignmentOperation(Guid operationId, AssignmentOperationStatus status, string? error, CancellationToken cancellationToken = default)
        {
            var request = await CreateRequestAsync($"v1/map-rotations/assignments/operations/{operationId}", Method.Patch).ConfigureAwait(false);

            request.AddQueryParameter("status", status.ToString());

            if (error != null)
                request.AddQueryParameter("error", error);

            var response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

            return response.ToApiResult();
        }
    }
}
