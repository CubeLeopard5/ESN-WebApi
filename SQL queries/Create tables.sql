CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    CanCreateEvents BIT NOT NULL,
    CanModifyEvents BIT NOT NULL,
    CanDeleteEvents BIT NOT NULL,
    CanCreateUsers BIT NOT NULL,
    CanModifyUsers BIT NOT NULL,
    CanDeleteUsers BIT NOT NULL,
);


CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    BirthDate DATETIME NOT NULL,
    PhoneNumber VARCHAR(20),
    ESNCardNumber VARCHAR(50),
    UniversityName VARCHAR(255),
    StudentType VARCHAR(50) CHECK (StudentType IN ('exchange', 'local', 'esn_member')),
    TransportPass VARCHAR(100),
    RoleId INT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    LastLoginAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId)
        REFERENCES Roles(Id)
        ON DELETE SET NULL
);


CREATE TABLE Admins (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    LastLoginAt DATETIME DEFAULT GETDATE()
);


CREATE TABLE Events (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Description TEXT NOT NULL,
    Location VARCHAR(255) NOT NULL,
    StartDate DATETIME NOT NULL, -- Registration date
    EndDate DATETIME NOT NULL, -- Registration date
    MaxParticipants INT NOT NULL,
    EventfrogLink VARCHAR(MAX) NOT NULL,
    UserId INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    SurveyJsData VARCHAR(MAX) NOT NULL,
    CONSTRAINT FK_Events_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id)
);

CREATE TABLE EventTemplates (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Description TEXT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    SurveyJsData VARCHAR(MAX) NOT NULL
);


CREATE TABLE EventRegistrations (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    EventId INT NOT NULL,
    SurveyJsData VARCHAR(MAX) NOT NULL,
    RegisteredAt DATETIME DEFAULT GETDATE(),
    Status VARCHAR(20) NOT NULL DEFAULT 'registered' CHECK (Status IN ('registered', 'cancelled')),
    CONSTRAINT FK_Registrations_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_Registrations_Events FOREIGN KEY (EventId)
        REFERENCES Events(Id)
        ON DELETE CASCADE,
    CONSTRAINT UQ_Registration UNIQUE (UserId, EventId)
);


CREATE TABLE Propositions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    UserId INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    ModifiedAt DATETIME NULL,
    VotesUp INT NOT NULL DEFAULT 0,
    VotesDown INT NOT NULL DEFAULT 0,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME NULL,
    CONSTRAINT FK_Propositions_Users FOREIGN KEY (UserId)
        REFERENCES Users(Id)
);


CREATE TABLE Calendars (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    EventDate DATETIME NOT NULL DEFAULT GETDATE(),
    EventId INT NULL,
    MainOrganizerId INT NULL,
    EventManagerId INT NULL,
    ResponsableComId INT NULL,
    CONSTRAINT FK_Calendars_Events FOREIGN KEY (EventId)
        REFERENCES Events(Id),
    CONSTRAINT FK_Calendars_MainOrganizer FOREIGN KEY (MainOrganizerId)
        REFERENCES Users(Id),
    CONSTRAINT FK_Calendars_EventManager FOREIGN KEY (EventManagerId)
        REFERENCES Users(Id),
    CONSTRAINT FK_Calendars_ResponsableCom FOREIGN KEY (ResponsableComId)
        REFERENCES Users(Id)
);


CREATE TABLE CalendarSubOrganizers (
    CalendarId INT NOT NULL,
    UserId INT NOT NULL,
    PRIMARY KEY (CalendarId, UserId),
    FOREIGN KEY (CalendarId) REFERENCES Calendars(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);