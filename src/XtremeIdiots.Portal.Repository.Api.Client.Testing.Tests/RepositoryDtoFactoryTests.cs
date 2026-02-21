using XtremeIdiots.Portal.Repository.Api.Client.Testing;
using XtremeIdiots.Portal.Repository.Abstractions.Constants.V1;

namespace XtremeIdiots.Portal.Repository.Api.Client.Testing.Tests;

[Trait("Category", "Unit")]
public class RepositoryDtoFactoryTests
{
    [Fact]
    public void CreatePlayer_WithDefaults_CreatesNonNullDto()
    {
        var player = RepositoryDtoFactory.CreatePlayer();

        Assert.NotEqual(Guid.Empty, player.PlayerId);
        Assert.Equal("TestPlayer", player.Username);
        Assert.Equal("test-guid", player.Guid);
        Assert.Equal(GameType.CallOfDuty4, player.GameType);
        Assert.Equal("192.168.1.1", player.IpAddress);
        Assert.NotNull(player.PlayerAliases);
        Assert.NotNull(player.PlayerIpAddresses);
        Assert.NotNull(player.AdminActions);
        Assert.NotNull(player.Reports);
        Assert.NotNull(player.RelatedPlayers);
        Assert.NotNull(player.ProtectedNames);
        Assert.NotNull(player.Tags);
    }

    [Fact]
    public void CreatePlayer_WithCustomValues_UsesProvidedValues()
    {
        var id = Guid.NewGuid();
        var player = RepositoryDtoFactory.CreatePlayer(
            playerId: id,
            username: "CustomPlayer",
            guid: "custom-guid",
            gameType: GameType.CallOfDuty5,
            ipAddress: "10.0.0.1");

        Assert.Equal(id, player.PlayerId);
        Assert.Equal("CustomPlayer", player.Username);
        Assert.Equal("custom-guid", player.Guid);
        Assert.Equal(GameType.CallOfDuty5, player.GameType);
        Assert.Equal("10.0.0.1", player.IpAddress);
    }

    [Fact]
    public void CreatePlayer_CollectionsDefaultToEmptyLists()
    {
        var player = RepositoryDtoFactory.CreatePlayer();

        Assert.Empty(player.PlayerAliases);
        Assert.Empty(player.PlayerIpAddresses);
        Assert.Empty(player.AdminActions);
        Assert.Empty(player.Reports);
        Assert.Empty(player.RelatedPlayers);
        Assert.Empty(player.ProtectedNames);
        Assert.Empty(player.Tags);
    }

    [Fact]
    public void CreateGameServer_WithDefaults_CreatesNonNullDto()
    {
        var server = RepositoryDtoFactory.CreateGameServer();

        Assert.NotEqual(Guid.Empty, server.GameServerId);
        Assert.Equal("Test Server", server.Title);
        Assert.Equal(GameType.CallOfDuty4, server.GameType);
        Assert.Equal("127.0.0.1", server.Hostname);
        Assert.Equal(28960, server.QueryPort);
    }

    [Fact]
    public void CreateAdminAction_WithDefaults_CreatesNonNullDto()
    {
        var action = RepositoryDtoFactory.CreateAdminAction();

        Assert.NotEqual(Guid.Empty, action.AdminActionId);
        Assert.NotEqual(Guid.Empty, action.PlayerId);
        Assert.Equal(AdminActionType.Warning, action.Type);
        Assert.Equal("Test admin action", action.Text);
    }

    [Fact]
    public void CreateReport_WithDefaults_CreatesNonNullDto()
    {
        var report = RepositoryDtoFactory.CreateReport();

        Assert.NotEqual(Guid.Empty, report.ReportId);
        Assert.NotEqual(Guid.Empty, report.PlayerId);
        Assert.Equal("Test report", report.Comments);
        Assert.False(report.Closed);
    }

    [Fact]
    public void CreateMap_WithDefaults_CreatesNonNullDto()
    {
        var map = RepositoryDtoFactory.CreateMap();

        Assert.NotEqual(Guid.Empty, map.MapId);
        Assert.Equal("mp_crash", map.MapName);
        Assert.Equal(GameType.CallOfDuty4, map.GameType);
        Assert.NotNull(map.MapFiles);
        Assert.Empty(map.MapFiles);
    }

    [Fact]
    public void CreateTag_WithDefaults_CreatesNonNullDto()
    {
        var tag = RepositoryDtoFactory.CreateTag();

        Assert.NotEqual(Guid.Empty, tag.TagId);
        Assert.Equal("TestTag", tag.Name);
        Assert.True(tag.UserDefined);
    }

    [Fact]
    public void CreateUserProfile_WithDefaults_CreatesNonNullDto()
    {
        var profile = RepositoryDtoFactory.CreateUserProfile();

        Assert.NotEqual(Guid.Empty, profile.UserProfileId);
        Assert.Equal("TestUser", profile.DisplayName);
        Assert.NotNull(profile.UserProfileClaims);
        Assert.Empty(profile.UserProfileClaims);
    }

    [Fact]
    public void CreateApiInfo_WithDefaults_CreatesNonNullDto()
    {
        var info = RepositoryDtoFactory.CreateApiInfo();

        Assert.Equal("1.0.0", info.Version);
        Assert.Equal("1.0.0.1", info.BuildVersion);
        Assert.Equal("1.0.0.0", info.AssemblyVersion);
    }

