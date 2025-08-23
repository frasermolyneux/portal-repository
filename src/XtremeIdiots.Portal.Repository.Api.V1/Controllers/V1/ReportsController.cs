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
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Reports;
using XtremeIdiots.Portal.Repository.Api.V1.Extensions;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    /// <summary>
    /// Controller for managing reports - handles CRUD operations and querying of report data.
    /// </summary>
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class ReportsController : ControllerBase, IReportsApi
    {
        private readonly PortalDbContext context;


        /// <summary>
        /// Initializes a new instance of the ReportsController.
        /// </summary>
        /// <param name="context">The portal database context.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public ReportsController(
            PortalDbContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Retrieves a specific report by its unique identifier.
        /// </summary>
        /// <param name="reportId">The unique identifier of the report to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The report details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("reports/{reportId:guid}")]
        [ProducesResponseType<ReportDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReport(Guid reportId, CancellationToken cancellationToken = default)
        {
            var response = await ((IReportsApi)this).GetReport(reportId, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific report by its unique identifier.
        /// </summary>
        /// <param name="reportId">The unique identifier of the report to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the report details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<ReportDto>> IReportsApi.GetReport(Guid reportId, CancellationToken cancellationToken)
        {
            var report = await context.Reports
                .Include(r => r.UserProfile)
                .Include(r => r.AdminUserProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReportId == reportId, cancellationToken);

            if (report == null)
                return new ApiResult<ReportDto>(HttpStatusCode.NotFound);

            var result = report.ToDto();

            return new ApiResponse<ReportDto>(result).ToApiResult();
        }

        /// <summary>
        /// Retrieves a paginated list of reports with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <param name="cutoff">Optional cutoff date for filtering reports (limited to 14 days ago).</param>
        /// <param name="filter">Optional filter criteria for reports.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 20).</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of reports.</returns>
        [HttpGet("reports")]
        [ProducesResponseType<CollectionModel<ReportDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReports(
            [FromQuery] GameType? gameType = null,
            [FromQuery] Guid? gameServerId = null,
            [FromQuery] DateTime? cutoff = null,
            [FromQuery] ReportsFilter? filter = null,
            [FromQuery] int skipEntries = 0,
            [FromQuery] int takeEntries = 20,
            [FromQuery] ReportsOrder? order = null,
            CancellationToken cancellationToken = default)
        {
            if (cutoff.HasValue && cutoff.Value < DateTime.UtcNow.AddDays(-14))
                cutoff = DateTime.UtcNow.AddDays(-14);

            var response = await ((IReportsApi)this).GetReports(gameType, gameServerId, cutoff, filter, skipEntries, takeEntries, order, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of reports with optional filtering and sorting.
        /// </summary>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <param name="cutoff">Optional cutoff date for filtering reports.</param>
        /// <param name="filter">Optional filter criteria for reports.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of reports.</returns>
        async Task<ApiResult<CollectionModel<ReportDto>>> IReportsApi.GetReports(
            GameType? gameType,
            Guid? gameServerId,
            DateTime? cutoff,
            ReportsFilter? filter,
            int skipEntries,
            int takeEntries,
            ReportsOrder? order,
            CancellationToken cancellationToken)
        {
            var baseQuery = context.Reports
                .Include(r => r.UserProfile)
                .Include(r => r.AdminUserProfile)
                .AsNoTracking()
                .AsQueryable();

            // Calculate total count before applying filters
            var totalCount = await baseQuery.CountAsync(cancellationToken);

            // Apply filters
            var filteredQuery = ApplyFilters(baseQuery, gameType, gameServerId, cutoff, filter);
            var filteredCount = await filteredQuery.CountAsync(cancellationToken);

            // Apply ordering and pagination
            var orderedQuery = ApplyOrderingAndPagination(filteredQuery, skipEntries, takeEntries, order);
            var results = await orderedQuery.ToListAsync(cancellationToken);

            var entries = results.Select(r => r.ToDto()).ToList();

            var result = new CollectionModel<ReportDto>
            {
                TotalCount = totalCount,
                FilteredCount = filteredCount,
                Items = entries
            };

            return new ApiResponse<CollectionModel<ReportDto>>(result)
            {
                Pagination = new ApiPagination(totalCount, filteredCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Creates multiple new reports.
        /// </summary>
        /// <param name="createReportDtos">The list of report data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating the reports were created.</returns>
        [HttpPost("reports")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateReports([FromBody] List<CreateReportDto> createReportDtos, CancellationToken cancellationToken = default)
        {
            if (createReportDtos == null || !createReportDtos.Any())
                return BadRequest();

            var response = await ((IReportsApi)this).CreateReports(createReportDtos, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates multiple new reports.
        /// </summary>
        /// <param name="createReportDtos">The list of report data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the reports were created.</returns>
        async Task<ApiResult> IReportsApi.CreateReports(List<CreateReportDto> createReportDtos, CancellationToken cancellationToken)
        {
            var reports = createReportDtos.Select(r => r.ToEntity()).ToList();

            foreach (var report in reports)
            {
                var gameServer = await context.GameServers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(gs => gs.GameServerId == report.GameServerId, cancellationToken);

                if (gameServer == null)
                    return new ApiResult(HttpStatusCode.BadRequest);

                report.GameType = gameServer.GameType;
            }

            await context.Reports.AddRangeAsync(reports, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Closes a specific report.
        /// </summary>
        /// <param name="reportId">The unique identifier of the report to close.</param>
        /// <param name="closeReportDto">The data required to close the report.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the report was closed; otherwise, a 404 Not Found or 400 Bad Request response.</returns>
        [HttpPatch("reports/{reportId:guid}/close")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CloseReport(Guid reportId, [FromBody] CloseReportDto closeReportDto, CancellationToken cancellationToken = default)
        {
            if (closeReportDto == null)
                return BadRequest();

            var response = await ((IReportsApi)this).CloseReport(reportId, closeReportDto, cancellationToken);

            return response.ToHttpResult();
        }

        /// <summary>
        /// Closes a specific report.
        /// </summary>
        /// <param name="reportId">The unique identifier of the report to close.</param>
        /// <param name="closeReportDto">The data required to close the report.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the report was closed if successful; otherwise, a 404 Not Found or 400 Bad Request response.</returns>
        async Task<ApiResult> IReportsApi.CloseReport(Guid reportId, CloseReportDto closeReportDto, CancellationToken cancellationToken)
        {
            var report = await context.Reports
                .FirstOrDefaultAsync(r => r.ReportId == reportId, cancellationToken);

            if (report == null)
                return new ApiResult(HttpStatusCode.NotFound);

            var userProfile = await context.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(up => up.UserProfileId == closeReportDto.AdminUserProfileId, cancellationToken);

            if (userProfile == null)
                return new ApiResult(HttpStatusCode.BadRequest);

            closeReportDto.ApplyTo(report);

            report.Closed = true;
            report.ClosedTimestamp = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);

            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Applies filters to the report query based on the specified criteria.
        /// </summary>
        /// <param name="query">The base query to apply filters to.</param>
        /// <param name="gameType">Optional filter by game type.</param>
        /// <param name="gameServerId">Optional filter by game server identifier.</param>
        /// <param name="cutoff">Optional cutoff date for filtering reports.</param>
        /// <param name="filter">Optional filter criteria for reports.</param>
        /// <returns>The filtered query.</returns>
        private static IQueryable<Report> ApplyFilters(IQueryable<Report> query, GameType? gameType, Guid? gameServerId, DateTime? cutoff, ReportsFilter? filter)
        {
            if (gameType.HasValue)
                query = query.Where(r => r.GameType == ((GameType)gameType).ToGameTypeInt());

            if (gameServerId.HasValue)
                query = query.Where(r => r.GameServerId == gameServerId);

            if (cutoff.HasValue)
                query = query.Where(r => r.Timestamp > cutoff);

            if (filter.HasValue)
            {
                query = filter.Value switch
                {
                    ReportsFilter.OpenReports => query.Where(r => !r.Closed),
                    ReportsFilter.ClosedReports => query.Where(r => r.Closed),
                    _ => query
                };
            }

            return query;
        }

        /// <summary>
        /// Applies ordering and pagination to the report query.
        /// </summary>
        /// <param name="query">The query to apply ordering and pagination to.</param>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="order">Optional ordering criteria for results.</param>
        /// <returns>The ordered and paginated query.</returns>
        private static IQueryable<Report> ApplyOrderingAndPagination(IQueryable<Report> query, int skipEntries, int takeEntries, ReportsOrder? order)
        {
            // Apply ordering
            var orderedQuery = order switch
            {
                ReportsOrder.TimestampAsc => query.OrderBy(r => r.Timestamp),
                ReportsOrder.TimestampDesc => query.OrderByDescending(r => r.Timestamp),
                _ => query.OrderByDescending(r => r.Timestamp)
            };

            return orderedQuery.Skip(skipEntries).Take(takeEntries);
        }
    }
}

