# Copilot Instructions for MovieManager

## Build, Test, and Lint Commands

### Build
```bash
dotnet build
```

### Run All Tests
```bash
dotnet test
```

### Run Single Test Project
```bash
dotnet test Application.Tests/Application.Tests.csproj
```

### Run and Debug UI
```bash
dotnet run --project Web/Web.csproj
```
The Blazor UI will be available at `https://localhost:7211` or the URL shown by `dotnet run`.

## Architecture

MovieManager is a **.NET 10 Blazor Server** application organized in a Clean
Architecture style inspired by *Clean Architecture: The Missing Chapter*.

The solution has five projects:

1. **Domain** - Core business entities and domain exceptions
   - Contains `Entities/` and `Exceptions/`
   - References no other project
   - Does not contain repository, application service, UI, or database concerns

2. **Application** - Application services, DTOs, business rules, and abstractions
   - References `Domain`
   - Contains `Dtos/`, `Exceptions/`, `Interfaces/`, `Services/`, and `DependencyInjection.cs`
   - Repository interfaces live in `Application/Interfaces`
   - Service interfaces and implementations live in `Application/Services`

3. **Infrastructure** - Persistence implementations and infrastructure wiring
   - References `Application` and `Domain`
   - Repository implementations live in `Infrastructure/Persistence`
   - Current implementation uses in-memory repositories as a temporary persistence detail

4. **Web** - Blazor server-side web application
   - References `Application` and `Infrastructure`
   - Razor components live in `Pages/` and `Shared/`
   - `Program.cs` is the composition root

5. **Application.Tests** - Unit tests for application behavior
   - References `Application`, `Domain`, and test dependencies
   - Uses test doubles for repository interfaces instead of concrete infrastructure classes

Dependency direction must remain:

```text
Web -> Application -> Domain
Web -> Infrastructure -> Application -> Domain
```

`Domain` must not reference any other project. `Application` must not reference
`Infrastructure` or `Web`.

The repository pins the SDK with `global.json`. Use .NET SDK `10.0.300` or a
compatible newer .NET 10 feature band selected by the configured roll-forward
policy.

## Public and Internal Types

- Domain entities and domain exceptions are public.
- DTOs, application exceptions, repository interfaces, service interfaces, and DI
  extension classes are public.
- Concrete application service implementations are internal.
- Concrete repository implementations are internal.
- Blazor UI code must use application service interfaces and must not inject or
  call repositories directly.

## Dependency Injection

Application registrations belong in:

```csharp
Application.DependencyInjection.AddApplication(IServiceCollection services)
```

Infrastructure registrations belong in:

```csharp
Infrastructure.DependencyInjection.AddInfrastructure(IServiceCollection services)
```

