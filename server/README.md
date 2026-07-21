# TaskFlow — Backend

ASP.NET Core Web API for TaskFlow. See [`../docs/backend-plan.md`](../docs/backend-plan.md) for the full technical spec and build plan.

## Solution layout

```
src/
  TaskFlow.Domain          # Entities, enums, no dependencies
  TaskFlow.Application      # DTOs, validators, service interfaces
  TaskFlow.Infrastructure    # EF Core DbContext, service implementations (auth, JWT, password hashing)
  TaskFlow.Api                # Minimal-API endpoint groups, middleware, DI composition root
tests/
  TaskFlow.UnitTests
  TaskFlow.IntegrationTests   # WebApplicationFactory against the docker-compose Postgres
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (for local Postgres)

## Database

Start a local Postgres instance:

```bash
docker compose up -d
```

The default connection string in `appsettings.json` (`Host=localhost;Port=5432;Database=taskflow;Username=taskflow;Password=taskflow`) matches the credentials in `docker-compose.yml`. In other environments, override it with the `ConnectionStrings__Default` environment variable.

### Migrations

Install the EF Core CLI tool once, if you don't already have it:

```bash
dotnet tool install --global dotnet-ef
```

Add a migration (run from `server/`):

```bash
dotnet ef migrations add <MigrationName> --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api
```

Apply migrations to the database:

```bash
dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api
```

## Running locally

```bash
dotnet build
dotnet run --project src/TaskFlow.Api
```

Serves the API at `http://localhost:5151`; Swagger UI is at `http://localhost:5151/swagger/index.html` in Development.

## Running tests

```bash
dotnet test
```

`TaskFlow.IntegrationTests` runs against the **same `docker-compose.yml` Postgres** used for local dev (not an isolated instance) — make sure `docker compose up -d` has been run first. Data persists across test runs, so tests that insert rows use unique values to avoid unique-constraint collisions.
