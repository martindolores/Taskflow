# TaskFlow — Backend

ASP.NET Core Web API for TaskFlow. See [`../docs/backend-plan.md`](../docs/backend-plan.md) for the full technical spec and build plan.

## Solution layout

```
src/
  TaskFlow.Domain          # Entities, enums, no dependencies
  TaskFlow.Application      # Services, validators, interfaces
  TaskFlow.Infrastructure    # EF Core DbContext, repositories, JWT implementation
  TaskFlow.Api                # Controllers, middleware, DI composition root
tests/
  TaskFlow.UnitTests
  TaskFlow.IntegrationTests   # WebApplicationFactory + Testcontainers Postgres
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Running locally

```bash
dotnet build
dotnet run --project src/TaskFlow.Api
```

## Running tests

```bash
dotnet test
```
