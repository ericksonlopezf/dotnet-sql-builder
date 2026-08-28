# Window Functions in EricksonLopez.SqlBuilder

EricksonLopez.SqlBuilder provides a fluent, strongly-typed API to construct SQL **Window Functions** without falling back to raw string snippets.

---

## 1. Fundamental Concepts

Window functions perform calculations across a set of table rows related to the current row without collapsing the result into a single row (unlike `GROUP BY`).

The static factory `Window` provides access to all supported window operations:
- **Ranking Functions:** `RowNumber()`, `Rank()`, `DenseRank()`, `Ntile(n)`
- **Value/Offset Functions:** `Lag()`, `Lead()`, `FirstValue()`, `LastValue()`, `NthValue(n)`
- **Window Aggregates:** `Sum()`, `Avg()`, `Min()`, `Max()`, `Count()`

---

## 2. Syntax & Practical Examples

### Ranking & Row Numbering
```csharp
var query = Sql.From<Employee>()
    .Select(
        Window.RowNumber<Employee>()
              .PartitionBy(e => e.DepartmentId)
              .OrderByDescending(e => e.Salary)
              .As("DepartmentRank"),
        Window.Rank<Employee>()
              .OrderByDescending(e => e.Salary)
              .As("GlobalRank")
    );
```
Generated SQL (e.g. PostgreSQL / SQL Server):
```sql
SELECT 
    ROW_NUMBER() OVER (PARTITION BY DepartmentId ORDER BY Salary DESC) AS DepartmentRank,
    RANK() OVER (ORDER BY Salary DESC) AS GlobalRank
FROM Employees
```

### Offset Functions (`LAG` and `LEAD`)
```csharp
var query = Sql.From<SalesRecord>()
    .Select(
        Window.Lag<SalesRecord, decimal>(s => s.Amount, offset: 1, defaultValue: 0)
              .PartitionBy(s => s.Region)
              .OrderBy(s => s.SaleDate)
              .As("PreviousSaleAmount"),
        Window.Lead<SalesRecord, decimal>(s => s.Amount, offset: 1)
              .PartitionBy(s => s.Region)
              .OrderBy(s => s.SaleDate)
              .As("NextSaleAmount")
    );
```

### Aggregates with `FILTER` Clause (PostgreSQL / SQLite)
```csharp
var query = Sql.From<Order>()
    .Select(
        Window.Sum<Order, decimal>(o => o.Total)
              .PartitionBy(o => o.CustomerId)
              .Filter(o => o.Status == "PAID")
              .As("PaidTotal")
    );
```
Generated SQL:
```sql
SELECT 
    SUM(Total) FILTER (WHERE Status = 'PAID') OVER (PARTITION BY CustomerId) AS PaidTotal
FROM Orders
```

---

## 3. Window Specifications & Frames

Configure window framing limits (`ROWS BETWEEN`, `RANGE BETWEEN`) via the fluent ordering and partitioning builder methods, preserving cross-dialect compilation guarantees.