    [Fact]
    public void CreateBanFileMonitor_WithDefaults_CreatesNonNullDto()
    {
        var monitor = RepositoryDtoFactory.CreateBanFileMonitor();

        Assert.NotEqual(Guid.Empty, monitor.BanFileMonitorId);
        Assert.Equal("/path/to/banfile.txt", monitor.FilePath);
    }

    [Fact]
    public void CreateChatMessage_WithDefaults_CreatesNonNullDto()
    {
        var msg = RepositoryDtoFactory.CreateChatMessage();

        Assert.NotEqual(Guid.Empty, msg.ChatMessageId);
        Assert.Equal("Test message", msg.Message);
        Assert.Equal("TestPlayer", msg.Username);
    }

    [Fact]
    public void CreateDemo_WithDefaults_CreatesNonNullDto()
    {
        var demo = RepositoryDtoFactory.CreateDemo();

        Assert.NotEqual(Guid.Empty, demo.DemoId);
        Assert.Equal("Test Demo", demo.Title);
        Assert.Equal("test.dm_1", demo.FileName);
    }

    [Fact]
    public void CreateMapPack_WithDefaults_CreatesNonNullDto()
    {
        var pack = RepositoryDtoFactory.CreateMapPack();

        Assert.NotEqual(Guid.Empty, pack.MapPackId);
        Assert.Equal("Test Map Pack", pack.Title);
    }

    [Fact]
    public void CreateGameServerStat_WithDefaults_CreatesNonNullDto()
    {
        var stat = RepositoryDtoFactory.CreateGameServerStat();

        Assert.NotEqual(Guid.Empty, stat.GameServerStatId);
        Assert.Equal("mp_crash", stat.MapName);
    }

    [Fact]
    public void CreateGameServerEvent_WithDefaults_CreatesNonNullDto()
    {
        var evt = RepositoryDtoFactory.CreateGameServerEvent();

        Assert.NotEqual(Guid.Empty, evt.GameServerEventId);
        Assert.Equal("TestEvent", evt.EventType);
    }

    [Fact]
    public void CreateLivePlayer_WithDefaults_CreatesNonNullDto()
    {
        var lp = RepositoryDtoFactory.CreateLivePlayer();

        Assert.NotEqual(Guid.Empty, lp.LivePlayerId);
        Assert.Equal("TestPlayer", lp.Name);
        Assert.Equal(50, lp.Ping);
    }

    [Fact]
    public void CreateRecentPlayer_WithDefaults_CreatesNonNullDto()
    {
        var rp = RepositoryDtoFactory.CreateRecentPlayer();

        Assert.NotEqual(Guid.Empty, rp.RecentPlayerId);
        Assert.Equal("TestPlayer", rp.Name);
    }

    [Fact]
    public void CreatePlayerAlias_WithDefaults_CreatesNonNullDto()
    {
        var alias = RepositoryDtoFactory.CreatePlayerAlias();

        Assert.NotEqual(Guid.Empty, alias.PlayerAliasId);
        Assert.Equal("TestAlias", alias.Name);
        Assert.Equal(100, alias.ConfidenceScore);
    }

    [Fact]
    public void CreateProtectedName_WithDefaults_CreatesNonNullDto()
    {
        var pn = RepositoryDtoFactory.CreateProtectedName();

        Assert.NotEqual(Guid.Empty, pn.ProtectedNameId);
        Assert.Equal("ProtectedName", pn.Name);
    }

    [Fact]
    public void CreatePlayerTag_WithDefaults_CreatesNonNullDto()
    {
        var pt = RepositoryDtoFactory.CreatePlayerTag();

        Assert.NotEqual(Guid.Empty, pt.PlayerTagId);
    }

    [Fact]
    public void CreateUserProfileClaim_WithDefaults_CreatesNonNullDto()
    {
        var claim = RepositoryDtoFactory.CreateUserProfileClaim();

        Assert.NotEqual(Guid.Empty, claim.UserProfileClaimId);
        Assert.Equal("TestClaim", claim.ClaimType);
        Assert.Equal("TestValue", claim.ClaimValue);
    }

    [Fact]
    public void CreateGameTrackerBanner_WithDefaults_CreatesNonNullDto()
    {
        var banner = RepositoryDtoFactory.CreateGameTrackerBanner();

        Assert.Equal("https://example.com/banner.png", banner.BannerUrl);
    }

    [Fact]
    public void CreateIpAddress_WithDefaults_CreatesNonNullDto()
    {
        var ip = RepositoryDtoFactory.CreateIpAddress();

        Assert.Equal("192.168.1.1", ip.Address);
        Assert.Equal(100, ip.ConfidenceScore);
    }

    [Fact]
    public void CreateRelatedPlayer_WithDefaults_CreatesNonNullDto()
    {
        var rp = RepositoryDtoFactory.CreateRelatedPlayer();

        Assert.NotEqual(Guid.Empty, rp.PlayerId);
        Assert.Equal("RelatedPlayer", rp.Username);
    }

    [Fact]
    public void CreateMapVote_WithDefaults_CreatesNonNullDto()
    {
        var vote = RepositoryDtoFactory.CreateMapVote();

        Assert.NotEqual(Guid.Empty, vote.MapVoteId);
        Assert.True(vote.Like);
    }
}
