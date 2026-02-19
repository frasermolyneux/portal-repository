using Moq;
using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;
using XtremeIdiots.Portal.Repository.Api.Client.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Tests.V1
{
    public class ApiVersionSelectorTests
    {
        [Fact]
        public void VersionedAdminActionsApi_ExposesV1()
        {
            var mock = new Mock<IAdminActionsApi>();
            var selector = new VersionedAdminActionsApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedBanFileMonitorsApi_ExposesV1()
        {
            var mock = new Mock<IBanFileMonitorsApi>();
            var selector = new VersionedBanFileMonitorsApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedChatMessagesApi_ExposesV1()
        {
            var mock = new Mock<IChatMessagesApi>();
            var selector = new VersionedChatMessagesApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedDataMaintenanceApi_ExposesV1()
        {
            var mock = new Mock<IDataMaintenanceApi>();
            var selector = new VersionedDataMaintenanceApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedDemosApi_ExposesV1()
        {
            var mock = new Mock<IDemosApi>();
            var selector = new VersionedDemosApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedGameServersApi_ExposesV1()
        {
            var mock = new Mock<IGameServersApi>();
            var selector = new VersionedGameServersApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedGameServersEventsApi_ExposesV1()
        {
            var mock = new Mock<IGameServersEventsApi>();
            var selector = new VersionedGameServersEventsApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedGameServersStatsApi_ExposesV1()
        {
            var mock = new Mock<IGameServersStatsApi>();
            var selector = new VersionedGameServersStatsApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedGameTrackerBannerApi_ExposesV1()
        {
            var mock = new Mock<IGameTrackerBannerApi>();
            var selector = new VersionedGameTrackerBannerApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedLivePlayersApi_ExposesV1()
        {
            var mock = new Mock<ILivePlayersApi>();
            var selector = new VersionedLivePlayersApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedMapsApi_ExposesV1()
        {
            var mock = new Mock<IMapsApi>();
            var selector = new VersionedMapsApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedMapPacksApi_ExposesV1()
        {
            var mock = new Mock<IMapPacksApi>();
            var selector = new VersionedMapPacksApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedPlayerAnalyticsApi_ExposesV1()
        {
            var mock = new Mock<IPlayerAnalyticsApi>();
            var selector = new VersionedPlayerAnalyticsApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedPlayersApi_ExposesV1()
        {
            var mock = new Mock<IPlayersApi>();
            var selector = new VersionedPlayersApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedRecentPlayersApi_ExposesV1()
        {
            var mock = new Mock<IRecentPlayersApi>();
            var selector = new VersionedRecentPlayersApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedReportsApi_ExposesV1()
        {
            var mock = new Mock<IReportsApi>();
            var selector = new VersionedReportsApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedTagsApi_ExposesV1()
        {
            var mock = new Mock<ITagsApi>();
            var selector = new VersionedTagsApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedUserProfileApi_ExposesV1()
        {
            var mock = new Mock<IUserProfileApi>();
            var selector = new VersionedUserProfileApi(mock.Object);
            Assert.Same(mock.Object, selector.V1);
        }

        [Fact]
        public void VersionedRootApi_ExposesV1AndV1_1()
        {
            var mockV1 = new Mock<IRootApi>();
            var mockV1_1 = new Mock<XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1_1.IRootApi>();
            var selector = new VersionedRootApi(mockV1.Object, mockV1_1.Object);

            Assert.Same(mockV1.Object, selector.V1);
            Assert.Same(mockV1_1.Object, selector.V1_1);
        }
    }
}
