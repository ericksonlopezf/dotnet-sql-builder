# ADR-020: Recursive CTE Support

## Status
Accepted

## Date
2026-08-12

## Context
Common Table Expressions (CTEs) — both non-recursive and recursive — are essential for hierarchical queries (org charts, category trees, bill-of-materials), graph traversal, and complex query decomposition.

## Problem
Recursive CTEs have a specific SQL structure (`WITH RECURSIVE` or `WITH` depending on dialect) that differs from regular CTEs:

```sql
-- PostgreSQL
WITH RECURSIVE category_tree AS (
    SELECT id, name, parent_id, 0 AS depth
    FROM categories WHERE parent_id IS NULL
    UNION ALL
    SELECT c.id, c.name, c.parent_id, ct.depth + 1
    FROM categories c
    JOIN category_tree ct ON c.parent_id = ct.id
)
SELECT * FROM category_tree ORDER BY depth, name;
```

The two sub-queries in a recursive CTE (anchor + recursive member) have fundamentally different semantics.

## Options Considered

### Option A: Raw SQL only
- Rejected: recursive CTE is too common in DDD/hierarchy scenarios to leave untyped

### Option B: Single fluent CTE API for both recursive and non-recursive
- Rejected: they have different structural requirements; conflating them produces a confusing API

### Option C: Separate `.Cte()` (non-recursive) and `.RecursiveCte()` APIs
- **Chosen**: Clear intent, separate code paths, correct `WITH RECURSIVE` keyword handling

## Decision

**Non-recursive CTE (implemented):**
```csharp
var baseQuery = Sql.From<Category>().Where(c => c.ParentId == null);
var query = Sql.From<Category>()
    .Cte("active_categories", baseQuery)
    .Join<Category>("active_categories", (c, ac) => c.Id == ac.Id);
```

**Recursive CTE (implemented):**
```csharp
var anchor = Sql.From<Category>().Where(c => c.ParentId == null)
    .Select(c => new { c.Id, c.Name, c.ParentId, Depth = 0 });

var recursive = Sql.From<Category>()
    .Join("category_tree", (c, ct) => c.ParentId == ct.Id)
    .Select(c => new { c.Id, c.Name, c.ParentId, Depth = Sql.Raw($"ct.depth + 1") });

var query = Sql.From("category_tree")
    .RecursiveCte("category_tree", anchor, recursive, unionAll: true)
    .OrderBy("depth")
    .ThenBy("name");
```

**Dialect keyword mapping:**
| Dialect | Keyword |
|---------|---------|
| PostgreSQL | `WITH RECURSIVE` |
| SQL Server | `WITH` (no `RECURSIVE` keyword) |
| MySQL 8+ | `WITH RECURSIVE` |
| SQLite 3.8.3+ | `WITH RECURSIVE` |
| Oracle | `WITH` + `CONNECT BY` (different semantics — deferred) |

**Oracle:** Recursive CTEs via `WITH RECURSIVE` are partially supported (Oracle 11gR2+). The `CONNECT BY` hierarchical query is Oracle-specific and deferred to a future ADR.

## Consequences

### Positive
- ✅ Type-safe hierarchical query composition
- ✅ Correct `WITH RECURSIVE` / `WITH` emission per dialect
- ✅ Composable with all other query features (WHERE, ORDER BY, pagination)

### Negative
- ❌ Recursive CTE API is more verbose than non-recursive
- ❌ Oracle's `CONNECT BY` is a separate implementation path
- ❌ Cycle detection (preventing infinite recursion) is user's responsibility

## Reconsideration Criteria
If Dapper adds native recursive CTE support in a future version, evaluate whether the builder layer adds sufficient value to maintain.

## References
- [FEATURE_MATRIX.md §4 — Complete Feature Discovery](../../FEATURE_MATRIX.md)
- `src/EricksonLopez.SqlBuilder.Abstractions/Nodes/CteNode.cs`
- `src/EricksonLopez.SqlBuilder/SelectQuery.cs` — `.Cte()` and `.RecursiveCte()` methods
