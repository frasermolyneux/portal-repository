using Moq;
using XtremeIdiots.Portal.Repository.Api.Client.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V2
{
    public class RepositoryApiClientTests
    {
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
