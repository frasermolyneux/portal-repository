﻿CREATE TABLE [dbo].[ChatMessages]
(
    [ChatMessageId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [GameServerId] UNIQUEIDENTIFIER NOT NULL,
    [PlayerId] UNIQUEIDENTIFIER NOT NULL,
    [Username] NVARCHAR (MAX) NOT NULL,
    [ChatType] INT NOT NULL,
    [Message] NVARCHAR (MAX) NOT NULL,
    [Timestamp] DATETIME NOT NULL,
    [Locked] BIT DEFAULT 0 NOT NULL,
    CONSTRAINT [PK_dbo.ChatMessage] PRIMARY KEY CLUSTERED ([ChatMessageId] ASC),
    CONSTRAINT [FK_dbo.ChatMessages_dbo.GameServers_GameServerId] FOREIGN KEY ([GameServerId]) REFERENCES [dbo].[GameServers] ([GameServerId]),
    CONSTRAINT [FK_dbo.ChatMessages_dbo.Players_PlayerId] FOREIGN KEY ([PlayerId]) REFERENCES [dbo].[Players] ([PlayerId])
);

GO
CREATE NONCLUSTERED INDEX [IX_GameServerId]
    ON [dbo].[ChatMessages]([GameServerId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_PlayerId]
    ON [dbo].[ChatMessages]([PlayerId] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_Timestamp]
    ON [dbo].[ChatMessages]([Timestamp] ASC);

GO
CREATE NONCLUSTERED INDEX [IX_ChatMessages]
    ON [dbo].[ChatMessages] ([PlayerId], [GameServerId]) 
    INCLUDE ([ChatType], [Message], [Timestamp], [Username]);

GO
CREATE NONCLUSTERED INDEX [IX_ChatMessages_Timestamp_Locked]
    ON [dbo].[ChatMessages]([Timestamp] DESC, [Locked])
    INCLUDE ([Username], [Message], [ChatType]);

GO
CREATE NONCLUSTERED INDEX [IX_ChatMessages_GameServerId_Timestamp]
    ON [dbo].[ChatMessages]([GameServerId], [Timestamp] DESC)
    INCLUDE ([Username], [Message], [ChatType]);

