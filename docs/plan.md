# Taskflow — Mobile-Responsive & Feature Plan

Single ordered build plan covering two things together: making the existing React/MUI app responsive down to phone-width viewports, and adding the two features the design prototype called out but the original build plan deferred (Projects, Activity Log). They're interleaved deliberately — each new feature is built against its screen's *already-responsive* version, so nothing gets built for desktop and redone for mobile later.

Supersedes `docs/legacy/backend-plan.md` and `docs/legacy/frontend-plan.md` (both fully shipped — PR-B0–B12, PR-F0–F13) and folds in their unbuilt "Nice to Have" sections (NH-B1/NH-B2, NH-F1/NH-F2). `docs/legacy/deployment-plan.md` is unaffected by this plan — the Render/Vercel/Neon setup and CI-gated tag-release flow it describes still stands as-is.

---

## 1. Current state (baseline)

No responsive handling exists anywhere in `client/src` today — confirmed by grep, zero hits on `useMediaQuery`, `theme.breakpoints`, `Drawer`, `useTheme`. Specifically:

| Area | File | Current layout |
|---|---|---|
| App shell | `client/src/routes/AppShell.tsx` | Fixed `width: 224` sidebar (`<aside>`), no collapse behavior at any width |
| Task List | `client/src/features/tasks/TaskListScreen.tsx` | Hand-rolled CSS Grid table, fixed `gridColumns = '1fr 148px 118px 90px 100px'` |
| Task Detail | `client/src/features/tasks/TaskDetailScreen.tsx` | Fixed two-column CSS Grid (`1fr 248px`), sticky metadata panel |
| Dashboard | `client/src/features/tasks/DashboardScreen.tsx` | Fixed 3-up stat card grid (`repeat(3, 1fr)`) |
| Org Settings | `client/src/features/organization/OrganizationSettingsScreen.tsx` | Hand-rolled CSS Grid members table, fixed-width invitation rows |
| Task form | `client/src/features/tasks/TaskFormModal.tsx` | `Dialog maxWidth="sm"`, fixed 3-column field row |
| Theme | `client/src/theme/theme.ts` | `createTheme()` with no `breakpoints` override — default MUI breakpoints (`xs:0, sm:600, md:900, lg:1200, xl:1536`) exist but are never referenced |

Every screen above is a rework target, not a from-scratch build — this plan is a retrofit plus two additive features.

---

## 2. Mobile design reference

**Breakpoint strategy** — use MUI's default breakpoints, no custom values needed:

