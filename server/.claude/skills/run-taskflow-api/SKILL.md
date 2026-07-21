---
name: run-taskflow-api
description: Build, run, and smoke-test the TaskFlow backend (ASP.NET Core Web API). Use when asked to start the API, run its tests, build it, apply EF Core migrations, or verify the backend works end-to-end.
---

TaskFlow's backend is an ASP.NET Core 8 Web API (Postgres via EF Core). Drive it with `.claude/skills/run-taskflow-api/smoke.sh`, which starts Postgres, builds, migrates, launches the API in the background, curls its endpoints, and tears it down. `curl` is the ongoing interaction surface — there's no separate driver needed beyond that script.

All paths below are relative to `server/` (the repo's backend unit).

## Prerequisites

- .NET 8 SDK (`dotnet --list-sdks` should show an `8.0.x` entry — this repo targets `net8.0` even if a newer SDK is also installed).
- Docker with `docker compose` (used both for the local Postgres and for Testcontainers in integration tests).
- `dotnet-ef` CLI tool:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

## Setup

No package installs beyond the SDK — `dotnet build` restores NuGet packages automatically.

## Build

```bash
dotnet build
```

## Run (agent path)

```bash
.claude/skills/run-taskflow-api/smoke.sh
```

This does the whole cycle: `docker compose up -d` for Postgres → wait for readiness → `dotnet build` → `dotnet ef database update` → launch `dotnet run --project src/TaskFlow.Api` in the background (log at `/tmp/taskflow-api.log`) → poll `http://localhost:5151/health` → curl `/health`, `/`, `/swagger/index.html`, and a 404 route → kill the port's listener to stop it. Exits non-zero (and dumps the last 30 log lines) if the API never becomes healthy.

Postgres is left running afterward (it's a shared dev dependency); only the API process is stopped.

To drive the API yourself instead of the script:

```bash
nohup dotnet run --project src/TaskFlow.Api --no-build &> /tmp/taskflow-api.log &
curl http://localhost:5151/health   # → {"status":"healthy"}
lsof -ti:5151 -sTCP:LISTEN | xargs -r kill   # stop it — $! is dotnet's wrapper PID and won't reliably stop Kestrel
```

### Environment

| Variable | Required | Default | Notes |
|---|---|---|---|
| `ConnectionStrings__Default` | No | `Host=localhost;Port=5432;Database=taskflow;Username=taskflow;Password=taskflow` | Matches `docker-compose.yml` credentials |
| `Cors__AllowedOrigins` | No | `""` (empty — no origins allowed) | Comma-separated list, e.g. `Cors__AllowedOrigins="http://localhost:3000,http://localhost:5173"` |
| `ASPNETCORE_ENVIRONMENT` | No | `Development` (via launchSettings.json) | Controls Swagger UI availability (Development-only) |

## Run (human path)

```bash
docker compose up -d
dotnet run --project src/TaskFlow.Api   # blocks; Ctrl-C to stop
```
Swagger UI at `http://localhost:5151/swagger/index.html` (Development only).

## Test

```bash
dotnet test
```

`TaskFlow.IntegrationTests` uses `WebApplicationFactory` + Testcontainers, spinning up its own ephemeral Postgres via Docker (separate from the `docker-compose.yml` instance) — Docker must be running. Expect 32 passing tests. `TaskFlow.UnitTests` currently has no test files, so `dotnet test` reports "No test is available" for that project — this is expected at this stage of the build, not a failure.

---

## Gotchas

- **The API's own PID isn't enough to stop it.** `dotnet run` forks a child `TaskFlow.Api` process; the shell's `$!` is the `dotnet` wrapper, which doesn't forward `SIGTERM` to Kestrel reliably. Kill by port instead: `lsof -ti:5151 -sTCP:LISTEN | xargs -r kill`.
- **CORS is closed by default.** `Cors:AllowedOrigins` is `""` in `appsettings.json`, so a plain preflight from any origin gets no `Access-Control-Allow-Origin` header (not an error — just silently no CORS headers). Set `Cors__AllowedOrigins` to test cross-origin behavior.
- **Two separate Postgres instances exist in this workflow**: the `docker-compose.yml` one (port 5432, persistent, used by `dotnet run`) and a throwaway Testcontainers instance spun up per integration test run. Don't assume the docker-compose DB reflects what integration tests see, or vice versa.
- **Swagger is Development-only** (`app.Environment.IsDevelopment()` gate in `Program.cs`) — it 404s if you override `ASPNETCORE_ENVIRONMENT` to `Production`.

## Troubleshooting

- **`dotnet ef` not found**: install it with `dotnet tool install --global dotnet-ef` and ensure `~/.dotnet/tools` is on `PATH`.
- **Port 5151 already in use** (leftover process from a previous run): `lsof -ti:5151 -sTCP:LISTEN | xargs -r kill` before relaunching.
- **Integration tests hang or fail to start containers**: confirm `docker ps` works without sudo/permission errors — Testcontainers needs a reachable Docker daemon.
