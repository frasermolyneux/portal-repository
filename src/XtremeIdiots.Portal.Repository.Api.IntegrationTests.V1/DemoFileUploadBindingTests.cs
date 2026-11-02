using System.Net;
using MX.Api.Abstractions;
using Newtonsoft.Json;

using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.IntegrationTests.V1;

[Trait("Category", "Integration")]
public class DemoFileUploadBindingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DemoFileUploadBindingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SetDemoFile_WithInvalidExtension_ReturnsInvalidFileType()
    {
        using var client = _factory.CreateClient();
        using var content = new MultipartFormDataContent
        {
            { new ByteArrayContent([1, 2, 3]), "files", "invalid.txt" }
        };

        var response = await client.PostAsync($"/v1.0/demos/{Guid.NewGuid()}/file", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = JsonConvert.DeserializeObject<ApiResponse>(await response.Content.ReadAsStringAsync());
        Assert.Equal(ApiErrorCodes.InvalidFileType, Assert.Single(result!.Errors!).Code);
    }
}
