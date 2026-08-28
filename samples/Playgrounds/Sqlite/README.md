# SQLite Playground — EricksonLopez.SqlBuilder

Run a full live demo against **SQLite (In-Memory)**. No Docker is required for this demo.

## Prerequisites

- .NET 10 SDK

## Quick Start

```bash
# Run the demo directly (database is created in-memory during execution)
dotnet run
```

## What This Demo Shows

| # | Feature | SQL Generated |
|---|---------|--------------|
| 1 | Basic SELECT + WHERE | `SELECT ... FROM "customers" WHERE ...` |
| 2 | FormattableString params | `WHERE price BETWEEN @p0 AND @p1` |
| 3 | INNER JOIN | `INNER JOIN "customers" AS "c"` |
| 4 | GROUP BY + aggregate | `GROUP BY status` |
| 5 | Pagination | `LIMIT 5 OFFSET 5` |
| 6 | CTE | `WITH "customer_revenue" AS (...)` |
| 7 | INSERT + RETURNING | `INSERT INTO ... RETURNING id, name, ...` |
| 8 | UPDATE | `UPDATE "customers" SET "name" = @p0 WHERE ...` |
| 9 | UPSERT | `ON CONFLICT ("email") DO UPDATE SET ...` |
| 10 | DELETE | `DELETE FROM "customers" WHERE ...` |
| 11 | LIMIT -1 OFFSET | `LIMIT -1 OFFSET 5` |
| 12 | Transaction | Atomic INSERT + items commit/rollback |

## Connection Details

| Property | Value |
|----------|-------|
| Connection String | `Data Source=InMemorySample;Mode=Memory;Cache=Shared` |
