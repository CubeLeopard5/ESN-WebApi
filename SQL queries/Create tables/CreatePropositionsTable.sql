USE [Esn-dev]
GO

/****** Object:  Table [dbo].[Propositions]    Script Date: 24.12.2025 14:55:04 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Propositions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[UserId] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[ModifiedAt] [datetime] NULL,
	[VotesUp] [int] NOT NULL,
	[VotesDown] [int] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[DeletedAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Propositions] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO

ALTER TABLE [dbo].[Propositions] ADD  DEFAULT ((0)) FOR [VotesUp]
GO

ALTER TABLE [dbo].[Propositions] ADD  DEFAULT ((0)) FOR [VotesDown]
GO

ALTER TABLE [dbo].[Propositions] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO

ALTER TABLE [dbo].[Propositions]  WITH CHECK ADD  CONSTRAINT [FK_Propositions_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO

ALTER TABLE [dbo].[Propositions] CHECK CONSTRAINT [FK_Propositions_Users]
GO


