# Oracle Playground — EricksonLopez.SqlBuilder

Run a full live demo against **Oracle Free 23c** using Docker.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- .NET 10 SDK

## Quick Start

```bash
# 1. Start the database
docker compose up -d

# 2. Wait ~30s for Oracle to initialize completely, then run the demo
dotnet run

# 3. Stop when done
docker compose down -v
```

## What This Demo Shows

| # | Feature | SQL Generated |
|---|---------|--------------|
| 1 | Basic SELECT + WHERE | `SELECT ... FROM "customers" WHERE ...` |
| 2 | FormattableString params | `WHERE price BETWEEN :p0 AND :p1` |
| 3 | INNER JOIN | `INNER JOIN "customers" "c"` |
| 4 | GROUP BY + aggregate | `GROUP BY status` |
| 5 | Pagination (Oracle 12c+) | `OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY` |
| 6 | CTE | `WITH "customer_revenue" AS (...)` |
| 7 | MERGE INTO | `MERGE INTO "customers" USING (...) ON ...` |
| 8 | UPDATE | `UPDATE "customers" SET "name" = :p0 WHERE ...` |
| 9 | DELETE | `DELETE FROM "customers" WHERE ...` |
| 10 | Transaction | Atomic INSERT + items commit/rollback |

## Connection Details

| Property | Value |
|----------|-------|
| Host | localhost |
| Port | 1522 |
| Service Name | FREEPDB1 |
| Username | SYSTEM |
| Password | Demo@SqlB1! |
