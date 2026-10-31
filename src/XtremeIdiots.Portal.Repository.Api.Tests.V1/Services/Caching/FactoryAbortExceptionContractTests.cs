using System.Net;
using System.Reflection;

using MX.Api.Abstractions;

using XtremeIdiots.Portal.Repository.Abstractions.Models.V1.Maps;
using XtremeIdiots.Portal.Repository.Api.V1.Services.Caching;

using Xunit;

namespace XtremeIdiots.Portal.Repository.Api.Tests.V1.Services.Caching;

public class FactoryAbortExceptionContractTests
{
    [Fact]
    public void FactoryAbortException_IsPublicSealedException()
    {
        var exceptionType = typeof(CachingMapReadService).GetNestedType(
            nameof(CachingMapReadService.FactoryAbortException),
            BindingFlags.Public);

        Assert.NotNull(exceptionType);
        Assert.True(exceptionType.IsNestedPublic);
        Assert.True(exceptionType.IsSealed);
        Assert.Equal(typeof(Exception), exceptionType.BaseType);
    }

    [Fact]
    public void FactoryAbortException_PreservesSuccessfulResultReference()
    {
        var result = new ApiResult<MapDto>(
            HttpStatusCode.OK,
            new ApiResponse<MapDto>(new MapDto()));

        var exception = new CachingMapReadService.FactoryAbortException(result);

        Assert.Same(result, exception.Result);
    }

    [Fact]
    public void FactoryAbortException_PreservesNonSuccessResultAndErrorDetails()
    {
        const string errorCode = "MAP_NOT_FOUND";
        const string errorMessage = "The requested map was not found.";
        var result = new ApiResult<MapDto>(
            HttpStatusCode.NotFound,
            new ApiResponse<MapDto>(new ApiError(errorCode, errorMessage)));

        var exception = new CachingMapReadService.FactoryAbortException(result);

        Assert.Same(result, exception.Result);
        var error = Assert.Single(exception.Result.Result!.Errors!);
        Assert.Equal(errorCode, error.Code);
        Assert.Equal(errorMessage, error.Message);
    }
}
