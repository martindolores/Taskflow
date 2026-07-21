# Taskflow — Frontend Technical Spec & Build Plan

React + TypeScript + MUI, deployed to Vercel.

Companion document: [`backend-plan.md`](./backend-plan.md) — has the full data model and every endpoint referenced below. Each frontend chunk lists the backend endpoints it needs; don't start a chunk until the corresponding backend PR has shipped.

Visual design source of truth: [`designs/project/Taskflow.dc.html`](../designs/project/Taskflow.dc.html), a working prototype covering Auth, App Shell, Dashboard, Task List, Task Detail, Settings, and the New Task modal (see [`designs/README.md`](../designs/README.md) for handoff notes). Tokens extracted from it are in [Section 2](#2-design-reference); each PR chunk below notes which of its screens the prototype already defines.

---

## 1. Architecture Overview

- **Build tool:** Vite + React + TypeScript
- **UI:** MUI v5 (Emotion), a small shared theme
- **Routing:** React Router
- **Server state:** TanStack Query (React Query) — no separate global store for API data
- **Forms:** React Hook Form + Zod resolvers
- **HTTP:** a single Axios instance with request/response interceptors (attach JWT, handle 401 → refresh)
- **Auth state:** a small `AuthContext` holding the current user + tokens, backed by `localStorage` for persistence across reloads

```
client/src/
  api/            # axios instance, per-resource api modules (authApi, tasksApi, ...)
  components/     # shared/dumb components
  features/
    auth/
    organization/
    tasks/
  routes/         # route components, ProtectedRoute / PublicRoute
  theme/
  App.tsx
```

---

## 2. Design Reference

Tokens and component conventions extracted from the Taskflow prototype (see link above). Encode these once as an MUI theme in PR-F0 rather than hand-styling each screen.

**Theme:** dark-only, no light-mode variant designed. `createTheme({ palette: { mode: 'dark' } })` as the base, overridden with the palette below.

**Colors**

| Token | Value | Usage |
|-------|-------|-------|
| `background.default` | `#09090c` | page background |
| `background.paper` | `#111117` | cards, tables, modals |
| Input / recessed surface | `#18181f` | text fields, selects, textareas |
| Border (default) | `rgba(255,255,255,0.07–0.09)` | hairline borders on cards/inputs |
| Border (hover) | `rgba(255,255,255,0.12)` | hover state on interactive cards |
| `primary.main` | `#6366f1` (indigo) | primary buttons, active state |
| `primary.light` | `#818cf8` | links, hover accents |
| Active nav text | `#a5b4fc` | selected sidebar item |
| Avatar gradient | `linear-gradient(135deg, #6366f1, #8b5cf6)` | user/assignee avatars |
| `text.primary` | `#f0f0f6` / `#e8e8f2` | headings, primary content |
| `text.secondary` | `#c8c8dc` / `#9898b0` | body copy, secondary labels |
| Muted text | `#71717a` / `#52526a` / `#3f3f52` | timestamps, meta, disabled |
| Success (`Done`) | `#22c55e` | status badge |
| Warning (`In Progress`) | `#f59e0b` | status badge, medium priority |
| Error (`High` priority, destructive actions) | `#ef4444` | priority label, delete buttons |

**Typography**

- Font: `DM Sans` (Google Fonts, weights 300/400/500/600), fallback `system-ui, sans-serif`
- Page titles: 21–23px / weight 600 / letter-spacing -0.4 to -0.5px
- Card/section headers: 13–14px / weight 600
- Body: 13.5–14px / weight 400
- Meta/small text: 11–12.5px
- Section eyebrow labels: 10.5–11px / weight 600 / uppercase / letter-spacing 0.6–0.7px / muted color

**Shape & spacing**

- Border radius: 7–8px (inputs, buttons, nav items), 10–14px (cards, modals)
- Card padding: ~20–24px
- Borders are low-opacity white overlays, not solid grays — keeps the dark surfaces feeling layered rather than outlined

**Shared component patterns to build once and reuse**

| Component | Pattern |
|-----------|---------|
| `StatusChip` | Pill, ~10% opacity background + full-opacity text in the status color. `To Do` = neutral gray, `In Progress` = amber, `Done` = green |
| `PriorityLabel` | Plain colored text, no pill. `Low` = muted gray, `Medium` = amber, `High` = red |
| `RoleBadge` | Small pill. `Admin` = indigo tint, `Member` = neutral gray |
| `UserAvatar` | Circular, gradient background, 2-letter initials |
| Primary button | Solid indigo, white text, 8px radius |
| Secondary/ghost button | Translucent white background, subtle border |
| Destructive button | Translucent red background/border, red text |
| Modal | Centered, backdrop blur, fade + slide-up entrance |

---

## 3. Frontend Build Plan

Each chunk is one PR, ordered so it never lands ahead of the backend endpoints it needs.

**Status: PR-F0 through PR-F13 are all shipped.** The core frontend is complete and deployed (see `docs/deployment-plan.md`). Only the [Nice to Have](#6-nice-to-have) items remain unbuilt.

### PR-F0 — Project scaffold ✅

- Vite + React + TS template
- MUI + Emotion installed, `ThemeProvider` configured with the palette/typography/shape tokens from [Section 2](#2-design-reference) (dark mode, DM Sans, indigo primary)
- ESLint + Prettier config, absolute imports via `tsconfig` paths
- Folder structure as above, empty placeholders

No screens, no backend dependency.

### PR-F1 — API client & env config ✅

- Axios instance, `baseURL` from `VITE_API_URL`
- Response interceptor: on `401`, attempt one silent refresh via `/api/auth/refresh`, then retry the original request; on failure, clear session and redirect to login
- TanStack Query `QueryClientProvider` set up at the app root
- `.env.example` documenting `VITE_API_URL`

**Depends on:** `GET /health` (smoke test only)

### PR-F2 — Auth screens & session management ✅

- Screens: **Register** (organization name + admin's own details), **Login** — the prototype's Auth screen defines both as a single centered card with a sign-in/register toggle link; build that as one `AuthScreen` component with local mode state, not two routes
- `AuthContext`: current user, `accessToken`/`refreshToken`, `login()`, `logout()`, tokens persisted to `localStorage`
- `ProtectedRoute` (redirects to `/login` if no session) and `PublicRoute` (redirects to `/tasks` if already logged in)
- On app load, if a token is present, call `GET /api/users/me` to hydrate the session before rendering protected routes

**Depends on:** `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`, `GET /api/users/me`

### PR-F3 — App shell & navigation ✅

- Layout: fixed left sidebar (per the prototype — logo + org name with a switcher affordance, nav items `Dashboard` / `Tasks` / `Settings`, user profile card pinned to the bottom with a logout menu), main content area to its right
- Route skeleton wired into React Router, wrapped in `ProtectedRoute`

**Depends on:** `GET /api/users/me` (already integrated in PR-F2, reused here for the app bar)

### PR-F4 — Organization settings screens ✅

- Inline "Invite team member" card (email field + role select + send button) above the members table, matching the prototype — shows an inline success state after sending rather than a separate confirmation screen
- Members table: avatar, name, email, `RoleBadge`, joined date
- Pending invitations list with a revoke action
- Role-change dropdown and "remove member" confirm dialog, both **Admin-only** (hidden/disabled for `Member` role, driven off `user.role` from `AuthContext`)

**Depends on:** `GET /api/organization`, `GET /api/organization/members`, `POST/GET/DELETE /api/organization/invitations`, `PATCH /api/organization/members/{id}/role`, `DELETE /api/organization/members/{id}`

### PR-F5 — Accept-invitation flow ✅

- Public screen at `/accept-invitation?token=...`: name + password form, submits and logs the new user straight in
- Handles expired/invalid token with a clear error state (no dead-end)

**Depends on:** `POST /api/auth/accept-invitation`

### PR-F6 — Task list screen ✅

- Table of tasks matching the prototype's columns: Title (+ project, if PR-N1 is in scope), Assignee (avatar + name), `StatusChip`, `PriorityLabel`, Due date
- Filter pill group (`All` / `To Do` / `In Progress` / `Done`) plus a search input, both client-side over the fetched page for now
- Pagination controls
- "New Task" button (opens the create modal from PR-F7)

**Depends on:** `GET /api/tasks`

### PR-F7 — Task create/edit form ✅

- Create and edit modal (same form component, different submit handler) — centered dialog with backdrop blur, matching the prototype's New Task modal: title field, description textarea, then a 3-column row of Assignee / Priority / Due date, Cancel + primary submit button pair
- Fields: title, description, status (edit only), priority select, assignee autocomplete (sourced from org members), due date picker
- React Hook Form + Zod validation matching backend constraints (title required/max length, etc.)

**Depends on:** `POST /api/tasks`, `PUT /api/tasks/{id}`, `GET /api/organization/members` (assignee options)

### PR-F8 — Task detail & comments ✅

- Dedicated route, two-column layout matching the prototype: left column has the title, status/priority/due-date summary line, description card, and the comment thread with an add-comment textarea; right column is a sticky metadata panel (Status, Priority, Assignee, Due Date) with Edit/Delete actions at the bottom
- Delete comment (visible to the comment's author or an Admin)
- Delete task action from the metadata panel

**Depends on:** `GET /api/tasks/{id}`, `GET/POST/DELETE /api/tasks/{taskId}/comments`, `DELETE /api/tasks/{id}`

### PR-F9 — Dashboard screen ✅

- Greeting header (user's first name + current date + org name)
- Three stat cards — To Do / In Progress / Completed counts, each clickable through to the Task List pre-filtered by that status
- "Open tasks" quick-view list (a handful of non-`Done` tasks, reusing the `StatusChip` from PR-F6) linking into Task Detail
- No activity feed in this chunk — see [Nice to Have: Activity Log](#nice-to-have-activity-log) for that
- Built after Task List and Task Detail so it can reuse their status/priority components and click-through navigation rather than duplicating them

**Depends on:** `GET /api/tasks` (same endpoint as PR-F6, just a smaller/filtered slice)

### PR-F10 — UX polish ✅

- Loading skeletons, empty states ("no tasks yet"), toast notifications (success/error) via MUI `Snackbar`
- A generic error boundary + 404 route
- Consistent display of backend validation errors on forms (mapping `ProblemDetails` field errors to React Hook Form)

**Depends on:** nothing new — hardening pass across PR-F2 through PR-F9

### PR-F11 — Playwright e2e setup & core flows ✅

- Playwright config, running against a locally-started build (or the deployed preview URL)
- Fixtures: seed a fresh org/user per test run via the real API (no mocking — these are true end-to-end tests)
- Specs: register → login, invite → accept invitation, task CRUD, add/delete comment, and the cross-tenant check (two separately-seeded orgs, confirm org A never sees org B's tasks in the UI)

**Depends on:** the full stack — run last, after PR-F10

### PR-F12 — Vercel deployment config ✅

- `vercel.json`: SPA rewrite (`/* → /index.html`) so React Router routes don't 404 on refresh
- Environment variables configured per Vercel environment (Production / Preview)
- Framework preset: Vite

No new screens.

### PR-F13 — GitHub Actions CI (frontend) ✅

See [Section 5](#5-github-actions-cicd) below.

---

## 4. Deployment Notes — Vercel

- **Framework preset:** Vite
- **Build command:** `npm run build` (root: `client/` if monorepo)
- **Output directory:** `dist`
- **Routing:** `vercel.json` rewrite so all paths serve `index.html` (client-side routing)
- **Preview deployments:** automatic per-PR via Vercel's GitHub integration — useful for manually reviewing each frontend PR against the live Render backend

**Environment variables**

| Variable | Purpose |
|----------|---------|
| `VITE_API_URL` | Base URL of the backend API (Render URL in Production, same or a staging URL in Preview) |

---

## 5. GitHub Actions CI/CD

Path-filtered to `client/**` so backend-only PRs don't trigger a Node build. See [`backend-plan.md`](./backend-plan.md#5-github-actions-cicd) for the backend pipeline.

**`.github/workflows/frontend-ci.yml`**

On every pull request touching `client/**`:
1. Checkout, `actions/setup-node`
2. `npm ci`
3. `npm run lint` (ESLint) and `npm run format:check` (Prettier)
4. `tsc --noEmit` (type-check gate)
5. `npm run build` (catches build-time errors before merge)
6. `npx playwright test` — run against the deployed staging/Render URL, or a docker-composed backend+db if e2e needs full isolation
7. `gitleaks` secret scan (shared with the backend workflow, or a single repo-wide job that isn't path-filtered)

On merge to `main`:
- Vercel's GitHub integration deploys automatically on push — no custom Action step is required for this. If deploys should instead be gated behind CI passing rather than firing independently, replace the Vercel git integration with a GitHub Actions job that runs `vercel deploy --prod` using the Vercel CLI and a `VERCEL_TOKEN` secret.

---

## 6. Nice to Have

Present in the design prototype but out of the core PR sequence above — pick these up post-MVP, once PR-F0–F13 (and their backend counterparts) have shipped. Each depends on its matching backend chunk in [`backend-plan.md`](./backend-plan.md#6-nice-to-have) shipping first.

**Status: not started.** Neither NH-F1 nor NH-F2 has any code in `client/src` yet, and both are blocked on their backend counterparts.

### NH-F1 — Projects

- Add a project switcher/list to the sidebar (color-coded dot + name, per the prototype)
- Project select field added to the task create/edit modal (PR-F7)
- Project shown as a small tag on task rows (PR-F6) and on the task detail header (PR-F8)
- Filter the Task List by project

**Depends on:** the backend Projects endpoints (`backend-plan.md` NH-B1)

### NH-F2 — Activity Log {#nice-to-have-activity-log}

- Wire the Dashboard's "Recent activity" feed (PR-F9) up to real data — avatar, actor name, action text, relative timestamp, per the prototype
- Optionally surface a filtered activity feed on the Task Detail screen ("history" for that task)

**Depends on:** the backend Activity Log endpoint (`backend-plan.md` NH-B2)
