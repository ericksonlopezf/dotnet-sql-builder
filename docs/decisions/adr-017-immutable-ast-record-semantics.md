# ADR-017: Immutable AST via C# Record Semantics

## Status
Accepted

## Date
2026-08-12

## Context
Query builders face a fundamental design tension: mutable builders are easy to understand but create subtle shared-state bugs when queries are shared or composed; immutable builders are safer but require clear semantics for "mutation" operations.

## Problem
Mutable fluent builders (like SqlKata) produce hidden state bugs:

```csharp
// SqlKata-style (mutable) — BUG:
var baseQuery = new Query("users").Where("active", true);
var adminQuery = baseQuery.Where("role", "admin"); // mutates baseQuery!
var userQuery  = baseQuery.Where("role", "user");   // same object!
// Both queries now have BOTH WHERE clauses
```

This is particularly dangerous when base queries are cached, shared across requests, or used in loops.

## Options Considered

### Option A: Mutable fluent builder (SqlKata style)
- Rejected: shared-state bugs, not thread-safe without cloning

### Option B: Immutable with explicit `.Clone()` calls
- Rejected: too verbose; users forget to clone

### Option C: Immutable record-based AST with `with` expression (chosen)
- **Chosen**: C# `record` provides structural equality + immutable `with` copies transparently

### Option D: Persistent data structures (ImmutableList, etc.)
- Partially adopted: `ImmutableArray<ISqlNode>` is used for the `Nodes` collection

## Decision
All `*Query<T>` types are `sealed partial record` with `ImmutableArray<ISqlNode> Nodes`.

**Key invariant:** Every fluent method that "modifies" the query returns a **new instance** with the updated node list. The original query is never mutated.

```csharp
// EricksonLopez.SqlBuilder — correct:
var baseQuery = Sql.From<User>().Where(u => u.Active);
var adminQuery = baseQuery.And(u => u.Role == "admin"); // new instance
var userQuery  = baseQuery.And(u => u.Role == "user");  // new instance, baseQuery unchanged

// baseQuery still has only: WHERE active = true
// adminQuery has: WHERE active = true AND role = 'admin'
// userQuery has: WHERE active = true AND role = 'user'
```

**Thread safety:** `*Query<T>` instances are safe to share across threads — no mutable state.

**Caching:** Compiled queries can be safely cached:
```csharp
private static readonly SelectQuery<User> _activeUsers = Sql.From<User>()
    .Where(u => u.Active)
    .OrderBy(u => u.Name);

// Per-request: add pagination without mutation
var page = _activeUsers.Page(pageNumber, pageSize);
```

**`AddNode` helper:** Internal method that produces a new query with the node appended:
```csharp
protected TQuery AddNode(ISqlNode node) =>
    this with { Nodes = Nodes.Add(node) };
```

## Consequences

### Positive
- ✅ No shared-state bugs — queries are safe to share and compose
- ✅ Thread-safe by construction — no locks required
- ✅ Cacheable — compiled base queries can be cached as static fields
- ✅ Structural equality from `record` — useful for caching and testing
- ✅ `with` expression syntax is idiomatic C# 9+

### Negative
- ❌ Slightly more memory per operation (new record per fluent call)
- ❌ `with` syntax for internal use — not immediately obvious to all devs
- ❌ Structural equality from `record` includes `Nodes` array — equality semantics may surprise users if node order differs

## Performance Impact
A new record allocation per fluent call is negligible — queries are built once, compiled once, then the built string is reused. The allocation happens at query-build time, not at query-execution time.

## Reconsideration Criteria
If `ImmutableArray` causes measurable performance issues in benchmarks (due to copy-on-add semantics for large node lists), evaluate moving to a persistent linked-list structure.

## References
- [FEATURE_MATRIX.md §4 — Complete Feature Discovery](../../FEATURE_MATRIX.md)
- `src/EricksonLopez.SqlBuilder/SelectQuery.cs`
- `src/EricksonLopez.SqlBuilder/InsertQuery.cs`
- [ADR-007: No Change Tracking](./adr-007-no-change-tracking.md)
