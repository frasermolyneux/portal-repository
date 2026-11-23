using System.Data;
using System.Net;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.SiegeMode;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Settings.Contracts.V1.Contracts.SiegeMode;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

[ApiController]
[Authorize(Roles = "ServiceAccount")]
[ApiVersion(ApiVersions.V1)]
[Route("v{version:apiVersion}")]
public partial class SiegeModeController(PortalDbContext context, ILogger<SiegeModeController>? logger = null) : ControllerBase
{
    private static readonly JsonSerializerOptions SettingsJsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("game-servers/{gameServerId:guid}/siege-sessions/current")]
    [ProducesResponseType<ApiResponse<SiegeSessionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentSession(Guid gameServerId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var session = await context.SiegeSessions.AsNoTracking()
            .Where(s => s.GameServerId == gameServerId && !s.GameServer.Deleted &&
                s.GameServer.GameType == s.GameType && s.EndedAtUtc == null && s.ExpiresAtUtc > now)
            .Select(s => new { Session = s, ActionCount = s.SiegeActions.Count })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return session is null
            ? new ApiResult<SiegeSessionDto>(HttpStatusCode.NotFound).ToHttpResult()
            : new ApiResponse<SiegeSessionDto>(ToDto(session.Session, session.ActionCount)).ToApiResult().ToHttpResult();
    }

    [HttpPost("game-servers/{gameServerId:guid}/siege-sessions")]
    [ProducesResponseType<ApiResponse<SiegeSessionDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartSession(Guid gameServerId, StartSiegeSessionDto request, CancellationToken cancellationToken = default)
    {
        if (gameServerId == Guid.Empty || request is null || request.OperationId == Guid.Empty ||
            !ValidActor(request.Actor) || request.ExpectedDurationMinutes <= 0 ||
            request.Mode is not (SiegeSessionMode.Enforce or SiegeSessionMode.DryRun))
        {
            return new ApiResult<SiegeSessionDto>(HttpStatusCode.BadRequest).ToHttpResult();
        }

        var result = await ExecuteMutationAsync(async () =>
        {
            await SiegeHistoryLock.AcquireAsync(context, cancellationToken).ConfigureAwait(false);
            await LockServerAsync(gameServerId, cancellationToken).ConfigureAwait(false);
            var existing = await context.SiegeSessions.FindAsync([request.OperationId], cancellationToken).ConfigureAwait(false);
            if (existing is not null)
            {
                var existingSettings = DeserializeSettings(existing.SettingsJson);
                return existing.GameServerId == gameServerId && existing.StartedBy == request.Actor &&
                    existingSettings.DurationMinutes == request.ExpectedDurationMinutes && existing.Mode == request.Mode.ToString()
                    ? await ResultAsync(existing, HttpStatusCode.OK, cancellationToken).ConfigureAwait(false)
                    : new ApiResult<SiegeSessionDto>(HttpStatusCode.Conflict);
            }
            if (await context.SiegeSessionEvents.AnyAsync(e => e.OperationId == request.OperationId, cancellationToken).ConfigureAwait(false))
            {
                return new ApiResult<SiegeSessionDto>(HttpStatusCode.Conflict);
            }

            var server = await context.GameServers.SingleOrDefaultAsync(
                s => s.GameServerId == gameServerId && !s.Deleted, cancellationToken).ConfigureAwait(false);
            if (server is null)
            {
                return new ApiResult<SiegeSessionDto>(HttpStatusCode.NotFound);
            }

            var now = DateTime.UtcNow;
            var active = await context.SiegeSessions.SingleOrDefaultAsync(
                s => s.GameServerId == gameServerId && s.EndedAtUtc == null, cancellationToken).ConfigureAwait(false);
            if (active is not null && active.ExpiresAtUtc > now)
            {
                return new ApiResult<SiegeSessionDto>(HttpStatusCode.Conflict);
            }

            var configuration = await context.GlobalConfigurations.AsNoTracking()
                .Where(c => c.Namespace.ToLower() == "siegemode")
                .Select(c => c.Configuration)
                .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            var settings = configuration is null ? new SiegeModeSettingsDocument() : DeserializeSettings(configuration);
            if (!new SiegeModeSettingsValidator().Validate(settings).IsValid)
            {
                throw new InvalidOperationException("The stored Siege Mode settings are invalid.");
            }
            if (settings.DurationMinutes != request.ExpectedDurationMinutes)
            {
                return new ApiResult<SiegeSessionDto>(HttpStatusCode.Conflict);
            }

            if (active is not null)
            {
                active.EndedAtUtc = active.ExpiresAtUtc;
                context.SiegeSessionEvents.Add(new SiegeSessionEvent
                {
                    OperationId = Guid.NewGuid(),
                    SessionId = active.SessionId,
                    EventType = "Expired",
                    Actor = "system",
                    OccurredAtUtc = active.ExpiresAtUtc,
                    ExpiresAtUtc = active.ExpiresAtUtc
                });
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await DeleteBaselineAsync(active.SessionId, cancellationToken).ConfigureAwait(false);
            }

            var guids = await context.Players.AsNoTracking()
                .Where(p => p.GameType == server.GameType && p.Guid != null && p.Guid != "")
                .Select(p => new { p.PlayerId, p.Guid }).ToListAsync(cancellationToken).ConfigureAwait(false);
            var addresses = await context.PlayerIpAddresses.AsNoTracking()
                .Where(ip => ip.Address != null && ip.Address != "")
                .Select(ip => new { ip.PlayerId, ip.Address }).ToListAsync(cancellationToken).ConfigureAwait(false);
            now = DateTime.UtcNow;
            var session = new SiegeSession
            {
                SessionId = request.OperationId,
                GameServerId = gameServerId,
                GameType = server.GameType,
                StartedBy = request.Actor,
                StartedAtUtc = now,
                ExpiresAtUtc = now.AddMinutes(settings.DurationMinutes),
                SettingsJson = JsonSerializer.Serialize(settings, SettingsJsonOptions),
                Mode = request.Mode.ToString()
            };
            context.SiegeSessions.Add(session);
            context.SiegeSessionEvents.Add(new SiegeSessionEvent
            {
                OperationId = request.OperationId,
                SessionId = session.SessionId,
                EventType = "Started",
                Actor = request.Actor,
                OccurredAtUtc = now,
                ExpiresAtUtc = session.ExpiresAtUtc
            });
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var guidGroups = guids.Select(g => new { g.PlayerId, Guid = g.Guid!.Trim() })
                .Where(g => g.Guid.Length is > 0 and <= 50)
                .GroupBy(g => g.Guid, StringComparer.OrdinalIgnoreCase);
            var addressGroups = addresses.Select(a => new { a.PlayerId, Address = NormalizeIp(a.Address) })
                .Where(a => a.Address is not null)
                .GroupBy(a => a.Address!, StringComparer.OrdinalIgnoreCase);
            foreach (var batch in guidGroups.Chunk(1000))
            {
                context.SiegeBaselineGuids.AddRange(batch.Select(g => new SiegeBaselineGuid { SessionId = session.SessionId, PlayerGuid = g.Key }));
                context.SiegeBaselineGuidOwners.AddRange(batch.SelectMany(g => g.Select(x => x.PlayerId).Distinct()
                    .Select(playerId => new SiegeBaselineGuidOwner { SessionId = session.SessionId, PlayerGuid = g.Key, PlayerId = playerId })));
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.ChangeTracker.Clear();
            }
            foreach (var batch in addressGroups.Chunk(1000))
            {
                context.SiegeBaselineIps.AddRange(batch.Select(a => new SiegeBaselineIp
                {
                    SessionId = session.SessionId,
                    Address = a.Key,
                    HasUnownedSource = a.Any(x => x.PlayerId is null)
                }));
                context.SiegeBaselineIpOwners.AddRange(batch.SelectMany(a => a.Where(x => x.PlayerId.HasValue)
                    .Select(x => x.PlayerId!.Value).Distinct()
                    .Select(playerId => new SiegeBaselineIpOwner { SessionId = session.SessionId, Address = a.Key, PlayerId = playerId })));
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                context.ChangeTracker.Clear();
            }
            return Result(session, HttpStatusCode.Created);
        }, cancellationToken).ConfigureAwait(false);
        if (result.StatusCode == HttpStatusCode.Created)
        {
            logger?.LogInformation("Siege session {SessionId} started on {GameServerId} in {Mode} mode",
                request.OperationId, gameServerId, request.Mode);
        }
        return result.ToHttpResult();
    }

    private static SiegeModeSettingsDocument DeserializeSettings(string json) =>
        JsonSerializer.Deserialize<SiegeModeSettingsDocument>(json, SettingsJsonOptions)
        ?? throw new InvalidOperationException("The stored Siege Mode settings are null.");

    private static string? NormalizeIp(string? value)
    {
        if (!IPAddress.TryParse(value, out var address))
        {
            return null;
        }
        return (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();
    }

    private static bool ValidActor(string? actor) => !string.IsNullOrWhiteSpace(actor) && actor.Length <= 256;

    private static ApiResult<SiegeSessionDto> Result(SiegeSession session, HttpStatusCode status) =>
        new ApiResponse<SiegeSessionDto>(ToDto(session, 0)).ToApiResult(status);

    private async Task<ApiResult<SiegeSessionDto>> ResultAsync(SiegeSession session, HttpStatusCode status, CancellationToken cancellationToken)
    {
        var actionCount = await context.SiegeActions.CountAsync(a => a.SessionId == session.SessionId, cancellationToken).ConfigureAwait(false);
        return new ApiResponse<SiegeSessionDto>(ToDto(session, actionCount)).ToApiResult(status);
    }

    private static SiegeSessionDto ToDto(SiegeSession session, int actionCount) => new()
    {
        SessionId = session.SessionId,
        GameServerId = session.GameServerId,
        GameType = session.GameType,
        StartedAtUtc = DateTime.SpecifyKind(session.StartedAtUtc, DateTimeKind.Utc),
        ExpiresAtUtc = DateTime.SpecifyKind(session.ExpiresAtUtc, DateTimeKind.Utc),
        EndedAtUtc = session.EndedAtUtc is { } ended ? DateTime.SpecifyKind(ended, DateTimeKind.Utc) : null,
        StartedBy = session.StartedBy,
        Mode = Enum.Parse<SiegeSessionMode>(session.Mode),
        SettingsJson = session.SettingsJson,
        ActionCount = actionCount
    };

    private async Task<T> ExecuteMutationAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken) =>
        await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            if (!context.Database.IsRelational())
            {
                return await operation().ConfigureAwait(false);
            }
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
            var result = await operation().ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }).ConfigureAwait(false);

    private Task LockServerAsync(Guid gameServerId, CancellationToken cancellationToken) =>
        LockServerAsync(context, gameServerId, cancellationToken);

    internal static async Task LockServerAsync(PortalDbContext context, Guid gameServerId, CancellationToken cancellationToken)
    {
        if (!context.Database.IsSqlServer())
        {
            return;
        }
        var resource = $"siege-server:{gameServerId:N}";
        var results = await context.Database.SqlQuery<int>($"""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 10000;
            SELECT @result AS [Value];
            """).ToListAsync(cancellationToken).ConfigureAwait(false);
        if (results.Single() < 0)
        {
            throw new InvalidOperationException("Could not acquire the Siege Mode server lock.");
        }
    }

    private async Task DeleteBaselineAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (context.Database.IsRelational())
        {
            await context.SiegeBaselineGuids.Where(g => g.SessionId == sessionId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
            await context.SiegeBaselineIps.Where(i => i.SessionId == sessionId).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }
        else
        {
            context.SiegeBaselineGuids.RemoveRange(context.SiegeBaselineGuids.Where(g => g.SessionId == sessionId));
            context.SiegeBaselineIps.RemoveRange(context.SiegeBaselineIps.Where(i => i.SessionId == sessionId));
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
