# PostgreSQL Playground — EricksonLopez.SqlBuilder

Run a full live demo against **PostgreSQL 16** using Docker.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- .NET 10 SDK

## Quick Start

```bash
# 1. Start the database
docker compose up -d

# 2. Wait ~5s for PostgreSQL to initialize, then run the demo
dotnet run

# 3. Stop when done
docker compose down -v
```

## What This Demo Shows

| # | Feature | SQL Generated |
|---|---------|--------------|
| 1 | Basic SELECT + WHERE | `SELECT ... FROM "customers" WHERE ...` |
| 2 | FormattableString params | `WHERE price BETWEEN $1 AND $2` |
| 3 | INNER JOIN | `INNER JOIN "customers" AS "c"` |
| 4 | GROUP BY + HAVING | `GROUP BY status HAVING SUM(...) > $1` |
| 5 | Pagination | `LIMIT 5 OFFSET 5` |
| 6 | CTE | `WITH customer_revenue AS (...)` |
| 7 | INSERT + RETURNING | `INSERT INTO ... RETURNING id, name, ...` |
| 8 | UPDATE | `UPDATE "customers" SET "name" = $1 WHERE ...` |
| 9 | UPSERT | `ON CONFLICT (email) DO UPDATE SET ...` |
| 10 | DELETE | `DELETE FROM "customers" WHERE ...` |
| 11 | Subquery | `WHERE id NOT IN (SELECT ...)` |
| 12 | Transaction | Atomic INSERT + items commit/rollback |

## Connection Details

| Property | Value |
|----------|-------|
| Host | localhost |
| Port | 5433 |
| Database | sqlbuilder_demo |
| Username | demo |
| Password | Demo@SqlB1! |
