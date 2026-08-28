# SQL Server Playground — EricksonLopez.SqlBuilder

Run a full live demo against **SQL Server 2022** using Docker.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- .NET 10 SDK

## Quick Start

```bash
# 1. Start the database
docker compose up -d

# 2. Wait ~10s for SQL Server to initialize, then run the demo
dotnet run

# 3. Stop when done
docker compose down -v
```

## What This Demo Shows

| # | Feature | SQL Generated |
|---|---------|--------------|
| 1 | Basic SELECT + WHERE | `SELECT ... FROM [customers] WHERE ...` |
| 2 | FormattableString params | `WHERE price BETWEEN @p0 AND @p1` |
| 3 | INNER JOIN | `INNER JOIN [customers] AS [c]` |
| 4 | GROUP BY + aggregate | `GROUP BY status` |
| 5 | Pagination | `OFFSET 5 ROWS FETCH NEXT 5 ROWS ONLY` |
| 6 | CTE | `WITH [customer_revenue] AS (...)` |
| 7 | TOP N | `SELECT TOP (10) ...` |
| 8 | INSERT OUTPUT INSERTED | `INSERT INTO ... OUTPUT INSERTED.id ...` |
| 9 | MERGE INTO | `MERGE INTO [customers] USING (...) ON ...` |
| 10 | UPDATE | `UPDATE [customers] SET [name] = @p0 WHERE ...` |
| 11 | DELETE | `DELETE FROM [customers] WHERE ...` |
| 12 | Transaction | Atomic INSERT + items commit/rollback |

## Connection Details

| Property | Value |
|----------|-------|
| Host | localhost |
| Port | 1434 |
| Database | sqlbuilder_demo |
| Username | sa |
| Password | Demo@SqlB1! |
