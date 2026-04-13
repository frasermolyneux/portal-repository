using System.Collections.Generic;
using System.Linq;

namespace XtremeIdiots.Portal.Repository.Abstractions.Constants.V1
{
    /// <summary>
    /// Defines all assignable additional permission claim types and their metadata.
    /// </summary>
    public static class AdditionalPermission
    {
        // Map Rotations
        public const string MapRotations_Read = "MapRotations.Read";
        public const string MapRotations_Write = "MapRotations.Write";
        public const string MapRotations_Deploy = "MapRotations.Deploy";

        // Maps
        public const string Maps_Read = "Maps.Read";

        // Game Servers — Core
        public const string GameServers_Read = "GameServers.Read";
        public const string GameServers_Write = "GameServers.Write";
        public const string GameServers_Delete = "GameServers.Delete";

        // Game Servers — Credentials
        public const string GameServers_Credentials_Ftp_Read = "GameServers.Credentials.Ftp.Read";
        public const string GameServers_Credentials_Ftp_Write = "GameServers.Credentials.Ftp.Write";
        public const string GameServers_Credentials_Rcon_Read = "GameServers.Credentials.Rcon.Read";
        public const string GameServers_Credentials_Rcon_Write = "GameServers.Credentials.Rcon.Write";

        // Game Servers — Maps
        public const string GameServers_Maps_Read = "GameServers.Maps.Read";
        public const string GameServers_Maps_Deploy = "GameServers.Maps.Deploy";

        // Game Servers — Ban File Monitors
        public const string GameServers_BanFileMonitors_Read = "GameServers.BanFileMonitors.Read";
        public const string GameServers_BanFileMonitors_Write = "GameServers.BanFileMonitors.Write";

        // Game Servers — Admin
        public const string GameServers_Admin_Read = "GameServers.Admin.Read";
        public const string GameServers_Admin_Rcon = "GameServers.Admin.Rcon";
        public const string GameServers_Admin_Rcon_Kick = "GameServers.Admin.Rcon.Kick";
        public const string GameServers_Admin_Rcon_Ban = "GameServers.Admin.Rcon.Ban";
        public const string GameServers_Admin_Rcon_Map = "GameServers.Admin.Rcon.Map";
        public const string GameServers_Admin_Rcon_Say = "GameServers.Admin.Rcon.Say";
        public const string GameServers_Admin_Rcon_Restart = "GameServers.Admin.Rcon.Restart";

        // Chat Log
        public const string ChatLog_Read = "ChatLog.Read";
        public const string ChatLog_ReadServer = "ChatLog.ReadServer";
        public const string ChatLog_Lock = "ChatLog.Lock";

        // Admin Actions
        public const string AdminActions_Read = "AdminActions.Read";
        public const string AdminActions_Create = "AdminActions.Create";
        public const string AdminActions_Edit = "AdminActions.Edit";
        public const string AdminActions_Delete = "AdminActions.Delete";
        public const string AdminActions_Claim = "AdminActions.Claim";
        public const string AdminActions_Lift = "AdminActions.Lift";
        public const string AdminActions_Reassign = "AdminActions.Reassign";
        public const string AdminActions_CreateTopic = "AdminActions.CreateTopic";

        // Players
        public const string Players_Read = "Players.Read";
        public const string Players_Delete = "Players.Delete";
        public const string Players_ProtectedNames_Write = "Players.ProtectedNames.Write";
        public const string Players_Tags_Write = "Players.Tags.Write";

        // Player Tags
        public const string Tags_Read = "Tags.Read";
        public const string Tags_Write = "Tags.Write";

        // Dashboard
        public const string Dashboard_Read = "Dashboard.Read";

        // Demos
        public const string Demos_Read = "Demos.Read";
        public const string Demos_Write = "Demos.Write";
        public const string Demos_Delete = "Demos.Delete";

