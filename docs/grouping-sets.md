# Analytical Aggregations: GROUPING SETS, ROLLUP and CUBE

EricksonLopez.SqlBuilder includes strongly-typed support for analytical `GROUP BY` extensions, enabling multi-level aggregations in a single SQL query for OLAP reports and analytical dashboards.

---

## 1. Available Operators

- **`GroupByRollup(...)`**: Emits hierarchical aggregations from the most granular level up to the grand total.
- **`GroupByCube(...)`**: Computes all possible subtotal combinations across specified dimensions.
- **`GroupingSets(...)`**: Explicitly defines exact aggregation groupings to compute.

---

## 2. Usage Examples

### ROLLUP (Hierarchical Totals & Subtotals)
```csharp
var query = Sql.From<Sale>()
    .Select("Year", "Quarter", "Region")
    .RawSelect($"SUM(Amount) AS TotalAmount")
    .GroupByRollup("Year", "Quarter", "Region");
```
Generated SQL:
```sql
SELECT Year, Quarter, Region, SUM(Amount) AS TotalAmount
FROM Sales
GROUP BY ROLLUP (Year, Quarter, Region)
```

### CUBE (All Cross-Dimensional Combinations)
```csharp
var query = Sql.From<Sale>()
    .Select("Department", "Category")
    .RawSelect($"SUM(Quantity) AS TotalQty")
    .GroupByCube("Department", "Category");
```
Generated SQL:
```sql
SELECT Department, Category, SUM(Quantity) AS TotalQty
FROM Sales
GROUP BY CUBE (Department, Category)
```

### Custom GROUPING SETS
```csharp
var query = Sql.From<Sale>()
    .Select("Brand", "Segment", "City")
    .RawSelect($"SUM(Amount) AS TotalAmount")
    .GroupingSets(
        new[] { "Brand", "Segment" },
        new[] { "City" },
        Array.Empty<string>() // Grand total
    );
```
Generated SQL:
```sql
SELECT Brand, Segment, City, SUM(Amount) AS TotalAmount
FROM Sales
GROUP BY GROUPING SETS (
    (Brand, Segment),
    (City),
    ()
)
```

---

## 3. Dialect Compatibility
- **SQL Server, PostgreSQL, Oracle:** Native support for `ROLLUP`, `CUBE`, and `GROUPING SETS`.
- **MySQL / MariaDB:** `WITH ROLLUP` (with dialect adaptation based on engine version).
- **SQLite:** Does not natively support multi-dimensional analytical groupings (compiles to standard `GROUP BY` or emits compatibility notice).
