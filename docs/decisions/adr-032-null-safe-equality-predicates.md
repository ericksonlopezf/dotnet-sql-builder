# ADR-032: Null-Safe Equality Predicates (IS DISTINCT FROM / IS NOT DISTINCT FROM)

## Status

Accepted

## Date

2026-08-15

## Context

In standard SQL-99 and PostgreSQL/SQLite, `IS DISTINCT FROM` and `IS NOT DISTINCT FROM` evaluate null-safe equality:
- `NULL IS DISTINCT FROM NULL` is `FALSE` (they are not distinct).
- `1 IS DISTINCT FROM NULL` is `TRUE`.
- Standard `=` and `<>` operators evaluate to `UNKNOWN / NULL` when comparing with `NULL`, creating subtle correctness bugs in WHERE predicates and trigger filters.

## Problem

Developers had no type-safe way to express null-safe comparisons in typed LINQ expressions without writing raw SQL strings.

## Decision

Introduce typed sentinel methods `Sql.IsDistinctFrom<T>(a, b)` and `Sql.IsNotDistinctFrom<T>(a, b)` in `Sql.cs`.
Extend `SqlExpressionVisitor` to translate these method calls into `[left] IS DISTINCT FROM [right]` and `[left] IS NOT DISTINCT FROM [right]`.

## Decision Drivers

- **Correctness:** Eliminates three-valued logic bugs when filtering nullable columns.
- **Type Safety:** Supports strongly-typed property expressions and parameter extraction.

## Consequences

### Positive
- Strongly-typed null-safe equality filters in PostgreSQL and SQLite.
- Seamless parameter management and escaping for arguments.
