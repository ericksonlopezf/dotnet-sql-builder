# Level 02: Window Functions & Common Table Expressions (CTEs)

## 1. Analytical Queries with Window Functions
`EricksonLopez.SqlBuilder` natively models advanced SQL windowing constructs: `ROW_NUMBER()`, `RANK()`, `DENSE_RANK()`, and `LAG()/LEAD()` with explicit partition and frame clauses.

```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.PostgreSql;

var query = SqlQuery.Select("order_id", "customer_id", "total_amount")
    .SelectWindow(w => w.RowNumber()
        .Over(o => o.PartitionBy("customer_id").OrderByDesc("total_amount")), 
        alias: "rank_per_customer")
    .From("orders")
    .Build(PostgreSqlDialect.Instance);
```

---

## 2. Common Table Expressions (CTEs) & Recursive Queries
Complex recursive hierarchies and materialization hints (`MATERIALIZED` / `NOT MATERIALIZED`) are modeled via immutable CTE builder nodes:

```csharp
var cte = SqlQuery.Cte("regional_sales")
    .As(SqlQuery.Select("region", "SUM(amount) AS total_sales")
        .From("sales")
        .GroupBy("region"));

var finalQuery = SqlQuery.With(cte)
    .Select("r.region", "r.total_sales")
    .From("regional_sales", alias: "r")
    .Where("r.total_sales", Op.GreaterThan, 100000)
    .Build(PostgreSqlDialect.Instance);
```

---

## 3. High Performance Batch Mutations & RETURNING Clauses
Cross-platform bulk inserts with database-specific mutation semantics (`RETURNING` in PostgreSQL, `OUTPUT` in SQL Server):

```csharp
var insertQuery = SqlQuery.InsertInto("orders")
    .Columns("customer_id", "status", "created_at")
    .Values("@customer_id", "@status", "@created_at")
    .Returning("id", "created_at")
    .Build(PostgreSqlDialect.Instance);
```
