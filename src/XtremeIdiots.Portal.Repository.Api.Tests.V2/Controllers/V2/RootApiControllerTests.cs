using Microsoft.AspNetCore.Mvc;
using MX.Api.Abstractions;
using System.Net;
using Xunit;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V2;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V2.Controllers.V2;

public class RootApiControllerTests
{
    private readonly RootApiController _controller;

    public RootApiControllerTests()
    {
        _controller = new RootApiController();
    }

    [Fact]
    public async Task GetRoot_ReturnsApiResult()
    {
        // Act
        var result = await _controller.GetRoot();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ApiResult>(result);
    }

    [Fact]
    public async Task GetRoot_ReturnsOkStatusCode()
    {
        // Act
        var result = await _controller.GetRoot();

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }
}
