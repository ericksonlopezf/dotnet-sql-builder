# MySQL Playground — EricksonLopez.SqlBuilder

Run a full live demo against **MySQL 8.0** using Docker.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- .NET 10 SDK

## Quick Start

```bash
# 1. Start the database
docker compose up -d

# 2. Wait ~5-10s for MySQL to initialize, then run the demo
dotnet run

# 3. Stop when done
docker compose down -v
```

## What This Demo Shows

| # | Feature | SQL Generated |
|---|---------|--------------|
| 1 | Basic SELECT + WHERE | ``SELECT ... FROM `customers` WHERE ...`` |
| 2 | FormattableString params | `WHERE price BETWEEN @p0 AND @p1` |
| 3 | INNER JOIN | ``INNER JOIN `customers` AS `c` `` |
| 4 | GROUP BY + aggregate | `GROUP BY status` |
| 5 | Pagination | `LIMIT 5 OFFSET 5` |
| 6 | CTE (MySQL 8+) | ``WITH `customer_revenue` AS (...)`` |
| 7 | UPDATE | ``UPDATE `customers` SET `name` = @p0 WHERE ...`` |
| 8 | UPSERT | ``ON DUPLICATE KEY UPDATE `name` = VALUES(`name`)`` |
| 9 | DELETE | ``DELETE FROM `customers` WHERE ...`` |
| 10 | Transaction | Atomic INSERT + items commit/rollback |

## Connection Details

| Property | Value |
|----------|-------|
| Host | localhost |
| Port | 3307 |
| Database | sqlbuilder_demo |
| Username | root |
| Password | Demo@SqlB1! |