        /// <summary>
        /// All additional permission definitions with metadata for UI and validation.
        /// </summary>
        public static IReadOnlyList<AdditionalPermissionDefinition> Definitions { get; } = new List<AdditionalPermissionDefinition>
        {
            // Map Rotations
            new(MapRotations_Read, "View Map Rotations", "View rotation list and details", "Map Rotations", null, PermissionScope.Game),
            new(MapRotations_Write, "Manage Map Rotations", "Create, edit, delete, clone, import rotations", "Map Rotations", null, PermissionScope.Game),
            new(MapRotations_Deploy, "Deploy Map Rotations", "Assign/unassign rotations to servers, sync, activate, deactivate", "Map Rotations", null, PermissionScope.GameOrServer),

            // Maps
            new(Maps_Read, "View Maps", "Browse maps catalogue, view images, vote log", "Maps", null, PermissionScope.Game),

            // Game Servers — Core
            new(GameServers_Read, "View Game Servers", "View and list game servers", "Game Servers", "Core", PermissionScope.GameOrServer),
            new(GameServers_Write, "Edit Game Servers", "Edit game server settings", "Game Servers", "Core", PermissionScope.GameOrServer),
            new(GameServers_Delete, "Delete Game Servers", "Delete game servers", "Game Servers", "Core", PermissionScope.Game),

            // Game Servers — Credentials
            new(GameServers_Credentials_Ftp_Read, "View FTP Credentials", "View FTP credentials for game servers", "Game Servers", "Credentials", PermissionScope.GameOrServer),
            new(GameServers_Credentials_Ftp_Write, "Edit FTP Configuration", "Edit FTP config and browse FTP on game servers", "Game Servers", "Credentials", PermissionScope.GameOrServer),
            new(GameServers_Credentials_Rcon_Read, "View RCON Credentials", "View RCON credentials for game servers", "Game Servers", "Credentials", PermissionScope.GameOrServer),
            new(GameServers_Credentials_Rcon_Write, "Edit RCON Configuration", "Edit RCON config on game servers", "Game Servers", "Credentials", PermissionScope.GameOrServer),

            // Game Servers — Maps
            new(GameServers_Maps_Read, "View Server Maps", "View Map Manager, server map files, rotation health", "Game Servers", "Maps", PermissionScope.GameOrServer),
            new(GameServers_Maps_Deploy, "Deploy Server Maps", "Push/delete map files on game servers", "Game Servers", "Maps", PermissionScope.GameOrServer),

            // Game Servers — Ban File Monitors
            new(GameServers_BanFileMonitors_Read, "View Ban File Monitors", "View ban file monitors", "Game Servers", "Ban File Monitors", PermissionScope.GameOrServer),
            new(GameServers_BanFileMonitors_Write, "Manage Ban File Monitors", "Create, edit, delete ban file monitors", "Game Servers", "Ban File Monitors", PermissionScope.GameOrServer),

            // Game Servers — Admin
            new(GameServers_Admin_Read, "Server Admin Dashboard", "Access server admin dashboard", "Game Servers", "Admin", PermissionScope.Game),
            new(GameServers_Admin_Rcon, "View RCON", "View RCON data, player list, server status, live chat", "Game Servers", "RCON", PermissionScope.Game),
            new(GameServers_Admin_Rcon_Kick, "RCON Kick Players", "Kick players via RCON", "Game Servers", "RCON", PermissionScope.Game),
            new(GameServers_Admin_Rcon_Ban, "RCON Ban Players", "TempBan and ban players via RCON", "Game Servers", "RCON", PermissionScope.Game),
            new(GameServers_Admin_Rcon_Map, "RCON Map Control", "Load, restart, and skip maps via RCON", "Game Servers", "RCON", PermissionScope.Game),
            new(GameServers_Admin_Rcon_Say, "RCON Say Command", "Send chat messages to players via RCON", "Game Servers", "RCON", PermissionScope.Game),
            new(GameServers_Admin_Rcon_Restart, "RCON Restart Server", "Restart game server via RCON", "Game Servers", "RCON", PermissionScope.Game),

            // Chat Log
            new(ChatLog_Read, "View Chat Logs", "View global and game chat logs", "Chat Log", null, PermissionScope.Game),
            new(ChatLog_ReadServer, "View Server Chat Log", "View server-specific chat log", "Chat Log", null, PermissionScope.Server),
            new(ChatLog_Lock, "Lock Chat Messages", "Lock and unlock chat messages", "Chat Log", null, PermissionScope.Game),

            // Admin Actions
            new(AdminActions_Read, "View Admin Actions", "View admin actions list", "Admin Actions", null, PermissionScope.Game),
            new(AdminActions_Create, "Create Admin Actions", "Create admin actions (bans, kicks, warnings)", "Admin Actions", null, PermissionScope.Game),
            new(AdminActions_Edit, "Edit Admin Actions", "Edit existing admin actions", "Admin Actions", null, PermissionScope.Game),
            new(AdminActions_Delete, "Delete Admin Actions", "Delete admin actions", "Admin Actions", null, PermissionScope.Game),
            new(AdminActions_Claim, "Claim Admin Actions", "Claim unclaimed admin actions", "Admin Actions", null, PermissionScope.Game),
            new(AdminActions_Lift, "Lift Admin Actions", "Lift active bans and actions", "Admin Actions", null, PermissionScope.Game),
            new(AdminActions_Reassign, "Reassign Admin Actions", "Change the admin assigned to an action", "Admin Actions", null, PermissionScope.Game),
            new(AdminActions_CreateTopic, "Create Admin Action Topics", "Force create forum topic for admin action", "Admin Actions", null, PermissionScope.Game),

            // Players
            new(Players_Read, "View Players", "View and list players, IP lookup, analytics", "Players", null, PermissionScope.Game),
            new(Players_Delete, "Delete Players", "Delete player records", "Players", null, PermissionScope.Game),
            new(Players_ProtectedNames_Write, "Manage Protected Names", "Create and delete protected player names", "Players", "Protected Names", PermissionScope.Game),
            new(Players_Tags_Write, "Assign Player Tags", "Assign and remove tags on players", "Players", "Tags", PermissionScope.Game),

            // Player Tags
            new(Tags_Read, "View Tags", "View player tag definitions", "Player Tags", null, PermissionScope.Game),
            new(Tags_Write, "Manage Tags", "Create, edit, and delete player tag definitions", "Player Tags", null, PermissionScope.Game),

            // Dashboard
            new(Dashboard_Read, "View Dashboard", "View admin dashboard", "Dashboard", null, PermissionScope.Game),

            // Demos
            new(Demos_Read, "View Demos", "Access demo recordings", "Demos", null, PermissionScope.Game),
            new(Demos_Write, "Manage Demos", "Create and edit demo recordings", "Demos", null, PermissionScope.Game),
            new(Demos_Delete, "Delete Demos", "Delete demo recordings", "Demos", null, PermissionScope.Game)
        };

