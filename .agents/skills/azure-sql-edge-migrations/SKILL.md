---
name: azure-sql-edge-migrations
description: Generate, review, and verify safe Azure SQL Edge migration scripts for the MovieManager .NET 6 Blazor project while it migrates from in-memory repositories to Dockerized Azure SQL Edge. Use when Codex or Copilot needs to create schema changes, seed data, rollback scripts, validation queries, sqlcmd commands, or data-access guidance for SQL-backed repository/service migration work.
---

# Azure SQL Edge Migrations

## Purpose

Create safe, incremental, production-oriented SQL migrations for MovieManager.
Assume the app is a .NET 6 Blazor Server project moving from in-memory
repositories to Azure SQL Edge running in Docker.

Preserve the architecture: UI -> BusinessLogic -> Repository ->
database/infrastructure. Do not couple Razor components or domain entities
directly to SQL.

## Core Workflow

1. Inspect the requested change and current schema/migration files.
2. Classify the change as additive, compatible, risky, or destructive.
3. Prefer an idempotent forward migration.
4. Generate a rollback script when practical.
5. Generate validation queries that prove the migration worked.
6. Warn explicitly about locks, breaking changes, missing indexes, FK risks, and
   large updates.
7. Suggest repository/service abstraction changes needed by the schema change.

## Safety Rules

- Prefer additive changes: new tables, nullable columns, new indexes, new
  constraints with pre-validation.
- Avoid destructive operations unless the user explicitly asks for them:
  `DROP TABLE`, `DROP COLUMN`, narrowing types, renaming columns without a
  compatibility bridge, deleting data, or making nullable columns non-null.
- Use idempotent guards with `IF NOT EXISTS`, `OBJECT_ID`, `COL_LENGTH`, and
  `sys.indexes`.
- Use explicit schemas, normally `dbo`.
- Use `XACT_ABORT ON` and transactions for bounded schema changes.
- Keep large data changes batched; do not run unbounded updates on large tables.
- Validate data before adding `NOT NULL`, `UNIQUE`, or `FOREIGN KEY`
  constraints.
- Add indexes for foreign keys and common lookup/filter columns.
- Keep seed scripts deterministic and idempotent.
- Keep SQL connection strings in configuration, user secrets, or environment
  variables, never in source code.

## Risk Warnings To Emit

When generating or reviewing a migration, call out these risks if present:

- **Table locks**: `ALTER TABLE`, index creation, constraint validation, or large
  updates can block reads/writes.
- **Breaking schema changes**: renamed/dropped columns, type narrowing, new
  non-null columns without defaults, or changed constraints.
- **Missing indexes**: foreign keys, unique lookups, joins, and frequently filtered
  columns need supporting indexes.
- **FK constraint risks**: orphaned rows, incorrect delete behavior, and missing
  parent rows must be checked before constraint creation.
- **Large data updates**: backfills should be batched and verified with counts.

## Migration Script Template

Use this structure unless the repo has an established migration format:

```sql
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

-- Migration: short-name-here
-- Purpose: explain the schema/data change
-- Compatibility: additive/backward-compatible unless noted

-- Idempotent schema/data changes go here.

COMMIT TRANSACTION;
GO

-- Validation queries go after the transaction.
```

For scripts that use variables or conditional batches, remember SQL Server batch
scope rules around `GO`.

## Examples

### Add a Table

```sql
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_Categories PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL
            CONSTRAINT DF_Categories_CreatedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Categories_Name'
      AND object_id = OBJECT_ID(N'dbo.Categories')
)
BEGIN
    CREATE UNIQUE INDEX UX_Categories_Name
    ON dbo.Categories(Name);
END;

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS CategoryCount FROM dbo.Categories;
SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Categories');
```

Rollback:

```sql
IF OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Categories;
END;
```

Only drop a newly created table when no production data depends on it.

### Modify a Column Safely

Prefer expand/backfill/contract instead of direct breaking changes.

```sql
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.Movies', N'DirectorName') IS NULL
BEGIN
    ALTER TABLE dbo.Movies ADD DirectorName NVARCHAR(200) NULL;
END;

UPDATE dbo.Movies
SET DirectorName = Director
WHERE DirectorName IS NULL
  AND Director IS NOT NULL;

COMMIT TRANSACTION;
GO

SELECT COUNT(*) AS MissingDirectorName
FROM dbo.Movies
WHERE Director IS NOT NULL AND DirectorName IS NULL;
```

