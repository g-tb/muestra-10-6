SELECT COUNT(*) AS MovieCount FROM dbo.Movies;
SELECT COUNT(*) AS ActorCount FROM dbo.Actors;
SELECT COUNT(*) AS CategoryCount FROM dbo.Categories;
SELECT COUNT(*) AS MovieActorCount FROM dbo.MovieActors;

SELECT Id, Title
FROM dbo.Movies
WHERE Title IS NULL OR LTRIM(RTRIM(Title)) = N'';

SELECT Title, COUNT(*) AS DuplicateCount
FROM dbo.Movies
GROUP BY Title
HAVING COUNT(*) > 1;

SELECT m.Id, m.CategoryId
FROM dbo.Movies m
LEFT JOIN dbo.Categories c ON c.Id = m.CategoryId
WHERE m.CategoryId IS NOT NULL
  AND c.Id IS NULL;

SELECT ma.MovieId, ma.ActorId
FROM dbo.MovieActors ma
LEFT JOIN dbo.Movies m ON m.Id = ma.MovieId
LEFT JOIN dbo.Actors a ON a.Id = ma.ActorId
WHERE m.Id IS NULL OR a.Id IS NULL;

SELECT name
FROM sys.tables
WHERE name IN (N'Movies', N'Actors', N'Categories', N'MovieActors');

SELECT name
FROM sys.indexes
WHERE name IN (N'UX_Movies_Title', N'IX_Movies_CategoryId', N'IX_MovieActors_ActorId');

SELECT name
FROM sys.foreign_keys
WHERE name IN (
    N'FK_Movies_Categories_CategoryId',
    N'FK_MovieActors_Movies_MovieId',
    N'FK_MovieActors_Actors_ActorId'
);
