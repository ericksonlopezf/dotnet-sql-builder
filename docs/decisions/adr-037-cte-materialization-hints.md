# ADR-037: Common Table Expression Materialization Hints

## Status

Accepted

## Date

2026-08-15

## Context

Starting with PostgreSQL 12, CTEs are inlined by default unless explicitly declared as `MATERIALIZED` or `NOT MATERIALIZED`.
This hint allows developers to control query optimizer behavior (preventing optimization fences or forcing caching).

## Problem

Developers had no typed API to specify `MATERIALIZED` or `NOT MATERIALIZED` on CTEs.

## Decision

1. Add `MaterializationHint` enum (`Default`, `Materialized`, `NotMaterialized`).
2. Add `CTE` and `RecursiveCTE` overloads accepting `MaterializationHint`.
3. In `PostgreSqlCompiler`, emit `AS MATERIALIZED (` or `AS NOT MATERIALIZED (`.
4. Other dialect compilers silently ignore the hint.

## Decision Drivers

- **Performance Control:** Critical for PostgreSQL query tuning.
- **Dialect Portability:** Queries with materialization hints run unchanged on other dialects without throwing errors.
