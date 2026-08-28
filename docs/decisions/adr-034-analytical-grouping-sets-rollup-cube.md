# ADR-034: Analytical Grouping Sets, Rollup, and Cube

## Status

Accepted

## Date

2026-08-15

## Context

OLAP and analytical reporting workloads frequently require subtotal and cross-tabulation aggregation queries using SQL extensions:
- `GROUP BY ROLLUP(c1, c2)`
- `GROUP BY CUBE(c1, c2)`
- `GROUP BY GROUPING SETS ((c1), (c2), (c1, c2))`

These operations are natively supported by SQL Server, PostgreSQL, MySQL, and Oracle, but are not supported by SQLite.

## Problem

Developers building reporting queries had to manually compose raw SQL strings for multi-level aggregations.

## Decision

1. Extend `GroupByNode` with `GroupByType` enum (`Standard`, `Rollup`, `Cube`, `GroupingSets`) and optional nested grouping sets list.
2. Add `.GroupByRollup(...)`, `.GroupByCube(...)`, and `.GroupingSets(...)` to `SelectQuery<T>`.
3. Support compilation in `SqlCompilerVisitor` for supported engines.
4. Throw descriptive `NotSupportedException` in `SqliteCompiler` if analytical groupings are attempted on SQLite.

## Decision Drivers

- **Analytical Completeness:** First-class support for hierarchical and multidimensional reporting.
- **Fail-Fast Safety:** SQLite raises a clear compile-time/visitor exception rather than emitting invalid syntax.

## Consequences

### Positive
- Type-safe analytical grouping on SQL Server, PostgreSQL, MySQL, and Oracle.
- Clear error diagnostics on SQLite.
