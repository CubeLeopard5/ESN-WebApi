USE [Esn-dev]
GO

/****** Object:  Table [dbo].[Calendars]    Script Date: 24.12.2025 14:52:44 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Calendars](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](255) NOT NULL,
	[EventDate] [datetime] NOT NULL,
	[EventId] [int] NULL,
	[MainOrganizerId] [int] NULL,
	[EventManagerId] [int] NULL,
	[ResponsableComId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Calendars] ADD  DEFAULT (getdate()) FOR [EventDate]
GO

ALTER TABLE [dbo].[Calendars]  WITH CHECK ADD  CONSTRAINT [FK_Calendars_EventManager] FOREIGN KEY([EventManagerId])
REFERENCES [dbo].[Users] ([Id])
GO

ALTER TABLE [dbo].[Calendars] CHECK CONSTRAINT [FK_Calendars_EventManager]
GO

ALTER TABLE [dbo].[Calendars]  WITH CHECK ADD  CONSTRAINT [FK_Calendars_Events] FOREIGN KEY([EventId])
REFERENCES [dbo].[Events] ([Id])
GO

ALTER TABLE [dbo].[Calendars] CHECK CONSTRAINT [FK_Calendars_Events]
GO

ALTER TABLE [dbo].[Calendars]  WITH CHECK ADD  CONSTRAINT [FK_Calendars_MainOrganizer] FOREIGN KEY([MainOrganizerId])
REFERENCES [dbo].[Users] ([Id])
GO

ALTER TABLE [dbo].[Calendars] CHECK CONSTRAINT [FK_Calendars_MainOrganizer]
GO

ALTER TABLE [dbo].[Calendars]  WITH CHECK ADD  CONSTRAINT [FK_Calendars_ResponsableCom] FOREIGN KEY([ResponsableComId])
REFERENCES [dbo].[Users] ([Id])
GO

ALTER TABLE [dbo].[Calendars] CHECK CONSTRAINT [FK_Calendars_ResponsableCom]
GO


