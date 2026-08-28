# Feature Record: P2-F001 — Window Function Typed FILTER (WHERE ...) Clause

**Category:** Dialect Capabilities & Syntax Extensibility  
**Status:** Implemented & Verified  
**Date:** 2026-08-14  

---

## 1. Context & Motivation

In standard SQL (SQL:2003 and later), aggregate functions used as window functions or standing aggregations support an optional `FILTER (WHERE <condition>)` clause. For instance:

```sql
SELECT
    "department",
    SUM("salary") FILTER (WHERE "salary" > 50000) OVER (PARTITION BY "department") AS "high_salary_sum",
    COUNT(*) FILTER (WHERE "status" = 'Active') OVER (PARTITION BY "department") AS "active_dept_count"
FROM "employees"
```

Prior to this feature, window functions in `EricksonLopez.SqlBuilder` supported `PARTITION BY`, `ORDER BY (ASC/DESC)`, offsets, and default values, but lacked support for expressing conditional aggregation filter clauses directly through the fluent `Window` builder API.

---

## 2. Technical Architecture & Implementation

### 2.1 AST Extensions: `WindowFunctionNode`
Added optional filter parameters to `WindowFunctionNode`:
- `System.Linq.Expressions.Expression? FilterExpression = null`
- `string? FilterRaw = null`
- `object?[]? FilterRawArgs = null`

Preserving parameter default values guarantees binary backwards compatibility.

### 2.2 Fluent Builder: `WindowBuilder<TEntity>`
Added three overloads to `WindowBuilder<TEntity>`:
1. `Filter(Expression<Func<TEntity, bool>> filterPredicate)`: Typed LINQ boolean predicate parsed into SQL and parameterized via `SqlExpressionVisitor`.
2. `Filter(FormattableString rawCondition)`: Interpolated raw SQL expression.
3. `Filter(string rawCondition, params object?[] parameters)`: Formatted raw SQL with explicit parameters.

### 2.3 Visitor Compilation: `SqlCompilerVisitor`
In `Visit(WindowFunctionNode node)`:
Emits ` FILTER (WHERE <parsed_expression>)` directly preceding the `OVER (...)` clause when a filter expression or raw SQL condition is present.

---

## 3. Verification & Test Evidence

### 3.1 Unit Test Coverage
Added tests in `tests/EricksonLopez.SqlBuilder.UnitTests/Queries/WindowBuilderTests.cs`:
- `Sum_WithTypedFilter_GeneratesFilterClause`: Verifies typed LINQ expression `e => e.Salary > 50000m` translates to `SUM("salary") FILTER (WHERE (salary > @p0)) OVER (PARTITION BY "department") AS "high_salary_sum"` with parameter binding.
- `Count_WithRawFilter_GeneratesFilterClause`: Verifies formatted raw SQL translates to `COUNT(*) FILTER (WHERE department = 'HR') OVER () AS "hr_count"`.

### 3.2 Compilation & Verification Results
- 14/14 tests in `WindowBuilderTests` pass.
- 0 build errors across `EricksonLopez.SqlBuilder.Abstractions` and `EricksonLopez.SqlBuilder`.
