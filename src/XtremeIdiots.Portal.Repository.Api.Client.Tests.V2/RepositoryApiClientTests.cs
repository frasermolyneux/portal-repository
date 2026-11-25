using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MX.Api.Client;
using RestSharp;
using XtremeIdiots.Portal.Repository.Api.Client.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V2
{
    public class RepositoryApiClientTests
    {
        [Theory]
        [InlineData(HttpStatusCode.OK, HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.ServiceUnavailable, HttpStatusCode.InternalServerError)]
        public async Task CheckHealth_RequestsReadinessAndReportsAvailability(HttpStatusCode statusCode, HttpStatusCode expectedStatus)
        {
            using var cancellation = new CancellationTokenSource();
            var rest = new Mock<IRestClientService>();
            rest.Setup(client => client.ExecuteAsync(
                    "https://example.test",
                    It.IsAny<RestRequest>(),
                    cancellation.Token))
                .ReturnsAsync(new RestResponse { StatusCode = statusCode, Content = "{}" });
            var api = new ApiHealthApi(
                NullLogger<BaseApi<RepositoryApiClientOptions>>.Instance,
                null,
                rest.Object,
                new RepositoryApiClientOptions { BaseUrl = "https://example.test", MaxRetryCount = 1 });

            var result = await api.CheckHealth(cancellation.Token);

            Assert.Equal(expectedStatus, result.StatusCode);
            rest.Verify(client => client.ExecuteAsync(
                "https://example.test",
                It.Is<RestRequest>(request => request.Resource == "v2/health/ready" && request.Method == Method.Get),
                cancellation.Token), Times.AtLeastOnce);
        }

        private readonly Mock<IVersionedApiHealthApi> _apiHealth = new();
        private readonly Mock<IVersionedApiInfoApi> _apiInfo = new();

        private RepositoryApiClient CreateClient() => new(_apiHealth.Object, _apiInfo.Object);

        [Fact]
        public void Constructor_StoresApiHealthProperty()
        {
            var client = CreateClient();
            Assert.Same(_apiHealth.Object, client.ApiHealth);
        }

        [Fact]
        public void Constructor_StoresApiInfoProperty()
        {
            var client = CreateClient();
            Assert.Same(_apiInfo.Object, client.ApiInfo);
        }

        [Fact]
        public void Constructor_NoPropertyIsNull()
        {
            var client = CreateClient();
            Assert.NotNull(client.ApiHealth);
            Assert.NotNull(client.ApiInfo);
        }

        [Fact]
        public void Properties_ReturnCorrectTypes()
        {
            var client = CreateClient();
            Assert.IsAssignableFrom<IVersionedApiHealthApi>(client.ApiHealth);
            Assert.IsAssignableFrom<IVersionedApiInfoApi>(client.ApiInfo);
        }
    }
}
