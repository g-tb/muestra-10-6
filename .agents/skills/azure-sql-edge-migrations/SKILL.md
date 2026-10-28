---
name: azure-sql-edge-migrations
description: Plan, generate, review, and verify safe EF Core migrations for the MovieManager .NET 10 Blazor project running on Azure SQL Edge in Docker. Use when an agent needs to change the database schema, add seed or backfill data, review a generated migration, write validation queries, or adjust repository/service code for a schema change.
---

# Azure SQL Edge Migrations (EF Core)

## Purpose

Make safe, incremental schema changes for MovieManager. The schema is owned by
EF Core migrations in `Infrastructure/Migrations`, generated from
`MovieManagerDbContext.OnModelCreating`. The database is Azure SQL Edge started
with `docker compose up -d` (see `docker-compose.yml`).

Preserve the architecture: Web -> Application -> Domain, and
Infrastructure -> Application -> Domain. Do not couple Razor components or domain
entities to EF Core or SQL.

## Core Workflow

1. Inspect the requested change, `OnModelCreating`, and the latest migration and
   model snapshot.
2. Classify the change as additive, compatible, risky, or destructive.
3. Change the model configuration in `OnModelCreating` (explicit types, lengths,
   nullability, indexes, FKs, delete behavior, constraint names).
4. Generate the migration (migrations project `Infrastructure`, startup project
   `Web`):
   - Rider: right-click `Infrastructure` → Entity Framework Core → Add Migration.
   - CLI: `dotnet tool restore` then
     `dotnet ef migrations add <Name> --project Infrastructure --startup-project Web`
5. Review the generated `Up` and `Down` line by line (see Review Checklist).
6. Apply it: Rider → Update Database, or
   `dotnet ef database update --project Infrastructure --startup-project Web`.
7. Run the validation queries and the repository tests.
8. Warn explicitly about locks, breaking changes, missing indexes, FK risks, and
   large updates.

Never edit the model snapshot by hand. To undo an unapplied migration, use
Remove Migration (`dotnet ef migrations remove`). To undo an applied one, update the
database to the previous migration first.

## Safety Rules

- Prefer additive changes: new tables, nullable columns, new indexes, new
  constraints with pre-validation.
- Avoid destructive operations unless the user explicitly asks for them: dropping
  tables or columns, narrowing types, renaming without a compatibility bridge,
  deleting data, or making nullable columns non-null.
- EF Core may generate a rename as drop + add. If it does, replace that with
  `RenameColumn`/`RenameTable` in the migration.
- Put data backfills in their own migration using `migrationBuilder.Sql(...)`,
  separate from schema changes. Keep them idempotent and batched for large tables.
- Validate data before adding `NOT NULL`, `UNIQUE`, or `FOREIGN KEY` constraints.
- Add indexes for foreign keys and common lookup/filter columns.
- Keep seed data deterministic. Do not hide application behavior in seed data.
- Keep connection strings in configuration, user secrets, or environment
  variables, never in source code.

## Review Checklist for a Generated Migration

- `Up` contains only the intended changes; nothing unexpected is dropped.
- `Down` correctly reverses `Up`, or the migration documents why it cannot.
- Column types, max lengths, and nullability match the domain rules.
- Index and constraint names match the names configured in `OnModelCreating`.
- FK delete behavior (`SetNull`, `Cascade`, `Restrict`) is what the business rules need.
- New non-null columns on existing tables have a default or a backfill.

## Risk Warnings To Emit

- **Table locks**: column changes, index creation, constraint validation, or large
  updates can block reads/writes.
- **Breaking schema changes**: renamed/dropped columns, type narrowing, new
  non-null columns without defaults, or changed constraints.
- **Missing indexes**: foreign keys, unique lookups, joins, and frequently filtered
  columns need supporting indexes.
- **FK constraint risks**: orphaned rows, incorrect delete behavior, and missing
  parent rows must be checked before constraint creation.
- **Large data updates**: backfills should be batched and verified with counts.

## Expand / Backfill / Contract

Prefer this sequence over direct breaking changes, one migration each:

1. Expand: add the new nullable column or table.
2. Backfill: `migrationBuilder.Sql(...)` copying data, guarded so it can re-run.
3. Switch code to the new column.
4. Contract: drop the old column in a separate, explicitly destructive migration,
   only when requested.

Example backfill migration body:

```csharp
migrationBuilder.Sql("""
    UPDATE dbo.Movies
    SET DirectorName = Director
    WHERE DirectorName IS NULL
      AND Director IS NOT NULL;
    """);
```

## Validation Checklist

After applying a migration, check:

- Object existence: tables, columns, indexes, constraints, and the
  `__EFMigrationsHistory` row.
- Row counts before and after the migration.
- Required field violations: nulls, empty strings, invalid ranges.
- Relationship integrity: orphaned child rows.
- Uniqueness: duplicate keys before unique indexes/constraints.
- Application compatibility: run `dotnet test` with
  `MOVIEMANAGER_SQL_TEST_CONNECTION` set so the SQL integration tests run.

Common checks (run from Rider's Database tool window or any SQL client):

```sql
SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;

SELECT COUNT(*) FROM dbo.Movies;

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
```

## Repository and Service Guidance

When a schema change requires .NET changes:

- Keep Blazor pages dependent on services, not repositories or `DbContext`.
- Keep services responsible for business rules and DTO conversion.
- Keep EF mapping, queries, and transactions inside `Infrastructure`.
- Follow "Adding a New Entity" in AGENTS.md for where the repository goes.
- Avoid behavior that only works with in-memory lists, such as relying on object
  identity, mutable returned collections, or sequential IDs.
- Add tests for service behavior and repository persistence behavior.

## Agent Output Checklist

For every migration task, provide:

- The model change in `OnModelCreating`.
- The migration name and the commands (or Rider steps) to generate and apply it.
- A summary of the generated `Up`/`Down` and anything you changed by hand.
- Validation queries.
- Risk warnings.
- Notes about required repository/service changes.
