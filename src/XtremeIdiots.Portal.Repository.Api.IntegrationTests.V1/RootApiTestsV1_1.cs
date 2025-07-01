using MxIO.ApiClient.Abstractions;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

public class RootApiTestsV1_1 : BaseApiTests
{
    [Fact]
    public async Task GetRoot_V1_1_ShouldReturnSuccessfulResponse()
    {
        // Act
        var response = await repositoryApiClient.Root.V1_1.GetRoot();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccess, "V1.1 root endpoint should return successful response");
    }
}
