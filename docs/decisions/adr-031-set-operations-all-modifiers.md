# ADR-031: Set Operations ALL Modifiers (INTERSECT ALL and EXCEPT ALL)

## Status

Accepted

## Date

2026-08-15

## Context

ANSI SQL defines set operations with optional `ALL` modifiers: `UNION [ALL]`, `INTERSECT [ALL]`, and `EXCEPT [ALL]`.
While `UNION` and `UNION ALL` were supported from v1.0, `INTERSECT ALL` and `EXCEPT ALL` were omitted from the fluent API, requiring developers to drop down to `Sql.Raw()` whenever multiset operations (preserving duplicate row counts) were required.

## Problem

How to expose `INTERSECT ALL` and `EXCEPT ALL` in a type-safe, immutable, and dialect-compliant manner across all supported engines.

## Decision

Extend `SelectQuery<T>` with explicit `.IntersectAll(ISqlQuery query)` and `.ExceptAll(ISqlQuery query)` methods that produce `SetOperationNode("INTERSECT ALL", query)` and `SetOperationNode("EXCEPT ALL", query)` respectively.

## Decision Drivers

- **SQL Completeness:** Closes the gap in ANSI SQL set operation coverage.
- **AST Immutability:** Preserves existing immutable record node architecture (`SetOperationNode`).
- **Zero Allocations:** Direct node emission with no runtime string formatting.

## Consequences

### Positive
- Developers can fluently construct multiset intersections and differences.
- Full parity across supported dialects.

### Negative
- None; additive API change.
