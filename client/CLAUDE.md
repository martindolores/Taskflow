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
npm run test:contracts     # Playwright request-contract tests — see tests/ below; no backend needed
npm run test:contracts:ui  # same, in Playwright's UI mode
```

Playwright (`tests/`) is the only test runner in this repo — no unit test framework is wired up.

**Running the contract suite**: these are frontend-only — every backend call is intercepted with `page.route` (see `tests/support/api.ts`), so no real API or Postgres is needed. Each spec drives a real UI flow (fill a form, click submit) and asserts the JSON body the app actually sent matches the shape of the backend's C# request DTO (e.g. `CreateTaskRequest`) for that endpoint — a regression check that the frontend and backend still agree on the wire format, not a check of backend behavior. `playwright.config.ts` auto-starts `npm run preview` on port 4173 against whatever's in `dist/` (run `npm run build` first) unless `PLAYWRIGHT_BASE_URL` is set, in which case that URL (e.g. a Vercel preview) is used as-is and nothing is started locally — mocking happens at the browser/network layer so it works the same either way.

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
tests/            # Playwright request-contract specs — mocked backend (reworked from PR-F11's real-backend e2e suite)
  support/
    api.ts        # mockJson (stub a read endpoint), captureRequest (stub a write endpoint, resolve with the sent JSON body)
    fixtures.ts   # fakeUser/fakeMembers fixture data, signInWithFakeSession (seeds localStorage tokens + mocks GET /api/users/me)
```

- **`src/theme/theme.ts`** — the dark MUI theme. Custom, non-standard palette tokens (`surface.input`, `border.default`/`border.hover`, `nav.activeText`, `avatarGradient`) are added via `declare module '@mui/material/styles'` augmentation in the same file — extend that block rather than reaching for raw hex values in components.
- **`src/api/client.ts`** — the shared `apiClient` Axios instance. Request interceptor attaches the JWT from `tokenStorage`; response interceptor does a single silent refresh via `POST /api/auth/refresh` on a 401 (concurrent 401s share one in-flight refresh via a module-level promise), retries the original request once, and on refresh failure clears tokens and hard-redirects to `/login`. Requests to `/api/auth/*` (login, register, refresh, logout) are exempt from this — a 401 there (e.g. bad login credentials) just rejects normally so the caller can show an inline error, instead of triggering a refresh attempt and hard-redirect. Use `apiClient` for all backend calls — don't instantiate a second Axios instance, and don't call the refresh endpoint through `apiClient` itself (it uses plain `axios` to avoid recursing into its own interceptor).
- **`src/api/tokenStorage.ts`** — thin `localStorage` wrapper (`taskflow.accessToken` / `taskflow.refreshToken`). `AuthContext` (PR-F2) will build on top of this rather than reading `localStorage` directly elsewhere.
- **`src/api/queryClient.ts`** — the single `QueryClient` instance, provided at the root in `App.tsx`. Its `QueryCache`/`MutationCache` show an error toast (via `src/components/toast.ts`) for any query/mutation failure by default; pass `meta: { suppressErrorToast: true }` on a mutation that already renders its own inline `Alert` (see `TaskFormModal`, `OrganizationSettingsScreen`'s invite form) to avoid double-reporting the same error.

## Key files

| What                                                                  | Path                                                                                |
| --------------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| Axios instance + interceptors                                         | `src/api/client.ts`                                                                 |
| Token storage                                                         | `src/api/tokenStorage.ts`                                                           |
| Query client                                                          | `src/api/queryClient.ts`                                                            |
| Per-resource API modules                                              | `src/api/{authApi,tasksApi,commentsApi,organizationApi}.ts`                         |
| Shared API error handling (`extractErrorMessage`, `applyFieldErrors`) | `src/api/errors.ts`                                                                 |
| Toast bus + host (`showToast`, mounted once in `App.tsx`)             | `src/components/{toast,ToastHost}.ts(x)`                                            |
| MUI theme + palette augmentation                                      | `src/theme/theme.ts`                                                                |
| Auth state                                                            | `src/features/auth/AuthContext.tsx`, `context.ts`, `useAuth.ts`                     |
| Auth screens                                                          | `src/features/auth/{AuthScreen,AcceptInvitationScreen}.tsx`                         |
| Task screens                                                          | `src/features/tasks/{TaskListScreen,TaskDetailScreen,TaskFormModal}.tsx`            |
| Task queries/schemas                                                  | `src/features/tasks/{tasksQueries,commentsQueries,taskSchemas}.ts`                  |
| Organization settings                                                 | `src/features/organization/OrganizationSettingsScreen.tsx`                          |
| Shared dumb components                                                | `src/components/{LabeledField,PriorityLabel,RoleBadge,StatusChip,UserAvatar}.tsx`   |
| Routing/guards                                                        | `src/routes/{AppShell,ProtectedRoute,PublicRoute,ErrorBoundary,NotFoundScreen}.tsx` |
| App entry                                                             | `src/App.tsx`                                                                       |
| Env typing                                                            | `src/vite-env.d.ts`                                                                 |
| Playwright config                                                     | `playwright.config.ts`                                                              |
| Request-contract specs                                                | `tests/{auth,invitation,tasks}.spec.ts`                                             |
| Mocking helpers + fixture data                                        | `tests/support/{api,fixtures}.ts`                                                   |

## Patterns to follow

- **Absolute imports**: `@/*` maps to `src/*` (`tsconfig.app.json` `paths`, picked up automatically by `vite-tsconfig-paths` in `vite.config.ts`). Prefer `@/theme`, `@/api/client`, etc. over relative `../../` chains.
- **New API calls**: add a per-resource module under `api/` (e.g. `tasksApi.ts`) that wraps `apiClient`, then consume it through a TanStack Query hook in the relevant `features/<name>/` folder — don't call `apiClient` directly from components.
- **Env config**: only `VITE_API_URL` so far (documented in `.env.example`); copy it to `.env` for local dev (gitignored). New env vars must be prefixed `VITE_` to be exposed to client code, added to `.env.example`, and typed in `src/vite-env.d.ts`'s `ImportMetaEnv`.
- **Linting/formatting**: oxlint (not ESLint — chosen over the spec's original ESLint pick for speed and zero-config; see `.oxlintrc.json`) plus Prettier for formatting. Both are separate steps (`npm run lint`, `npm run format:check`); oxlint does not format.
- **MUI version**: the project runs MUI v9, not the v5 named in `docs/frontend-plan.md` §1 (the doc predates several MUI majors). The theming API used by the doc's design tokens (palette/typography/shape) is stable across that range; watch for v6+ breaking changes in individual components (e.g. `Grid`) when implementing new screens.
- **New request-contract spec**: mock every endpoint the flow touches with `mockJson` (reads) and `captureRequest` (the one write endpoint under test) from `tests/support/api.ts` — there's no real backend, so an unmocked call just hangs. Use `signInWithFakeSession` from `tests/support/fixtures.ts` for specs that need an already-authenticated page. Assert the resolved `captureRequest` body with `toEqual` against an object literal that mirrors the backend's C# request DTO field-for-field (see the existing specs) — that's the actual point of the test, not just UI interaction. Locate elements the same way the app is built to be labeled — `getByLabel`/`getByRole`/`getByPlaceholder` — rather than adding `data-testid`s; every form field in this codebase already has a real `<label>` via `LabeledField` for exactly this reason.
