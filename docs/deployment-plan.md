# Taskflow — Deployment Plan

Free-tier deployment: Render (backend), Vercel (frontend), Neon (Postgres).

Companion documents: [`backend-plan.md`](./backend-plan.md) §4 and [`frontend-plan.md`](./frontend-plan.md) §4 cover the Render/Vercel config each PR (PR-B11, PR-F12) added to the repo. This doc is the step-by-step runbook for standing that config up, plus the database choice those docs left open.

---

## 1. Prerequisites

- Repo pushed to GitHub (Render and Vercel both deploy via GitHub integration)
- `render.yaml` (repo root), `server/Dockerfile`, and `client/vercel.json` already committed — nothing to write, only accounts to create and env vars to fill in

---

## 2. Database — Neon (Postgres)

Render's own free Postgres is deleted after 90 days. Neon's free tier has no expiry, so use it instead.

1. Create a project at [neon.tech](https://neon.tech)
2. Copy the pooled connection string: `postgresql://user:pass@host/dbname?sslmode=require`
3. Hold onto it for §3 (`ConnectionStrings__Default`)

---

## 3. Backend — Render (free Web Service)

`render.yaml` defines a Docker web service on the free plan, built from `server/Dockerfile`, health-checked at `/health`. `autoDeploy` is set to `false` — Render does not watch pushes itself. Deploys are instead triggered by `backend-ci.yml`'s `deploy` job, which only fires after `build-and-test` and `gitleaks` pass (see §7).

1. Render dashboard → **New → Blueprint** → select the repo. Render reads `render.yaml` automatically.
2. Fill in the env vars marked `sync: false`:

   | Variable | Value |
   |---|---|
   | `ConnectionStrings__Default` | Neon connection string from §2 |
   | `Jwt__Secret` | random secret, e.g. `openssl rand -base64 32` |
   | `Cors__AllowedOrigins` | Vercel URL from §4 (comes second — see §5) |

3. Deploy manually once from the dashboard to get the initial build live. Confirm `https://<service>.onrender.com/health` returns healthy.
4. Service → **Settings → Deploy Hook** → copy the URL. In the GitHub repo, add two Actions secrets:

   | Secret | Value |
   |---|---|
   | `RENDER_DEPLOY_HOOK_URL` | the deploy hook URL from this step |
   | `RENDER_HEALTH_URL` | the service's base URL, e.g. `https://<service>.onrender.com` |

   Without these, `backend-ci.yml`'s `deploy` job fails on the next push to `main`.

**Free-tier caveat:** the service spins down after 15 minutes idle and cold-starts (~30-50s) on the next request. Expected behavior, not a bug.

---

## 4. Frontend — Vercel (free Hobby plan)

`client/vercel.json` has the SPA rewrite (`/* → /index.html`) so React Router routes survive a refresh. It also sets `git.deploymentEnabled.main` to `false` — Vercel's GitHub integration still builds preview deployments for PRs/other branches, but won't auto-deploy `main` to Production on push. Production deploys are instead triggered by `frontend-ci.yml`'s `deploy` job, which only fires after `build-and-test` and `gitleaks` pass (see §7).

1. Import the repo in Vercel, set **root directory** to `client/`
2. Framework preset: Vite (auto-detected)
3. Env var: `VITE_API_URL` = the Render backend URL from §3
4. Deploy manually once from the dashboard to get the initial build live. Copy the resulting `*.vercel.app` URL.
5. Project → **Settings → General** → copy the **Project ID**, and org/team → **Settings** → copy the **Team ID** (this is the org ID; for a personal account use the account ID shown there instead). Then **Account → Settings → Tokens** → create a token scoped to this project. In the GitHub repo, add three Actions secrets:

   | Secret | Value |
   |---|---|
   | `VERCEL_TOKEN` | the token just created |
   | `VERCEL_ORG_ID` | the Team/account ID |
   | `VERCEL_PROJECT_ID` | the Project ID |

   Without these, `frontend-ci.yml`'s `deploy` job fails on the next push to `main`.

---

## 5. Wire CORS

Backend and frontend each need the other's URL, so this is necessarily a two-pass setup:

1. Deploy backend (§3) → get Render URL → set as `VITE_API_URL` in Vercel (§4)
2. Deploy frontend (§4) → get Vercel URL → set as `Cors__AllowedOrigins` in Render (§3) → redeploy backend

---

## 6. Verify

- `GET https://<render-url>/health` → healthy
- Load the Vercel URL, register an organization, confirm requests succeed (check the Network tab for CORS errors if not — usually means step 5.2 wasn't done or the backend hasn't redeployed since)

---

## 7. Ongoing deploys

Both sides are now CI-gated the same way — neither platform's native GitHub integration is allowed to deploy `main` to production on its own:

- **Backend:** `render.yaml` has `autoDeploy: false`. `backend-ci.yml`'s `deploy` job (`needs: [build-and-test, gitleaks]`) runs only on push to `main`, and only after both jobs pass — it then POSTs `RENDER_DEPLOY_HOOK_URL` and polls `RENDER_HEALTH_URL/health` until the new deploy is live.
- **Frontend:** `client/vercel.json` has `git.deploymentEnabled.main: false`. `frontend-ci.yml`'s `deploy` job (`needs: [build-and-test, gitleaks]`) runs only on push to `main`, and only after both jobs pass — it then runs `vercel build --prod` + `vercel deploy --prebuilt --prod` using `VERCEL_TOKEN`/`VERCEL_ORG_ID`/`VERCEL_PROJECT_ID`.

In both cases a failing test, lint, type-check, or secret scan blocks the production deploy outright. PR branches still get Vercel preview deployments as before — only `main` → Production is gated.
