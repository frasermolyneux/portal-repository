MERGE INTO [dbo].[NotificationTypes] AS target
USING (VALUES
    ('weekly-admin-digest', 'Weekly Admin Summary', 'Weekly summary of admin activity, player statistics, and server health for your games.', 'Admin', 0, 1, '["Email"]', 1, 10),
    ('ban-expiring-soon', 'Ban Expiring Soon', 'Alert when a temporary ban is expiring within 24 hours.', 'Moderation', 1, 1, '["InSite","Email"]', 1, 20),
    ('new-player-report', 'New Player Report', 'A new player report has been submitted for a game you moderate.', 'Moderation', 1, 1, '["InSite","Email"]', 1, 30),
    ('unclaimed-ban', 'Unclaimed Ban', 'A ban has been imported that needs admin review.', 'Moderation', 1, 0, '["InSite"]', 1, 40),
    ('admin-action-new', 'New Admin Action', 'A new admin action (ban, kick, warning) has been created on a game you moderate.', 'Admin', 1, 1, '["InSite","Email"]', 1, 50),
    ('admin-action-claimed', 'Admin Action Claimed', 'An unclaimed admin action has been claimed by an admin on a game you moderate.', 'Admin', 1, 0, '["InSite"]', 1, 60),
    ('admin-action-lifted', 'Admin Action Lifted', 'An admin action has been lifted on a game you moderate.', 'Admin', 1, 1, '["InSite","Email"]', 1, 70),
    ('unclaimed-action-reminder', 'Unclaimed Action Reminder', 'A reminder that there are unclaimed admin actions that need review.', 'Admin', 1, 1, '["InSite","Email"]', 1, 80),
    ('server-offline', 'Server Offline', 'A game server you manage has gone offline.', 'Server', 1, 1, '["InSite","Email"]', 1, 90),
    ('server-online', 'Server Online', 'A game server you manage has come back online.', 'Server', 1, 0, '["InSite"]', 1, 100),
    ('player-followed-connected', 'Followed Player Connected', 'A player you follow has connected to a game server.', 'Player', 1, 0, '["InSite"]', 1, 110),
    ('player-followed-admin-action', 'Admin Action on Followed Player', 'An admin action has been taken against a player you follow.', 'Player', 1, 1, '["InSite","Email"]', 1, 120)
) AS source ([NotificationTypeId], [DisplayName], [Description], [Category], [SupportsInSite], [SupportsEmail], [DefaultChannels], [IsEnabled], [SortOrder])
ON target.[NotificationTypeId] = source.[NotificationTypeId]
WHEN MATCHED THEN
    UPDATE SET
        [DisplayName] = source.[DisplayName],
        [Description] = source.[Description],
        [Category] = source.[Category],
        [SupportsInSite] = source.[SupportsInSite],
        [SupportsEmail] = source.[SupportsEmail],
        [DefaultChannels] = source.[DefaultChannels],
        [IsEnabled] = source.[IsEnabled],
        [SortOrder] = source.[SortOrder]
WHEN NOT MATCHED THEN
    INSERT ([NotificationTypeId], [DisplayName], [Description], [Category], [SupportsInSite], [SupportsEmail], [DefaultChannels], [IsEnabled], [SortOrder])
    VALUES (source.[NotificationTypeId], source.[DisplayName], source.[Description], source.[Category], source.[SupportsInSite], source.[SupportsEmail], source.[DefaultChannels], source.[IsEnabled], source.[SortOrder]);
