namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1
{
    /// <summary>
    /// Filter options for querying user profiles by claim-based role groupings.
    /// </summary>
    public enum UserProfileFilter
    {
        /// <summary>
        /// User profiles that have a SeniorAdmin claim.
        /// </summary>
        SeniorAdmins = 0,

        /// <summary>
        /// User profiles that have a HeadAdmin claim.
        /// </summary>
        HeadAdmins = 1,

        /// <summary>
        /// User profiles that have a GameAdmin claim.
        /// </summary>
        GameAdmins = 2,

        /// <summary>
        /// User profiles that have a Moderator claim.
        /// </summary>
        Moderators = 3,

        /// <summary>
        /// User profiles that have any administrative claim (SeniorAdmin, HeadAdmin, GameAdmin, Moderator).
        /// </summary>
        AnyAdmin = 4,

        /// <summary>
        /// User profiles that have any custom permission claim (FtpCredentials, RconCredentials, GameServer, BanFileMonitor, RconMonitor, ServerAdmin, LiveRcon).
        /// </summary>
        HasCustomPermissions = 5
    }
}
