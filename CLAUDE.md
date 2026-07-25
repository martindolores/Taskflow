# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Taskflow is a multi-tenant task/project management app: an ASP.NET Core Web API backend (PostgreSQL, JWT auth, Clean Architecture) and a React frontend (Vite, TypeScript, MUI).

## Repo layout

- `server/` — the ASP.NET Core backend. **Read `server/CLAUDE.md` before working here** — it has the build/test/migration commands and architecture notes. Nothing backend-specific is repeated in this file.
- `client/` — the React frontend. **Read `client/CLAUDE.md` before working here** — it has the build/lint/format commands, folder structure, and API-client conventions. Nothing frontend-specific is repeated in this file.
- `designs/` — a Claude Design handoff bundle (HTML/CSS/JS prototypes), not production code. Read `designs/README.md` first — it explains how to read `designs/project/Taskflow.dc.html` before implementing any frontend work from it.
- `RUNBOOK.md` — the Render/Vercel/Neon deployment runbook, still current.

Work tracking lives on the [GitHub Project board](https://github.com/users/martindolores/projects/1) (Todo / In Progress / Done), backed by issues in this repo — not in a `docs/` folder. The board holds both the active invite-email work (issues tagged `PR-E0`…`PR-E4`) and the full shipped history (`PR-B*`/`PR-F*`/`PR-M*`, all closed/Done) that used to live in `docs/legacy/`.

## Key files

| What | Path |
|---|---|
| Work tracking (active + historical, kanban) | [GitHub Project board](https://github.com/users/martindolores/projects/1) |
| Deployment runbook (Render + Vercel + Neon, free tier) | `RUNBOOK.md` |
| Design handoff bundle (read `designs/README.md` first) | `designs/project/Taskflow.dc.html` |
| Backend build/test/migration commands & architecture | `server/CLAUDE.md` (§ Key files has the file map) |
| Frontend build/lint/format commands & conventions | `client/CLAUDE.md` (§ Key files has the file map) |
| Local Postgres for backend dev/tests | `server/docker-compose.yml` |
| Agent-path shortcuts (build/run/query DB) | `server/.claude/skills/{run-taskflow-api,query-taskflow-db}/` |
| `/deploy` skill — bump version, tag, push to trigger Render/Vercel deploy | `.claude/skills/deploy/SKILL.md` |

## Working conventions

- Check the project board for active work and its chunk id (e.g. `PR-E1`) before starting; commit messages should reference it (e.g. `Add Brevo email service (PR-E1)`), following the existing convention.
- Commits go directly to `main` — no PR/branch workflow is in use in this repo.
