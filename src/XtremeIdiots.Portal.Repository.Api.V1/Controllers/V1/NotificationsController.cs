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
    /// Controller for managing notifications - handles CRUD operations and querying of notification data.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    public class NotificationsController : ControllerBase, INotificationsApi
    {
        private readonly PortalDbContext context;

        /// <summary>
        /// Initializes a new instance of the NotificationsController.
        /// </summary>
        /// <param name="context">The portal database context.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public NotificationsController(PortalDbContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
        }

        /// <summary>
        /// Retrieves a paginated list of notifications for a specific user with optional filtering and sorting.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="unreadOnly">Optional filter to return only unread notifications.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of notifications.</returns>
        [HttpGet("notifications/{userProfileId:guid}")]
        [ProducesResponseType<CollectionModel<NotificationDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotifications(
            [FromRoute] Guid userProfileId,
            [FromQuery] bool? unreadOnly = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            [FromQuery] NotificationOrder? order = null,
            CancellationToken cancellationToken = default)
        {
            var response = await ((INotificationsApi)this).GetNotifications(userProfileId, unreadOnly, skipEntries, takeEntries, order, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of notifications for a specific user with optional filtering and sorting.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="unreadOnly">Optional filter to return only unread notifications.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of notifications.</returns>
        async Task<ApiResult<CollectionModel<NotificationDto>>> INotificationsApi.GetNotifications(
            Guid userProfileId,
            bool? unreadOnly,
            int skipEntries,
            int takeEntries,
            NotificationOrder? order,
            CancellationToken cancellationToken)
        {
            // Use lightweight query without includes for counting
            var countQuery = context.Notifications
                .Where(n => n.UserProfileId == userProfileId)
                .AsNoTracking()
                .AsQueryable();

            var totalCount = await countQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var filteredCountQuery = ApplyFilters(countQuery, unreadOnly);
            var filteredCount = await filteredCountQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            // Build data query with includes only for the paginated results
            var dataQuery = context.Notifications
                .Include(n => n.NotificationType)
                .Where(n => n.UserProfileId == userProfileId)
                .AsNoTracking()
                .AsQueryable();

            var filteredDataQuery = ApplyFilters(dataQuery, unreadOnly);
            var orderedQuery = ApplyOrderingAndPagination(filteredDataQuery, skipEntries, takeEntries, order);
            var notifications = await orderedQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

            var entries = notifications.Select(n => n.ToDto()).ToList();

            var data = new CollectionModel<NotificationDto>(entries);

            return new ApiResponse<CollectionModel<NotificationDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Retrieves the count of unread notifications for a specific user.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The count of unread notifications.</returns>
        [HttpGet("notifications/{userProfileId:guid}/unread-count")]
        [ProducesResponseType<int>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadNotificationCount(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var response = await ((INotificationsApi)this).GetUnreadNotificationCount(userProfileId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves the count of unread notifications for a specific user.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the count of unread notifications.</returns>
        async Task<ApiResult<int>> INotificationsApi.GetUnreadNotificationCount(Guid userProfileId, CancellationToken cancellationToken)
        {
            var count = await context.Notifications
                .Where(n => n.UserProfileId == userProfileId && !n.IsRead)
                .AsNoTracking()
                .CountAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse<int>(count).ToApiResult();
        }

        /// <summary>
        /// Creates a new notification.
        /// </summary>
        /// <param name="createNotificationDto">The notification data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating the notification was created.</returns>
        [HttpPost("notifications")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto createNotificationDto, CancellationToken cancellationToken = default)
        {
            var response = await ((INotificationsApi)this).CreateNotification(createNotificationDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new notification.
        /// </summary>
        /// <param name="createNotificationDto">The notification data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the notification was created.</returns>
        async Task<ApiResult> INotificationsApi.CreateNotification(CreateNotificationDto createNotificationDto, CancellationToken cancellationToken)
        {
            var userExists = await context.UserProfiles
                .AnyAsync(up => up.UserProfileId == createNotificationDto.UserProfileId, cancellationToken).ConfigureAwait(false);

            if (!userExists)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.UserProfileNotFoundMessage)));

            var notificationTypeExists = await context.NotificationTypes
                .AnyAsync(nt => nt.NotificationTypeId == createNotificationDto.NotificationTypeId, cancellationToken).ConfigureAwait(false);

            if (!notificationTypeExists)
                return new ApiResult(HttpStatusCode.BadRequest, new ApiResponse(new ApiError(ApiErrorCodes.EntityNotFound, ApiErrorMessages.EntityNotFound)));

            var notification = createNotificationDto.ToEntity();

            context.Notifications.Add(notification);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Marks a specific notification as read.
        /// </summary>
        /// <param name="notificationId">The unique identifier of the notification to mark as read.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the notification was marked as read; otherwise, a 404 Not Found response.</returns>
        [HttpPatch("notifications/{notificationId:guid}/read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> MarkNotificationAsRead(Guid notificationId, CancellationToken cancellationToken = default)
        {
            var response = await ((INotificationsApi)this).MarkNotificationAsRead(notificationId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Marks a specific notification as read.
        /// </summary>
        /// <param name="notificationId">The unique identifier of the notification to mark as read.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the notification was marked as read if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> INotificationsApi.MarkNotificationAsRead(Guid notificationId, CancellationToken cancellationToken)
        {
            var notification = await context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId, cancellationToken).ConfigureAwait(false);

            if (notification == null)
                return new ApiResult(HttpStatusCode.NotFound);

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Marks all unread notifications as read for a specific user.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating all notifications were marked as read.</returns>
        [HttpPatch("notifications/{userProfileId:guid}/read-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllNotificationsAsRead(Guid userProfileId, CancellationToken cancellationToken = default)
        {
            var response = await ((INotificationsApi)this).MarkAllNotificationsAsRead(userProfileId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Marks all unread notifications as read for a specific user.
        /// </summary>
        /// <param name="userProfileId">The unique identifier of the user profile.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating all notifications were marked as read.</returns>
        async Task<ApiResult> INotificationsApi.MarkAllNotificationsAsRead(Guid userProfileId, CancellationToken cancellationToken)
        {
            var unreadNotifications = await context.Notifications
                .Where(n => n.UserProfileId == userProfileId && !n.IsRead)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var now = DateTime.UtcNow;
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new ApiResponse().ToApiResult();
        }

        private static IQueryable<Notification> ApplyFilters(IQueryable<Notification> query, bool? unreadOnly)
        {
            if (unreadOnly.HasValue && unreadOnly.Value)
                query = query.Where(n => !n.IsRead);

            return query;
        }

        private static IQueryable<Notification> ApplyOrderingAndPagination(IQueryable<Notification> query, int skipEntries, int takeEntries, NotificationOrder? order)
        {
            var orderedQuery = order switch
            {
                NotificationOrder.CreatedAtAsc => query.OrderBy(n => n.CreatedAt),
                NotificationOrder.CreatedAtDesc => query.OrderByDescending(n => n.CreatedAt),
                _ => query.OrderByDescending(n => n.CreatedAt)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}
