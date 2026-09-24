# Level 3: Real Use Cases

## Overview

Level 3 dives into real-world database querying patterns: Common Table Expressions (CTEs), analytical Window Functions (`ROW_NUMBER`, `RANK`, `SUM OVER`), aggregation, grouping sets (`ROLLUP`, `CUBE`), differential updates (`ApplyDiff`), date extractions, and subquery existential checks.

---

## Key Features & APIs Demonstrated

### 1. Window Functions & CTEs
Compose analytical reports by partitioning and ordering datasets in memory-efficient single round trips:
```csharp
var cteQuery = Sql.From<Employee>()
    .Select("department", "AVG(salary) as AvgSalary")
    .GroupBy("department");

var reportQuery = Sql.From<Employee>()
    .CTE("DeptStats", cteQuery)
    .Join("DeptStats", "ds", "employees.department = ds.department")
    .Select("employees.name", "employees.salary", "ds.AvgSalary")
    .Window("RowNumber", partitionBy: new[] { "employees.department" }, orderBy: new[] { "employees.salary DESC" });
```

### 2. Aggregations & Grouping Sets
Support for `AsCount`, `AsSum`, `AsAvg`, `AsMin`, `AsMax`, and enterprise grouping dimensions:
```csharp
var aggSql = Sql.From<Employee>()
    .AsCount("total_employees")
    .AsSum("salary", "total_salary")
    .AsAvg("salary", "avg_salary")
    .GroupBy("department");

// Multi-dimensional reporting (SQL Server & PostgreSQL)
var rollupQuery = Sql.From<Category>()
    .Select("name", "year").AsSum("revenue", "total_revenue")
    .GroupByRollup("name", "year");
```

### 3. Differential Updates (`ApplyDiff`)
Compute minimum delta changes between two entity snapshots to avoid overwriting concurrently modified columns:
```csharp
var original = new Employee { Id = 1, Name = "Alice", Department = "Engineering", Salary = 90000 };
var updated  = new Employee { Id = 1, Name = "Alice", Department = "Engineering", Salary = 95000 };

var updateQuery = Sql.Update<Employee>()
    .ApplyDiff(original, updated)
    .And(e => e.Id == original.Id);
```

### 4. INSERT INTO ... SELECT (`Sql.InsertFrom<T>`)
Move records between tables directly on the database engine without materializing them into application memory:
```csharp
var hrQuery = Sql.From<Employee>().Where(e => e.Department == "HR");
var insertFromSql = Sql.InsertFrom<ArchivedEmployee>(hrQuery, "name", "department", "salary");
```

---

## Execution Output

```text
=== LEVEL 3: REAL USE CASES ===
[+] 1. Report with CTE and Window Functions
[+] 2. Recursive CTE (hierarchy traversal)
[+] 3. Aggregate Functions — AsCount / AsSum / AsAvg / AsMin / AsMax
[+] 4. GroupBy + Having
[+] 5. GROUP BY ROLLUP — Hierarchical subtotals
[+] 6. GROUP BY CUBE — All crossed dimensions
[+] 7. GROUPING SETS — Explicit multi-level groupings
[+] 8. WhereDate / WhereYear / WhereMonth
[+] 9. WhereColumns — Column-to-column comparison
[+] 10. Between / ILike / Coalesce / NullIf / IsDistinctFrom
[+] 11. Diff Updates
[+] 12. Pagination with PagedList
[+] 13. NullsPosition — ORDER BY with NULLs control
[+] 14. Merge (Upsert / Synchronization)
[+] 15. Sql.InsertFrom<T> — INSERT INTO ... SELECT
[+] 16. InsertQuery.DefaultValues() — INSERT with all defaults
[+] 17. WhereDay — EXTRACT(DAY FROM column)
[+] 18. WithTag — attaching diagnostic tags to queries
[+] 19. IntersectAll / ExceptAll — set operations with duplicates
[+] 20. OrHaving — OR condition on HAVING clause
[+] 21. OrExists / OrNotExists — set existential checks
[+] 22. Alias — naming SelectQuery for use as subquery
[+] 23. SelectQuery.From(ISqlQuery, alias) — derived table
```
