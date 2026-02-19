using Moq;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V2;
using XtremeIdiots.Portal.Repository.Api.Client.V2;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V2
{
    public class ApiVersionSelectorTests
    {
        [Fact]
        public void VersionedRootApi_ExposesV2()
        {
            var mock = new Mock<IRootApi>();
            var selector = new VersionedRootApi(mock.Object);
            Assert.Same(mock.Object, selector.V2);
        }

        [Fact]
        public void VersionedRootApi_V2IsNotNull()
        {
            var mock = new Mock<IRootApi>();
            var selector = new VersionedRootApi(mock.Object);
            Assert.NotNull(selector.V2);
        }

        [Fact]
        public void VersionedRootApi_V2ReturnsCorrectType()
        {
            var mock = new Mock<IRootApi>();
            var selector = new VersionedRootApi(mock.Object);
            Assert.IsAssignableFrom<IRootApi>(selector.V2);
        }
    }
}
