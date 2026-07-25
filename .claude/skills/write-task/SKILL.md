---
name: write-task
description: Turn a task being discussed in conversation into a GitHub issue on the Taskflow project board. Use when the user says to write up, log, file, add, or create a task/issue for something just discussed.
---

Taskflow tracks work on a GitHub Project board, not in `docs/` files (see root `CLAUDE.md`).

- Board: https://github.com/users/martindolores/projects/1 (owner `martindolores`, project number `1`)
- Repo: `martindolores/Taskflow`
- New items land in the "Todo" column automatically — no status field to set explicitly.

## Steps

0. **Failsafe — if the task isn't clear from the conversation, ask.** Don't guess at scope, title, or implementation details from a vague mention. If you can't confidently draft a real body (concrete enough to match the depth of #17–21), stop and ask the user to describe the task directly rather than inventing plausible-sounding details.

1. **Figure out if this is one task or several.** If the user described multiple distinct pieces of work (e.g. "do X, then Y needs X, then Z"), split them into separate issues rather than one giant one — matching how the invite-email work is chunked (PR-E0…PR-E4) rather than filed as a single issue.

2. **Draft each issue from the conversation**, not from a one-line paraphrase. Match the depth of the existing board issues (e.g. #17–21):
   - **Title** — short, imperative. If this task is the next chunk in an existing lettered sequence (grep open/closed issues for the prefix, e.g. `PR-E`), continue that numbering; otherwise use a plain descriptive title.
   - **Body** — include: the *why*/context if it came up in discussion, concrete implementation bullets (files, endpoints, behavior — whatever was actually decided, not generic filler), and anything explicitly called out as out of scope. Don't pad with boilerplate sections that have no real content.
   - **Dependencies** — if this task depends on another one from the same batch (or an existing open issue), note that in the body prose (e.g. "builds on #<n>" once known), but record the actual dependency as a native GitHub issue relationship (step 6), not as a separate `**Depends on:**` line. Since issue numbers only exist after creation, create issues in dependency order (earliest prerequisite first) so later issues can reference and link to the real number of the one(s) they depend on.

3. **Show the drafted title + body for every issue and confirm** before creating anything — this writes to a shared system (real GitHub issues), so a quick "does this look right?" beats creating something that immediately needs editing. Skip this only if the user has already dictated the exact title/body themselves.

4. **Create each issue, in dependency order:**
   ```bash
   gh issue create --repo martindolores/Taskflow --title "<title>" --body-file <tmp-file>
   ```
   Use `--body-file` (write the draft to a scratch file first) for anything with code blocks or tables — avoids shell-escaping problems.

5. **Add each to the board:**
   ```bash
   gh project item-add 1 --owner martindolores --url <issue-url>
   ```

6. **Link dependencies using GitHub's native issue relationship** (not a text convention) — for each issue that depends on another from this batch (or an existing issue), mark it *blocked by* the prerequisite:
   ```bash
   gh issue edit <dependent-issue-number> --add-blocked-by <prerequisite-issue-number>
   ```
   This is the relationship visible in the issue sidebar/board (distinct from parent/sub-issue). Do this after both issues in the pair exist.

7. **Report back** the issue URL(s) and confirm they landed in Todo, noting the dependency chain if there was one.

## Notes

- `gh` needs the `project`/`read:project` scopes for step 5 — already granted on this machine. If it errors with a missing-scope message, the fix is `gh auth refresh -s project -s read:project`.
- Don't create duplicate issues — if the task sounds like it overlaps an existing open card, ask whether to update that one instead (`gh issue edit <n> --body-file <tmp-file>`) rather than filing a new one.