| Range | Treatment |
|---|---|
| `xs` (< 600px, phone) | Sidebar becomes a bottom nav bar (Dashboard / Tasks / Settings) or a full-height overlay drawer opened via hamburger — pick one in PR-M1, see options there. Tables become stacked cards. Dialogs go `fullScreen`. Metadata panels move from sticky-sidebar to inline sections below content. |
| `sm` (600–900px, tablet portrait) | Sidebar collapses to an icon-only rail or overlay drawer (reuse the `xs` drawer if that's the PR-M1 choice). Tables still reflow to cards — 148px/118px/90px fixed columns don't have room. Two-column screens (Task Detail) stack. |
| `md`+ (≥ 900px) | Current desktop layout, unchanged. |

**New shared pieces to build once (PR-M0), reuse everywhere:**

| Piece | Purpose |
|---|---|
| `useIsMobile()` hook (`theme.breakpoints.down('sm')` via `useMediaQuery`) | Single source of truth for the mobile/desktop branch in every screen below |
| `ResponsiveDataGrid`-style pattern (not a new component necessarily — a convention) | Each hand-rolled CSS-grid table gets a card-list rendering path for `xs`/`sm`, keeping the same row data and click-through behavior |

No new visual design tokens — colors, typography, spacing all carry over from the existing theme (`client/src/theme/theme.ts`). This is a layout retrofit, not a redesign.

---

## 3. Build plan

Each chunk is one PR. Chunks are ordered — later chunks depend on earlier ones. Prefix: `PR-M<N>`.

### PR-M0 — Responsive foundations ⬜

- `useIsMobile()` hook in `client/src/hooks/` wrapping `useMediaQuery(theme.breakpoints.down('sm'))`
- Audit `theme.ts`: confirm default breakpoints are fine as-is (no widths in the current design need custom values); document the `xs`/`sm`/`md` treatment table above as a code comment or short doc if useful
- No visible UI change — this is scaffolding the rest of the plan depends on

### PR-M1 — Responsive app shell & navigation ⬜

- `AppShell.tsx` rework: below `sm`, replace the fixed `<aside>` with either (a) an MUI `BottomNavigation` bar (Dashboard/Tasks/Settings, matches the prototype's 3 nav items exactly) or (b) an overlay `Drawer` behind a hamburger in a new top app bar — **decide before starting**, bottom nav is usually the better fit for a 3-item nav on phone, drawer is more scalable if PR-M6 (Projects) needs a 4th nav-adjacent entry (project switcher)
- User profile menu (currently pinned to sidebar bottom) needs a mobile home — likely an avatar in a new top app bar that only renders below `md`
- `md`+ behavior unchanged

**Depends on:** PR-M0

### PR-M2 — Responsive Task List ⬜

- Below `sm`: replace the CSS-grid table rows with stacked cards — title + `StatusChip` + `PriorityLabel` + assignee avatar + due date, same data, same click-through to Task Detail
- Filter pills and search input: confirm they wrap/scroll sensibly at phone width (horizontal scroll strip is fine, matches common mobile table-filter patterns)
- Pagination controls: simplify to prev/next + page indicator below `sm` (numbered page list doesn't fit)

**Depends on:** PR-M0

### PR-M3 — Responsive Task Detail ⬜

- Below `md`: the `1fr 248px` two-column grid stacks to one column — metadata panel (Status/Priority/Assignee/Due Date/Edit/Delete) moves from sticky-right to an inline card directly under the title block, above the description
- Comment thread and add-comment textarea: full-width, unchanged otherwise

**Depends on:** PR-M0

### PR-M4 — Responsive Task Form modal & dialogs ⬜

- `TaskFormModal.tsx`: `Dialog` goes `fullScreen` below `sm` (standard MUI pattern — `fullScreen={isMobile}`); the fixed 3-column Assignee/Priority/Due-date row stacks to one column below `sm`
- Confirm dialogs (delete task, delete comment, remove member) in `TaskDetailScreen.tsx` / `OrganizationSettingsScreen.tsx`: no fixed-width issues expected, verify at phone width and adjust only if clipped

**Depends on:** PR-M0

### PR-M5 — Backend: Projects endpoints ⬜

Carried over unchanged from `docs/legacy/backend-plan.md` NH-B1.

**Data model addition** — `projects` table:

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | uuid | PK | |
| organization_id | uuid | FK → organizations.id, not null, indexed | |
| name | varchar(100) | not null | |
| color | varchar(7) | not null | Hex color for the sidebar dot/tag |
| created_at | timestamptz | not null, default now() | |

`tasks` gains: `project_id uuid, FK → projects.id, nullable` (a task may be unassigned to any project).

**Endpoints**

| Method | Route | Request body | Response |
|--------|-------|---------------|----------|
| GET | `/api/projects` | — | `200 [{ id, name, color }]` |
| POST | `/api/projects` *(Admin)* | `{ name, color }` | `201 { id, name, color }` |
| DELETE | `/api/projects/{id}` *(Admin)* | — | `204` — tasks referencing it fall back to `project_id = null` |

`POST /api/tasks` and `PUT /api/tasks/{id}` gain an optional `projectId` field.

**Depends on:** nothing new — builds on the existing PR-B7 task endpoints

### PR-M6 — Frontend: Projects, built responsive from the start ⬜

Built directly against the responsive shell/list/detail/form from PR-M1–M4 — no separate mobile pass needed. The design bundle (`designs/project/Taskflow.dc.html`, updated 2026-07-23) upgraded Projects from a sidebar-only filter into a full top-level screen — this chunk's scope grew to match; it's no longer just "a switcher in the sidebar."

- **Backend follow-up (small, do first in this chunk)**: PR-M5 shipped before the design added a `description` field to projects — `Project.cs`, the `AddProjects` migration, `CreateProjectRequest`, and `ProjectResponse` are all `{ name, color }`-only today (confirmed by reading the current code). Add: nullable `Description` (string?) on `Project.cs`; a new EF Core migration adding nullable `description varchar(280)` to `projects`; optional `Description` on `CreateProjectRequest`/`ProjectResponse`. `tasks.project_id` wiring needs no changes — already fully in place from PR-M5
- **Projects nav destination**: `Projects` becomes a 4th top-level nav item (Dashboard/Tasks/Projects/Settings) — PR-M1 shipped a 3-item nav (Dashboard/Tasks/Settings) before this design update, so this chunk extends `AppShell.tsx`'s bottom-nav/drawer (whichever PR-M1 picked) to carry a 4th item rather than folding Projects into a drawer-only switcher
- **New `ProjectsScreen.tsx`** (`client/src/features/projects/`): grid of project cards (desktop `repeat(3, 1fr)`, per the prototype). Each card shows the color swatch, name, description, task-count stats (total / in-progress / done), a progress bar (% of the project's tasks marked done), up to 3 assignee avatars, and click-through to Task List filtered by that project. Stats are computed client-side by filtering the already-fetched task list per project — no new backend stats endpoint needed, mirrors the prototype's `renderVals()` approach. Below `sm`/`md` the card grid stacks to a single column, same convention as PR-M2's task cards
- **Sidebar Projects section** (existing per-org list in `AppShell.tsx`, desktop `md`+ only): gains a task-count badge per row and a "+" icon-button that opens the New Project modal directly (in addition to the Projects screen's own "New project" button). Clicking a sidebar project row filters Task List by that project — same behavior as clicking a project card. Below `md` there's no persistent sidebar to hold this list (per PR-M1) — mobile users reach the same thing via the Projects nav item's card grid, which already has its own "New project" button and click-through, so nothing further to build for mobile here
- **New Project modal**: `Dialog` goes `fullScreen` below `sm`, same as `TaskFormModal.tsx` (PR-M4). Fields: name, optional description (needs the backend follow-up above), and a fixed 6-swatch color picker (`#6366f1 #22d3ee #f59e0b #22c55e #f43f5e #a78bfa`, per the prototype) rather than a free-form hex input
- **Task List heading changes when project-filtered** (extends the existing filter-pill pattern): heading switches from "Tasks" to the project name, and the subheading shows the filtered count + project name instead of the org-wide total — mirrors the prototype's `tasksHeading`/`tasksSubheading` logic
- Project select field in `TaskFormModal.tsx` (already stacks correctly below `sm` from PR-M4)
- Project tag on Task List rows (PR-M2's card layout) and Task Detail header (PR-M3's stacked layout)
- `Project`/`TaskListItem`/`TaskDetail` TS interfaces in `client/src/api/` gain `description` / `projectId` / `projectId` respectively

**Depends on:** PR-M5, PR-M1, PR-M2, PR-M3, PR-M4

### PR-M7 — Responsive Dashboard ⬜

- Below `sm`: the 3-up stat card grid (`repeat(3, 1fr)`) stacks to a single column (or a horizontal scroll strip if 3 cards side-by-side at reduced size still reads fine — decide by eyeballing on a real phone viewport)
- "Open tasks" quick-view list: already list-shaped, should carry over with minor spacing adjustments only

**Depends on:** PR-M0

### PR-M8 — Backend: Activity Log endpoint ⬜

Carried over unchanged from `docs/legacy/backend-plan.md` NH-B2.

**Data model addition** — `activity_log` table:

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| id | uuid | PK | |
| organization_id | uuid | FK → organizations.id, not null, indexed | |
| actor_id | uuid | FK → users.id, not null | |
| task_id | uuid | FK → tasks.id, nullable, on delete set null | Null for org-level events (e.g. member invited) |
| type | enum | not null — `TaskCreated` \| `TaskStatusChanged` \| `TaskAssigned` \| `CommentAdded` \| `MemberInvited` \| ... | |
| summary | text | not null | Precomputed display string, avoids reconstructing sentences client-side |
| created_at | timestamptz | not null, default now() | |

Written by the relevant Application-layer service (e.g. `TaskService.UpdateStatusAsync` also inserts an `activity_log` row) rather than via a generic event bus.

**Endpoints**

| Method | Route | Request | Response |
|--------|-------|---------|----------|
| GET | `/api/activity` | query: `limit=20` | `200 [{ id, actorId, actorName, taskId, type, summary, createdAt }]` — org-scoped, most recent first |

**Depends on:** nothing new — builds on existing tenant/task infrastructure

### PR-M9 — Frontend: Activity Log, built responsive from the start ⬜

Built directly against the responsive Dashboard (PR-M7) and Task Detail (PR-M3) — no separate mobile pass needed.

- Dashboard "Recent activity" feed: avatar, actor name, action text, relative timestamp, per the prototype. Reuses PR-M7's stacked/scroll layout at phone width automatically since it's just another list section
- Optional filtered activity feed on Task Detail ("history" for that task) — placed in the stacked-metadata area from PR-M3

**Depends on:** PR-M8, PR-M7, PR-M3

### PR-M10 — Responsive Organization Settings ⬜

- Below `sm`: members table (hand-rolled CSS grid, `memberGridColumns`) becomes stacked cards — avatar, name, email, `RoleBadge`, joined date, actions menu
- Pending invitations list (currently fixed-width flex row) reflows to stacked cards at the same breakpoint
- "Invite team member" inline card: form fields stack to one column below `sm`

**Depends on:** PR-M0

### PR-M11 — Mobile e2e coverage ⬜

- Extend the existing Playwright setup (`client/tests/`, reworked from legacy PR-F11's real-backend e2e suite into mocked-backend request-contract tests) with a mobile viewport project (Playwright's built-in device presets, e.g. `Pixel 5` or a plain `375x667` viewport)
- Re-run the core flows (task CRUD, org settings, accept-invitation) against the mobile viewport project — not full duplication, just the specs that touch the reworked screens (List, Detail, Dashboard, Settings, Task form)
- Wire into `frontend-ci.yml` as an additional `playwright test --project=mobile` step (or matrix entry)

**Depends on:** PR-M1 through PR-M10 (needs the full responsive surface to test against)

### PR-M12 — Dependabot for backend & frontend dependencies ⬜

- Add `.github/dependabot.yml` with two update configs:
  - `package-ecosystem: nuget`, `directory: /server`, weekly schedule — covers the ASP.NET Core backend's NuGet packages
  - `package-ecosystem: npm`, `directory: /client`, weekly schedule — covers the React frontend's npm packages
- Group minor/patch updates per ecosystem to avoid a separate PR per package; leave major version bumps ungrouped so they surface individually for review
- Cap `open-pull-requests-limit` (e.g. 10) per ecosystem to avoid flooding
- No code changes — config-only, runs against existing `backend-ci.yml`/`frontend-ci.yml` checks on the PRs it opens
- Set `target-branch: main` explicitly — Dependabot only operates via PRs (no direct-commit mode), which is a deliberate exception to this repo's normal direct-to-`main` convention (see root `CLAUDE.md` § Working conventions). Each dependency-update PR needs manual review and merge; nothing lands automatically

**Depends on:** nothing — independent of the responsive/Projects/Activity Log work, can land anytime

---

## 4. Deployment

No changes needed. `docs/legacy/deployment-plan.md` still describes the live Render/Vercel/Neon setup and the tag-triggered, CI-gated release flow accurately — this plan only touches `client/` (and `server/` for PR-M5/PR-M8's new endpoints), not infrastructure.
