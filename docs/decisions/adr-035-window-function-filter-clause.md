# ADR-035: Window Function FILTER (WHERE ...) Clause

## Status

Accepted

## Date

2026-08-15

## Context

ANSI SQL:2003 defines the `FILTER (WHERE predicate)` clause for aggregate and window functions:
```sql
SUM(amount) FILTER (WHERE status = 'completed') OVER (PARTITION BY customer_id)
```
This syntax is natively supported in PostgreSQL and SQLite, while SQL Server and Oracle require conditional `CASE WHEN` aggregation.

## Problem

`WindowBuilder<T>` previously declared `_filterExpression` and `Filter(...)` methods, but full compilation and dialect-level gating were pending.

## Decision

1. Finalize `WindowBuilder<T>.Filter(...)` accepting typed expressions (`Expression<Func<T, bool>>`) and interpolated strings (`FormattableString`).
2. `SqlCompilerVisitor` emits `FILTER (WHERE ...)` after the aggregate function.
3. `SqlServerCompiler` explicitly intercepts window functions with filter clauses and throws `NotSupportedException` directing users to conditional `CASE WHEN` aggregation or `Sql.Raw()`.

## Decision Drivers

- **PostgreSQL / SQLite Native Parity:** Enables concise filtered window aggregations.
- **Dialect Safety:** Explicitly rejects filter syntax on engines lacking ANSI FILTER support.
