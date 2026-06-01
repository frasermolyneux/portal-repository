using System.Net;
using System.Security.Cryptography;
using System.Text;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using MX.Observability.ApplicationInsights.Auditing;
using MX.Observability.ApplicationInsights.Auditing.Models;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.ConnectedPlayers;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using XtremeIdiots.Portal.Repository.DataLib;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    public class ConnectedPlayersController : ControllerBase, IConnectedPlayersApi
    {
        private const int ActivationCodeExpiryMinutes = 5;
        private const int ActivationCodeMaxAttempts = 5;
        private const string DefaultActivationSource = "WebsiteActivation";

        private readonly PortalDbContext context;
        private readonly IAuditLogger auditLogger;

        public ConnectedPlayersController(PortalDbContext context, IAuditLogger auditLogger)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(auditLogger);
            this.context = context;
            this.auditLogger = auditLogger;
        }

        [HttpPost("connected-players/activation-code")]
        [ProducesResponseType<ConnectedPlayerActivationCodeDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ActivateConnectedPlayerActivationCode([FromBody] ActivateConnectedPlayerActivationCodeDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            var response = await ((IConnectedPlayersApi)this)
                .ActivateConnectedPlayerActivationCode(dto, cancellationToken)
                .ConfigureAwait(false);

            return response.ToHttpResult();
        }

        async Task<ApiResult<ConnectedPlayerActivationCodeDto>> IConnectedPlayersApi.ActivateConnectedPlayerActivationCode(
            ActivateConnectedPlayerActivationCodeDto dto,
            CancellationToken cancellationToken)
        {
            var userProfileExists = await context.UserProfiles
                .AsNoTracking()
                .AnyAsync(u => u.UserProfileId == dto.UserProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (!userProfileExists)
            {
                return new ApiResult<ConnectedPlayerActivationCodeDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<ConnectedPlayerActivationCodeDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.UserProfileNotFoundMessage)));
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var existingActiveCodes = await context.ConnectedPlayerActivationCodes
                .Where(code => code.UserProfileId == dto.UserProfileId && code.IsActive)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var now = DateTime.UtcNow;
            var invalidatedCodeIds = new List<Guid>();
            foreach (var existingCode in existingActiveCodes)
            {
                existingCode.IsActive = false;
                existingCode.InvalidatedAtUtc = now;
                invalidatedCodeIds.Add(existingCode.ConnectedPlayerActivationCodeId);
            }

            var codeValue = await GenerateUniqueActivationCode(cancellationToken).ConfigureAwait(false);
            var entity = new ConnectedPlayerActivationCode
            {
                UserProfileId = dto.UserProfileId,
                Code = codeValue,
                CodeHash = HashToken(codeValue),
                ExpiresAtUtc = now.AddMinutes(ActivationCodeExpiryMinutes),
                AttemptCount = 0,
                MaxAttempts = ActivationCodeMaxAttempts,
                IsActive = true,
                ActivatedAtUtc = now,
                ActivatedBy = DefaultActivationSource
            };

            try
            {
                await context.ConnectedPlayerActivationCodes.AddAsync(entity, cancellationToken).ConfigureAwait(false);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

                return new ApiResult<ConnectedPlayerActivationCodeDto>(HttpStatusCode.Conflict,
                    new ApiResponse<ConnectedPlayerActivationCodeDto>(new ApiError(
                        ApiErrorCodes.ConnectedPlayerActivationCodeConflict,
                        ApiErrorMessages.ConnectedPlayerActivationCodeConflictMessage)));
            }

            foreach (var invalidatedCodeId in invalidatedCodeIds)
            {
                auditLogger.LogAudit(AuditEvent.SystemAction("ConnectedPlayerActivationCodeInvalidated", AuditAction.Update)
                    .WithActor(dto.UserProfileId.ToString(), "ConnectedPlayersController")
                    .WithTarget(invalidatedCodeId.ToString(), "ConnectedPlayerActivationCode")
                    .WithSource("ConnectedPlayersController")
                    .Build());
            }

            auditLogger.LogAudit(AuditEvent.SystemAction("ConnectedPlayerActivationCodeActivated", AuditAction.Create)
                .WithActor(dto.UserProfileId.ToString(), "ConnectedPlayersController")
                .WithTarget(dto.UserProfileId.ToString(), "UserProfile")
                .WithSource("ConnectedPlayersController")
                .Build());

            return new ApiResponse<ConnectedPlayerActivationCodeDto>(entity.ToDto()).ToApiResult();
        }

        [HttpGet("user-profiles/{userProfileId:guid}/connected-player-activation-code")]
        [ProducesResponseType<ConnectedPlayerActivationCodeDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetActiveConnectedPlayerActivationCode(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var response = await ((IConnectedPlayersApi)this)
                .GetActiveConnectedPlayerActivationCode(userProfileId, cancellationToken)
                .ConfigureAwait(false);

            return response.ToHttpResult();
        }

        async Task<ApiResult<ConnectedPlayerActivationCodeDto>> IConnectedPlayersApi.GetActiveConnectedPlayerActivationCode(
            Guid userProfileId,
            CancellationToken cancellationToken)
        {
            var userProfileExists = await context.UserProfiles
                .AsNoTracking()
                .AnyAsync(u => u.UserProfileId == userProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (!userProfileExists)
            {
                return new ApiResult<ConnectedPlayerActivationCodeDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<ConnectedPlayerActivationCodeDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.UserProfileNotFoundMessage)));
            }

            var entity = await context.ConnectedPlayerActivationCodes
                .OrderByDescending(code => code.ActivatedAtUtc)
                .FirstOrDefaultAsync(code => code.UserProfileId == userProfileId && code.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (entity == null)
            {
                return new ApiResult<ConnectedPlayerActivationCodeDto>(HttpStatusCode.NotFound,
                    new ApiResponse<ConnectedPlayerActivationCodeDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
            }

            if (entity.ExpiresAtUtc <= DateTime.UtcNow)
            {
                entity.IsActive = false;
                entity.InvalidatedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                auditLogger.LogAudit(AuditEvent.SystemAction("ConnectedPlayerActivationCodeExpired", AuditAction.Update)
                    .WithActor(userProfileId.ToString(), "ConnectedPlayersController")
                    .WithTarget(entity.ConnectedPlayerActivationCodeId.ToString(), "ConnectedPlayerActivationCode")
                    .WithSource("ConnectedPlayersController")
                    .Build());

                return new ApiResult<ConnectedPlayerActivationCodeDto>(HttpStatusCode.NotFound,
                    new ApiResponse<ConnectedPlayerActivationCodeDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
            }

            return new ApiResponse<ConnectedPlayerActivationCodeDto>(entity.ToDto()).ToApiResult();
        }

        [HttpPost("connected-players/link")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateConnectedPlayerLink([FromBody] CreateConnectedPlayerLinkDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            var response = await ((IConnectedPlayersApi)this)
                .CreateConnectedPlayerLink(dto, cancellationToken)
                .ConfigureAwait(false);

            return response.ToHttpResult();
        }

        async Task<ApiResult> IConnectedPlayersApi.CreateConnectedPlayerLink(CreateConnectedPlayerLinkDto dto, CancellationToken cancellationToken)
        {
            var playerExists = await context.Players
                .AsNoTracking()
                .AnyAsync(p => p.PlayerId == dto.PlayerId, cancellationToken)
                .ConfigureAwait(false);

            if (!playerExists)
            {
                return new ApiResult(HttpStatusCode.BadRequest,
                    new ApiResponse(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
            }

            var userProfileExists = await context.UserProfiles
                .AsNoTracking()
                .AnyAsync(u => u.UserProfileId == dto.UserProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (!userProfileExists)
            {
                return new ApiResult(HttpStatusCode.BadRequest,
                    new ApiResponse(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.UserProfileNotFoundMessage)));
            }

            if (dto.LinkedByUserProfileId.HasValue)
            {
                var linkedByUserProfileExists = await context.UserProfiles
                    .AsNoTracking()
                    .AnyAsync(u => u.UserProfileId == dto.LinkedByUserProfileId.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (!linkedByUserProfileExists)
                {
                    return new ApiResult(HttpStatusCode.BadRequest,
                        new ApiResponse(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.UserProfileNotFoundMessage)));
                }
            }

            var activeLinkExists = await context.ConnectedPlayerProfiles
                .AsNoTracking()
                .AnyAsync(cp => cp.PlayerId == dto.PlayerId && cp.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (activeLinkExists)
            {
                return new ApiResult(HttpStatusCode.Conflict,
                    new ApiResponse(new ApiError(ApiErrorCodes.ConnectedPlayerAlreadyLinked, ApiErrorMessages.ConnectedPlayerAlreadyLinkedMessage)));
            }

            var entity = dto.ToEntity();

            try
            {
                await context.ConnectedPlayerProfiles.AddAsync(entity, cancellationToken).ConfigureAwait(false);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                return new ApiResult(HttpStatusCode.Conflict,
                    new ApiResponse(new ApiError(ApiErrorCodes.ConnectedPlayerAlreadyLinked, ApiErrorMessages.ConnectedPlayerAlreadyLinkedMessage)));
            }

            var linkActorId = dto.LinkedByUserProfileId?.ToString() ?? "ServiceAccount";
            auditLogger.LogAudit(AuditEvent.SystemAction("ConnectedPlayerLinked", AuditAction.Create)
                .WithActor(linkActorId, "ConnectedPlayersController")
                .WithTarget(dto.PlayerId.ToString(), "Player")
                .WithSource("ConnectedPlayersController")
                .Build());

            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        [HttpPost("connected-players/tokens/issue")]
        [ProducesResponseType<IssueConnectedPlayerRegistrationTokenResultDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> IssueConnectedPlayerRegistrationToken([FromBody] IssueConnectedPlayerRegistrationTokenDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            var response = await ((IConnectedPlayersApi)this)
                .IssueConnectedPlayerRegistrationToken(dto, cancellationToken)
                .ConfigureAwait(false);

            return response.ToHttpResult();
        }

        async Task<ApiResult<IssueConnectedPlayerRegistrationTokenResultDto>> IConnectedPlayersApi.IssueConnectedPlayerRegistrationToken(
            IssueConnectedPlayerRegistrationTokenDto dto,
            CancellationToken cancellationToken)
        {
            if (dto.ExpiryMinutes <= 0 || dto.MaxAttempts <= 0)
            {
                return new ApiResult<IssueConnectedPlayerRegistrationTokenResultDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<IssueConnectedPlayerRegistrationTokenResultDto>(new ApiError(ApiErrorCodes.InvalidRequest, ApiErrorMessages.InvalidRequestBodyMessage)));
            }

            var playerExists = await context.Players
                .AsNoTracking()
                .AnyAsync(p => p.PlayerId == dto.PlayerId, cancellationToken)
                .ConfigureAwait(false);

            if (!playerExists)
            {
                return new ApiResult<IssueConnectedPlayerRegistrationTokenResultDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<IssueConnectedPlayerRegistrationTokenResultDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var existingTokens = await context.ConnectedPlayerRegistrationTokens
                .Where(t => t.PlayerId == dto.PlayerId && t.IsActive)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var now = DateTime.UtcNow;
            foreach (var existingToken in existingTokens)
            {
                existingToken.IsActive = false;
                existingToken.InvalidatedAtUtc = now;
            }

            var tokenValue = GenerateToken();
            var tokenEntity = new ConnectedPlayerRegistrationToken
            {
                PlayerId = dto.PlayerId,
                TokenHash = HashToken(tokenValue),
                ExpiresAtUtc = now.AddMinutes(dto.ExpiryMinutes),
                AttemptCount = 0,
                MaxAttempts = dto.MaxAttempts,
                IsActive = true,
                IssuedAtUtc = now,
                IssuedBy = string.IsNullOrWhiteSpace(dto.IssuedBy) ? "RegisterCommand" : dto.IssuedBy
            };

            await context.ConnectedPlayerRegistrationTokens.AddAsync(tokenEntity, cancellationToken).ConfigureAwait(false);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            var tokenActorId = string.IsNullOrWhiteSpace(dto.IssuedBy) ? "ServiceAccount" : dto.IssuedBy;
            auditLogger.LogAudit(AuditEvent.SystemAction("ConnectedPlayerTokenIssued", AuditAction.Create)
                .WithActor(tokenActorId, "ConnectedPlayersController")
                .WithTarget(dto.PlayerId.ToString(), "Player")
                .WithSource("ConnectedPlayersController")
                .Build());

            var resultDto = tokenEntity.ToResultDto(tokenValue);
            return new ApiResponse<IssueConnectedPlayerRegistrationTokenResultDto>(resultDto).ToApiResult();
        }

        [HttpPost("connected-players/verify-token")]
        [ProducesResponseType<ConnectedPlayerDto>(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> VerifyConnectedPlayerRegistrationToken([FromBody] VerifyConnectedPlayerRegistrationTokenDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            var response = await ((IConnectedPlayersApi)this)
                .VerifyConnectedPlayerRegistrationToken(dto, cancellationToken)
                .ConfigureAwait(false);

            return response.ToHttpResult();
        }

        async Task<ApiResult<ConnectedPlayerDto>> IConnectedPlayersApi.VerifyConnectedPlayerRegistrationToken(
            VerifyConnectedPlayerRegistrationTokenDto dto,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
            {
                return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.InvalidRequest, ApiErrorMessages.InvalidRequestBodyMessage)));
            }

            var userProfileExists = await context.UserProfiles
                .AsNoTracking()
                .AnyAsync(u => u.UserProfileId == dto.UserProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (!userProfileExists)
            {
                return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.UserProfileNotFoundMessage)));
            }

            if (dto.LinkedByUserProfileId.HasValue)
            {
                var linkedByUserProfileExists = await context.UserProfiles
                    .AsNoTracking()
                    .AnyAsync(u => u.UserProfileId == dto.LinkedByUserProfileId.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (!linkedByUserProfileExists)
                {
                    return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                        new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.UserProfileNotFoundMessage)));
                }
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var token = await context.ConnectedPlayerRegistrationTokens
                .OrderByDescending(t => t.IssuedAtUtc)
                .FirstOrDefaultAsync(t => t.PlayerId == dto.PlayerId && t.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (token == null)
            {
                return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerTokenInactive, ApiErrorMessages.ConnectedPlayerTokenInactiveMessage)));
            }

            var now = DateTime.UtcNow;
            if (token.ExpiresAtUtc <= now)
            {
                token.IsActive = false;
                token.InvalidatedAtUtc = now;
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerTokenExpired, ApiErrorMessages.ConnectedPlayerTokenExpiredMessage)));
            }

            if (token.AttemptCount >= token.MaxAttempts)
            {
                token.IsActive = false;
                token.InvalidatedAtUtc = now;
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerTokenAttemptsExceeded, ApiErrorMessages.ConnectedPlayerTokenAttemptsExceededMessage)));
            }

            var hashedCandidate = HashToken(dto.Token.Trim());
            if (!string.Equals(token.TokenHash, hashedCandidate, StringComparison.Ordinal))
            {
                token.AttemptCount += 1;

                if (token.AttemptCount >= token.MaxAttempts)
                {
                    token.IsActive = false;
                    token.InvalidatedAtUtc = now;

                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                    return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                        new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerTokenAttemptsExceeded, ApiErrorMessages.ConnectedPlayerTokenAttemptsExceededMessage)));
                }

                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.BadRequest,
                    new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerTokenInvalid, ApiErrorMessages.ConnectedPlayerTokenInvalidMessage)));
            }

            var activeLinkExists = await context.ConnectedPlayerProfiles
                .AsNoTracking()
                .AnyAsync(cp => cp.PlayerId == dto.PlayerId && cp.IsActive, cancellationToken)
                .ConfigureAwait(false);

            if (activeLinkExists)
            {
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.Conflict,
                    new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerAlreadyLinked, ApiErrorMessages.ConnectedPlayerAlreadyLinkedMessage)));
            }

            token.IsActive = false;
            token.VerifiedAtUtc = now;

            var profile = new ConnectedPlayerProfile
            {
                PlayerId = dto.PlayerId,
                UserProfileId = dto.UserProfileId,
                LinkMethod = ConnectedPlayerLinkMethod.TokenVerified.ToString(),
                LinkedAtUtc = now,
                LinkedByUserProfileId = dto.LinkedByUserProfileId,
                IsActive = true
            };

            try
            {
                await context.ConnectedPlayerProfiles.AddAsync(profile, cancellationToken).ConfigureAwait(false);
                await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);

                return new ApiResult<ConnectedPlayerDto>(HttpStatusCode.Conflict,
                    new ApiResponse<ConnectedPlayerDto>(new ApiError(ApiErrorCodes.ConnectedPlayerAlreadyLinked, ApiErrorMessages.ConnectedPlayerAlreadyLinkedMessage)));
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            var verifyActorId = dto.UserProfileId.ToString();
            auditLogger.LogAudit(AuditEvent.SystemAction("ConnectedPlayerTokenVerified", AuditAction.Update)
                .WithActor(verifyActorId, "ConnectedPlayersController")
                .WithTarget(dto.PlayerId.ToString(), "Player")
                .WithSource("ConnectedPlayersController")
                .Build());

            var hydrated = await context.ConnectedPlayerProfiles
                .AsNoTracking()
                .Include(cp => cp.Player)
                .FirstAsync(cp => cp.ConnectedPlayerProfileId == profile.ConnectedPlayerProfileId, cancellationToken)
                .ConfigureAwait(false);

            return new ApiResponse<ConnectedPlayerDto>(hydrated.ToDto()).ToApiResult(HttpStatusCode.Created);
        }

        [HttpGet("user-profiles/{userProfileId:guid}/connected-players")]
        [ProducesResponseType<CollectionModel<ConnectedPlayerDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConnectedPlayersByUserProfile(
            Guid userProfileId,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            CancellationToken cancellationToken = default)
        {
            var response = await ((IConnectedPlayersApi)this)
                .GetConnectedPlayersByUserProfile(userProfileId, skipEntries, takeEntries, cancellationToken)
                .ConfigureAwait(false);

            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<ConnectedPlayerDto>>> IConnectedPlayersApi.GetConnectedPlayersByUserProfile(
            Guid userProfileId,
            int skipEntries,
            int takeEntries,
            CancellationToken cancellationToken)
        {
            var query = context.ConnectedPlayerProfiles
                .AsNoTracking()
                .Include(cp => cp.Player)
                .Where(cp => cp.UserProfileId == userProfileId && cp.IsActive)
                .OrderByDescending(cp => cp.LinkedAtUtc);

            var filteredCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var entities = await query
                .Skip(skipEntries)
                .Take(takeEntries)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = entities.Select(cp => cp.ToDto()).ToList();

            var model = new CollectionModel<ConnectedPlayerDto>(entries);
            return new ApiResponse<CollectionModel<ConnectedPlayerDto>>(model)
            {
                Pagination = new ApiPagination(filteredCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        [HttpGet("connected-players")]
        [ProducesResponseType<CollectionModel<ConnectedPlayerDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetConnectedPlayers(
            [FromQuery] Guid? playerId = null,
            [FromQuery] Guid? userProfileId = null,
            [FromQuery] GameType? gameType = null,
            [FromQuery] bool? isActive = true,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            CancellationToken cancellationToken = default)
        {
            var response = await ((IConnectedPlayersApi)this)
                .GetConnectedPlayers(playerId, userProfileId, gameType, isActive, skipEntries, takeEntries, cancellationToken)
                .ConfigureAwait(false);

            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<ConnectedPlayerDto>>> IConnectedPlayersApi.GetConnectedPlayers(
            Guid? playerId,
            Guid? userProfileId,
            GameType? gameType,
            bool? isActive,
            int skipEntries,
            int takeEntries,
            CancellationToken cancellationToken)
        {
            var query = context.ConnectedPlayerProfiles
                .AsNoTracking()
                .Include(cp => cp.Player)
                .AsQueryable();

            if (playerId.HasValue)
                query = query.Where(cp => cp.PlayerId == playerId.Value);

            if (userProfileId.HasValue)
                query = query.Where(cp => cp.UserProfileId == userProfileId.Value);

            if (gameType.HasValue)
                query = query.Where(cp => cp.Player.GameType == (int)gameType.Value);

            if (isActive.HasValue)
                query = query.Where(cp => cp.IsActive == isActive.Value);

            var filteredCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var entities = await query
                .OrderByDescending(cp => cp.LinkedAtUtc)
                .Skip(skipEntries)
                .Take(takeEntries)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            var entries = entities.Select(cp => cp.ToDto()).ToList();

            var model = new CollectionModel<ConnectedPlayerDto>(entries);
            return new ApiResponse<CollectionModel<ConnectedPlayerDto>>(model)
            {
                Pagination = new ApiPagination(filteredCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        [HttpDelete("connected-players/{connectedPlayerProfileId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForceUnlinkConnectedPlayer(
            Guid connectedPlayerProfileId,
            [FromBody] ForceUnlinkConnectedPlayerDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto == null)
            {
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNull, ApiErrorMessages.RequestBodyNullMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();
            }

            var response = await ((IConnectedPlayersApi)this)
                .ForceUnlinkConnectedPlayer(connectedPlayerProfileId, dto, cancellationToken)
                .ConfigureAwait(false);

            return response.ToHttpResult();
        }

        async Task<ApiResult> IConnectedPlayersApi.ForceUnlinkConnectedPlayer(
            Guid connectedPlayerProfileId,
            ForceUnlinkConnectedPlayerDto dto,
            CancellationToken cancellationToken)
        {
            if (dto.UnlinkedByUserProfileId.HasValue)
            {
                var unlinkedByUserProfileExists = await context.UserProfiles
                    .AsNoTracking()
                    .AnyAsync(u => u.UserProfileId == dto.UnlinkedByUserProfileId.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (!unlinkedByUserProfileExists)
                {
                    return new ApiResult(HttpStatusCode.BadRequest,
                        new ApiResponse(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.UserProfileNotFoundMessage)));
                }
            }

            var link = await context.ConnectedPlayerProfiles
                .FirstOrDefaultAsync(cp => cp.ConnectedPlayerProfileId == connectedPlayerProfileId, cancellationToken)
                .ConfigureAwait(false);

            if (link == null)
            {
                return new ApiResult(HttpStatusCode.NotFound,
                    new ApiResponse(new ApiError(ApiErrorCodes.ConnectedPlayerNotFound, ApiErrorMessages.ConnectedPlayerNotFoundMessage)));
            }

            if (!link.IsActive)
                return new ApiResponse().ToApiResult();

            link.IsActive = false;
            link.UnlinkedAtUtc = DateTime.UtcNow;
            link.UnlinkedByUserProfileId = dto.UnlinkedByUserProfileId;

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var unlinkActorId = dto.UnlinkedByUserProfileId?.ToString() ?? "ServiceAccount";
            auditLogger.LogAudit(AuditEvent.SystemAction("ConnectedPlayerForceUnlinked", AuditAction.Delete)
                .WithActor(unlinkActorId, "ConnectedPlayersController")
                .WithTarget(link.PlayerId.ToString(), "Player")
                .WithSource("ConnectedPlayersController")
                .Build());

            return new ApiResponse().ToApiResult();
        }

        private static string GenerateToken()
        {
            var value = RandomNumberGenerator.GetInt32(0, 1000000);
            return value.ToString("D6");
        }

        private static string GenerateActivationCode()
        {
            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            Span<char> chars = stackalloc char[6];

            for (var index = 0; index < chars.Length; index++)
            {
                chars[index] = alphabet[RandomNumberGenerator.GetInt32(0, alphabet.Length)];
            }

            return new string(chars);
        }

        private async Task<string> GenerateUniqueActivationCode(CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                var code = GenerateActivationCode();
                var exists = await context.ConnectedPlayerActivationCodes
                    .AsNoTracking()
                    .AnyAsync(existingCode => existingCode.Code == code && existingCode.IsActive, cancellationToken)
                    .ConfigureAwait(false);

                if (!exists)
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Failed to generate a unique connected-player activation code.");
        }

        private static string HashToken(string token)
        {
            var bytes = Encoding.UTF8.GetBytes(token);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }
    }
}