Later, after code reads/writes `DirectorName`, remove the old column in a
separate explicitly destructive migration if requested.

### Seed Data

```sql
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

MERGE dbo.Categories AS target
USING (VALUES
    (N'Action'),
    (N'Drama'),
    (N'Comedy')
) AS source (Name)
ON target.Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Name) VALUES (source.Name);

COMMIT TRANSACTION;
GO

SELECT Name FROM dbo.Categories WHERE Name IN (N'Action', N'Drama', N'Comedy');
```

Avoid seed rows with environment-specific IDs unless the schema requires stable
reference identifiers.

### Create an Index

```sql
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
GO

SELECT name, is_unique
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.Movies')
  AND name = N'IX_Movies_CategoryId';
```

Warn that index creation can lock the table. For larger tables, recommend a
maintenance window or staged deployment plan.

### Add a Foreign Key

```sql
SELECT m.Id, m.CategoryId
FROM dbo.Movies m
LEFT JOIN dbo.Categories c ON c.Id = m.CategoryId
WHERE m.CategoryId IS NOT NULL
  AND c.Id IS NULL;
```

Only add the FK after the orphan query returns zero rows:

```sql
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Movies_Categories_CategoryId'
)
BEGIN
    ALTER TABLE dbo.Movies WITH CHECK
    ADD CONSTRAINT FK_Movies_Categories_CategoryId
    FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id);
END;
```

### Rollback Strategies

- Add table: drop only if it is new and safe to remove.
- Add nullable column: drop only if the app no longer needs it and no data must be
  preserved.
- Add index: drop the index.
- Add FK: drop the constraint.
- Data backfill: rollback only if previous values were captured, or provide a
  compensating script with clear limitations.

Example:

```sql
IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Movies_Categories_CategoryId'
)
BEGIN
    ALTER TABLE dbo.Movies
    DROP CONSTRAINT FK_Movies_Categories_CategoryId;
END;
```

## Validation Checklist

Include validation queries for:

- Object existence: tables, columns, indexes, constraints.
- Row counts before and after the migration.
- Required field violations: nulls, empty strings, invalid ranges.
- Relationship integrity: orphaned child rows.
- Uniqueness: duplicate keys before unique indexes/constraints.
- Application compatibility: queries expected by repository methods.

Common checks:

```sql
SELECT COUNT(*) FROM dbo.Movies;

SELECT Id, Title
FROM dbo.Movies
WHERE Title IS NULL OR LTRIM(RTRIM(Title)) = N'';

SELECT Title, COUNT(*) AS DuplicateCount
FROM dbo.Movies
GROUP BY Title
HAVING COUNT(*) > 1;
```

## Dockerized Azure SQL Edge Commands

Use `sqlcmd` from the host when installed:

```bash
sqlcmd -S localhost,1433 -U sa -P "$SA_PASSWORD" -d MovieManager -i migrations/001_add_categories.sql -b
```

Use `sqlcmd` inside the Azure SQL Edge container when the host does not have it:

```bash
docker exec -i azure-sql-edge /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -d MovieManager -b \
  -i /scripts/001_add_categories.sql
```

If the script is on the host and not mounted into the container:

```bash
docker exec -i azure-sql-edge /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "$SA_PASSWORD" -d MovieManager -b \
  < migrations/001_add_categories.sql
```

Use `-b` so CI or agent runs fail on SQL errors. Use environment variables or
secret stores for passwords.

## Repository and Service Guidance

When migrations require .NET changes:

- Add or use repository interfaces before introducing SQL implementations.
- Keep Blazor pages dependent on services, not repositories or SQL clients.
- Keep services responsible for business rules and DTO conversion.
- Keep SQL mapping, commands, and transactions inside repository/infrastructure
  classes.
- Avoid behavior that only works with in-memory lists, such as relying on object
  identity, mutable returned collections, or sequential IDs.
- Add tests for service behavior and repository persistence behavior.

## Agent Output Checklist

For every migration task, provide:

- Forward migration script.
- Rollback script or explicit explanation why rollback is not safe.
- Validation queries.
- Risk warnings.
- Recommended `sqlcmd` command.
- Notes about required repository/service changes.
