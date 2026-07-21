# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Taskflow is a multi-tenant task/project management app: an ASP.NET Core Web API backend (PostgreSQL, JWT auth, Clean Architecture) and a React frontend (Vite, TypeScript, MUI).

## Repo layout

- `server/` — the ASP.NET Core backend. **Read `server/CLAUDE.md` before working here** — it has the build/test/migration commands and architecture notes. Nothing backend-specific is repeated in this file.
- `client/` — the React frontend. **Read `client/CLAUDE.md` before working here** — it has the build/lint/format commands, folder structure, and API-client conventions. Nothing frontend-specific is repeated in this file.
- `docs/backend-plan.md` — the authoritative backend spec: full data model, endpoint contracts, and the ordered PR-B0…PR-B12 build plan. Check it before adding or changing an endpoint.
- `docs/frontend-plan.md` — the authoritative frontend spec: architecture, design tokens, and the ordered PR-F0…PR-F13 build plan (companion doc to `backend-plan.md`; endpoints are ordered so the frontend never waits on a backend PR it needs). Check it before adding or changing a screen.
- `designs/` — a Claude Design handoff bundle (HTML/CSS/JS prototypes), not production code. Read `designs/README.md` first — it explains how to read `designs/project/Taskflow.dc.html` before implementing any frontend work from it.

## Working conventions

- Backend work ships as the ordered `PR-B<N>` chunks defined in `docs/backend-plan.md` §3, one PR per chunk, later chunks depending on earlier ones. Commit messages reference the chunk, e.g. `Add auth: registration, login, JWT issuance (PR-B4)`. Check `git log` and `docs/backend-plan.md` to see which chunk is next before starting new backend work.
- Frontend work ships as the ordered `PR-F<N>` chunks defined in `docs/frontend-plan.md` §3, one PR per chunk, later chunks depending on earlier ones and on the backend PR that provides their endpoints. Commit messages reference the chunk, e.g. `Add API client & env config (PR-F1)`. Check `git log` and `docs/frontend-plan.md` to see which chunk is next before starting new frontend work.
- Commits go directly to `main` — no PR/branch workflow is in use in this repo.
