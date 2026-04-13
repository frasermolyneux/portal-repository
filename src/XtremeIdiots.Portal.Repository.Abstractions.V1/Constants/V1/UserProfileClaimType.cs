using System;

namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1
{
    public static class UserProfileClaimType
    {
        public static string UserProfileId => nameof(UserProfileId);
        public static string XtremeIdiotsId => nameof(XtremeIdiotsId);
        public static string PhotoUrl => nameof(PhotoUrl);
        public static string Email => nameof(Email);
        public static string SeniorAdmin => nameof(SeniorAdmin);
        public static string HeadAdmin => nameof(HeadAdmin);
        public static string GameAdmin => nameof(GameAdmin);
        public static string Moderator => nameof(Moderator);

        [Obsolete("Use AdditionalPermission.GameServers_Credentials_Ftp_Read instead")]
        public static string FtpCredentials => nameof(FtpCredentials);

        [Obsolete("Use AdditionalPermission.GameServers_Credentials_Rcon_Read instead")]
        public static string RconCredentials => nameof(RconCredentials);

        [Obsolete("Use AdditionalPermission.GameServers_Read instead")]
        public static string GameServer => nameof(GameServer);

        [Obsolete("Use AdditionalPermission.GameServers_BanFileMonitors_Read instead")]
        public static string BanFileMonitor => nameof(BanFileMonitor);

        [Obsolete("Unused. Will be removed in a future version")]
        public static string FileMonitor => nameof(FileMonitor);

        [Obsolete("Unused. Will be removed in a future version")]
        public static string RconMonitor => nameof(RconMonitor);

        [Obsolete("Use AdditionalPermission.GameServers_Admin_Read instead")]
        public static string ServerAdmin => nameof(ServerAdmin);

        [Obsolete("Use AdditionalPermission.GameServers_Admin_Rcon instead")]
        public static string LiveRcon => nameof(LiveRcon);

        public static string TimeZone => nameof(TimeZone);
    }
}
