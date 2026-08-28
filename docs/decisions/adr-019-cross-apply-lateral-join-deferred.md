# ADR-019: CROSS APPLY / LATERAL JOIN Deferred

## Status
Proposed (deferred to v1.3+)

## Date
2026-08-12

## Context
`CROSS APPLY` (SQL Server) and `LATERAL JOIN` (PostgreSQL, MySQL 8.0.14+) allow joining a table-valued function or a subquery that references columns from the outer query. They are essential for:
- Calling table-valued functions per row
- "Top N per group" queries
- Unnesting arrays per row (PostgreSQL `UNNEST`)

## Problem
These constructs are fundamentally different from regular JOINs in that the right side of the join depends on the left side at the row level. Expressing this in a typed fluent API is non-trivial.

## Current State
- `UNNEST` is partially supported in PostgreSQL via `UnnestNode`
- `CROSS APPLY` / `OUTER APPLY` require a subquery AST node that references outer columns
- No current `LATERAL` or `APPLY` AST node exists

## Options Considered

### Option A: Raw SQL only (current workaround)
- Accepted as current state: `Sql.Raw("CROSS APPLY (...) AS sub")`

### Option B: Typed `CrossApply(subquery)` / `Lateral(subquery)` join methods
- **Chosen** (deferred): Requires AST support for outer column references in subqueries

### Option C: Expression-based LATERAL
- Rejected for now: Expression tree support for outer-reference correlated subqueries is extremely complex

## Decision

**Deferred to v1.3** due to:
1. Low frequency of use vs. high implementation complexity
2. Correct outer-reference resolution in AST requires cross-node scope analysis
3. Dialect differences: SQL Server uses `CROSS APPLY` / `OUTER APPLY`; PostgreSQL uses `LATERAL`; MySQL uses `LATERAL` (8.0.14+); SQLite and Oracle have limited support

**Current workaround:**
```csharp
// PostgreSQL LATERAL:
query.JoinRaw("CROSS JOIN LATERAL (SELECT * FROM unnest(@ids) AS id) AS t",
    new { ids = idArray });

// SQL Server CROSS APPLY:
query.JoinRaw("CROSS APPLY GetTopOrdersForCustomer(c.id) AS o");
```

**Planned API (v1.3):**
```csharp
// LATERAL (PostgreSQL, MySQL)
query.LateralJoin(
    subquery: Sql.From<Order>().Where(o => o.CustomerId == Sql.Outer<Customer>(c => c.Id)).Limit(3),
    alias: "top_orders"
);

// CROSS APPLY (SQL Server)
query.CrossApply(
    tableFunction: "GetTopOrders",
    args: c => c.Id,
    alias: "top_orders"
);
```

**Dialect mapping:**
| Syntax | SQL Server | PostgreSQL | MySQL 8+ | SQLite | Oracle |
|--------|-----------|-----------|---------|--------|--------|
| CROSS APPLY | ✅ native | → CROSS JOIN LATERAL | → CROSS JOIN LATERAL | ❌ | ✅ (12c+) |
| OUTER APPLY | ✅ native | → LEFT JOIN LATERAL | → LEFT JOIN LATERAL | ❌ | ✅ (12c+) |

## Consequences

### Positive (when implemented)
- ✅ Type-safe lateral/apply joins
- ✅ Dialect translation (CROSS APPLY ↔ LATERAL)

### Negative (while deferred)
- ❌ Users must use `JoinRaw()` — no compile-time validation of outer column references
- ❌ No AST node for LATERAL exists today

## Reconsideration Criteria
If a user scenario requires LATERAL/APPLY that cannot be expressed via `JoinRaw`, reprioritize.

## References
- [FEATURE_MATRIX.md §4 — Complete Feature Discovery](../../FEATURE_MATRIX.md)
- `src/EricksonLopez.SqlBuilder/SelectQuery.cs` — `JoinRaw()`
- [ADR-009: Dialect Isolation](./adr-009-dialect-isolation-separate-packages.md)
