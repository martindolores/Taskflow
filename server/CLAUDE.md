# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. It applies to the `server/` directory (the ASP.NET Core backend) and is loaded in addition to the root `CLAUDE.md`.

## Stack

ASP.NET Core 8 (`net8.0`) Web API, Clean Architecture, PostgreSQL via EF Core (`Npgsql` + `EFCore.NamingConventions` for snake_case columns), JWT bearer auth, minimal APIs (no MVC controllers). Full spec, data model, and endpoint contracts: `../docs/backend-plan.md`.

## Commands

Run from `server/`.

```bash
docker compose up -d                                                   # local Postgres — required before `dotnet run` or `dotnet test`
dotnet build                                                           # warnings are errors (Directory.Build.props: TreatWarningsAsErrors)
dotnet run --project src/TaskFlow.Api                                  # blocks; http://localhost:5151, Swagger at /swagger/index.html (dev only)
dotnet test                                                            # full suite: TaskFlow.UnitTests + TaskFlow.IntegrationTests
dotnet test --filter "FullyQualifiedName~ClassName"                    # one test class
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"         # one test
dotnet format                                                          # apply formatting
dotnet ef migrations add <Name> --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api
dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api
```

Agent-path shortcuts (see `.claude/skills/`): `run-taskflow-api/smoke.sh` does build → migrate → run → curl → teardown in one step; `query-taskflow-db/query.sh "<sql>"` runs ad-hoc SQL against the local Postgres (no host `psql` — it shells into the `postgres` container).

**Stopping a background `dotnet run`**: it forks a child process, so the shell's `$!` won't stop it reliably — kill by port instead: `lsof -ti:5151 -sTCP:LISTEN | xargs -r kill`.

## Key files

| What | Path |
|---|---|
| DI composition root (Infrastructure) | `src/TaskFlow.Infrastructure/DependencyInjection.cs` |
| DI/middleware composition root (Api) | `src/TaskFlow.Api/Program.cs` |
| EF Core `DbContext` | `src/TaskFlow.Infrastructure/Persistence/AppDbContext.cs` |
| Entity configurations | `src/TaskFlow.Infrastructure/Persistence/Configurations/*Configuration.cs` |
| Migrations | `src/TaskFlow.Infrastructure/Migrations/` |
| Domain entities | `src/TaskFlow.Domain/Entities/{User,Organization,TaskItem,TaskComment,Invitation,RefreshToken}.cs` |
| Endpoint groups | `src/TaskFlow.Api/Endpoints/{Auth,User,Organization,Task,TaskComment}Endpoints.cs` |
| Global exception → `ProblemDetails` mapping | `src/TaskFlow.Api/Middleware/GlobalExceptionHandler.cs` |
| Validation endpoint filter | `src/TaskFlow.Api/Filters/ValidationFilter.cs` |
| Per-feature DTOs/exceptions/validators | `src/TaskFlow.Application/<Feature>/{Dtos,Exceptions,Validators}/` |
| Config defaults | `src/TaskFlow.Api/appsettings.json`, `appsettings.Development.json` |
| Unit tests | `tests/TaskFlow.UnitTests/<Feature>/` |
| Integration tests | `tests/TaskFlow.IntegrationTests/` |
| Local Postgres | `docker-compose.yml` |

## Architecture

Four projects, strict dependency direction (`Api → Infrastructure → Application → Domain`):

- **`TaskFlow.Domain`** — entities (`User`, `Organization`, `TaskItem`, `TaskComment`, `Invitation`, `RefreshToken`) and enums. No dependencies, no EF Core attributes.
- **`TaskFlow.Application`** — interfaces implemented elsewhere (`IAuthService`, `IPasswordHasher`, `IJwtTokenService`, `ICurrentUserService`), request/response DTOs, FluentValidation validators, and feature-level exceptions (e.g. `InvalidCredentialsException`). Depends only on Domain.
- **`TaskFlow.Infrastructure`** — `AppDbContext` with one `IEntityTypeConfiguration<T>` per entity under `Persistence/Configurations/`, EF Core migrations, and the concrete implementations of Application interfaces that need EF Core or external libs (`AuthService`, `JwtTokenService`, `BCryptPasswordHasher`). `DependencyInjection.AddInfrastructure()` is the composition point.
- **`TaskFlow.Api`** — minimal-API endpoint groups under `Endpoints/` (e.g. `AuthEndpoints.MapAuthEndpoints`), `Program.cs` as the DI/middleware composition root, `Middleware/GlobalExceptionHandler` (maps exceptions → RFC 7807 `ProblemDetails`), `Filters/ValidationFilter<T>` (resolves `IValidator<T>` and short-circuits with 400 before the handler runs).

## Patterns to follow

- **New endpoint**: add DTOs + a validator + a service method in Application, implement it in Infrastructure, map it in `Endpoints/`, and add `.AddEndpointFilter<ValidationFilter<TRequest>>()`. Validators are auto-registered via `AddValidatorsFromAssemblyContaining<RegisterRequestValidator>()` in `Program.cs` — don't register them individually.
- **New domain exception**: put it in `TaskFlow.Application/<Feature>/Exceptions/`, then add a case to `GlobalExceptionHandler.MapException` — otherwise it falls through to a generic 500.
- **Enums**: stored as `varchar` in Postgres (`HasConversion<string>()` in the entity configuration) and serialized as strings over the wire (`JsonStringEnumConverter` registered globally in `Program.cs`) — update both when adding a new enum.
- **JWT claims** are the short names `sub` / `org` / `role`, not the long `ClaimTypes.*` URIs — `MapInboundClaims = false` in `Program.cs` is what keeps them that way. Read the current user via `ICurrentUserService`, not `HttpContext.User` directly.
- **Multi-tenancy**: every tenant-scoped table has `organization_id`, enforced per-request by a global EF Core query filter on `User`, `TaskItem`, `TaskComment`, and `Invitation` scoped to `ICurrentTenantService.OrganizationId` (see `AppDbContext.OnModelCreating`) — covered by `TenantIsolationTests`.
- **Config**: nested keys use double-underscore env var overrides (`ConnectionStrings__Default`, `Jwt__Secret`, `Cors__AllowedOrigins`); `appsettings.json` dev defaults already match `docker-compose.yml` credentials, so no local `.env` setup is needed.

## Tests

- `TaskFlow.UnitTests` — pure logic (validators, `BCryptPasswordHasher`, `JwtTokenService`), no DB.
- `TaskFlow.IntegrationTests` — `WebApplicationFactory<Program>` against the **real `docker-compose.yml` Postgres** (not an isolated per-run instance), so Docker must be running and the DB persists across test runs. Tests that insert data (e.g. registering a user) must use unique values to avoid unique-constraint collisions — see `AuthEndpointsTests.UniqueEmail()` for the pattern.
