using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Api.V1.Services;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

public partial class ConnectedPlayersController
{
    [HttpPost("connected-players/claim-code")]
    public async Task<IActionResult> IssueConnectedPlayerClaimCode([FromBody] ActivateConnectedPlayerActivationCodeDto dto, CancellationToken cancellationToken = default)
        => (await ((IConnectedPlayersApi)this).IssueConnectedPlayerClaimCode(dto, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    Task<ApiResult<ConnectedPlayerActivationCodeDto>> IConnectedPlayersApi.IssueConnectedPlayerClaimCode(
        ActivateConnectedPlayerActivationCodeDto dto, CancellationToken cancellationToken)
        => IssueActivationCode(dto, ClaimActivationSource, 60, cancellationToken);

    [HttpPost("connected-players/claims")]
    public async Task<IActionResult> SubmitConnectedPlayerClaim([FromBody] SubmitConnectedPlayerClaimDto dto, CancellationToken cancellationToken = default)
        => (await ((IConnectedPlayersApi)this).SubmitConnectedPlayerClaim(dto, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<ConnectedPlayerClaimDto>> IConnectedPlayersApi.SubmitConnectedPlayerClaim(
        SubmitConnectedPlayerClaimDto dto, CancellationToken cancellationToken)
    {
        if (dto is null || dto.RequestId == Guid.Empty || dto.PlayerId == Guid.Empty || dto.GameServerId == Guid.Empty || string.IsNullOrWhiteSpace(dto.Code))
        {
            return ClaimError(HttpStatusCode.BadRequest, "A request ID, player ID, server ID and code are required.");
        }

        var code = dto.Code.Trim().ToUpperInvariant();
        return await UserProfileLifecycle.ExecuteAsync(context, async () =>
        {
            var replay = await context.ConnectedPlayerClaims.Include(c => c.Player).Include(c => c.ConnectedPlayerActivationCode)
                .FirstOrDefaultAsync(c => c.ConnectedPlayerClaimId == dto.RequestId, cancellationToken).ConfigureAwait(false);
            if (replay is not null)
            {
                return replay.PlayerId == dto.PlayerId && replay.GameServerId == dto.GameServerId && replay.ConnectedPlayerActivationCode.Code == code
                    ? ClaimResult(replay)
                    : ClaimError(HttpStatusCode.Conflict, "The request ID belongs to a different claim.");
            }

            var activation = await context.ConnectedPlayerActivationCodes
                .FirstOrDefaultAsync(c => c.Code == code && c.IsActive && c.ActivatedBy == ClaimActivationSource, cancellationToken).ConfigureAwait(false);
            var now = DateTime.UtcNow;
            if (activation is null || activation.ExpiresAtUtc <= now ||
                !await context.UserProfiles.AnyAsync(u => u.UserProfileId == activation.UserProfileId && u.ArchivedAtUtc == null, cancellationToken).ConfigureAwait(false))
            {
                return ClaimError(HttpStatusCode.BadRequest, "The registration code is invalid or expired.");
            }

            var player = await context.Players.FirstOrDefaultAsync(p => p.PlayerId == dto.PlayerId, cancellationToken).ConfigureAwait(false);
            if (player is null)
            {
                return ClaimError(HttpStatusCode.BadRequest, "The player was not found.");
            }

            if (await context.ConnectedPlayerProfiles.AnyAsync(p => p.PlayerId == dto.PlayerId && p.IsActive, cancellationToken).ConfigureAwait(false))
            {
                return ClaimError(HttpStatusCode.Conflict, "The player is already linked.");
            }

            activation.IsActive = false;
            activation.ConsumedAtUtc = now;
            var serverName = await context.GameServers.AsNoTracking().Where(s => s.GameServerId == dto.GameServerId)
                .Select(s => s.Title).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            var claim = new ConnectedPlayerClaim
            {
                ConnectedPlayerClaimId = dto.RequestId,
                ConnectedPlayerActivationCodeId = activation.ConnectedPlayerActivationCodeId,
                PlayerId = dto.PlayerId,
                Player = player,
                UserProfileId = activation.UserProfileId,
                GameServerId = dto.GameServerId,
                GameServerName = serverName,
                Status = "Pending",
                CreatedAtUtc = now,
                ExpiresAtUtc = now.AddHours(24)
            };
            context.ConnectedPlayerClaims.Add(claim);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ClaimResult(claim, HttpStatusCode.Created);
        }, cancellationToken).ConfigureAwait(false);
    }

    [HttpGet("user-profiles/{userProfileId:guid}/connected-player-claims")]
    public async Task<IActionResult> GetPendingConnectedPlayerClaims(Guid userProfileId, CancellationToken cancellationToken = default)
        => (await ((IConnectedPlayersApi)this).GetPendingConnectedPlayerClaims(userProfileId, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    async Task<ApiResult<CollectionModel<ConnectedPlayerClaimDto>>> IConnectedPlayersApi.GetPendingConnectedPlayerClaims(
        Guid userProfileId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var claims = await context.ConnectedPlayerClaims.AsNoTracking().Include(c => c.Player)
            .Where(c => c.UserProfileId == userProfileId && c.UserProfile.ArchivedAtUtc == null && c.Status == "Pending" && c.ExpiresAtUtc > now)
            .OrderByDescending(c => c.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new ApiResponse<CollectionModel<ConnectedPlayerClaimDto>>(
            new CollectionModel<ConnectedPlayerClaimDto>(claims.Select(ToClaimDto).ToList())).ToApiResult();
    }

    [HttpPost("user-profiles/{userProfileId:guid}/connected-player-claims/{claimId:guid}/approve")]
    public async Task<IActionResult> ApproveConnectedPlayerClaim(Guid userProfileId, Guid claimId, CancellationToken cancellationToken = default)
        => (await ((IConnectedPlayersApi)this).ApproveConnectedPlayerClaim(userProfileId, claimId, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    Task<ApiResult<ConnectedPlayerClaimDto>> IConnectedPlayersApi.ApproveConnectedPlayerClaim(Guid userProfileId, Guid claimId, CancellationToken cancellationToken)
        => ResolveClaim(userProfileId, claimId, true, cancellationToken);

    [HttpPost("user-profiles/{userProfileId:guid}/connected-player-claims/{claimId:guid}/decline")]
    public async Task<IActionResult> DeclineConnectedPlayerClaim(Guid userProfileId, Guid claimId, CancellationToken cancellationToken = default)
        => (await ((IConnectedPlayersApi)this).DeclineConnectedPlayerClaim(userProfileId, claimId, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    Task<ApiResult<ConnectedPlayerClaimDto>> IConnectedPlayersApi.DeclineConnectedPlayerClaim(Guid userProfileId, Guid claimId, CancellationToken cancellationToken)
        => ResolveClaim(userProfileId, claimId, false, cancellationToken);

    private Task<ApiResult<ConnectedPlayerClaimDto>> ResolveClaim(Guid userProfileId, Guid claimId, bool approve, CancellationToken cancellationToken)
        => UserProfileLifecycle.ExecuteAsync(context, async () =>
        {
            var claim = await context.ConnectedPlayerClaims.Include(c => c.Player)
                .FirstOrDefaultAsync(c => c.ConnectedPlayerClaimId == claimId && c.UserProfileId == userProfileId
                    && c.UserProfile.ArchivedAtUtc == null, cancellationToken).ConfigureAwait(false);
            if (claim is null)
            {
                return ClaimError(HttpStatusCode.NotFound, "The claim was not found.");
            }

            var desired = approve ? "Approved" : "Declined";
            if (claim.Status == desired)
            {
                return ClaimResult(claim);
            }

            if (claim.Status != "Pending")
            {
                return ClaimError(HttpStatusCode.Conflict, "The claim is no longer pending.");
            }

            var now = DateTime.UtcNow;
            if (claim.ExpiresAtUtc <= now)
            {
                claim.Status = "Expired";
                claim.ResolvedAtUtc = now;
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ClaimError(HttpStatusCode.Conflict, "The claim has expired.");
            }
            if (approve)
            {
                if (await context.ConnectedPlayerProfiles.AnyAsync(p => p.PlayerId == claim.PlayerId && p.IsActive, cancellationToken).ConfigureAwait(false))
                {
                    return ClaimError(HttpStatusCode.Conflict, "The player is already linked.");
                }

                var link = new ConnectedPlayerProfile
                {
                    ConnectedPlayerProfileId = Guid.NewGuid(),
                    PlayerId = claim.PlayerId,
                    UserProfileId = userProfileId,
                    LinkMethod = ConnectedPlayerLinkMethod.ActivationCode.ToString(),
                    LinkedAtUtc = now,
                    LinkedByUserProfileId = userProfileId,
                    IsActive = true
                };
                context.ConnectedPlayerProfiles.Add(link);
                claim.ConnectedPlayerProfileId = link.ConnectedPlayerProfileId;
            }
            claim.Status = desired;
            claim.ResolvedAtUtc = now;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (approve)
            {
                await ConnectedPlayerTagProjection.ReconcileAsync(context, claim.PlayerId, cancellationToken).ConfigureAwait(false);
            }

            return ClaimResult(claim);
        }, cancellationToken);

    [HttpDelete("user-profiles/{userProfileId:guid}/connected-players/{connectedPlayerProfileId:guid}")]
    public async Task<IActionResult> UnlinkOwnConnectedPlayer(Guid userProfileId, Guid connectedPlayerProfileId, CancellationToken cancellationToken = default)
        => (await ((IConnectedPlayersApi)this).UnlinkOwnConnectedPlayer(userProfileId, connectedPlayerProfileId, cancellationToken).ConfigureAwait(false)).ToHttpResult();

    Task<ApiResult> IConnectedPlayersApi.UnlinkOwnConnectedPlayer(Guid userProfileId, Guid connectedPlayerProfileId, CancellationToken cancellationToken)
        => UserProfileLifecycle.ExecuteAsync(context, async () =>
        {
            var link = await context.ConnectedPlayerProfiles.FirstOrDefaultAsync(p => p.ConnectedPlayerProfileId == connectedPlayerProfileId
                && p.UserProfileId == userProfileId && p.UserProfile.ArchivedAtUtc == null, cancellationToken).ConfigureAwait(false);
            if (link is null)
            {
                return new ApiResult(HttpStatusCode.NotFound);
            }

            if (link.IsActive)
            {
                link.IsActive = false;
                link.UnlinkedAtUtc = DateTime.UtcNow;
                link.UnlinkedByUserProfileId = userProfileId;
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await ConnectedPlayerTagProjection.ReconcileAsync(context, link.PlayerId, cancellationToken).ConfigureAwait(false);
            }
            return new ApiResponse().ToApiResult();
        }, cancellationToken);

    private static ConnectedPlayerClaimDto ToClaimDto(ConnectedPlayerClaim claim) => new()
    {
        ClaimId = claim.ConnectedPlayerClaimId,
        UserProfileId = claim.UserProfileId,
        PlayerId = claim.PlayerId,
        GameServerId = claim.GameServerId,
        GameServerName = claim.GameServerName,
        Username = claim.Player.Username ?? string.Empty,
        GameType = (GameType)claim.Player.GameType,
        Status = claim.Status,
        CreatedAtUtc = claim.CreatedAtUtc,
        ExpiresAtUtc = claim.ExpiresAtUtc,
        ResolvedAtUtc = claim.ResolvedAtUtc,
        ConnectedPlayerProfileId = claim.ConnectedPlayerProfileId
    };

    private static ApiResult<ConnectedPlayerClaimDto> ClaimResult(ConnectedPlayerClaim claim, HttpStatusCode status = HttpStatusCode.OK)
        => new ApiResponse<ConnectedPlayerClaimDto>(ToClaimDto(claim)).ToApiResult(status);

    private static ApiResult<ConnectedPlayerClaimDto> ClaimError(HttpStatusCode status, string message)
        => new(status, new ApiResponse<ConnectedPlayerClaimDto>(new ApiError(ApiErrorCodes.EntityConflict, message)));
}
