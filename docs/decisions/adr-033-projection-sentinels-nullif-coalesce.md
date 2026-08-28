# ADR-033: Projection Sentinels (NULLIF and Multi-Argument COALESCE)

## Status

Accepted

## Date

2026-08-15

## Context

SQL standard scalar functions `NULLIF(val, target)` and multi-argument `COALESCE(val1, val2, ..., fallback)` are fundamental for data transformation, projection fallback expressions, and division-by-zero guards.
Previously, only a 2-argument extension method `value.Coalesce(fallback)` was available.

## Problem

Developers could not express `NULLIF` or multi-argument `COALESCE` in typed expressions without writing raw SQL.

## Decision

1. Expose `Sql.NullIf<T>(T value, T target)` as a compiler-recognized sentinel.
2. Expose `Sql.Coalesce<T>(T val1, T val2, T fallback)` as a compiler-recognized sentinel.
3. Handle these method calls inside `SqlExpressionVisitor` to emit standard SQL function calls with parameterized values and property expressions.

## Decision Drivers

- **Expressiveness:** Enables complex fallback logic in WHERE clauses and projections.
- **AOT Compatibility:** Expressions are parsed without IL emission or reflection compilation.

## Consequences

### Positive
- Direct expression of `NULLIF` and multi-argument `COALESCE`.
- Zero runtime reflection or IL generation.
