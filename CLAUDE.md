# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Taskflow is a multi-tenant task/project management app: an ASP.NET Core Web API backend (PostgreSQL, JWT auth, Clean Architecture) and a React frontend (Vite, TypeScript, MUI).

## Repo layout

- `server/` — the ASP.NET Core backend. **Read `server/CLAUDE.md` before working here** — it has the build/test/migration commands and architecture notes. Nothing backend-specific is repeated in this file.
- `client/` — the React frontend. **Read `client/CLAUDE.md` before working here** — it has the build/lint/format commands, folder structure, and API-client conventions. Nothing frontend-specific is repeated in this file.
- `docs/legacy/` — all build plans, fully shipped, kept for historical reference: the original backend (`backend-plan.md`, PR-B0…PR-B12) and frontend (`frontend-plan.md`, PR-F0…PR-F13), the mobile-responsive + Projects/Activity Log plan (`mobile-plan.md`, PR-M0…PR-M12), and `deployment-plan.md`, the still-accurate Render/Vercel/Neon runbook. No plan doc currently tracks new work — check `git log` for the latest shipped state before starting something new.
- `designs/` — a Claude Design handoff bundle (HTML/CSS/JS prototypes), not production code. Read `designs/README.md` first — it explains how to read `designs/project/Taskflow.dc.html` before implementing any frontend work from it.

## Key files

| What | Path |
|---|---|
| Shipped mobile-responsive + Projects/Activity Log spec, historical (PR-M0…PR-M12) | `docs/legacy/mobile-plan.md` |
| Shipped backend spec, historical (PR-B0…PR-B12) | `docs/legacy/backend-plan.md` |
| Shipped frontend spec, historical (PR-F0…PR-F13) | `docs/legacy/frontend-plan.md` |
| Deployment runbook (Render + Vercel + Neon, free tier) — still current | `docs/legacy/deployment-plan.md` |
| Design handoff bundle (read `designs/README.md` first) | `designs/project/Taskflow.dc.html` |
| Backend build/test/migration commands & architecture | `server/CLAUDE.md` (§ Key files has the file map) |
| Frontend build/lint/format commands & conventions | `client/CLAUDE.md` (§ Key files has the file map) |
| Local Postgres for backend dev/tests | `server/docker-compose.yml` |
| Agent-path shortcuts (build/run/query DB) | `server/.claude/skills/{run-taskflow-api,query-taskflow-db}/` |
| `/deploy` skill — bump version, tag, push to trigger Render/Vercel deploy | `.claude/skills/deploy/SKILL.md` |

## Working conventions

- No plan doc currently tracks new work — all `docs/legacy/` plans are fully shipped. When a new build plan is introduced, commit messages should reference its chunk id (e.g. `Add responsive app shell & navigation (PR-M1)`), following the convention of the shipped plans.
- Commits go directly to `main` — no PR/branch workflow is in use in this repo.
