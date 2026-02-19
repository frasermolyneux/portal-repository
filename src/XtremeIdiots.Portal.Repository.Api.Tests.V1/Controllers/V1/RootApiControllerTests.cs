using System.Net;
using Microsoft.AspNetCore.Mvc;
using MX.Api.Abstractions;
using Xunit;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class RootApiControllerTests
{
    private readonly RootApiController _controller;

    public RootApiControllerTests()
    {
        _controller = new RootApiController();
    }

    [Fact]
    public async Task GetRoot_ReturnsOkResult()
    {
        var result = await _controller.GetRoot();
        Assert.NotNull(result);
    }
}
