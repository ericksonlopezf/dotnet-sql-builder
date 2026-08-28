# ADR-036: Typed LATERAL JOIN with Outer Reference Resolution

## Status

Accepted

## Date

2026-08-15

## Context

`LATERAL` joins (PostgreSQL / MySQL 8.0.14+) and `CROSS/OUTER APPLY` (SQL Server / Oracle 12c+) allow subqueries to reference columns from preceding tables in the `FROM` clause.
Previously, outer column references in subqueries required raw string conditions.

## Problem

How to enable type-safe outer column references within subquery lambda expressions without violating AST immutability or introducing implicit state.

## Decision

Introduce the `Sql.Outer<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> column)` sentinel.
When visited by `SqlExpressionVisitor`, `Sql.Outer` resolves the referenced outer entity's member into an escaped column identifier.

## Decision Drivers

- **Type Safety:** Refactoring or renaming properties immediately fails compilation if broken.
- **Composable Subqueries:** Clean expression syntax for correlated subqueries and lateral joins.
