MERGE INTO [dbo].[NotificationTypes] AS target
USING (VALUES
    ('weekly-admin-digest', 'Weekly Admin Summary', 'Weekly summary of admin activity, player statistics, and server health for your games.', 'Admin', 0, 1, '["Email"]', 1, 10),
    ('ban-expiring-soon', 'Ban Expiring Soon', 'Alert when a temporary ban is expiring within 24 hours.', 'Moderation', 1, 1, '["InSite","Email"]', 1, 20),
    ('new-player-report', 'New Player Report', 'A new player report has been submitted for a game you moderate.', 'Moderation', 1, 1, '["InSite","Email"]', 1, 30),
    ('unclaimed-ban', 'Unclaimed Ban', 'A ban has been imported that needs admin review.', 'Moderation', 1, 0, '["InSite"]', 1, 40)
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
