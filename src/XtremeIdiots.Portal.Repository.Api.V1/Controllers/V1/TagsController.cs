using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;
using XtremeIdiots.Portal.Repository.Api.V1.Mapping;
using MX.Api.Abstractions;
using MX.Api.Web.Extensions;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;
using Asp.Versioning;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("api/v{version:apiVersion}")]
    public class TagsController : ControllerBase, ITagsApi
    {
        internal const string TagPlayerCountsCacheKey = "TagsController:PlayerCounts";

        private readonly PortalDbContext context;
        private readonly IMemoryCache memoryCache;

        /// <summary>
        /// Initializes a new instance of the TagsController class.
        /// </summary>
        /// <param name="context">The database context for accessing tag data.</param>
        /// <exception cref="ArgumentNullException">Thrown when context is null.</exception>
        public TagsController(PortalDbContext context, IMemoryCache memoryCache)
        {
            ArgumentNullException.ThrowIfNull(context);
            this.context = context;
            ArgumentNullException.ThrowIfNull(memoryCache);
            this.memoryCache = memoryCache;
        }

        private async Task<Dictionary<Guid, int>> GetTagPlayerCountsAsync(CancellationToken cancellationToken)
        {
            var counts = await memoryCache.GetOrCreateAsync(TagPlayerCountsCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                return await context.PlayerTags
                    .AsNoTracking()
                    .Where(pt => pt.TagId.HasValue)
                    .GroupBy(pt => pt.TagId!.Value)
                    .Select(group => new
                    {
                        TagId = group.Key,
                        Count = group.Count()
                    })
                    .ToDictionaryAsync(x => x.TagId, x => x.Count, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);

            return counts ?? [];
        }

        /// <summary>
        /// Retrieves a paginated list of tags.
        /// </summary>
        /// <param name="skipEntries">Number of entries to skip for pagination (default: 0).</param>
        /// <param name="takeEntries">Number of entries to take for pagination (default: 50).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A paginated collection of tags.</returns>
        [HttpGet("tags")]
        [ProducesResponseType<CollectionModel<TagDto>>(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTags([FromQuery] int skipEntries = 0, [FromQuery] int takeEntries = 50, CancellationToken cancellationToken = default)
        {
            var response = await ((ITagsApi)this).GetTags(skipEntries, takeEntries, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a paginated list of tags.
        /// </summary>
        /// <param name="skipEntries">Number of entries to skip for pagination.</param>
        /// <param name="takeEntries">Number of entries to take for pagination.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing a paginated collection of tags.</returns>
        async Task<ApiResult<CollectionModel<TagDto>>> ITagsApi.GetTags(int skipEntries, int takeEntries, CancellationToken cancellationToken)
        {
            var baseQuery = context.Tags.AsNoTracking();

            // Calculate total count before applying ordering and pagination
            var totalCount = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);

            var tags = await baseQuery
                .OrderBy(t => t.Name)
                .Skip(skipEntries)
                .Take(takeEntries)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var playerCounts = await GetTagPlayerCountsAsync(cancellationToken).ConfigureAwait(false);

            var entries = tags
                .Select(tag =>
                {
                    var hasCount = playerCounts.TryGetValue(tag.TagId, out var count);
                    return tag.ToDto(hasCount ? count : 0);
                })
                .ToList();

            var data = new CollectionModel<TagDto>(entries);

            return new ApiResponse<CollectionModel<TagDto>>(data)
            {
                Pagination = new ApiPagination(totalCount, totalCount, skipEntries, takeEntries)
            }.ToApiResult();
        }

        /// <summary>
        /// Retrieves a specific tag by its unique identifier.
        /// </summary>
        /// <param name="tagId">The unique identifier of the tag to retrieve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The tag details if found; otherwise, a 404 Not Found response.</returns>
        [HttpGet("tags/{tagId:guid}")]
        [ProducesResponseType<TagDto>(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTag(Guid tagId, CancellationToken cancellationToken = default)
        {
            var response = await ((ITagsApi)this).GetTag(tagId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Retrieves a specific tag by its unique identifier.
        /// </summary>
        /// <param name="tagId">The unique identifier of the tag to retrieve.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result containing the tag details if found; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult<TagDto>> ITagsApi.GetTag(Guid tagId, CancellationToken cancellationToken)
        {
            var tag = await context.Tags
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TagId == tagId, cancellationToken).ConfigureAwait(false);

            if (tag == null)
                return new ApiResult<TagDto>(HttpStatusCode.NotFound);

            var playerCounts = await GetTagPlayerCountsAsync(cancellationToken).ConfigureAwait(false);
            playerCounts.TryGetValue(tag.TagId, out var playerCount);

            var result = tag.ToDto(playerCount);
            return new ApiResponse<TagDto>(result).ToApiResult();
        }

        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="tagDto">The tag data to create.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response indicating the tag was created.</returns>
        [HttpPost("tags")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateTag([FromBody] TagDto tagDto, CancellationToken cancellationToken = default)
        {
            var response = await ((ITagsApi)this).CreateTag(tagDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Creates a new tag.
        /// </summary>
        /// <param name="tagDto">The tag data to create.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the tag was created.</returns>
        async Task<ApiResult> ITagsApi.CreateTag(TagDto tagDto, CancellationToken cancellationToken)
        {
            var tag = tagDto.ToEntity();
            context.Tags.Add(tag);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult(HttpStatusCode.Created);
        }

        /// <summary>
        /// Partially updates an existing tag. Only non-null properties are applied.
        /// </summary>
        /// <param name="tagId">The unique identifier of the tag.</param>
        /// <param name="tagDto">The tag data to update.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the tag was updated; otherwise, a 404 Not Found response.</returns>
        [HttpPatch("tags/{tagId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateTag([FromRoute] Guid tagId, [FromBody] TagDto tagDto, CancellationToken cancellationToken = default)
        {
            if (tagDto.TagId != tagId)
                return new ApiResponse(new ApiError(ApiErrorCodes.EntityIdMismatch, ApiErrorMessages.RequestEntityMismatchMessage)).ToBadRequestResult().ToHttpResult();

            var response = await ((ITagsApi)this).UpdateTag(tagDto, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Updates an existing tag.
        /// </summary>
        /// <param name="tagDto">The tag data to update.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the tag was updated if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> ITagsApi.UpdateTag(TagDto tagDto, CancellationToken cancellationToken)
        {
            var tag = await context.Tags
                .FirstOrDefaultAsync(t => t.TagId == tagDto.TagId, cancellationToken).ConfigureAwait(false);

            if (tag == null)
                return new ApiResult(HttpStatusCode.NotFound);

            tagDto.ApplyTo(tag);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }

        /// <summary>
        /// Deletes a tag by its unique identifier.
        /// </summary>
        /// <param name="tagId">The unique identifier of the tag to delete.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A success response if the tag was deleted; otherwise, a 404 Not Found response.</returns>
        [HttpDelete("tags/{tagId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTag(Guid tagId, CancellationToken cancellationToken = default)
        {
            var response = await ((ITagsApi)this).DeleteTag(tagId, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        }

        /// <summary>
        /// Deletes a tag by its unique identifier.
        /// </summary>
        /// <param name="tagId">The unique identifier of the tag to delete.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>An API result indicating the tag was deleted if successful; otherwise, a 404 Not Found response.</returns>
        async Task<ApiResult> ITagsApi.DeleteTag(Guid tagId, CancellationToken cancellationToken)
        {
            var tag = await context.Tags
                .FirstOrDefaultAsync(t => t.TagId == tagId, cancellationToken).ConfigureAwait(false);

            if (tag == null)
                return new ApiResult(HttpStatusCode.NotFound);

            context.Tags.Remove(tag);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new ApiResponse().ToApiResult();
        }
    }
}

