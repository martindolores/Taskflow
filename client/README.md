# Taskflow — Frontend

React + TypeScript frontend for Taskflow, built with Vite and MUI. See [`../docs/frontend-plan.md`](../docs/frontend-plan.md) for the full technical spec and build plan.

## Stack

Vite, React 19, TypeScript, MUI v9 (Emotion), React Router, TanStack Query, React Hook Form + Zod, Axios.

## Structure

```
src/
  api/            # axios instance, tokenStorage, per-resource API modules, query client
  components/     # shared/dumb components
  features/
    auth/
    organization/
    tasks/
  routes/         # route components, ProtectedRoute / PublicRoute
  theme/          # MUI theme (palette/typography/shape tokens)
  App.tsx
```

## Prerequisites

- Node.js `^20.19.0 || >=22.12.0` — use [nvm](https://github.com/nvm-sh/nvm) with the repo's `.nvmrc`:
  ```bash
  nvm use
  ```
- The Taskflow backend running locally (see [`../server/README.md`](../server/README.md)) — the app expects it at the URL configured below.

## Setup

```bash
npm install
cp .env.example .env   # VITE_API_URL=http://localhost:5151, correct as-is for local dev
```

## Running locally

```bash
npm run dev       # http://localhost:5173
```

## Other commands

```bash
npm run build             # tsc -b && vite build
npm run lint               # oxlint
npm run format              # prettier --write .
npm run format:check         # prettier --check . (CI gate)
npm run preview              # serve the production build locally
npm run test:contracts       # Playwright request-contract tests (mocked backend, no server needed)
npm run test:contracts:ui    # same, in Playwright's UI mode
```
