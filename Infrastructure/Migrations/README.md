# Azure SQL Edge local database

Azure SQL Edge is retired as of September 30, 2025. This setup is intended for
local migration practice for MovieManager.

Build the local database image:

```bash
docker build -t moviemanager-sql-edge -f docker/azure-sql-edge/Dockerfile .
```

Run Azure SQL Edge:

```bash
docker run --cap-add SYS_PTRACE -e ACCEPT_EULA=1 -e MSSQL_SA_PASSWORD=<strong-password> -p 1433:1433 --name moviemanager-sql-edge -d moviemanager-sql-edge
```

Create the database:

```bash
sqlcmd -S localhost,1433 -U sa -P "<strong-password>" -Q "IF DB_ID(N'MovieManager') IS NULL CREATE DATABASE MovieManager;" -b
```

Apply and validate the schema:

```bash
sqlcmd -S localhost,1433 -U sa -P "<strong-password>" -d MovieManager -i Infrastructure/Migrations/001_create_movie_manager_schema.sql -b
sqlcmd -S localhost,1433 -U sa -P "<strong-password>" -d MovieManager -i Infrastructure/Migrations/001_validate_movie_manager_schema.sql -b
```

Configure the web app with an environment variable or user secret:

```bash
ConnectionStrings__MovieManagerDatabase="Server=localhost,1433;Database=MovieManager;User Id=sa;Password=<strong-password>;TrustServerCertificate=True;Encrypt=True"
```
