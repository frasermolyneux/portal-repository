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
    /// Controller for managing notification types - handles retrieval of notification type data.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    public class NotificationTypesController : ControllerBase, INotificationTypesApi
    {
        private readonly PortalDbContext context;

        /// <summary>
        /// Initializes a new instance of the NotificationTypesController.
        /// </summary>
        /// <param name="context">The portal database context.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public NotificationTypesController(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        /// <summary>
        /// Retrieves all enabled notification types ordered by sort order.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A collection of notification types.</returns>
        [HttpGet("notification-types")]
        [ProducesResponseType<CollectionModel<NotificationTypeDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotificationTypes(CancellationToken cancellationToken = default)
        {
            var response = await ((INotificationTypesApi)this).GetNotificationTypes(cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves all enabled notification types ordered by sort order.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a collection of notification types.</returns>
        async Task<ApiResult<CollectionModel<NotificationTypeDto>>> INotificationTypesApi.GetNotificationTypes(CancellationToken cancellationToken)
        {
            var notificationTypes = await context.NotificationTypes
                .Where(nt => nt.IsEnabled)
                .OrderBy(nt => nt.SortOrder)
                .AsNoTracking()
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = notificationTypes.Select(nt => nt.ToDto()).ToList();

            var data = new CollectionModel<NotificationTypeDto>(entries);

            return new ApiResponse<CollectionModel<NotificationTypeDto>>(data).ToApiResult();
        }
    }
}
