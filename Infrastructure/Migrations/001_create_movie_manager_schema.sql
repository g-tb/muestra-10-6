SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

-- Migration: 001_create_movie_manager_schema
-- Purpose: create the MovieManager durable persistence schema.
-- Compatibility: additive for a fresh database.

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Categories PRIMARY KEY,
        Name NVARCHAR(200) NULL
    );
END;

IF OBJECT_ID(N'dbo.Actors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Actors
    (
        Id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Actors PRIMARY KEY,
        Name NVARCHAR(200) NULL,
        Bio NVARCHAR(MAX) NULL,
        BirthDate DATETIME2(7) NULL
    );
END;

IF OBJECT_ID(N'dbo.Movies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Movies
    (
        Id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Movies PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Director NVARCHAR(200) NULL,
        ReleaseYear INT NULL,
        CategoryId INT NULL
    );
END;

IF OBJECT_ID(N'dbo.MovieActors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MovieActors
    (
        MovieId INT NOT NULL,
        ActorId INT NOT NULL,
        CONSTRAINT PK_MovieActors PRIMARY KEY (MovieId, ActorId)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Movies_Title'
      AND object_id = OBJECT_ID(N'dbo.Movies')
)
BEGIN
    CREATE UNIQUE INDEX UX_Movies_Title
    ON dbo.Movies(Title);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Movies_CategoryId'
      AND object_id = OBJECT_ID(N'dbo.Movies')
)
BEGIN
    CREATE INDEX IX_Movies_CategoryId
    ON dbo.Movies(CategoryId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_MovieActors_ActorId'
      AND object_id = OBJECT_ID(N'dbo.MovieActors')
)
BEGIN
    CREATE INDEX IX_MovieActors_ActorId
    ON dbo.MovieActors(ActorId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Movies_Categories_CategoryId'
)
BEGIN
    ALTER TABLE dbo.Movies WITH CHECK
    ADD CONSTRAINT FK_Movies_Categories_CategoryId
    FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id)
    ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_MovieActors_Movies_MovieId'
)
BEGIN
    ALTER TABLE dbo.MovieActors WITH CHECK
    ADD CONSTRAINT FK_MovieActors_Movies_MovieId
    FOREIGN KEY (MovieId) REFERENCES dbo.Movies(Id)
    ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_MovieActors_Actors_ActorId'
)
BEGIN
    ALTER TABLE dbo.MovieActors WITH CHECK
    ADD CONSTRAINT FK_MovieActors_Actors_ActorId
    FOREIGN KEY (ActorId) REFERENCES dbo.Actors(Id)
    ON DELETE CASCADE;
END;

COMMIT TRANSACTION;
GO
