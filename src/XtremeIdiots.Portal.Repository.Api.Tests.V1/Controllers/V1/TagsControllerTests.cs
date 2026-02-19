using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Tags;
using XtremeIdiots.Portal.Repository.Api.Tests.V1.TestHelpers;
using XtremeIdiots.Portal.Repository.DataLib;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class TagsControllerTests
{
    private TagsController CreateController(PortalDbContext context)
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 100 });
        return new TagsController(context, memoryCache);
    }

    [Fact]
    public async Task GetTag_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var tagId = Guid.NewGuid();
        context.Tags.Add(new Tag { TagId = tagId, Name = "TestTag" });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (ITagsApi)controller;
        var result = await api.GetTag(tagId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetTag_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (ITagsApi)controller;
        var result = await api.GetTag(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetTags_ReturnsCollection()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        context.Tags.Add(new Tag { TagId = Guid.NewGuid(), Name = "Tag1" });
        context.Tags.Add(new Tag { TagId = Guid.NewGuid(), Name = "Tag2" });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (ITagsApi)controller;
        var result = await api.GetTags(0, 50);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task CreateTag_CreatesEntity()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (ITagsApi)controller;

        var tagDto = new TagDto { TagId = Guid.NewGuid(), Name = "NewTag" };
        var result = await api.CreateTag(tagDto);

        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        Assert.Single(context.Tags);
    }

    [Fact]
    public async Task UpdateTag_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var tagId = Guid.NewGuid();
        context.Tags.Add(new Tag { TagId = tagId, Name = "Original" });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (ITagsApi)controller;

        var tagDto = new TagDto { TagId = tagId, Name = "Updated" };
        var result = await api.UpdateTag(tagDto);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task UpdateTag_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (ITagsApi)controller;

        var tagDto = new TagDto { TagId = Guid.NewGuid(), Name = "Updated" };
        var result = await api.UpdateTag(tagDto);

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }

    [Fact]
    public async Task DeleteTag_WithValidId_ReturnsOk()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var tagId = Guid.NewGuid();
        context.Tags.Add(new Tag { TagId = tagId, Name = "ToDelete" });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var api = (ITagsApi)controller;
        var result = await api.DeleteTag(tagId);

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Empty(context.Tags);
    }

    [Fact]
    public async Task DeleteTag_WithInvalidId_ReturnsNotFound()
    {
        using var context = DbContextHelper.CreateInMemoryContext();
        var controller = CreateController(context);
        var api = (ITagsApi)controller;
        var result = await api.DeleteTag(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
    }
}