        /// <summary>
        /// Set of all valid additional permission claim types for fast validation.
        /// </summary>
        public static HashSet<string> AllowedTypes { get; } = new HashSet<string>(Definitions.Select(d => d.ClaimType));

        /// <summary>
        /// Set of all valid system-generated claim types. These are created by the portal-sync
        /// service from forum group membership and identity data — not manually assigned.
        /// </summary>
        public static HashSet<string> SystemAllowedTypes { get; } = new HashSet<string>
        {
            UserProfileClaimType.UserProfileId,
            UserProfileClaimType.XtremeIdiotsId,
            UserProfileClaimType.PhotoUrl,
            UserProfileClaimType.SeniorAdmin,
            UserProfileClaimType.HeadAdmin,
            UserProfileClaimType.GameAdmin,
            UserProfileClaimType.Moderator,
            UserProfileClaimType.TimeZone,
            UserProfileClaimType.Email
        };

        /// <summary>
        /// Returns true if the given claim type is a valid additional permission.
        /// </summary>
        public static bool IsAllowed(string claimType) => AllowedTypes.Contains(claimType);

        /// <summary>
        /// Returns true if the given claim type is a valid system-generated claim.
        /// </summary>
        public static bool IsSystemAllowed(string claimType) => SystemAllowedTypes.Contains(claimType);

        /// <summary>
        /// Returns the definition for the given claim type, or null if not found.
        /// </summary>
        public static AdditionalPermissionDefinition? GetDefinition(string claimType) =>
            Definitions.FirstOrDefault(d => d.ClaimType == claimType);
    }
}
