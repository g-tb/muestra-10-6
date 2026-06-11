SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

-- Rollback: 001_create_movie_manager_schema
-- Warning: this removes MovieManager tables and destroys stored data.

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_MovieActors_Actors_ActorId'
)
BEGIN
    ALTER TABLE dbo.MovieActors DROP CONSTRAINT FK_MovieActors_Actors_ActorId;
END;

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_MovieActors_Movies_MovieId'
)
BEGIN
    ALTER TABLE dbo.MovieActors DROP CONSTRAINT FK_MovieActors_Movies_MovieId;
END;

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Movies_Categories_CategoryId'
)
BEGIN
    ALTER TABLE dbo.Movies DROP CONSTRAINT FK_Movies_Categories_CategoryId;
END;

IF OBJECT_ID(N'dbo.MovieActors', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.MovieActors;
END;

IF OBJECT_ID(N'dbo.Movies', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Movies;
END;

IF OBJECT_ID(N'dbo.Actors', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Actors;
END;

IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Categories;
END;

COMMIT TRANSACTION;
GO
