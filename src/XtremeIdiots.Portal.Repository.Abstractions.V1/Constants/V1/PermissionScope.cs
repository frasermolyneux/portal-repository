namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1
{
    /// <summary>
    /// Defines the scope types that an additional permission can target.
    /// </summary>
    public enum PermissionScope
    {
        /// <summary>Permission value must be a GameType string.</summary>
        Game,
        /// <summary>Permission value must be a game server GUID.</summary>
        Server,
        /// <summary>Permission value can be either a GameType string or a game server GUID.</summary>
        GameOrServer
    }
}