`Web/Program.cs` should call those extension methods instead of registering
concrete services or repositories directly:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure();
```

## Key Conventions

### DTO Conversion Pattern
Always use explicit conversion methods rather than direct casting:
- DTOs have `toEntity()` method to convert to Domain objects
- DTOs have static `fromEntity()` method to convert from Domain objects

Example:
```csharp
Movie newMovie = movieDTO.toEntity();
MovieDTO result = MovieDTO.fromEntity(newMovie);
```

### Validation Strategy
- **Domain layer**: Validates entity state during property assignment
- **Application layer**: Validates business rules such as duplicates
- Throw `DomainException` for domain validation failures
- Throw `LogicException` for application/business rule failures

### Repository Boundary
- Repository interfaces belong in `Application/Interfaces`.
- Repository implementations belong in `Infrastructure/Persistence`.
- Services must depend on repository interfaces, not concrete memory or SQL classes.
- Keep query, mapping, and transaction logic inside repository/infrastructure code,
  not in Blazor pages.

### Nullable Reference Types
All projects have `<Nullable>enable</Nullable>`. Use nullable annotations (`?`) appropriately:
- Use `string?` for optional strings
- Use `int?` for optional integers
- Non-nullable types require values

### Testing Patterns
- Create fresh dependencies in `[TestInitialize]`
- Use repository-interface test doubles for application service tests
- Use `Assert.AreEqual()` for value comparisons
- Use `Assert.ThrowsException<ExceptionType>()` for expected failures
- Test both happy path and exception cases

## File Structure

```text
MovieManager/
├── Domain/
│   ├── Entities/
│   └── Exceptions/
├── Application/
│   ├── Dtos/
│   ├── Exceptions/
│   ├── Interfaces/
│   ├── Services/
│   └── DependencyInjection.cs
├── Infrastructure/
│   ├── Persistence/
│   └── DependencyInjection.cs
├── Web/
│   ├── Program.cs
│   ├── Pages/
│   └── Shared/
└── Application.Tests/
```

## Agent Reference Documents

Reference documents for agents, including reusable skills and supporting
guidance, are stored in the `.agents/` directory. Check that directory before
duplicating agent-facing instructions or creating new skill-specific guidance.

## Persistence: Azure SQL Edge

Persistence uses EF Core against Azure SQL Edge running in Docker. Code should be
compatible with durable, relational storage. Azure SQL Edge was retired on
September 30, 2025; it is used here for local practice only.

Local database:
```bash
docker compose up -d      # starts SQL Edge on localhost:1433, data in ./data (gitignored)
docker compose down       # stops it; data is kept
```
The development connection string lives in `Web/appsettings.Development.json` and
matches the `SA_PASSWORD` in `docker-compose.yml`.

Guidelines:
- Do not assume IDs are assigned before save, start at `1`, or are sequential
  without gaps.
- Do not depend on object reference equality after reads; SQL-backed repositories
  may materialize new instances.
- Do not return mutable internal collections from repositories.
- Keep business rules in `Application` and entity invariants in `Domain`.
- Prefer async repository APIs for new SQL-bound work when practical.
- Keep DTO/entity mapping explicit and centralized.

## EF Core Migrations

The schema is managed with EF Core migrations in `Infrastructure/Migrations`, generated
from `MovieManagerDbContext.OnModelCreating`. Do not write hand-made SQL schema scripts.

Creating and applying a migration (migrations project `Infrastructure`, startup
project `Web`):
- Rider: right-click `Infrastructure` → Entity Framework Core → Add Migration, then
  Update Database.
- CLI (uses the `dotnet-ef` version pinned in `dotnet-tools.json`):
  ```bash
  dotnet tool restore
  dotnet ef migrations add <Name> --project Infrastructure --startup-project Web
  dotnet ef database update --project Infrastructure --startup-project Web
  ```

Migration safety:
- Change the model in `OnModelCreating` first, then generate the migration. Never
  edit the model snapshot by hand.
- Review the generated `Up` and `Down` before applying it. Watch for dropped
  columns or tables and renames that EF generated as drop + add.
- Keep migrations small. Put data backfills in their own migration
  (`migrationBuilder.Sql(...)`), separate from schema changes.
- Use explicit column types, nullability, indexes, foreign keys, and constraints in
  the model configuration.
- Avoid destructive changes unless there is a clear migration path and tests.
- Seed only deterministic reference data; do not hide application behavior in seed
  data.
- Store connection strings in configuration/user secrets/environment variables,
  not in code.
- Validate migrations against a fresh database (`docker compose down`, delete
  `./data`, `docker compose up -d`, Update Database) before assuming they work.

Useful validation checks after a migration:
```sql
SELECT COUNT(*) FROM Movies;
SELECT COUNT(*) FROM Actors;
SELECT COUNT(*) FROM Categories;

SELECT m.Id, m.Title
FROM Movies m
WHERE m.Title IS NULL OR LTRIM(RTRIM(m.Title)) = '';

SELECT m.Id, m.CategoryId
FROM Movies m
LEFT JOIN Categories c ON c.Id = m.CategoryId
WHERE m.CategoryId IS NOT NULL AND c.Id IS NULL;
```

Adjust table/column names to the actual schema. Prefer targeted validation
queries that confirm row counts, required fields, relationships, uniqueness, and
rollback expectations.

## Maintainability Guidance for Agents

- Preserve the layered architecture and dependency direction.
- Keep Blazor components thin; they should coordinate UI state and call services.
- Do not introduce database-specific concepts into `Domain`.
- Add tests at the service layer for business rules and at the repository layer for
  SQL persistence behavior.
- When replacing memory repositories, keep behavior compatible from the service
  perspective unless the task explicitly changes behavior.
- Prefer clear names over matching current mistakes. For example, new actor or
  category methods should not copy the existing `AddMovie` naming pattern.
- Favor small, incremental migration steps over broad rewrites.

## Git Workflow

This project uses Gitflow.

- Before changing code, always check the current branch.
- Do not implement features directly on `develop`.
- Feature work must happen on a related branch whose name starts with `feat/`,
  for example `feat/sql-repository-abstractions`.
- If the current branch is `develop`, create or switch to an appropriate
  `feat/...` branch before editing.
- Keep commits focused and tied to the work performed.
- Commit after each agent run so the repository records the incremental work done
  by the agent.

## Common Tasks

### Adding a New Entity
1. Create entity class in `Domain/Entities/`
2. Add validation logic in property setters
3. Add repository interface in `Application/Interfaces/`
4. Add repository implementation in `Infrastructure/Persistence/`
5. Create DTO in `Application/Dtos/`
6. Create service interface and implementation in `Application/Services/`
7. Add DTO conversion methods (`toEntity()` and `fromEntity()`)
8. Register service and repository in the appropriate DI extension methods
9. Add unit tests in `Application.Tests/`

### Adding a New Service Method
1. Add method to the service interface and implementation in `Application/Services/`
2. Handle business logic validation and throw `LogicException` on violation
3. Use repository interfaces for data access
4. Return DTOs, not entities
5. Add corresponding unit tests

### Adding Unit Tests
- Test file should end with `Test.cs`
- Use `[TestInitialize]` to set up fresh dependencies
- Test both success and failure scenarios
- Use descriptive test names: `TestMethod_Scenario_ExpectedResult`
