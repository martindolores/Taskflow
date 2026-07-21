# Taskflow

A multi-tenant task and project management app. ASP.NET Core Web API backend (PostgreSQL, JWT auth, Clean Architecture) with a React frontend (Vite, TypeScript, MUI).

Each organization is an isolated tenant with its own users and tasks. Admins invite members by email, assign tasks, track status/priority, and comment on tasks; every request is scoped to the caller's organization via a server-enforced tenant filter.

## Features

- Email/password auth with JWT access + refresh tokens
- Organization creation, member invitations, and role-based access (Admin / Member)
- Task CRUD with status, priority, assignee, and due date
- Threaded comments on tasks
- Dashboard with task/status overview
- Enforced multi-tenant data isolation at the database query layer

## Stack

| | |
|---|---|
| Backend | ASP.NET Core 8, EF Core (PostgreSQL, snake_case via `EFCore.NamingConventions`), JWT bearer auth, minimal APIs, Clean Architecture (Domain → Application → Infrastructure → Api) |
| Frontend | Vite, React 19, TypeScript, MUI, TanStack Query, React Hook Form + Zod, Axios |
| Database | PostgreSQL 16 (via Docker) |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (`dotnet --list-sdks` should show an `8.0.x` entry)
- [Node.js](https://nodejs.org/) `^20.19.0 || >=22.12.0` — use [nvm](https://github.com/nvm-sh/nvm) with the repo's `.nvmrc` (`nvm use` from `client/`)
- [Docker](https://www.docker.com/) with `docker compose` (runs the local Postgres instance)
- [`dotnet-ef`](https://learn.microsoft.com/ef/core/cli/dotnet) CLI tool:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Getting started

Clone the repo, then set up the backend and frontend in two terminals.

### 1. Backend

```bash
cd server
docker compose up -d                                                    # starts local Postgres on :5432
dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api
dotnet run --project src/TaskFlow.Api                                   # http://localhost:5151
```

No `.env` setup needed — `appsettings.Development.json` already points at the `docker-compose.yml` Postgres credentials. Swagger UI is available at `http://localhost:5151/swagger/index.html` while running in Development.

### 2. Frontend

```bash
cd client
nvm use              # pins Node to the version in .nvmrc
npm install
cp .env.example .env  # VITE_API_URL=http://localhost:5151, already correct for local dev
npm run dev           # http://localhost:5173
```

Open `http://localhost:5173`, register an account (this creates a new organization as its Admin), and you're in.

## Running tests

```bash
cd server
dotnet test           # unit + integration suites — requires the docker-compose Postgres to be running
```

No frontend test runner is wired up yet.

## Project structure

```
server/    ASP.NET Core Web API — see server/CLAUDE.md for architecture, commands, and conventions
client/    React frontend — see client/CLAUDE.md for structure, commands, and conventions
docs/      Backend and frontend technical specs (data model, endpoint contracts, build plan)
designs/   Design handoff bundle (HTML/CSS/JS prototypes) — visual source of truth for screens
```

For a deeper dive into either half of the stack, read `server/CLAUDE.md` or `client/CLAUDE.md`.

## Troubleshooting

- **`dotnet ef` not found** — install it globally: `dotnet tool install --global dotnet-ef`.
- **Frontend fails to install/run with a cryptic native-binding error** — you're likely on a Node version outside `^20.19.0 || >=22.12.0` (a system-installed patch like 22.5.x fails silently). Run `nvm use` from `client/` to pick up `.nvmrc`.
- **Backend can't reach Postgres** — confirm the container is up with `docker compose ps` (run from `server/`); `docker compose up -d` starts it in the background.
- **Stopping a background `dotnet run`** — it forks a child process, so `Ctrl+C` on the wrong shell may not kill it; free the port with `lsof -ti:5151 -sTCP:LISTEN | xargs -r kill`.
