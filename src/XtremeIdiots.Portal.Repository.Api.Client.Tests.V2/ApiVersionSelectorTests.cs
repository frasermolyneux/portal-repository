using Moq;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;
using XtremeIdiots.Portal.Repository.Api.Client.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V2
{
    public class ApiVersionSelectorTests
    {
        [Fact]
        public void VersionedApiHealthApi_ExposesV2()
        {
            var mock = new Mock<IApiHealthApi>();
            var selector = new VersionedApiHealthApi(mock.Object);
            Assert.Same(mock.Object, selector.V2);
        }

        [Fact]
        public void VersionedApiInfoApi_ExposesV2()
        {
            var mock = new Mock<IApiInfoApi>();
            var selector = new VersionedApiInfoApi(mock.Object);
            Assert.Same(mock.Object, selector.V2);
        }

        [Fact]
        public void RepositoryApiClient_ExposesApiHealth()
        {
            var mockHealth = new Mock<IVersionedApiHealthApi>();
            var mockInfo = new Mock<IVersionedApiInfoApi>();
            var client = new RepositoryApiClient(mockHealth.Object, mockInfo.Object);
            Assert.Same(mockHealth.Object, client.ApiHealth);
        }

        [Fact]
        public void RepositoryApiClient_ExposesApiInfo()
        {
            var mockHealth = new Mock<IVersionedApiHealthApi>();
            var mockInfo = new Mock<IVersionedApiInfoApi>();
            var client = new RepositoryApiClient(mockHealth.Object, mockInfo.Object);
            Assert.Same(mockInfo.Object, client.ApiInfo);
        }
    }
}
