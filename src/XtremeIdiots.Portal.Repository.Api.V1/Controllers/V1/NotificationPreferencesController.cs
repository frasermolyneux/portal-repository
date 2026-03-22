using System.Net;
using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using MX.Api.Abstractions;
using MX.Api.Web.Extensions;

using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Notifications;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    /// <summary>
    /// Controller for managing notification preferences - handles retrieval and update of user notification preferences.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    public class NotificationPreferencesController : ControllerBase, INotificationPreferencesApi
    {
        private readonly PortalDbContext context;

        /// <summary>
        /// Initializes a new instance of the NotificationPreferencesController.
        /// </summary>
        /// <param name="context">The portal database context.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public NotificationPreferencesController(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        /// <summary>
        /// Retrieves all notification preferences for a specific user.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A collection of notification preferences for the user.</returns>
        [HttpGet("notification-preferences/{userProfileId:guid}")]
        [ProducesResponseType<CollectionModel<NotificationPreferenceDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationPreferences(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var response = await ((INotificationPreferencesApi)this).GetNotificationPreferences(userProfileId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves all notification preferences for a specific user.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a collection of notification preferences.</returns>
        async Task<ApiResult<CollectionModel<NotificationPreferenceDto>>> INotificationPreferencesApi.GetNotificationPreferences(Guid userProfileId, CancellationToken cancellationToken)
        {
            var preferences = await context.NotificationPreferences
                .Include(np => np.NotificationType)
                .Where(np => np.UserProfileId == userProfileId)
                .AsNoTracking()
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = preferences.Select(np => np.ToDto()).ToList();

            var data = new CollectionModel<NotificationPreferenceDto>(entries);

            return new ApiResponse<CollectionModel<NotificationPreferenceDto>>(data).ToApiResult();
        }

        /// <summary>
        /// Updates notification preferences for a specific user.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="editNotificationPreferenceDtos">The list of notification preference updates to apply.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the preferences were updated; otherwise, a 400 Bad Request response.</returns>
        [HttpPut("notification-preferences/{userProfileId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateNotificationPreferences([FromRoute] Guid userProfileId, [FromBody] List<EditNotificationPreferenceDto> editNotificationPreferenceDtos, CancellationToken cancellationToken = default)
        {
            if (editNotificationPreferenceDtos == null || !editNotificationPreferenceDtos.Any())
                return new ApiResponse(new ApiError(ApiErrorCodes.RequestBodyNullOrEmpty, ApiErrorMessages.RequestBodyNullOrEmptyMessage))
                    .ToBadRequestResult()
                    .ToHttpResult();

            var response = await ((INotificationPreferencesApi)this).UpdateNotificationPreferences(userProfileId, editNotificationPreferenceDtos, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates notification preferences for a specific user.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="editNotificationPreferenceDtos">The list of notification preference updates to apply.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the preferences were updated if successful; otherwise, a 400 Bad Request response.</returns>
        async Task<ApiResult> INotificationPreferencesApi.UpdateNotificationPreferences(Guid userProfileId, List<EditNotificationPreferenceDto> editNotificationPreferenceDtos, CancellationToken cancellationToken)
        {
            var existingPreferences = await context.NotificationPreferences
                .Where(np => np.UserProfileId == userProfileId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var requestedTypeIds = editNotificationPreferenceDtos.Select(d => d.NotificationTypeId).Distinct().ToList();
            var validTypeIds = await context.NotificationTypes
                .Where(nt => requestedTypeIds.Contains(nt.NotificationTypeId))
                .Select(nt => nt.NotificationTypeId)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var validTypeIdSet = new HashSet<string>(validTypeIds, StringComparer.OrdinalIgnoreCase);

            foreach (var dto in editNotificationPreferenceDtos)
            {
                if (!validTypeIdSet.Contains(dto.NotificationTypeId))
                    return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));

                var existing = existingPreferences
                    .FirstOrDefault(np => np.NotificationTypeId.Equals(dto.NotificationTypeId, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    dto.ApplyTo(existing);
                }
                else
                {
                    var newPreference = new NotificationPreference
                    {
                        UserProfileId = userProfileId,
                        NotificationTypeId = dto.NotificationTypeId,
                        InSiteEnabled = dto.InSiteEnabled ?? true,
                        EmailEnabled = dto.EmailEnabled ?? true,
                        Created = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };

                    context.NotificationPreferences.Add(newPreference);
                    existingPreferences.Add(newPreference);
                }
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }
    }
}
