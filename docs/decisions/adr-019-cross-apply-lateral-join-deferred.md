# ADR-019: CROSS APPLY / LATERAL JOIN Deferred

## Status
Accepted (implemented in v1.0; superseded deferral via ADR-036 / P2-F002)

## Date
2026-08-12 (Revised 2026-09-14)

## Context
`CROSS APPLY` (SQL Server) and `LATERAL JOIN` (PostgreSQL, MySQL 8.0.14+) allow joining a table-valued function or a subquery that references columns from the outer query.

## Decision
Originally deferred to v1.3, this feature was prioritized and **fully implemented in v1.0** via `SelectQuery<T>.LateralJoin` and `SelectQuery<T>.LateralLeftJoin` (incorporating automatic outer reference resolution as formalized in [ADR-036](./adr-036-lateral-join-outer-reference-resolution.md) and feature specification P2-F002).

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
| CROSS APPLY | âœ… native | â†’ CROSS JOIN LATERAL | â†’ CROSS JOIN LATERAL | âŒ | âœ… (12c+) |
| OUTER APPLY | âœ… native | â†’ LEFT JOIN LATERAL | â†’ LEFT JOIN LATERAL | âŒ | âœ… (12c+) |

## Consequences

### Positive (when implemented)
- âœ… Type-safe lateral/apply joins
- âœ… Dialect translation (CROSS APPLY â†” LATERAL)

### Negative (while deferred)
- âŒ Users must use `JoinRaw()` â€” no compile-time validation of outer column references
- âŒ No AST node for LATERAL exists today

## Reconsideration Criteria
If a user scenario requires LATERAL/APPLY that cannot be expressed via `JoinRaw`, reprioritize.

## References
- [FEATURE_MATRIX.md Â§4 â€” Complete Feature Discovery](../master-feature-matrix.md)
- `src/EricksonLopez.SqlBuilder/SelectQuery.cs` â€” `JoinRaw()`
- [ADR-009: Dialect Isolation](./adr-009-dialect-isolation-separate-packages.md)
