---
name: deploy
description: Cut a tagged production release for Taskflow — bump the version, commit, tag, and push to trigger the Render/Vercel deploy pipeline. Use when the user asks to deploy, release, ship, or cut a new version.
---

Taskflow deploys are triggered by pushing a `v*` git tag, not by merging to `main` (see `RUNBOOK.md` §7). `backend-ci.yml` and `frontend-ci.yml` each have a `deploy` job gated on `github.ref_type == 'tag'` plus `build-and-test`/`gitleaks` passing; one tag push deploys backend (Render) and frontend (Vercel) together.

## Steps

1. **Determine the current version.** Read the `version` field in `client/package.json` and cross-check the latest tag with `git tag -l 'v*' --sort=-v:refname | head -5`. `client/package.json` is the only versioned file in the repo today — there's no app-level `<Version>` in the backend `.csproj` files.
2. **Ask the user what the new version should be** — show them the current version/tag so they can pick the bump (patch/minor/major). Don't decide it yourself.
3. **Update version info:**
   - `client/package.json` → `version` field.
   - Grep the repo for the *current* version string (excluding `node_modules/`, `bin/`, `obj/`) to catch anything else that moves in lockstep, e.g. `client/package-lock.json`'s top-level `version` fields. Update those too. Use judgment — a coincidental match inside an unrelated dependency's version number is not a hit.
4. **Stop for review before committing.** This user does not want commits made before they've reviewed the diff — show `git diff` and wait for explicit go-ahead. Do not push on the same go-ahead as the commit; pushing the tag is the step that actually fires the production deploy on Render + Vercel, and needs its own separate confirmation since it's a real, hard-to-reverse, shared-system action.
5. **Once the diff is approved, commit:**
   ```
   git add client/package.json <any other updated files>
   git commit -m "Bump version to vX.Y.Z"
   ```
   No `Co-Authored-By` trailer on the commit.
6. **Create the tag** on the new commit:
   ```
   git tag vX.Y.Z
   ```
   Tags in this repo so far are lightweight (no `-m`) — match that convention unless asked for an annotated tag.
7. **Get explicit confirmation, then push** — this is what triggers the real deploy:
   ```
   git push origin main
   git push origin vX.Y.Z
   ```
8. **Report back** where to watch it land: the repo's GitHub Actions tab (`backend-ci.yml` / `frontend-ci.yml` deploy jobs), then the Render health URL and Vercel production URL once those jobs go green.

## Notes

- Version format is bare semver with a `v` prefix (`v0.1.0`), matching the existing tag.
- If `RENDER_DEPLOY_HOOK_URL` / `RENDER_HEALTH_URL` / `VERCEL_TOKEN` / `VERCEL_ORG_ID` / `VERCEL_PROJECT_ID` secrets aren't set on the repo, the tag push still succeeds but the `deploy` jobs fail — that's one-time setup covered in `RUNBOOK.md` §3–4, not something this skill fixes.
