using Microsoft.AspNetCore.Mvc;
using Xunit;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V1;
using XtremeIdiots.Portal.RepositoryWebApi.Models;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Controllers.V1;

public class ApiInfoControllerTests
{
    private readonly ApiInfoController _controller;

    public ApiInfoControllerTests()
    {
        _controller = new ApiInfoController();
    }

    [Fact]
    public void GetInfo_ReturnsOkResult()
    {
        var result = _controller.GetInfo();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void GetInfo_ReturnsApiInfoDto()
    {
        var result = _controller.GetInfo();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiInfo = Assert.IsType<ApiInfoDto>(okResult.Value);
        Assert.NotNull(apiInfo.Version);
        Assert.NotNull(apiInfo.BuildVersion);
        Assert.NotNull(apiInfo.AssemblyVersion);
    }
}
