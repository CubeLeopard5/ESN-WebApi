USE [Esn-dev]
GO

/****** Object:  Table [dbo].[CalendarSubOrganizers]    Script Date: 24.12.2025 14:53:18 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[CalendarSubOrganizers](
	[CalendarId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[CalendarId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[CalendarSubOrganizers]  WITH CHECK ADD FOREIGN KEY([CalendarId])
REFERENCES [dbo].[Calendars] ([Id])
GO

ALTER TABLE [dbo].[CalendarSubOrganizers]  WITH CHECK ADD FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
GO


