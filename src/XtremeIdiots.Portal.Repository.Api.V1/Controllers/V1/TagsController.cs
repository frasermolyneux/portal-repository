using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;
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
        private readonly PortalDbContext context;
        private readonly IMapper mapper;

        public TagsController(PortalDbContext context, IMapper mapper)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [Route("tags")]
        public async Task<IActionResult> GetTags([FromQuery] int skipEntries = 0, [FromQuery] int takeEntries = 50, CancellationToken cancellationToken = default)
        {
            var response = await ((ITagsApi)this).GetTags(skipEntries, takeEntries, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<CollectionModel<TagDto>>> ITagsApi.GetTags(int skipEntries, int takeEntries, CancellationToken cancellationToken)
        {
            var totalCount = await context.Tags.CountAsync(cancellationToken);
            var tags = await context.Tags.OrderBy(t => t.Name).Skip(skipEntries).Take(takeEntries).ToListAsync(cancellationToken);

            var result = new CollectionModel<TagDto>
            {
                TotalCount = totalCount,
                FilteredCount = totalCount, // No filtering for tags
                Items = tags.Select(mapper.Map<TagDto>).ToList()
            };

            return new ApiResult<CollectionModel<TagDto>>(HttpStatusCode.OK, new ApiResponse<CollectionModel<TagDto>>(result));
        }

        [HttpGet]
        [Route("tags/{tagId}")]
        public async Task<IActionResult> GetTag(Guid tagId, CancellationToken cancellationToken = default)
        {
            var response = await ((ITagsApi)this).GetTag(tagId, cancellationToken);
            return response.ToHttpResult();
        }

        async Task<ApiResult<TagDto>> ITagsApi.GetTag(Guid tagId, CancellationToken cancellationToken)
        {
            var tag = await context.Tags.FindAsync(new object[] { tagId }, cancellationToken);
            if (tag == null)
                return new ApiResult<TagDto>(HttpStatusCode.NotFound);

            return new ApiResult<TagDto>(HttpStatusCode.OK, new ApiResponse<TagDto>(mapper.Map<TagDto>(tag)));
        }

        [HttpPost]
        [Route("tags")]
        public async Task<IActionResult> CreateTag([FromBody] TagDto tagDto, CancellationToken cancellationToken = default)
        {
            var response = await ((ITagsApi)this).CreateTag(tagDto, CancellationToken.None);
            return response.ToHttpResult();
        }

        async Task<ApiResult> ITagsApi.CreateTag(TagDto tagDto, CancellationToken cancellationToken)
        {
            var tag = mapper.Map<Tag>(tagDto);
            context.Tags.Add(tag);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.Created, new ApiResponse());
        }

        [HttpPut]
        [Route("tags")]
        public async Task<IActionResult> UpdateTag([FromBody] TagDto tagDto, CancellationToken cancellationToken = default)
        {
            var response = await ((ITagsApi)this).UpdateTag(tagDto, CancellationToken.None);
            return response.ToHttpResult();
        }

        async Task<ApiResult> ITagsApi.UpdateTag(TagDto tagDto, CancellationToken cancellationToken)
        {
            var tag = await context.Tags.FindAsync(new object[] { tagDto.TagId }, cancellationToken);
            if (tag == null)
                return new ApiResult(HttpStatusCode.NotFound, new ApiResponse());

            mapper.Map(tagDto, tag);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.OK, new ApiResponse());
        }

        [HttpDelete]
        [Route("tags/{tagId}")]
        public async Task<IActionResult> DeleteTag(Guid tagId, CancellationToken cancellationToken = default)
        {
            var response = await ((ITagsApi)this).DeleteTag(tagId, CancellationToken.None);
            return response.ToHttpResult();
        }
        async Task<ApiResult> ITagsApi.DeleteTag(Guid tagId, CancellationToken cancellationToken)
        {
            var tag = await context.Tags.FindAsync(new object[] { tagId }, cancellationToken);
            if (tag == null)
                return new ApiResult(HttpStatusCode.NotFound, new ApiResponse());

            context.Tags.Remove(tag);
            await context.SaveChangesAsync(cancellationToken);
            return new ApiResult(HttpStatusCode.OK, new ApiResponse());
        }
    }
}

