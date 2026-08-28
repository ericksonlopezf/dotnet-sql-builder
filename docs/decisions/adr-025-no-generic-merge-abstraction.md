# ADR-025: No Generic Cross-Dialect MERGE Abstraction

## Status

Accepted

## Date

2026-08-14

## Context

MERGE (SQL Server / Oracle) and ON CONFLICT (PostgreSQL / SQLite) and ON DUPLICATE KEY UPDATE (MySQL) all perform "upsert" operations but with fundamentally different semantics, syntax, and correctness guarantees.

SQL Server's MERGE has well-documented concurrency bugs (race conditions under concurrent execution even with isolation levels). Oracle's MERGE requires strict ON condition formulation to avoid duplication. PostgreSQL's ON CONFLICT is safe and composable. MySQL's ON DUPLICATE KEY is based on primary key / unique index detection, with no target-column specification.

## Problem

Should EricksonLopez.SqlBuilder expose a single `Upsert<T>()` or `MergeOrUpsert<T>()` API that works across all 5 dialects?

## Options Considered

### Option A: Generic `Upsert<T>()` abstraction (rejected)

A single `Sql.Upsert<T>(entity)` API that compiles to the appropriate dialect-specific syntax.

**Problems:**
- The conflict target columns mean different things per dialect:
  - PostgreSQL: explicit column list required
  - MySQL: implicit (uses primary key / unique index) — target columns are ignored
  - SQL Server: requires a full MERGE statement with ON condition, subject to concurrency bugs
  - Oracle: same as SQL Server
  - SQLite: explicit conflict target
- SQL Server MERGE has race conditions under concurrent writes that cannot be hidden behind an abstraction without lying to users
- Returning semantics differ completely (SQL Server: OUTPUT clause; PG: RETURNING; Oracle: RETURNING INTO)
- A single abstraction would either be the least-common-denominator (MySQL behavior) or would require dialect-specific configuration, at which point it's not a useful abstraction

### Option B: Dialect-specific upsert APIs in dialect packages (chosen)

- PostgreSQL: `InsertQuery<T>.OnConflict(cols[]).DoUpdate(expr)` — already implemented
- MySQL: `InsertQuery<T>.OnConflict().DoUpdate(expr)` → compiles to `ON DUPLICATE KEY UPDATE` — already implemented
- SQLite: same as PostgreSQL path — already implemented
- SQL Server: `MergeQuery<T>` (deprecated) + recommended `Sql.Raw()` for MERGE syntax
- Oracle: `MergeQuery<T>` (deprecated) + recommended `Sql.Raw()` for MERGE INTO syntax

### Option C: Thin `IUpsertStrategy<T>` plugin (rejected)

Similar to `IBulkStrategy`. Would require users to pick a strategy per dialect.

**Problem:** This complexity is not justified for the upsert use case when dialect-specific APIs (Option B) are cleaner.

## Decision

**Option B.** No generic cross-dialect MERGE or upsert abstraction will be built.

Each dialect exposes its own upsert mechanism. `MergeQuery<T>` is `[Obsolete]` and will be removed in v2.0.

## Decision Drivers

- **Correctness:** SQL Server MERGE is not safe under concurrent load. Hiding this in a generic abstraction is dangerous.
- **AOT:** Generic abstractions tend to require runtime dispatch, adding complexity.
- **API complexity:** Each dialect's upsert semantics require dialect-specific configuration; a unified API would either be unusable or would require per-dialect parameters.
- **Scope:** Users who need MERGE on SQL Server should use `Sql.Raw()`. This is a deliberate escape hatch, not a missing feature.

## Consequences

### Positive

- No false sense of safety from a "works everywhere" MERGE API
- SQL Server concurrency issues are the developer's responsibility, as they should be
- Each dialect's upsert is clean and composable

### Negative

- Cross-dialect upsert requires dialect detection in application code
- Users migrating from SqlKata (which has a cross-dialect approach) may be surprised

## Why We Reject Option A

SQL Server MERGE with concurrent inserts can produce duplicates, even inside a transaction. This is a known bug in the SQL Server optimizer. Exposing this via an abstraction that looks safe is a correctness trap. See: https://www.mssqltips.com/sqlservertip/3074/use-caution-with-sql-servers-merge-statement/

## Relationship to Project Philosophy

> EricksonLopez.SqlBuilder is a SQL compiler, not a business logic layer. Upsert semantics are fundamentally provider-specific. Trying to unify them violates the "dialect-aware" principle.

## Migration / Future Reconsideration

`MergeQuery<T>` will be removed in v2.0. Users should migrate to:
- PG/SQLite: `InsertQuery<T>.OnConflict(...).DoUpdate(...)`
- MySQL: `InsertQuery<T>.OnConflict(...).DoUpdate(...)` → `ON DUPLICATE KEY UPDATE`
- SS/Oracle: `Sql.Raw(FormattableString)` with native MERGE syntax

## Reconsideration Criteria

This decision may be revisited if:
- SQL Server MERGE concurrency bugs are officially documented as fixed by Microsoft
- A unified upsert specification emerges across ANSI SQL
- A concrete user scenario is presented where `Sql.Raw()` is insufficient
