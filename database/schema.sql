-- =============================================================
--  Notes application schema (SQL Server)
--  Idempotent: safe to run repeatedly. Executed automatically on
--  API start-up by Data/DatabaseInitializer.cs.
-- =============================================================

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY
                                               CONSTRAINT DF_Users_Id DEFAULT NEWID(),
        Email        NVARCHAR(256)    NOT NULL,
        DisplayName  NVARCHAR(100)    NOT NULL,
        PasswordHash NVARCHAR(256)    NOT NULL,
        CreatedAt    DATETIME2(3)     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME()
    );

    -- Email is the login identifier, so it must be unique.
    CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email);
END;

IF OBJECT_ID('dbo.Notes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notes
    (
        Id        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Notes PRIMARY KEY
                                            CONSTRAINT DF_Notes_Id DEFAULT NEWID(),
        UserId    UNIQUEIDENTIFIER NOT NULL,
        Title     NVARCHAR(200)    NOT NULL,
        Content   NVARCHAR(MAX)    NULL,
        CreatedAt DATETIME2(3)     NOT NULL CONSTRAINT DF_Notes_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2(3)     NOT NULL CONSTRAINT DF_Notes_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Notes_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );

    -- Every list query is "my notes, newest first", so lead with UserId.
    CREATE INDEX IX_Notes_UserId_UpdatedAt ON dbo.Notes (UserId, UpdatedAt DESC);
    CREATE INDEX IX_Notes_UserId_CreatedAt ON dbo.Notes (UserId, CreatedAt DESC);
END;
