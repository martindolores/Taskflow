# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Taskflow is a multi-tenant task/project management app: an ASP.NET Core Web API backend (PostgreSQL, JWT auth, Clean Architecture) and a React frontend (Vite, TypeScript, MUI).

## Repo layout

- `server/` — the ASP.NET Core backend. **Read `server/CLAUDE.md` before working here** — it has the build/test/migration commands and architecture notes. Nothing backend-specific is repeated in this file.
- `client/` — the React frontend. **Read `client/CLAUDE.md` before working here** — it has the build/lint/format commands, folder structure, and API-client conventions. Nothing frontend-specific is repeated in this file.
- `docs/plan.md` — the authoritative current spec: mobile-responsive retrofit of the app plus the Projects and Activity Log features, as the ordered PR-M0…PR-M11 build plan. Check it before adding or changing an endpoint or a screen.
- `docs/legacy/` — the original backend (`backend-plan.md`, PR-B0…PR-B12) and frontend (`frontend-plan.md`, PR-F0…PR-F13) build plans, both fully shipped, plus `deployment-plan.md`, the still-accurate Render/Vercel/Neon runbook. Kept for historical reference on the shipped core; `docs/plan.md` is where new work is tracked.
- `designs/` — a Claude Design handoff bundle (HTML/CSS/JS prototypes), not production code. Read `designs/README.md` first — it explains how to read `designs/project/Taskflow.dc.html` before implementing any frontend work from it.

## Key files

| What | Path |
|---|---|
| Current spec (mobile-responsive + Projects/Activity Log, PR-M0…PR-M11 plan) | `docs/plan.md` |
| Shipped backend spec, historical (PR-B0…PR-B12) | `docs/legacy/backend-plan.md` |
| Shipped frontend spec, historical (PR-F0…PR-F13) | `docs/legacy/frontend-plan.md` |
| Deployment runbook (Render + Vercel + Neon, free tier) — still current | `docs/legacy/deployment-plan.md` |
| Design handoff bundle (read `designs/README.md` first) | `designs/project/Taskflow.dc.html` |
| Backend build/test/migration commands & architecture | `server/CLAUDE.md` (§ Key files has the file map) |
| Frontend build/lint/format commands & conventions | `client/CLAUDE.md` (§ Key files has the file map) |
| Local Postgres for backend dev/tests | `server/docker-compose.yml` |
| Agent-path shortcuts (build/run/query DB) | `server/.claude/skills/{run-taskflow-api,query-taskflow-db}/` |

## Working conventions

- Work ships as the ordered `PR-M<N>` chunks defined in `docs/plan.md` §3, one PR per chunk, later chunks depending on earlier ones. Commit messages reference the chunk, e.g. `Add responsive app shell & navigation (PR-M1)`. Check `git log` and `docs/plan.md` to see which chunk is next before starting new work.
- Commits go directly to `main` — no PR/branch workflow is in use in this repo.
