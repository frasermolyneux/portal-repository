CREATE TABLE [dbo].[PlayerSessionMapSegment]
(
    [SegmentId] UNIQUEIDENTIFIER NOT NULL,
    [SessionId] UNIQUEIDENTIFIER NOT NULL,
    [MapRunId] UNIQUEIDENTIFIER NULL,
    [StartUtc] DATETIME2 NOT NULL,
    [EndUtc] DATETIME2 NOT NULL,
    [UnknownMap] BIT NOT NULL CONSTRAINT [DF_PlayerSessionMapSegment_UnknownMap] DEFAULT (0),
    CONSTRAINT [PK_dbo.PlayerSessionMapSegment] PRIMARY KEY CLUSTERED ([SegmentId] ASC),
    CONSTRAINT [FK_PlayerSessionMapSegment_Session] FOREIGN KEY ([SessionId]) REFERENCES [dbo].[PlayerSession]([SessionId]),
    CONSTRAINT [FK_PlayerSessionMapSegment_MapRun] FOREIGN KEY ([MapRunId]) REFERENCES [dbo].[ServerMapRun]([RunId]),
    CONSTRAINT [CK_PlayerSessionMapSegment_Range] CHECK ([StartUtc] < [EndUtc]),
    CONSTRAINT [CK_PlayerSessionMapSegment_Map] CHECK (([UnknownMap] = 1 AND [MapRunId] IS NULL) OR ([UnknownMap] = 0 AND [MapRunId] IS NOT NULL))
);
GO

CREATE INDEX [IX_PlayerSessionMapSegment_Session_Time]
    ON [dbo].[PlayerSessionMapSegment]([SessionId], [StartUtc], [EndUtc]) INCLUDE ([MapRunId], [UnknownMap]);
GO

CREATE INDEX [IX_PlayerSessionMapSegment_Map_Time]
    ON [dbo].[PlayerSessionMapSegment]([MapRunId], [StartUtc], [EndUtc])
    WHERE [MapRunId] IS NOT NULL;
GO
