# Taskflow — Backend Technical Spec & Build Plan

ASP.NET Core Web API, Clean Architecture, PostgreSQL, JWT auth, deployed to Render.

Companion document: [`frontend-plan.md`](./frontend-plan.md) — depends on the endpoints defined here. Backend chunks are ordered so the frontend never has to wait on an endpoint it needs before the corresponding backend PR ships.

---

## 1. Data Model

All tables use `uuid` primary keys (`gen_random_uuid()`), `timestamptz` for timestamps (UTC), and soft multi-tenancy via an `organization_id` foreign key on every tenant-scoped table.

### `organizations`

| Column       | Type          | Constraints              | Notes |
|--------------|---------------|---------------------------|-------|
| id           | uuid          | PK                         | |
| name         | varchar(200)  | not null                   | |
| slug         | varchar(100)  | unique, not null           | URL-safe identifier, generated from name at creation |
| created_at   | timestamptz   | not null, default now()    | |
| updated_at   | timestamptz   | not null, default now()    | |

**Notes:** Root tenant entity. Every other business table (except `refresh_tokens`, which hangs off `users`) carries `organization_id` and is filtered by it.

### `users`

| Column          | Type         | Constraints                                  | Notes |
|-----------------|--------------|-----------------------------------------------|-------|
| id              | uuid         | PK                                             | |
| organization_id | uuid         | FK → organizations.id, not null, indexed       | |
| email           | varchar(320) | unique (global), not null                     | Login identifier. Global uniqueness keeps auth simple — this demo does not support one user belonging to multiple orgs. |
| password_hash   | varchar(200) | not null                                       | BCrypt hash |
| first_name      | varchar(100) | not null                                       | |
| last_name       | varchar(100) | not null                                       | |
| role            | enum         | not null — `Admin` \| `Member`                 | |
| status          | enum         | not null — `Invited` \| `Active` \| `Deactivated` | `Invited` rows are created on invitation acceptance in `Active` state; `Invited` status is really only transient/unused if invitations stay in their own table — kept for symmetry and future direct-provisioning flows |
| created_at      | timestamptz  | not null, default now()                        | |
| updated_at      | timestamptz  | not null, default now()                        | |

**Notes:** Tenant isolation is enforced with an EF Core **global query filter** (`HasQueryFilter(u => u.OrganizationId == _currentTenant.OrganizationId)`) applied to this and every other tenant-scoped entity. The current tenant is resolved once per request from the JWT `org` claim — never from client-supplied input — by a scoped `ICurrentTenantService`.

### `tasks`

| Column          | Type         | Constraints                                    | Notes |
|-----------------|--------------|--------------------------------------------------|-------|
| id              | uuid         | PK                                                | |
| organization_id | uuid         | FK → organizations.id, not null, indexed          | |
| title           | varchar(200) | not null                                          | |
| description     | text         | nullable                                          | |
| status          | enum         | not null, default `ToDo` — `ToDo` \| `InProgress` \| `Done` | |
| priority        | enum         | not null, default `Medium` — `Low` \| `Medium` \| `High` | |
| assignee_id     | uuid         | FK → users.id, nullable                           | Must belong to the same organization; enforced in the application layer, not the DB, since cross-tenant FK checks aren't expressible in a simple constraint |
| due_date        | date         | nullable                                          | |
| created_by_id   | uuid         | FK → users.id, not null                           | |
| created_at      | timestamptz  | not null, default now()                           | |
| updated_at      | timestamptz  | not null, default now()                           | |

**Indexes:** `(organization_id, status)`, `(organization_id, assignee_id)` — support the list/filter queries the UI needs.

### `task_comments`

| Column          | Type   | Constraints                                     | Notes |
|-----------------|--------|----------------------------------------------------|-------|
| id              | uuid   | PK                                                   | |
| task_id         | uuid   | FK → tasks.id, not null, on delete cascade           | |
| organization_id | uuid   | FK → organizations.id, not null, indexed             | Denormalized from the parent task so the global tenant query filter can apply directly to this table without a join. The application layer must verify `task.organization_id == currentTenant` before writing, so this column never drifts from its parent. |
| author_id       | uuid   | FK → users.id, not null                              | |
| body            | text   | not null                                             | |
| created_at      | timestamptz | not null, default now()                         | |

