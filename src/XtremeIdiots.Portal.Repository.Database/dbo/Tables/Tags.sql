CREATE TABLE [dbo].[Tags]
(
  [TagId] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
  [Name] NVARCHAR(60) NOT NULL,
  [Description] NVARCHAR(255) NULL,
  [UserDefined] BIT NOT NULL DEFAULT 0,
  [TagHtml] NVARCHAR(255) NULL,
  CONSTRAINT [PK_dbo.Tags] PRIMARY KEY CLUSTERED ([TagId] ASC)
)
