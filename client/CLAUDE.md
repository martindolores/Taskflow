# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository. It applies to the `client/` directory (the React frontend) and is loaded in addition to the root `CLAUDE.md`.

## Stack

Vite + React 19 + TypeScript, MUI v9 (Emotion) for components/theming, React Router for routing, TanStack Query for server state, React Hook Form + Zod for forms, a single Axios instance for HTTP. Full spec, design tokens, and PR build plan: `../docs/frontend-plan.md`. Visual source of truth for screens not yet built: `../designs/project/Taskflow.dc.html` (read `../designs/README.md` first).

## Commands

Run from `client/`.

```bash
nvm use                # pin Node to .nvmrc (22) — see Node version note below
npm run dev             # http://localhost:5173
npm run build            # tsc -b && vite build
npm run lint             # oxlint
npm run format            # prettier --write .
npm run format:check       # prettier --check . (CI gate)
npm run preview           # serve the production build locally
```

No test runner is wired up yet — Playwright e2e lands in PR-F11 per the build plan; nothing before that introduces a unit test framework.

**Node version**: oxlint and Vite's native bindings require Node `^20.19.0 || >=22.12.0`; a lower patch (e.g. system-installed 22.5.x) fails silently on `npm install` (missing native binding, not a version-check error). Use `nvm use` (reads `.nvmrc`) rather than whatever `node` resolves to on `$PATH`.

## Architecture

```
src/
  api/            # axios instance (client.ts), tokenStorage, TanStack Query client
  components/     # shared/dumb components
  features/
    auth/
    organization/
    tasks/
  routes/         # route components, ProtectedRoute / PublicRoute (added PR-F3)
  theme/          # MUI theme (palette/typography/shape tokens)
  App.tsx
```

- **`src/theme/theme.ts`** — the dark MUI theme. Custom, non-standard palette tokens (`surface.input`, `border.default`/`border.hover`, `nav.activeText`, `avatarGradient`) are added via `declare module '@mui/material/styles'` augmentation in the same file — extend that block rather than reaching for raw hex values in components.
- **`src/api/client.ts`** — the shared `apiClient` Axios instance. Request interceptor attaches the JWT from `tokenStorage`; response interceptor does a single silent refresh via `POST /api/auth/refresh` on a 401 (concurrent 401s share one in-flight refresh via a module-level promise), retries the original request once, and on refresh failure clears tokens and hard-redirects to `/login`. Use `apiClient` for all backend calls — don't instantiate a second Axios instance, and don't call the refresh endpoint through `apiClient` itself (it uses plain `axios` to avoid recursing into its own interceptor).
- **`src/api/tokenStorage.ts`** — thin `localStorage` wrapper (`taskflow.accessToken` / `taskflow.refreshToken`). `AuthContext` (PR-F2) will build on top of this rather than reading `localStorage` directly elsewhere.
- **`src/api/queryClient.ts`** — the single `QueryClient` instance, provided at the root in `App.tsx`.

## Patterns to follow

- **Absolute imports**: `@/*` maps to `src/*` (`tsconfig.app.json` `paths`, picked up automatically by `vite-tsconfig-paths` in `vite.config.ts`). Prefer `@/theme`, `@/api/client`, etc. over relative `../../` chains.
- **New API calls**: add a per-resource module under `api/` (e.g. `tasksApi.ts`) that wraps `apiClient`, then consume it through a TanStack Query hook in the relevant `features/<name>/` folder — don't call `apiClient` directly from components.
- **Env config**: only `VITE_API_URL` so far (documented in `.env.example`); copy it to `.env` for local dev (gitignored). New env vars must be prefixed `VITE_` to be exposed to client code, added to `.env.example`, and typed in `src/vite-env.d.ts`'s `ImportMetaEnv`.
- **Linting/formatting**: oxlint (not ESLint — chosen over the spec's original ESLint pick for speed and zero-config; see `.oxlintrc.json`) plus Prettier for formatting. Both are separate steps (`npm run lint`, `npm run format:check`); oxlint does not format.
- **MUI version**: the project runs MUI v9, not the v5 named in `docs/frontend-plan.md` §1 (the doc predates several MUI majors). The theming API used by the doc's design tokens (palette/typography/shape) is stable across that range; watch for v6+ breaking changes in individual components (e.g. `Grid`) when implementing new screens.
