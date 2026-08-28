# ADR-042: Scalar Subquery in SELECT Clause

## Status
Accepted

## Date
2026-08-19

## Context
In analytics, reporting, and dashboard queries, embedding scalar subqueries directly within the `SELECT` projection (e.g., `SELECT Id, Name, (SELECT COUNT(*) FROM Orders o WHERE o.CustomerId = c.Id) AS OrderCount FROM Customers c`) is a very common requirement (GAP-01 in functional parity audit).

Previously, this required escaping to `RawSelect(FormattableString)`.

## Decision
1. Add `ScalarSubquerySelectNode` to the AST in `EricksonLopez.SqlBuilder.Abstractions.Nodes`.
2. Introduce a typed overload on `SelectQuery<T>`:
   ```csharp
   public SelectQuery<T> Select(ISqlQuery subquery, string alias);
   ```
3. Update `ISqlVisitor` and `SqlVisitor` to compile the scalar subquery as `(subquery_sql) AS alias` using proper dialect identifier quoting and parameter binding.

## Consequences
- ✅ Parity with SqlKata for scalar projection without breaking type safety.
- ✅ Full AST composability and zero reflection overhead in NativeAOT.
- ✅ Immutable query construction.
