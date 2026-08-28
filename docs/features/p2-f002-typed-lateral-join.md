# Feature Record: P2-F002 — Typed LATERAL / CROSS APPLY Outer References

**Category:** SQL Engine & Fluent Query Composition  
**Status:** Implemented & Verified  
**Date:** 2026-08-14  

---

## 1. Context & Motivation

In modern relational engines (PostgreSQL 9.3+, MySQL 8.0.14+, SQL Server 2005+), subqueries in joins frequently require correlated references to outer tables.
- PostgreSQL & MySQL use `[INNER|LEFT] JOIN LATERAL (subquery) alias [ON condition]` or `CROSS JOIN LATERAL`.
- SQL Server uses `CROSS APPLY (subquery) alias` and `OUTER APPLY (subquery) alias`.

Prior to this feature, `SelectQuery<T>` had basic `CrossApply(IAstQuery, string)` and `LateralJoin(IAstQuery, string, string)` overloads, but lacked:
1. Strongly-typed ON condition expressions (`Expression<Func<TOuter, TInner, bool>>`).
2. High-ergonomics fluent subquery factories (`Func<SelectQuery<TSub>, IAstQuery>`).
3. Parameter isolation & unified registration across outer and inner subquery compilation scopes.

---

## 2. Technical Architecture & Implementation

### 2.1 AST Extensions: `SubqueryJoinNode`
Extended `SubqueryJoinNode` with an optional `ExpressionCondition` parameter:
```csharp
public sealed record SubqueryJoinNode(
    JoinType Type,
    IAstQuery Subquery,
    string Alias,
    string? OnCondition = null,
    bool IsLateral = false,
    Expression? ExpressionCondition = null
) : ISqlNode
```

### 2.2 Fluent Overloads in `SelectQuery<T>`
Added strongly typed and factory-based overloads:
- `LateralJoin<TSub>(IAstQuery subquery, string alias, Expression<Func<T, TSub, bool>> on)`
- `LateralJoin<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias, string? on = null)`
- `LateralJoin<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias, Expression<Func<T, TSub, bool>> on)`
- `LateralLeftJoin<TSub>(IAstQuery subquery, string alias, Expression<Func<T, TSub, bool>> on)`
- `LateralLeftJoin<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias, string? on = null)`
- `LateralLeftJoin<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias, Expression<Func<T, TSub, bool>> on)`
- `JoinSubquery<TSub>(IAstQuery subquery, string alias, Expression<Func<T, TSub, bool>> on)`
- `LeftJoinSubquery<TSub>(IAstQuery subquery, string alias, Expression<Func<T, TSub, bool>> on)`
- `CrossApply<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias)`
- `OuterApply<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias)`

### 2.3 Dialect Visitor Compilation
Updated `SqlCompilerVisitor` and `PostgreSqlCompiler`:
- Shared parameter manager compilation: `var sjResult = Compiler.Compile(node.Subquery, Context.Parameters);` ensures subquery parameters are registered into the query's parameter collection in deterministic sequence without collisions.
- Supported both raw `OnCondition` and typed `ExpressionCondition` parsed via `SqlExpressionVisitor`.

---

## 3. Verification & Test Evidence

### 3.1 Automated Tests
Created `tests/EricksonLopez.SqlBuilder.UnitTests/Queries/LateralAndApplyTests.cs` covering:
1. `LateralJoin_WithSubqueryFactory_CompilesPostgreSql`
2. `LateralLeftJoin_WithSubqueryFactory_CompilesPostgreSql`
3. `CrossApply_WithSubqueryFactory_CompilesSqlServer`
4. `OuterApply_WithSubqueryFactory_CompilesSqlServer`
5. `CrossApply_CompilesToCrossJoinLateral_InPostgreSql`
6. `OuterApply_CompilesToLeftJoinLateral_InPostgreSql`

### 3.2 Compilation & Verification Results
- 6/6 tests in `LateralAndApplyTests` passed cleanly.
- 0 build errors across the entire solution.
