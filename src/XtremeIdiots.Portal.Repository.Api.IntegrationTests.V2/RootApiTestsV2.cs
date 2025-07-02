using MxIO.ApiClient.Abstractions;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V2;

public class RootApiTestsV2 : BaseApiTests
{
    [Fact]
    public async Task GetRoot_V2_ShouldReturnSuccessfulResponse()
    {
        // Act
        var response = await repositoryApiClient.Root.V2.GetRoot();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccess, "V2 root endpoint should return successful response");
    }
}
