# ADR-018: Window Function Expression Support

## Status
Proposed (deferred to v2.0)

## Date
2026-08-12

## Context
Window functions (`ROW_NUMBER()`, `RANK()`, `DENSE_RANK()`, `LAG()`, `LEAD()`, `SUM() OVER`, etc.) are essential for analytics queries, pagination (WindowPage), and ranking. The library already uses `ROW_NUMBER()` internally for `WindowPage` pagination.

## Problem
Currently, window functions can only be expressed via `Sql.Raw()`. Users need:
```sql
SELECT name, salary,
       RANK() OVER (PARTITION BY department_id ORDER BY salary DESC) AS salary_rank,
       LAG(salary) OVER (ORDER BY hire_date) AS prev_salary
FROM employees
```

Writing this as `Sql.Raw(...)` loses compile-time safety and AOT compatibility.

## Options Considered

### Option A: Raw SQL only (current)
- Partially accepted as current state, but insufficient for type-safe analytics

### Option B: Fluent typed window expression API
- **Chosen** (deferred): Type-safe, composable, compile-time validated

### Option C: Source Generator generates window function expressions from expression trees
- Complementary to Option B — generator could detect `Window.RankOver(...)` patterns

### Option D: LINQ expression-based window functions
- Rejected: Expression tree support for window functions is complex and dialect-specific

## Decision

**Deferred to v2.0** due to the complexity of the typed window API and the low frequency of this use case vs. the high complexity of implementation.

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
- `ROW_NUMBER()`, `RANK()`, `DENSE_RANK()`, `NTILE(n)` — ranking functions
- `LAG(col, offset)`, `LEAD(col, offset)` — value access functions
- `SUM()`, `AVG()`, `COUNT()`, `MIN()`, `MAX()` — aggregate window functions
- `PARTITION BY` + `ORDER BY` + `ROWS/RANGE BETWEEN` frame specification

**Dialect support:** All 5 dialects support ANSI window functions (PostgreSQL, SQL Server, MySQL 8+, Oracle). SQLite supports ROW_NUMBER since 3.25.0.

## Why Deferred
1. The `WindowNode` in the AST is already present — the compile path exists
2. Expression tree parsing for window functions is significantly more complex than WHERE/SELECT
3. The majority of users use `WindowPage()` (already typed) or raw SQL for analytics
4. v1.x focus is on infrastructure (UoW, Resilience) and SQL completeness for common patterns

## Consequences

### Positive (when implemented)
- ✅ Type-safe window function expressions
- ✅ IDE completion for partition/order columns
- ✅ Compile-time detection of invalid column references

### Negative (while deferred)
- ❌ Users must use `Sql.Raw()` for window functions — losing type safety
- ❌ `WindowNode` exists in AST but unused by user-facing API

## Reconsideration Criteria
If user demand for typed window functions becomes a frequent GitHub issue, reprioritize to v0.9.

## References
- [FEATURE_MATRIX.md §4 — Complete Feature Discovery](../../FEATURE_MATRIX.md)
- `src/EricksonLopez.SqlBuilder.Abstractions/Nodes/WindowNode.cs`
- [ADR-012: Pagination Strategy](./adr-012-pagination-strategy.md)
