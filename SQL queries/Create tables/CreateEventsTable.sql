USE [Esn-dev]
GO

/****** Object:  Table [dbo].[Events]    Script Date: 24.12.2025 14:54:12 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Events](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [varchar](255) NOT NULL,
	[Description] [text] NOT NULL,
	[Location] [varchar](255) NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NOT NULL,
	[MaxParticipants] [int] NOT NULL,
	[EventfrogLink] [varchar](max) NOT NULL,
	[UserId] [int] NOT NULL,
	[CreatedAt] [datetime] NULL,
	[SurveyJsData] [varchar](max) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[Events] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO

ALTER TABLE [dbo].[Events]  WITH CHECK ADD  CONSTRAINT [FK_Events_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO

ALTER TABLE [dbo].[Events] CHECK CONSTRAINT [FK_Events_Users]
GO


