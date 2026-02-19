using Moq;
using XtremeIdiots.Portal.Repository.Api.Client.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V2
{
    public class RepositoryApiClientTests
    {
        [Fact]
        public void Constructor_StoresRootProperty()
        {
            var mockRoot = new Mock<IVersionedRootApi>();
            var client = new RepositoryApiClient(mockRoot.Object);
            Assert.Same(mockRoot.Object, client.Root);
        }

        [Fact]
        public void Constructor_RootIsNotNull()
        {
            var mockRoot = new Mock<IVersionedRootApi>();
            var client = new RepositoryApiClient(mockRoot.Object);
            Assert.NotNull(client.Root);
        }

        [Fact]
        public void Root_ReturnsCorrectType()
        {
            var mockRoot = new Mock<IVersionedRootApi>();
            var client = new RepositoryApiClient(mockRoot.Object);
            Assert.IsAssignableFrom<IVersionedRootApi>(client.Root);
        }
    }
}
