using System.Net;
using System.Linq.Expressions;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.RulesEngine;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public sealed class RuleConnectionBindingsController(PortalDbContext context) : ControllerBase, IRuleConnectionBindingsApi
{
    private static readonly TimeSpan SnapshotLifetime = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpPost("game-servers/{gameServerId:guid}/rule-connection-status")]
    [ProducesResponseType<ApiResponse<RuleConnectionStatusSnapshotResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ObserveSnapshot(
        Guid gameServerId,
        RuleConnectionStatusSnapshotDto request,
        CancellationToken cancellationToken = default)
    {
        var result = await ((IRuleConnectionBindingsApi)this)
            .ObserveSnapshot(gameServerId, request, cancellationToken).ConfigureAwait(false);
        return result.ToHttpResult();
    }

    [HttpGet("game-servers/{gameServerId:guid}/rule-connections/{slotId:int}/{playerGuid}")]
    [ProducesResponseType<ApiResponse<RuleConnectionBindingDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentBinding(
        Guid gameServerId,
        int slotId,
        string playerGuid,
        CancellationToken cancellationToken = default)
    {
        var result = await ((IRuleConnectionBindingsApi)this)
            .GetCurrentBinding(gameServerId, slotId, playerGuid, cancellationToken).ConfigureAwait(false);
        return result.ToHttpResult();
    }

    [HttpGet("game-servers/{gameServerId:guid}/rule-capabilities")]
    [ProducesResponseType<ApiResponse<RuleServerCapabilitiesDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentCapabilities(
        Guid gameServerId,
        CancellationToken cancellationToken = default)
    {
        var result = await ((IRuleConnectionBindingsApi)this)
            .GetCurrentCapabilities(gameServerId, cancellationToken).ConfigureAwait(false);
        return result.ToHttpResult();
    }

    async Task<ApiResult<RuleConnectionStatusSnapshotResultDto>> IRuleConnectionBindingsApi.ObserveSnapshot(
        Guid gameServerId,
        RuleConnectionStatusSnapshotDto request,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        if (gameServerId == Guid.Empty || request is null || request.GameServerId != gameServerId ||
            !ValidSnapshot(request, now))
        {
            return new ApiResult<RuleConnectionStatusSnapshotResultDto>(HttpStatusCode.BadRequest);
        }

        return await ExecuteMutationAsync(gameServerId, async () =>
        {
            var server = await context.GameServers.AsNoTracking().SingleOrDefaultAsync(
                s => s.GameServerId == gameServerId && !s.Deleted, cancellationToken).ConfigureAwait(false);
            if (server is null)
            {
                return new ApiResult<RuleConnectionStatusSnapshotResultDto>(HttpStatusCode.NotFound);
            }
            if (server.GameType != (int)request.GameType)
            {
                return new ApiResult<RuleConnectionStatusSnapshotResultDto>(HttpStatusCode.Conflict);
            }

            var integrationCapabilities = NormalizeCapabilities(request.IntegrationCapabilities);
            var runtimeCapabilities = NormalizeCapabilities(request.Runtime.Capabilities);
            var snapshotJson = JsonSerializer.Serialize(request, JsonOptions);
            var observedAt = request.ObservedAtUtc.UtcDateTime;
            var expiresAt = observedAt + SnapshotLifetime;
            var current = await context.RuleConnectionStatusSnapshots.SingleOrDefaultAsync(
                s => s.GameServerId == gameServerId, cancellationToken).ConfigureAwait(false);
            var reportedIdentities = request.Players
                .Where(player => player.State != RuleConnectionState.Unknown)
                .Select(player => (player.SlotId, PlayerGuid: NormalizePlayerGuid(player.PlayerGuid)))
                .ToArray();
            var connectedSlots = request.Players
                .Where(player => player.State == RuleConnectionState.Connected)
                .Select(player => player.SlotId)
                .Distinct()
                .ToArray();
            var bindings = await context.RuleConnectionBindings
                .Where(CreateRelevantBindingsPredicate(
                    gameServerId, reportedIdentities, connectedSlots, now.UtcDateTime))
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            var bindingsByIdentity = bindings.ToDictionary(binding => (binding.SlotId, binding.PlayerGuid));
            var bindingsBySlot = bindings.GroupBy(binding => binding.SlotId)
                .ToDictionary(group => group.Key, group => group.ToList());

            if (current is not null)
            {
                if (current.SnapshotId == request.SnapshotId)
                {
                    if (current.SequenceId == request.SequenceId && current.SnapshotJson == snapshotJson)
                    {
                        return new ApiResponse<RuleConnectionStatusSnapshotResultDto>(
                            new RuleConnectionStatusSnapshotResultDto
                            {
                                SnapshotId = request.SnapshotId,
                                SequenceId = request.SequenceId,
                                Duplicate = true
                            }).ToApiResult();
                    }
                    return new ApiResult<RuleConnectionStatusSnapshotResultDto>(HttpStatusCode.Conflict);
                }

                if (current.RuntimeInstanceId == request.Runtime.InstanceId)
                {
                    if (current.RuntimeStartedAtUtc != request.Runtime.StartedAtUtc.UtcDateTime ||
                        current.RuntimeId != request.Runtime.Id ||
                        current.RuntimeVersion != request.Runtime.Version ||
                        current.RuntimeProtocolVersion != request.Runtime.ProtocolVersion ||
                        current.RuntimeIdentityModel != (int)request.Runtime.IdentityModel ||
                        request.SequenceId <= current.SequenceId ||
                        observedAt < current.ObservedAtUtc)
                    {
                        return new ApiResult<RuleConnectionStatusSnapshotResultDto>(HttpStatusCode.Conflict);
                    }
                }
                else if (request.Runtime.StartedAtUtc.UtcDateTime <= current.RuntimeStartedAtUtc ||
                    observedAt < current.ObservedAtUtc)
                {
                    return new ApiResult<RuleConnectionStatusSnapshotResultDto>(HttpStatusCode.Conflict);
                }
            }

            var duplicateSnapshotId = await context.RuleConnectionStatusSnapshots.AsNoTracking().AnyAsync(
                s => s.SnapshotId == request.SnapshotId && s.GameServerId != gameServerId,
                cancellationToken).ConfigureAwait(false);
            if (duplicateSnapshotId)
            {
                return new ApiResult<RuleConnectionStatusSnapshotResultDto>(HttpStatusCode.Conflict);
            }

            var updatedBindings = 0;
            foreach (var player in request.Players)
            {
                if (player.State == RuleConnectionState.Unknown)
                {
                    continue;
                }
                var playerGuid = NormalizePlayerGuid(player.PlayerGuid);

                bindingsByIdentity.TryGetValue((player.SlotId, playerGuid), out var binding);

                if (player.State == RuleConnectionState.Connected)
                {
                    if (binding is not null && !CanAdvanceBinding(binding, request))
                    {
                        continue;
                    }

                    if (bindingsBySlot.TryGetValue(player.SlotId, out var slotBindings))
                    {
                        foreach (var previousOccupant in slotBindings.Where(
                            binding => binding.PlayerGuid != playerGuid &&
                                binding.State == RuleConnectionState.Connected.ToString()))
                        {
                            if (!CanAdvanceBinding(previousOccupant, request))
                            {
                                continue;
                            }

                            previousOccupant.State = RuleConnectionState.Disconnected.ToString();
                            previousOccupant.SnapshotId = request.SnapshotId;
                            previousOccupant.SequenceId = request.SequenceId;
                            previousOccupant.ObservedAtUtc = observedAt;
                            previousOccupant.ExpiresAtUtc = expiresAt;
                            previousOccupant.Version++;
                            previousOccupant.UpdatedAtUtc = now.UtcDateTime;
                            updatedBindings++;
                        }
                    }

                    if (binding is null)
                    {
                        binding = new RuleConnectionBinding
                        {
                            GameServerId = gameServerId,
                            SlotId = player.SlotId,
                            PlayerGuid = playerGuid
                        };
                        context.RuleConnectionBindings.Add(binding);
                        bindings.Add(binding);
                        bindingsByIdentity.Add((player.SlotId, playerGuid), binding);
                        if (!bindingsBySlot.TryGetValue(player.SlotId, out slotBindings))
                        {
                            slotBindings = [];
                            bindingsBySlot.Add(player.SlotId, slotBindings);
                        }
                        slotBindings.Add(binding);
                    }

                    binding.GameType = (int)request.GameType;
                    binding.State = RuleConnectionState.Connected.ToString();
                    binding.ConnectionId = player.ConnectionId!.Value;
                    binding.ConnectionGeneration = player.ConnectionGeneration is { } generation
                        ? generation
                        : null;
                    binding.RuntimeId = request.Runtime.Id;
                    binding.RuntimeVersion = request.Runtime.Version;
                    binding.RuntimeProtocolVersion = request.Runtime.ProtocolVersion;
                    binding.RuntimeIdentityModel = (int)request.Runtime.IdentityModel;
                    binding.RuntimeInstanceId = request.Runtime.InstanceId;
                    binding.RuntimeStartedAtUtc = request.Runtime.StartedAtUtc.UtcDateTime;
                    binding.SnapshotId = request.SnapshotId;
                    binding.SequenceId = request.SequenceId;
                    binding.ObservedAtUtc = observedAt;
                    binding.ExpiresAtUtc = expiresAt;
                    binding.Version = binding.Version == 0 ? 1 : binding.Version + 1;
                    binding.UpdatedAtUtc = now.UtcDateTime;
                    updatedBindings++;
                    continue;
                }

                if (binding is null || binding.State != RuleConnectionState.Connected.ToString() ||
                    !MatchesExactBinding(binding, player, request) ||
                    request.SequenceId <= binding.SequenceId)
                {
                    continue;
                }

                binding.State = RuleConnectionState.Disconnected.ToString();
                binding.SnapshotId = request.SnapshotId;
                binding.SequenceId = request.SequenceId;
                binding.ObservedAtUtc = observedAt;
                binding.ExpiresAtUtc = expiresAt;
                binding.Version++;
                binding.UpdatedAtUtc = now.UtcDateTime;
                updatedBindings++;
            }

            if (current is null)
            {
                current = new RuleConnectionStatusSnapshot { GameServerId = gameServerId };
                context.RuleConnectionStatusSnapshots.Add(current);
            }
            current.SnapshotId = request.SnapshotId;
            current.GameType = (int)request.GameType;
            current.RuntimeId = request.Runtime.Id;
            current.RuntimeVersion = request.Runtime.Version;
            current.RuntimeProtocolVersion = request.Runtime.ProtocolVersion;
            current.RuntimeIdentityModel = (int)request.Runtime.IdentityModel;
            current.RuntimeInstanceId = request.Runtime.InstanceId;
            current.RuntimeStartedAtUtc = request.Runtime.StartedAtUtc.UtcDateTime;
            current.SequenceId = request.SequenceId;
            current.ObservedAtUtc = observedAt;
            current.ExpiresAtUtc = expiresAt;
            current.IntegrationCapabilitiesJson = JsonSerializer.Serialize(integrationCapabilities, JsonOptions);
            current.RuntimeCapabilitiesJson = JsonSerializer.Serialize(runtimeCapabilities, JsonOptions);
            current.SnapshotJson = snapshotJson;
            current.UpdatedAtUtc = now.UtcDateTime;

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse<RuleConnectionStatusSnapshotResultDto>(
                new RuleConnectionStatusSnapshotResultDto
                {
                    SnapshotId = request.SnapshotId,
                    SequenceId = request.SequenceId,
                    UpdatedBindings = updatedBindings
                }).ToApiResult();
        }, cancellationToken).ConfigureAwait(false);
    }

    async Task<ApiResult<RuleConnectionBindingDto>> IRuleConnectionBindingsApi.GetCurrentBinding(
        Guid gameServerId,
        int slotId,
        string playerGuid,
        CancellationToken cancellationToken)
    {
        if (gameServerId == Guid.Empty || slotId < 0 || string.IsNullOrWhiteSpace(playerGuid))
        {
            return new ApiResult<RuleConnectionBindingDto>(HttpStatusCode.BadRequest);
        }

        var now = DateTime.UtcNow;
        var binding = await (
            from candidate in context.RuleConnectionBindings.AsNoTracking()
            join snapshot in context.RuleConnectionStatusSnapshots.AsNoTracking()
                on candidate.GameServerId equals snapshot.GameServerId
            where candidate.GameServerId == gameServerId &&
                candidate.SlotId == slotId &&
                candidate.PlayerGuid == NormalizePlayerGuid(playerGuid) &&
                candidate.State == RuleConnectionState.Connected.ToString() &&
                candidate.ExpiresAtUtc > now &&
                !candidate.GameServer.Deleted &&
                candidate.GameServer.GameType == candidate.GameType &&
                candidate.SequenceId <= snapshot.SequenceId &&
                snapshot.ExpiresAtUtc > now &&
                candidate.RuntimeId == snapshot.RuntimeId &&
                candidate.RuntimeInstanceId == snapshot.RuntimeInstanceId &&
                candidate.RuntimeStartedAtUtc == snapshot.RuntimeStartedAtUtc &&
                candidate.RuntimeIdentityModel == snapshot.RuntimeIdentityModel
            select candidate).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (binding is null)
        {
            return new ApiResult<RuleConnectionBindingDto>(HttpStatusCode.NotFound);
        }

        return new ApiResponse<RuleConnectionBindingDto>(ToDto(binding)).ToApiResult();
    }

    async Task<ApiResult<RuleServerCapabilitiesDto>> IRuleConnectionBindingsApi.GetCurrentCapabilities(
        Guid gameServerId,
        CancellationToken cancellationToken)
    {
        if (gameServerId == Guid.Empty)
        {
            return new ApiResult<RuleServerCapabilitiesDto>(HttpStatusCode.BadRequest);
        }

        var server = await context.GameServers.AsNoTracking().SingleOrDefaultAsync(
            s => s.GameServerId == gameServerId && !s.Deleted, cancellationToken).ConfigureAwait(false);
        if (server is null)
        {
            return new ApiResult<RuleServerCapabilitiesDto>(HttpStatusCode.NotFound);
        }

        var snapshot = await context.RuleConnectionStatusSnapshots.AsNoTracking().SingleOrDefaultAsync(
            s => s.GameServerId == gameServerId, cancellationToken).ConfigureAwait(false);
        var snapshotMatchesServerGame = snapshot is not null && snapshot.GameType == server.GameType;
        var isFresh = snapshotMatchesServerGame && snapshot!.ExpiresAtUtc > DateTime.UtcNow;
        var data = new RuleServerCapabilitiesDto
        {
            GameServerId = gameServerId,
            GameType = (GameType)server.GameType,
            HasFreshStatus = isFresh,
            IntegrationCapabilities = isFresh
                ? DeserializeCapabilities(snapshot!.IntegrationCapabilitiesJson)
                : [],
            RuntimeCapabilities = isFresh
                ? DeserializeCapabilities(snapshot!.RuntimeCapabilitiesJson)
                : [],
            Runtime = isFresh ? ToRuntime(snapshot!) : null,
            SnapshotId = snapshotMatchesServerGame ? snapshot!.SnapshotId : Guid.Empty,
            SequenceId = snapshotMatchesServerGame ? snapshot!.SequenceId : 0,
            ObservedAtUtc = snapshotMatchesServerGame ? AsUtc(snapshot!.ObservedAtUtc) : default,
            ExpiresAtUtc = snapshotMatchesServerGame ? AsUtc(snapshot!.ExpiresAtUtc) : default
        };
        return new ApiResponse<RuleServerCapabilitiesDto>(data).ToApiResult();
    }

    private async Task<ApiResult<T>> ExecuteMutationAsync<T>(
        Guid gameServerId,
        Func<Task<ApiResult<T>>> operation,
        CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
        {
            return await operation().ConfigureAwait(false);
        }

        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            await SiegeModeController.LockServerAsync(context, gameServerId, cancellationToken)
                .ConfigureAwait(false);

            var result = await operation().ConfigureAwait(false);
            var status = (int)result.StatusCode;
            if (status is >= 200 and < 300)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
            return result;
        }).ConfigureAwait(false);
    }

    private static bool ValidSnapshot(RuleConnectionStatusSnapshotDto request, DateTimeOffset now)
    {
        if (request.ProtocolVersion != 1 || request.SnapshotId == Guid.Empty || request.SequenceId < 0 ||
            !Enum.IsDefined(request.GameType) || request.Runtime is null ||
            string.IsNullOrWhiteSpace(request.Runtime.Id) || request.Runtime.Id.Length > 128 ||
            string.IsNullOrWhiteSpace(request.Runtime.Version) || request.Runtime.Version.Length > 128 ||
            request.Runtime.ProtocolVersion != 1 || !Enum.IsDefined(request.Runtime.IdentityModel) ||
            request.Runtime.IdentityModel == RuleRuntimeIdentityModel.Unknown || request.Runtime.InstanceId == Guid.Empty ||
            request.Runtime.StartedAtUtc == default || request.Runtime.StartedAtUtc > request.ObservedAtUtc ||
            (request.Runtime.Id == "cod4x-plugin" &&
                request.Runtime.IdentityModel != RuleRuntimeIdentityModel.PluginRuntimeGeneration) ||
            (request.Runtime.Id == "server-agent" &&
                request.Runtime.IdentityModel != RuleRuntimeIdentityModel.AgentObservedConnection) ||
            request.Runtime.Id is not ("cod4x-plugin" or "server-agent") ||
            request.ObservedAtUtc < now - SnapshotLifetime || request.ObservedAtUtc > now + MaximumFutureSkew ||
            request.Players is null || request.Players.Length > 256 ||
            request.IntegrationCapabilities is null || request.Runtime.Capabilities is null ||
            !ValidCapabilities(request.IntegrationCapabilities) || !ValidCapabilities(request.Runtime.Capabilities))
        {
            return false;
        }

        if (request.Players.Any(p => p is null || p.SlotId < 0 || !Enum.IsDefined(p.State)) ||
            request.Players.Select(p => p.SlotId).Distinct().Count() != request.Players.Length)
        {
            return false;
        }

        return request.Players.All(player =>
        {
            if (player.State == RuleConnectionState.Unknown)
            {
                return player.PlayerGuid is null || ValidPlayerGuid(player.PlayerGuid);
            }

            return ValidPlayerGuid(player.PlayerGuid) && player.ConnectionId is { } connectionId &&
                connectionId != Guid.Empty &&
                (request.Runtime.IdentityModel == RuleRuntimeIdentityModel.PluginRuntimeGeneration
                    ? player.ConnectionGeneration.HasValue
                    : player.ConnectionGeneration is null);
        });
    }

    private static bool ValidPlayerGuid(string? playerGuid) =>
        !string.IsNullOrWhiteSpace(playerGuid) && playerGuid.Trim().Length <= 50;

    private static bool ValidCapabilities(string[] values) =>
        values.Length <= 128 &&
        values.All(value => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 &&
            value.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-')) &&
        values.Distinct(StringComparer.Ordinal).Count() == values.Length;

    private static string[] NormalizeCapabilities(string[] values) =>
        values.Order(StringComparer.Ordinal).ToArray();

    private static string[] DeserializeCapabilities(string value) =>
        JsonSerializer.Deserialize<string[]>(value, JsonOptions) ?? [];

    private static string NormalizePlayerGuid(string? playerGuid) => playerGuid!.Trim().ToUpperInvariant();

    private static Expression<Func<RuleConnectionBinding, bool>> CreateRelevantBindingsPredicate(
        Guid gameServerId,
        IReadOnlyCollection<(int SlotId, string PlayerGuid)> reportedIdentities,
        IReadOnlyCollection<int> connectedSlots,
        DateTime nowUtc)
    {
        var binding = Expression.Parameter(typeof(RuleConnectionBinding), "binding");
        Expression identityMatch = Expression.Constant(false);
        foreach (var identity in reportedIdentities)
        {
            var slotMatch = Expression.Equal(
                Expression.Property(binding, nameof(RuleConnectionBinding.SlotId)),
                Expression.Constant(identity.SlotId));
            var playerMatch = Expression.Equal(
                Expression.Property(binding, nameof(RuleConnectionBinding.PlayerGuid)),
                Expression.Constant(identity.PlayerGuid));
            identityMatch = Expression.OrElse(identityMatch, Expression.AndAlso(slotMatch, playerMatch));
        }

        Expression connectedSlotMatch = Expression.Constant(false);
        foreach (var slotId in connectedSlots)
        {
            connectedSlotMatch = Expression.OrElse(
                connectedSlotMatch,
                Expression.Equal(
                    Expression.Property(binding, nameof(RuleConnectionBinding.SlotId)),
                    Expression.Constant(slotId)));
        }

        var activeConnectedOccupant = Expression.AndAlso(
            Expression.AndAlso(
                connectedSlotMatch,
                Expression.Equal(
                    Expression.Property(binding, nameof(RuleConnectionBinding.State)),
                    Expression.Constant(RuleConnectionState.Connected.ToString()))),
            Expression.GreaterThan(
                Expression.Property(binding, nameof(RuleConnectionBinding.ExpiresAtUtc)),
                Expression.Constant(nowUtc)));
        var serverMatch = Expression.Equal(
            Expression.Property(binding, nameof(RuleConnectionBinding.GameServerId)),
            Expression.Constant(gameServerId));
        var relevantBinding = Expression.OrElse(identityMatch, activeConnectedOccupant);
        return Expression.Lambda<Func<RuleConnectionBinding, bool>>(
            Expression.AndAlso(serverMatch, relevantBinding), binding);
    }

    private static bool CanAdvanceBinding(RuleConnectionBinding binding, RuleConnectionStatusSnapshotDto request)
    {
        if (binding.RuntimeInstanceId != request.Runtime.InstanceId)
        {
            return request.Runtime.StartedAtUtc.UtcDateTime > binding.RuntimeStartedAtUtc;
        }

        return binding.RuntimeStartedAtUtc == request.Runtime.StartedAtUtc.UtcDateTime &&
            binding.RuntimeIdentityModel == (int)request.Runtime.IdentityModel &&
            request.SequenceId > binding.SequenceId;
    }

    private static bool MatchesExactBinding(
        RuleConnectionBinding binding,
        RuleConnectionStatusPlayerDto player,
        RuleConnectionStatusSnapshotDto request) =>
        binding.ConnectionId == player.ConnectionId &&
        binding.ConnectionGeneration == (player.ConnectionGeneration is { } generation
            ? generation
            : null) &&
        binding.RuntimeInstanceId == request.Runtime.InstanceId &&
        binding.RuntimeStartedAtUtc == request.Runtime.StartedAtUtc.UtcDateTime &&
        binding.RuntimeIdentityModel == (int)request.Runtime.IdentityModel;

    private static RuleConnectionBindingDto ToDto(RuleConnectionBinding binding) =>
        new()
        {
            GameServerId = binding.GameServerId,
            GameType = (GameType)binding.GameType,
            SlotId = binding.SlotId,
            PlayerGuid = binding.PlayerGuid,
            ConnectionId = binding.ConnectionId,
            ConnectionGeneration = binding.ConnectionGeneration is { } generation
                ? checked((ulong)generation)
                : null,
            Runtime = new RuleRuntimeDescriptor
            {
                Id = binding.RuntimeId,
                Version = binding.RuntimeVersion,
                ProtocolVersion = binding.RuntimeProtocolVersion,
                IdentityModel = (RuleRuntimeIdentityModel)binding.RuntimeIdentityModel,
                InstanceId = binding.RuntimeInstanceId,
                StartedAtUtc = AsUtc(binding.RuntimeStartedAtUtc)
            },
            SnapshotId = binding.SnapshotId,
            SequenceId = binding.SequenceId,
            ObservedAtUtc = AsUtc(binding.ObservedAtUtc),
            ExpiresAtUtc = AsUtc(binding.ExpiresAtUtc),
            Version = binding.Version
        };

    private static RuleRuntimeDescriptor ToRuntime(RuleConnectionStatusSnapshot snapshot) =>
        new()
        {
            Id = snapshot.RuntimeId,
            Version = snapshot.RuntimeVersion,
            ProtocolVersion = snapshot.RuntimeProtocolVersion,
            IdentityModel = (RuleRuntimeIdentityModel)snapshot.RuntimeIdentityModel,
            InstanceId = snapshot.RuntimeInstanceId,
            StartedAtUtc = AsUtc(snapshot.RuntimeStartedAtUtc),
            Capabilities = DeserializeCapabilities(snapshot.RuntimeCapabilitiesJson)
        };

    private static DateTimeOffset AsUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
