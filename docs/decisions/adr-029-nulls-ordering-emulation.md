# ADR-029: NULLS FIRST / NULLS LAST Emulation Across Non-Conforming Dialects

## Status

Accepted

## Date

2026-08-15

## Context

SQL dialects differ significantly in how they place `NULL` values during `ORDER BY` operations:
- **PostgreSQL / Oracle / SQLite (partial):** Native support for `NULLS FIRST` and `NULLS LAST` clauses (`ORDER BY col ASC NULLS LAST`).
- **SQL Server / MySQL / SQLite (pre-3.30):** No native `NULLS FIRST / LAST` keyword syntax.
  - SQL Server treats `NULL` as the lowest possible value (sorted first in ASC, last in DESC).
  - MySQL treats `NULL` as the lowest possible value.

Previously, `SqlServerCompiler`, `MySqlCompiler`, and `SqliteCompiler` treated `NullsPosition.First` and `NullsPosition.Last` as silent NOPs (no-operation), silently ignoring the user's explicit sorting intent and returning incorrectly ordered recordsets.

## Problem

How should `EricksonLopez.SqlBuilder` ensure deterministic, consistent null-ordering behavior across all 5 dialects without producing silent wrong results or requiring users to hand-write conditional ordering expressions?

## Options Considered

### Option A: Silent NOP on unsupported dialects (Rejected)
- Silently ignores developer intent.
- Causes subtle data corruption bugs where UI/pagination expectations fail without error.

### Option B: Throw `NotSupportedException` at compilation time (Rejected)
- Destroys cross-dialect query portability.
- Forces developers to write custom queries per dialect even when standard SQL can express the ordering.

### Option C: Emulate via `CASE WHEN col IS NULL THEN ... ELSE ... END` in compiler (Chosen)
- When a dialect lacks native `NULLS FIRST / LAST` syntax, the dialect compiler intercepts the ordering node and emits a deterministic boolean sort key before the primary column:
  - `NULLS FIRST (ASC)`: `CASE WHEN [col] IS NULL THEN 0 ELSE 1 END, [col] ASC`
  - `NULLS LAST (ASC)`: `CASE WHEN [col] IS NULL THEN 1 ELSE 0 END, [col] ASC`
  - `NULLS FIRST (DESC)`: `CASE WHEN [col] IS NULL THEN 0 ELSE 1 END, [col] DESC`
  - `NULLS LAST (DESC)`: `CASE WHEN [col] IS NULL THEN 1 ELSE 0 END, [col] DESC`

## Decision

**Option C.** All dialect compilers must either emit native `NULLS FIRST / LAST` (PostgreSQL, Oracle) or inject equivalent deterministic `CASE WHEN` null-sorting expressions (SQL Server, MySQL, SQLite). No dialect compiler is permitted to silently ignore `NullsPosition`.

## Decision Drivers

- **Zero Silent Bugs:** Code must never silently generate incorrect results.
- **Portability:** A single query containing `.OrderBy(x => x.DeletedAt, NullsPosition.Last)` executes with identical sort semantics across all 5 database providers.
- **Optimizer Friendly:** Most SQL query optimizers index-match or easily evaluate boolean case expressions for ordering.

## Consequences

### Positive

- Identical sorting semantics across SQL Server, PostgreSQL, MySQL, SQLite, and Oracle.
- Completely transparent to the caller.
- Eliminates silent ordering discrepancies during cursor/keyset pagination.

### Negative

- Queries on SQL Server/MySQL contain a slightly more verbose `ORDER BY` clause when explicit `NullsPosition` is requested.

## Reconsideration Criteria

If a future version of SQL Server or MySQL introduces native ANSI `NULLS FIRST/LAST` syntax, the respective dialect compiler will be updated to emit native keywords.
