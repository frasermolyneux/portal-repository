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

namespace XtremeIdiots.Portal.RepositoryWebApi.Controllers
{
    [ApiController]
    [Authorize(Roles = "ServiceAccount")]
    [Route("tags")]
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

        [HttpGet("{tagId}")]
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

        [HttpDelete("{tagId}")]
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

        [HttpGet("player/{playerId}")]
        public async Task<IActionResult> GetPlayerTags(Guid playerId)
        {
            var response = await ((ITagsApi)this).GetPlayerTags(playerId);
            return response.ToHttpResult();
        }

        async Task<ApiResponseDto<PlayerTagsCollectionDto>> ITagsApi.GetPlayerTags(Guid playerId)
        {
            var playerTags = await context.PlayerTags.Where(pt => pt.PlayerId == playerId).ToListAsync();
            var result = new PlayerTagsCollectionDto { Entries = playerTags.Select(mapper.Map<PlayerTagDto>).ToList() };
            return new ApiResponseDto<PlayerTagsCollectionDto>(HttpStatusCode.OK, result);
        }

        [HttpPost("player")]
        public async Task<IActionResult> AddPlayerTag([FromBody] PlayerTagDto playerTagDto)
        {
            var response = await ((ITagsApi)this).AddPlayerTag(playerTagDto);
            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> ITagsApi.AddPlayerTag(PlayerTagDto playerTagDto)
        {
            var playerTag = mapper.Map<PlayerTag>(playerTagDto);
            context.PlayerTags.Add(playerTag);
            await context.SaveChangesAsync();
            return new ApiResponseDto(HttpStatusCode.OK);
        }

        [HttpDelete("player/{playerTagId}")]
        public async Task<IActionResult> RemovePlayerTag(Guid playerTagId)
        {
            var response = await ((ITagsApi)this).RemovePlayerTag(playerTagId);
            return response.ToHttpResult();
        }

        async Task<ApiResponseDto> ITagsApi.RemovePlayerTag(Guid playerTagId)
        {
            var playerTag = await context.PlayerTags.FindAsync(playerTagId);
            if (playerTag == null)
                return new ApiResponseDto(HttpStatusCode.NotFound);
            context.PlayerTags.Remove(playerTag);
            await context.SaveChangesAsync();
            return new ApiResponseDto(HttpStatusCode.OK);
        }
    }
}
