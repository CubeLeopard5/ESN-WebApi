USE [Esn-dev]
GO

/****** Object:  Table [dbo].[PropositionVotes]    Script Date: 24.12.2025 14:55:38 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PropositionVotes](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PropositionId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[VoteType] [int] NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[UpdatedAt] [datetime] NULL,
 CONSTRAINT [PK__PropositionVotes__3214EC07] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[PropositionVotes] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO

ALTER TABLE [dbo].[PropositionVotes]  WITH CHECK ADD  CONSTRAINT [FK_PropositionVotes_Propositions] FOREIGN KEY([PropositionId])
REFERENCES [dbo].[Propositions] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[PropositionVotes] CHECK CONSTRAINT [FK_PropositionVotes_Propositions]
GO

ALTER TABLE [dbo].[PropositionVotes]  WITH CHECK ADD  CONSTRAINT [FK_PropositionVotes_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[PropositionVotes] CHECK CONSTRAINT [FK_PropositionVotes_Users]
GO


