namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1
{
    public enum AdminActionFilter
    {
        /// <summary>
        /// Active permanent bans or currently active temporary bans.
        /// (Existing option - value preserved)
        /// </summary>
        ActiveBans = 0,

        /// <summary>
        /// Permanent bans that have not yet been associated with a user profile.
        /// (Existing option - value preserved)
        /// </summary>
        UnclaimedBans = 1,

        /// <summary>
        /// Observation / note entries only.
        /// </summary>
        Observations = 2,

        /// <summary>
        /// Warning entries only.
        /// </summary>
        Warnings = 3,

        /// <summary>
        /// Kick entries only.
        /// </summary>
        Kicks = 4,

        /// <summary>
        /// Temporary ban entries (any state).
        /// </summary>
        TempBans = 5,

        /// <summary>
        /// Permanent ban entries.
        /// </summary>
        PermanentBans = 6,

        /// <summary>
        /// All ban-related entries (temporary + permanent, any state).
        /// </summary>
        AllBans = 7,

        /// <summary>
        /// Disciplinary actions (warnings, kicks, temp & permanent bans).
        /// </summary>
        Disciplinary = 8,

        /// <summary>
        /// Non-ban actions (observations, warnings, kicks).
        /// </summary>
        NonBan = 9
    }
}
