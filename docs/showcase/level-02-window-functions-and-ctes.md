# Level 02: Window Functions & Common Table Expressions (CTEs)

## 1. Analytical Queries with Window Functions
`EricksonLopez.SqlBuilder` natively models advanced SQL windowing constructs: `ROW_NUMBER()`, `RANK()`, `DENSE_RANK()`, and `LAG()/LEAD()` with explicit partition and ordering specifications.

```csharp
using System;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.PostgreSql;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
}

var query = Sql.From<Order>()
    .Select(o => o.Id, o => o.CustomerId, o => o.TotalAmount)
    .Select(
        Window.RowNumber<Order>()
              .PartitionBy(o => o.CustomerId)
              .OrderByDescending(o => o.TotalAmount)
              .As("rank_per_customer")
    );

var compiler = new PostgreSqlCompiler();
SqlResult result = query.Build(compiler);

Console.WriteLine(result.Sql);
// SELECT "Id", "CustomerId", "TotalAmount", ROW_NUMBER() OVER(PARTITION BY "CustomerId" ORDER BY "TotalAmount" DESC) AS "rank_per_customer" FROM "Orders"
```

---

## 2. Common Table Expressions (CTEs) & Modular Queries
Complex multi-stage aggregations and CTE materialization hints (`MATERIALIZED` / `NOT MATERIALIZED`) are modeled via immutable CTE nodes:

```csharp
// 1. Define the CTE query
var regionalSalesCte = Sql.From<Order>()
    .Select("Region")
    .RawSelect($"SUM(TotalAmount) AS TotalSales")
    .GroupBy("Region");

// 2. Consume the CTE in the main query
var finalQuery = Sql.From<Order>()
    .CTE("RegionalSales", regionalSalesCte)
    .From("RegionalSales", alias: "r")
    .Select("r.Region", "r.TotalSales")
    .Where($"r.TotalSales > {100000m}");

SqlResult cteResult = finalQuery.Build(compiler);
```

---

## 3. High Performance Mutations & RETURNING / OUTPUT Clauses
Cross-platform mutations support engine-specific identity and column extraction (`RETURNING` in PostgreSQL/SQLite, `OUTPUT` in SQL Server):

```csharp
var newOrder = new Order { CustomerId = 42, TotalAmount = 199.99m };

// PostgreSQL / SQLite: INSERT ... RETURNING "Id"
var insertPg = Sql.Insert(newOrder)
                  .Returning(o => o.Id);

SqlResult pgInsertResult = insertPg.Build(compiler);
// INSERT INTO "Orders" ("CustomerId", "TotalAmount") VALUES (@p0, @p1) RETURNING "Id"
```
