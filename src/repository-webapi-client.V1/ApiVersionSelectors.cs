using XtremeIdiots.Portal.RepositoryApi.Abstractions.Interfaces;

namespace XtremeIdiots.Portal.RepositoryApiClient.V1
{
    // Version selectors for APIs that only have V1
    public interface IVersionedAdminActionsApi
    {
        IAdminActionsApi V1 { get; }
    }

    public interface IVersionedBanFileMonitorsApi
    {
        IBanFileMonitorsApi V1 { get; }
    }

    public interface IVersionedChatMessagesApi
    {
        IChatMessagesApi V1 { get; }
    }

    public interface IVersionedDataMaintenanceApi
    {
        IDataMaintenanceApi V1 { get; }
    }

    public interface IVersionedDemosApi
    {
        IDemosApi V1 { get; }
    }

    public interface IVersionedGameServersApi
    {
        IGameServersApi V1 { get; }
    }

    public interface IVersionedGameServersEventsApi
    {
        IGameServersEventsApi V1 { get; }
    }

    public interface IVersionedGameServersStatsApi
    {
        IGameServersStatsApi V1 { get; }
    }

    public interface IVersionedGameTrackerBannerApi
    {
        IGameTrackerBannerApi V1 { get; }
    }

    public interface IVersionedLivePlayersApi
    {
        ILivePlayersApi V1 { get; }
    }

    public interface IVersionedMapsApi
    {
        IMapsApi V1 { get; }
    }

    public interface IVersionedMapPacksApi
    {
        IMapPacksApi V1 { get; }
    }

    public interface IVersionedPlayerAnalyticsApi
    {
        IPlayerAnalyticsApi V1 { get; }
    }

    public interface IVersionedPlayersApi
    {
        IPlayersApi V1 { get; }
    }

    public interface IVersionedRecentPlayersApi
    {
        IRecentPlayersApi V1 { get; }
    }

    public interface IVersionedReportsApi
    {
        IReportsApi V1 { get; }
    }

    public interface IVersionedTagsApi
    {
        ITagsApi V1 { get; }
    }

    public interface IVersionedUserProfileApi
    {
        IUserProfileApi V1 { get; }
    }

    public interface IVersionedRootApi
    {
        IRootApi V1 { get; }
        IRootApi V1_1 { get; }
    }
}
