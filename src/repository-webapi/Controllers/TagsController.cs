using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using XtremeIdiots.Portal.DataLib;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Models.Tags;
using MxIO.ApiClient.Abstractions;
using MxIO.ApiClient.WebExtensions;
using XtremeIdiots.Portal.RepositoryApi.Abstractions.Constants;

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [ApiVersion(ApiVersions.V1)]
    [Route("v{version:apiVersion}")]
    [Route("")]
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
        public async Task<IActionResult> GetTags([FromQuery] int skipEntries = 0, [FromQuery] int takeEntries = 50)
        {
            var response = await ((ITagsApi)this).GetTags(skipEntries, takeEntries);
            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<TagsCollectionDto>> ITagsApi.GetTags(int skipEntries, int takeEntries)
        {
            var tags = await context.Tags.OrderBy(t => t.Name).Skip(skipEntries).Take(takeEntries).ToListAsync();
            var result = new TagsCollectionDto { Entries = tags.Select(mapper.Map<TagDto>).ToList() };
            return new ApiResponseDto<TagsCollectionDto>(HttpStatusCode.OK, result);
        }

        [HttpGet]
        [Route("tags/{tagId}")]
        public async Task<IActionResult> GetTag(Guid tagId)
        {
            var response = await ((ITagsApi)this).GetTag(tagId);
            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<TagDto>> ITagsApi.GetTag(Guid tagId)
        {
            var tag = await context.Tags.FindAsync(tagId);
            if (tag == null)
                return new ApiResponseDto<TagDto>(HttpStatusCode.NotFound);
            return new ApiResponseDto<TagDto>(HttpStatusCode.OK, mapper.Map<TagDto>(tag));
        }

        [HttpPost]
        [Route("tags")]
        public async Task<IActionResult> CreateTag([FromBody] TagDto tagDto)
        {
            var response = await ((ITagsApi)this).CreateTag(tagDto);
            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> ITagsApi.CreateTag(TagDto tagDto)
        {
            var tag = mapper.Map<Tag>(tagDto);
            context.Tags.Add(tag);
            await context.SaveChangesAsync();
            return new ApiResponseDto(HttpStatusCode.OK);
        }

        [HttpPut]
        [Route("tags")]
        public async Task<IActionResult> UpdateTag([FromBody] TagDto tagDto)
        {
            var response = await ((ITagsApi)this).UpdateTag(tagDto);
            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> ITagsApi.UpdateTag(TagDto tagDto)
        {
            var tag = await context.Tags.FindAsync(tagDto.TagId);
            if (tag == null)
                return new ApiResponseDto(HttpStatusCode.NotFound);
            mapper.Map(tagDto, tag);
            await context.SaveChangesAsync();
            return new ApiResponseDto(HttpStatusCode.OK);
        }

        [HttpDelete]
        [Route("tags/{tagId}")]
        public async Task<IActionResult> DeleteTag(Guid tagId)
        {
            var response = await ((ITagsApi)this).DeleteTag(tagId);
            return response.ToHttpResult();
        }
        async Task<ApiResponseDto> ITagsApi.DeleteTag(Guid tagId)
        {
            var tag = await context.Tags.FindAsync(tagId);
            if (tag == null)
                return new ApiResponseDto(HttpStatusCode.NotFound);
            context.Tags.Remove(tag);
            await context.SaveChangesAsync();
            return new ApiResponseDto(HttpStatusCode.OK);
        }
    }
}
