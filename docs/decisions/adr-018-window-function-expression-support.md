# ADR-018: Window Function Expression Support

## Status
Accepted (implemented in v1.0; extended in ADR-035)

## Date
2026-08-12 (Revised 2026-09-14)

## Context
Window functions (`ROW_NUMBER()`, `RANK()`, `DENSE_RANK()`, `LAG()`, `LEAD()`, `SUM() OVER`, etc.) are essential for analytics queries, pagination (WindowPage), and ranking.

## Decision
Originally deferred to v2.0, this capability was prioritized and **fully implemented in v1.0** via the static `Window` factory class, `WindowBuilder<T>`, and `SelectQuery<T>.Select(params WindowFunctionNode[])`. Filter clause support was subsequently added in [ADR-035](./adr-035-window-function-filter-clause.md).

**Current workaround:**
```csharp
query.Select(Sql.Raw($"RANK() OVER (PARTITION BY {deptId} ORDER BY salary DESC) AS salary_rank"))
```

**Planned API (v2.0):**
```csharp
query.Select(
    Window.Rank()
        .PartitionBy(e => e.DepartmentId)
        .OrderByDescending(e => e.Salary)
        .As("salary_rank"),
    Window.Lag(e => e.Salary)
        .OrderBy(e => e.HireDate)
        .As("prev_salary")
);
```

**Scope for v2.0:**
- `ROW_NUMBER()`, `RANK()`, `DENSE_RANK()`, `NTILE(n)` â€” ranking functions
- `LAG(col, offset)`, `LEAD(col, offset)` â€” value access functions
- `SUM()`, `AVG()`, `COUNT()`, `MIN()`, `MAX()` â€” aggregate window functions
- `PARTITION BY` + `ORDER BY` + `ROWS/RANGE BETWEEN` frame specification

**Dialect support:** All 5 dialects support ANSI window functions (PostgreSQL, SQL Server, MySQL 8+, Oracle). SQLite supports ROW_NUMBER since 3.25.0.

## Why Deferred
1. The `WindowNode` in the AST is already present â€” the compile path exists
2. Expression tree parsing for window functions is significantly more complex than WHERE/SELECT
3. The majority of users use `WindowPage()` (already typed) or raw SQL for analytics
4. v1.x focus is on infrastructure (UoW, Resilience) and SQL completeness for common patterns

## Consequences

### Positive (when implemented)
- âœ… Type-safe window function expressions
- âœ… IDE completion for partition/order columns
- âœ… Compile-time detection of invalid column references

### Negative (while deferred)
- âŒ Users must use `Sql.Raw()` for window functions â€” losing type safety
- âŒ `WindowNode` exists in AST but unused by user-facing API

## Reconsideration Criteria
If user demand for typed window functions becomes a frequent GitHub issue, reprioritize to v0.9.

## References
- [FEATURE_MATRIX.md Â§4 â€” Complete Feature Discovery](../master-feature-matrix.md)
- `src/EricksonLopez.SqlBuilder.Abstractions/Nodes/WindowNode.cs`
- [ADR-012: Pagination Strategy](./adr-012-pagination-strategy.md)
