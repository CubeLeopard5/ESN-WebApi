USE [Esn-dev]
GO

/****** Object:  Table [dbo].[Users]    Script Date: 24.12.2025 14:50:30 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Users](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Email] [varchar](255) NOT NULL,
	[PasswordHash] [varchar](255) NOT NULL,
	[FirstName] [varchar](100) NOT NULL,
	[LastName] [varchar](100) NOT NULL,
	[BirthDate] [datetime] NOT NULL,
	[PhoneNumber] [varchar](20) NULL,
	[ESNCardNumber] [varchar](50) NULL,
	[UniversityName] [varchar](255) NULL,
	[StudentType] [varchar](50) NULL,
	[TransportPass] [varchar](100) NULL,
	[RoleId] [int] NULL,
	[CreatedAt] [datetime] NULL,
	[LastLoginAt] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[Email] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Users] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO

ALTER TABLE [dbo].[Users] ADD  DEFAULT (getdate()) FOR [LastLoginAt]
GO

ALTER TABLE [dbo].[Users]  WITH CHECK ADD  CONSTRAINT [FK_Users_Roles] FOREIGN KEY([RoleId])
REFERENCES [dbo].[Roles] ([Id])
ON DELETE SET NULL
GO

ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_Users_Roles]
GO

ALTER TABLE [dbo].[Users]  WITH CHECK ADD CHECK  (([StudentType]='esn_member' OR [StudentType]='local' OR [StudentType]='exchange'))
GO