### `invitations`

| Column          | Type         | Constraints                                              | Notes |
|-----------------|--------------|-------------------------------------------------------------|-------|
| id              | uuid         | PK                                                            | |
| organization_id | uuid         | FK → organizations.id, not null, indexed                     | |
| email           | varchar(320) | not null                                                      | |
| role            | enum         | not null — `Admin` \| `Member`                                | Role the invitee will get on acceptance |
| token           | varchar(100) | unique, not null                                              | Opaque random token, emailed as a link (or, for the demo, surfaced directly in the API response / admin UI, since no email provider is wired up) |
| status          | enum         | not null, default `Pending` — `Pending` \| `Accepted` \| `Revoked` \| `Expired` | |
| invited_by_id   | uuid         | FK → users.id, not null                                       | |
| expires_at      | timestamptz  | not null                                                      | 7 days from creation |
| created_at      | timestamptz  | not null, default now()                                       | |
| updated_at      | timestamptz  | not null, default now()                                       | |

**Notes:** Kept separate from `users` because an invitee has no account yet. Accepting an invitation creates the `users` row and flips `invitations.status` to `Accepted` in one transaction.

### `refresh_tokens`

| Column      | Type         | Constraints                       | Notes |
|-------------|--------------|-------------------------------------|-------|
| id          | uuid         | PK                                   | |
| user_id     | uuid         | FK → users.id, not null, indexed     | |
| token_hash  | varchar(200) | not null                             | Never store the raw token, only its hash |
| expires_at  | timestamptz  | not null                             | |
| revoked_at  | timestamptz  | nullable                             | Set on logout or refresh rotation |
| created_at  | timestamptz  | not null, default now()              | |

**Notes:** Not tenant-scoped (no `organization_id`) — it's a pure auth artifact tied to a user. Access tokens are short-lived (15 min); refresh tokens are long-lived (7–30 days) and rotated on every use.

---

## 2. Architecture Overview

Clean Architecture, four projects:

```
src/
  TaskFlow.Domain         # Entities, enums, no dependencies
  TaskFlow.Application     # Services, validators, interfaces (ICurrentTenantService, IJwtTokenService, ITaskService, ...)
  TaskFlow.Infrastructure   # EF Core DbContext, repositories, JWT implementation, password hashing
  TaskFlow.Api              # Controllers, middleware, DI composition root
tests/
  TaskFlow.UnitTests
  TaskFlow.IntegrationTests  # WebApplicationFactory + Testcontainers Postgres
```

Every write and read (other than auth) flows through an Application-layer service (interface in `TaskFlow.Application`, implementation alongside it or in `TaskFlow.Infrastructure` where it needs EF Core). Controllers stay thin — parse request, call the service, map the result. FluentValidation validators run through a small ASP.NET Core endpoint filter that resolves `IValidator<TRequest>` and short-circuits with a `400 ProblemDetails` before the controller action runs, so validation stays consistent without needing a mediator pipeline.

---

## 3. Backend Build Plan

Each chunk is one PR. Chunks are strictly ordered — later chunks depend on earlier ones.

