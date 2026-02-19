using Microsoft.AspNetCore.Mvc;
using Xunit;
using XtremeIdiots.Portal.RepositoryWebApi.Controllers.V2;
using XtremeIdiots.Portal.RepositoryWebApi.Models;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V2.Controllers.V2;

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
        // Act
        var result = _controller.GetInfo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void GetInfo_ReturnsApiInfoDto()
    {
        // Act
        var result = _controller.GetInfo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiInfo = Assert.IsType<ApiInfoDto>(okResult.Value);
        Assert.NotNull(apiInfo.Version);
        Assert.NotNull(apiInfo.BuildVersion);
        Assert.NotNull(apiInfo.AssemblyVersion);
    }

    [Fact]
    public void GetInfo_VersionIsNotEmpty()
    {
        // Act
        var result = _controller.GetInfo();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var apiInfo = Assert.IsType<ApiInfoDto>(okResult.Value);
        Assert.NotEmpty(apiInfo.Version);
        Assert.NotEmpty(apiInfo.BuildVersion);
        Assert.NotEmpty(apiInfo.AssemblyVersion);
    }
}
