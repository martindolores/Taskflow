---
name: query-taskflow-db
description: Query the TaskFlow Postgres database running in Docker. Use when asked to inspect data, check table contents, count rows, describe schema, or run ad-hoc SQL against the local database.
---

TaskFlow's local Postgres runs as the `postgres` service in `docker-compose.yml` (container `server-postgres-1`, db `taskflow`, user `taskflow`). There's no `psql` on the host — query it through the container via `.claude/skills/query-taskflow-db/query.sh`, which wraps `docker compose exec`.

All paths below are relative to `server/` (the repo's backend unit).

## Prerequisites

Postgres must be running:

```bash
docker compose up -d
```

## Run (agent path)

```bash
.claude/skills/query-taskflow-db/query.sh "select * from users limit 5;"
```

Add `--csv` before the query for machine-parsable output:

```bash
.claude/skills/query-taskflow-db/query.sh --csv "select table_name from information_schema.tables where table_schema='public';"
```

Or pipe multi-line SQL via stdin (omit the query argument):

```bash
.claude/skills/query-taskflow-db/query.sh <<'SQL'
select relname, n_live_tup from pg_stat_user_tables order by relname;
SQL
```

Useful queries:

| Query | What it shows |
|---|---|
| `\dt` | List tables |
| `\d <table>` | Describe a table's columns, indexes, foreign keys |
| `select relname, n_live_tup from pg_stat_user_tables order by relname;` | Approximate row counts per table (fast, no full scan) |
| `select count(*) from <table>;` | Exact row count |

Current tables: `organizations`, `users`, `invitations`, `refresh_tokens`, `tasks`, `task_comments`, `__EFMigrationsHistory`.

## Run (human path)

```bash
docker compose exec postgres psql -U taskflow -d taskflow
```
Drops into an interactive `psql` shell (omit `-T` since a TTY is attached). `\q` to exit.

---

## Gotchas

- **No host `psql`.** Don't reach for a locally installed `psql` client — this environment doesn't have one; everything goes through `docker compose exec` into the container, which does have it (bundled with the `postgres:16-alpine` image).
- **This is the docker-compose Postgres, not the test database.** Integration tests (`dotnet test`) spin up their own ephemeral Postgres via Testcontainers — querying this instance won't show test data, and vice versa.
- **Database currently empty except migrations history** — as of this writing all app tables have 0 rows (`__EFMigrationsHistory` has 1, tracking the applied migration). Don't be surprised by empty result sets; it means no seed data, not a broken query.

## Troubleshooting

- **`service "postgres" is not running`**: run `docker compose up -d` first.
- **Connection refused / container not found**: confirm you're in `server/` — `docker compose` resolves services relative to the `docker-compose.yml` in the current directory.