**Status: PR-B0 through PR-B12 are all shipped.** The core backend is complete and deployed (see `docs/deployment-plan.md`). Only the [Nice to Have](#6-nice-to-have) items remain unbuilt.

### PR-B0 — Repo & solution scaffolding ✅

No endpoints. Sets up the skeleton everything else builds on.

- `TaskFlow.sln` with the four `src/` projects and two `tests/` projects wired up
- `.editorconfig`, `.gitignore`, `Directory.Build.props` (nullable enabled, warnings-as-errors)
- Empty `Program.cs` that boots and returns 200 on `/`
- `README.md` stub

### PR-B1 — Web API bootstrap & cross-cutting concerns ✅

- Serilog console logging (structured, request logging middleware)
- Global exception-handling middleware → RFC 7807 `ProblemDetails` JSON responses
- Swagger/OpenAPI (`Microsoft.AspNetCore.OpenApi` + Swashbuckle), enabled in dev only
- `appsettings.json` / `appsettings.Development.json` scaffolding
- CORS policy registered (origins read from config, locked down later once frontend URL is known)

**Endpoints**
| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/health` | — | `200 { "status": "healthy" }` |

### PR-B2 — Database & EF Core setup ✅

- `Npgsql.EntityFrameworkCore.PostgreSQL` + `AppDbContext` (no entities yet)
- Connection string from `ConnectionStrings__Default` env var
- `docker-compose.yml` with a local Postgres for development
- EF Core migrations tooling documented in README (`dotnet ef migrations add`, `dotnet ef database update`)

No endpoints.

### PR-B3 — Domain entities & initial migration ✅

- Entities: `Organization`, `User`, `Task`, `TaskComment`, `Invitation`, `RefreshToken` (per the data model above), each with an `IEntityTypeConfiguration<T>` in Infrastructure
- Enums: `UserRole`, `UserStatus`, `TaskStatus`, `TaskPriority`, `InvitationStatus`, stored as Postgres `varchar` via EF Core value conversion (readable in the DB, no enum-migration pain)
- Indexes as specified in the data model
- `InitialCreate` migration

No endpoints.

### PR-B4 — Auth: registration, login, JWT issuance ✅

- `IPasswordHasher` (BCrypt), `IJwtTokenService` (issues access + refresh tokens), `ICurrentUserService`
- `AddAuthentication().AddJwtBearer(...)` wired in the API; claims: `sub` (user id), `org` (organization id), `role`
- `IAuthService` with `RegisterOrganizationAsync`, `LoginAsync`, `RefreshTokenAsync`, `LogoutAsync`, each with a matching FluentValidation validator for its request DTO

**Endpoints**
| Method | Route | Request body | Response |
|--------|-------|---------------|----------|
| POST | `/api/auth/register` | `{ organizationName, email, password, firstName, lastName }` | `201 { userId, organizationId, accessToken, refreshToken }` — creates the org and its first `Admin` user |
| POST | `/api/auth/login` | `{ email, password }` | `200 { accessToken, refreshToken, user: { id, email, firstName, lastName, role, organizationId } }` |
| POST | `/api/auth/refresh` | `{ refreshToken }` | `200 { accessToken, refreshToken }` — rotates the refresh token |
| POST | `/api/auth/logout` | `{ refreshToken }` | `204` — revokes the refresh token |

### PR-B5 — Tenant isolation & authorization infrastructure ✅

- `ICurrentTenantService` populated from JWT claims per-request (scoped DI)
- EF Core global query filters on `User`, `Task`, `TaskComment`, `Invitation` scoped to `CurrentTenant.OrganizationId`
- `AdminOnly` authorization policy from the `role` claim
- First protected endpoint, proving the whole chain (JWT → claims → tenant filter) works end to end

**Endpoints**
| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/users/me` | — (JWT required) | `200 { id, email, firstName, lastName, role, organizationId, organizationName }` |

### PR-B6 — Organization members & invitations ✅

**Endpoints**
| Method | Route | Request body | Response |
|--------|-------|---------------|----------|
| GET | `/api/organization` | — | `200 { id, name, slug, memberCount }` |
| GET | `/api/organization/members` | — | `200 [{ id, email, firstName, lastName, role, status }]` |
| POST | `/api/organization/invitations` *(Admin)* | `{ email, role }` | `201 { id, email, role, status, expiresAt }` |
| GET | `/api/organization/invitations` *(Admin)* | — | `200 [{ id, email, role, status, expiresAt }]` |
| DELETE | `/api/organization/invitations/{id}` *(Admin)* | — | `204` — revokes |
| POST | `/api/auth/accept-invitation` | `{ token, password, firstName, lastName }` | `200 { accessToken, refreshToken, user }` — creates the user, marks invitation `Accepted` |
| PATCH | `/api/organization/members/{userId}/role` *(Admin)* | `{ role }` | `200 { id, role }` |
| DELETE | `/api/organization/members/{userId}` *(Admin)* | — | `204` — sets status to `Deactivated` (no hard delete, preserves task/comment history) |

### PR-B7 — Task CRUD ✅

**Endpoints**
| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/tasks` | query: `status?, priority?, assigneeId?, page=1, pageSize=20` | `200 { items: [{ id, title, status, priority, assigneeId, assigneeName, dueDate, createdAt }], total, page, pageSize }` |
| GET | `/api/tasks/{id}` | — | `200 { id, title, description, status, priority, assigneeId, dueDate, createdById, createdAt, updatedAt }` |
| POST | `/api/tasks` | `{ title, description?, priority, assigneeId?, dueDate? }` | `201 { id, title, description, status, priority, assigneeId, dueDate, createdAt }` |
| PUT | `/api/tasks/{id}` | `{ title, description?, status, priority, assigneeId?, dueDate? }` | `200 { id, ... }` |
| PATCH | `/api/tasks/{id}/status` | `{ status }` | `200 { id, status }` — lightweight endpoint for board drag-and-drop |
| DELETE | `/api/tasks/{id}` *(Admin or task creator)* | — | `204` |

### PR-B8 — Task comments ✅

**Endpoints**
| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/tasks/{taskId}/comments` | — | `200 [{ id, body, authorId, authorName, createdAt }]` |
| POST | `/api/tasks/{taskId}/comments` | `{ body }` | `201 { id, body, authorId, authorName, createdAt }` |
| DELETE | `/api/tasks/{taskId}/comments/{commentId}` *(author or Admin)* | — | `204` |

### PR-B9 — Validation, error handling & pagination polish ✅

- Validation endpoint filter applied consistently across all mutating endpoints → `400` `ProblemDetails` with field-level errors
- Standard `PagedResult<T>` wrapper reused across list endpoints
- 403 vs 404 policy: return `404` (not `403`) when a resource exists but belongs to another tenant, so cross-tenant probing can't distinguish "doesn't exist" from "not yours"

No new endpoints — hardening pass across PR-B4 through PR-B8.

### PR-B10 — xUnit test suite ✅

- Unit tests: Application-layer services, validators
- Integration tests: `WebApplicationFactory` + Testcontainers Postgres — full HTTP round trips
- **Tenant isolation test is the one that matters most for this project**: seed two orgs, assert org A's JWT can never read/write org B's tasks, comments, or members

No new endpoints.

### PR-B11 — Dockerfile & Render deployment config ✅

- Multi-stage `Dockerfile` (SDK build stage → ASP.NET runtime stage)
- `render.yaml` (Blueprint) or manual Render Web Service pointing at the Dockerfile
- Health check path configured to `/health`
- Startup migration runner (`db.Database.Migrate()` behind an `ApplyMigrationsOnStartup` config flag, on for this demo)

No new endpoints.

### PR-B12 — GitHub Actions CI (backend) ✅

See [Section 5](#5-github-actions-cicd) below.

---

## 4. Deployment Notes — Render

- **Service type:** Web Service, built from the `Dockerfile` in PR-B11
- **Port:** Render injects `PORT`; `ASPNETCORE_URLS=http://+:$PORT`
- **Database:** Render Postgres (or Railway, whichever is cheaper/available) — connection string wired via env var, never committed
- **Health check path:** `/health`
- **Migrations:** applied on startup for this demo (see PR-B11); a real production setup would use a separate release/pre-deploy step instead

**Environment variables**

| Variable | Purpose |
|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | Postgres connection string |
| `Jwt__Secret` | Signing key for access tokens |
| `Jwt__Issuer` | Token issuer |
| `Jwt__Audience` | Token audience |
| `Jwt__AccessTokenMinutes` | e.g. `15` |
| `Jwt__RefreshTokenDays` | e.g. `14` |
| `Cors__AllowedOrigins` | Comma-separated frontend origin(s), e.g. Vercel URL |

---

## 5. GitHub Actions CI/CD

Assuming a monorepo with `server/` and `client/` — see [`frontend-plan.md`](./frontend-plan.md) for the frontend pipeline. Backend workflow is path-filtered to `server/**` so frontend-only PRs don't trigger a .NET build.

**`.github/workflows/backend-ci.yml`**

On every pull request touching `server/**`:
1. Checkout, `actions/setup-dotnet`
2. `dotnet restore`
3. `dotnet format --verify-no-changes` (formatting gate)
4. `dotnet build --warnaserror`
5. Spin up a Postgres service container; `dotnet test` (unit + integration suites)
6. `gitleaks` secret scan across the whole repo (not path-filtered — secrets can land anywhere)

On merge to `main`:
1. Same build/test steps as a safety net
2. Build and push the Docker image (if using Render's Docker deploy path), or simply `curl` Render's deploy hook URL to trigger a redeploy from the new commit
3. Post-deploy: hit `/health` on the live URL to confirm the deploy succeeded

Render's own GitHub integration can also auto-deploy on push to `main` without a custom Action step — the deploy-hook approach above is only needed if you want deploys gated behind CI passing rather than firing independently.

---

## 6. Nice to Have

Two features surfaced by the frontend design ([`designs/project/Taskflow.dc.html`](../designs/project/Taskflow.dc.html)) that aren't part of the original spec. Kept out of the core PR-B0–B12 sequence so the MVP stays lean; pick these up afterward if there's time. Each is its own PR, built on top of PR-B12.

**Status: not started.** Neither NH-B1 nor NH-B2 has any code in `server/src` yet.

### NH-B1 — Projects

**Data model addition**

`projects`

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | uuid | PK | |
| organization_id | uuid | FK → organizations.id, not null, indexed | |
| name | varchar(100) | not null | |
| color | varchar(7) | not null | Hex color for the sidebar dot/tag |
| created_at | timestamptz | not null, default now() | |

`tasks` gains: `project_id uuid, FK → projects.id, nullable` (a task may be unassigned to any project).

**Endpoints**

| Method | Route | Request body | Response |
|--------|-------|---------------|----------|
| GET | `/api/projects` | — | `200 [{ id, name, color }]` |
| POST | `/api/projects` *(Admin)* | `{ name, color }` | `201 { id, name, color }` |
| DELETE | `/api/projects/{id}` *(Admin)* | — | `204` — tasks referencing it fall back to `project_id = null` |

`POST /api/tasks` and `PUT /api/tasks/{id}` (PR-B7) gain an optional `projectId` field once this ships.

### NH-B2 — Activity Log

**Data model addition**

`activity_log`

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | uuid | PK | |
| organization_id | uuid | FK → organizations.id, not null, indexed | |
| actor_id | uuid | FK → users.id, not null | |
| task_id | uuid | FK → tasks.id, nullable, on delete set null | Null for org-level events (e.g. member invited) |
| type | enum | not null — `TaskCreated` \| `TaskStatusChanged` \| `TaskAssigned` \| `CommentAdded` \| `MemberInvited` \| ... | |
| summary | text | not null | Precomputed display string (e.g. "moved API rate limiting to In Progress"), avoids reconstructing sentences client-side |
| created_at | timestamptz | not null, default now() | |

Written by the relevant Application-layer service (e.g. `TaskService.UpdateStatusAsync` also inserts an `activity_log` row) rather than via a generic event bus — keeps this simple for a demo-scale app.

**Endpoints**

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/activity` | query: `limit=20` | `200 [{ id, actorId, actorName, taskId, type, summary, createdAt }]` — org-scoped, most recent first |
