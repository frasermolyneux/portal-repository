using XtremeIdiots.Portal.Repository.Abstractions.Interfaces.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.V1
{
    public interface IRepositoryApiClient
    {
        IVersionedAdminActionsApi AdminActions { get; }
        IVersionedBanFileMonitorsApi BanFileMonitors { get; }
        IVersionedChatMessagesApi ChatMessages { get; }
        IVersionedDataMaintenanceApi DataMaintenance { get; }
        IVersionedDemosApi Demos { get; }
        IVersionedGameServersApi GameServers { get; }
        IVersionedGameServersEventsApi GameServersEvents { get; }
        IVersionedGameServersStatsApi GameServersStats { get; }
        IVersionedGameTrackerBannerApi GameTrackerBanner { get; }
        IVersionedLivePlayersApi LivePlayers { get; }
        IVersionedMapsApi Maps { get; }
        IVersionedMapPacksApi MapPacks { get; }
        IVersionedPlayerAnalyticsApi PlayerAnalytics { get; }
        IVersionedPlayersApi Players { get; }
        IVersionedRecentPlayersApi RecentPlayers { get; }
        IVersionedReportsApi Reports { get; }
        IVersionedRootApi Root { get; }
        IVersionedUserProfileApi UserProfiles { get; }
        IVersionedTagsApi Tags { get; }
    }
}
