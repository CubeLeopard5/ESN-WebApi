USE [Esn-dev]
GO

/****** Object:  Table [dbo].[EventRegistrations]    Script Date: 24.12.2025 14:53:50 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[EventRegistrations](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[EventId] [int] NOT NULL,
	[SurveyJsData] [varchar](max) NOT NULL,
	[RegisteredAt] [datetime] NULL,
	[Status] [varchar](20) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Registration] UNIQUE NONCLUSTERED 
(
	[UserId] ASC,
	[EventId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[EventRegistrations] ADD  DEFAULT (getdate()) FOR [RegisteredAt]
GO

ALTER TABLE [dbo].[EventRegistrations] ADD  DEFAULT ('registered') FOR [Status]
GO

ALTER TABLE [dbo].[EventRegistrations]  WITH CHECK ADD  CONSTRAINT [FK_Registrations_Events] FOREIGN KEY([EventId])
REFERENCES [dbo].[Events] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[EventRegistrations] CHECK CONSTRAINT [FK_Registrations_Events]
GO

ALTER TABLE [dbo].[EventRegistrations]  WITH CHECK ADD  CONSTRAINT [FK_Registrations_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[EventRegistrations] CHECK CONSTRAINT [FK_Registrations_Users]
GO

ALTER TABLE [dbo].[EventRegistrations]  WITH CHECK ADD CHECK  (([Status]='cancelled' OR [Status]='registered'))
GO


