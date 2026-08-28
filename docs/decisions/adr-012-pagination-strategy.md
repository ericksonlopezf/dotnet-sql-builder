# ADR-012: Pagination Strategy

## Status
Accepted

## Date
2026-08-12

## Context
Pagination is a universal requirement. Three fundamentally different strategies exist, each with distinct trade-offs in correctness, performance, and user experience.

## The Four Strategies

| Strategy | SQL Pattern | Correctness | Performance at depth | Random access | Stable? |
|----------|-------------|-------------|----------------------|---------------|---------|
| **Offset** | `LIMIT n OFFSET m` | ❌ Phantom rows on insert | O(n) — full scan to offset | ✅ | ❌ |
| **Window (ROW_NUMBER)** | `ROW_NUMBER() OVER (ORDER BY ...)` | ✅ | O(n) — index on ORDER BY column | ✅ | ✅ |
| **Keyset (Seek)** | `WHERE id > @lastId ORDER BY id LIMIT n` | ✅ | O(1) — index seek | ❌ | ✅ |
| **Composite Cursor** | `WHERE (c1 > @k1) OR (c1 = @k1 AND c2 > @k2) ... LIMIT n` | ✅ | O(1) — composite index seek | ❌ | ✅ |

## Problem
Most libraries implement only offset pagination, leaving users to discover its O(n) degradation at large offsets and phantom row bugs when data changes between pages.

## Options Considered

### Option A: Offset-only
- Rejected: leaves users with a footgun for large datasets

### Option B: Keyset-only
- Rejected: breaks random page access (important for admin UIs)

### Option C: All four, as first-class APIs
- **Chosen**: Users should choose the right strategy; the library makes all four trivial

## Decision
Provide four distinct pagination methods in `SelectQuery<T>`:

```csharp
// 1. Offset — simple, O(n) at depth
query.Limit(20).Offset(40);

// 2. Window — ROW_NUMBER, O(n) but stable
query.WindowPage(pageNumber: 3, pageSize: 20, orderBy: x => x.CreatedAt);

// 3. Keyset / Seek — O(1), stable single column
query.Seek(after: lastId, pageSize: 20, keySelector: x => x.Id);

// 4. Composite Cursor — O(1), multi-column keyset for deterministic order
query.OrderBy(x => x.CreatedAt)
     .ThenBy(x => x.Id)
     .SeekAfter(new CursorKey("CreatedAt", lastDate), new CursorKey("Id", lastId))
     .Limit(20);
```

**No default pagination strategy.** If pagination methods are not called, the query returns all rows. This is intentional — the library does not make assumptions about desired pagination behavior.

## Consequences

### Positive
- ✅ Users choose the right strategy for their use case
- ✅ All three are compile-time typed (no string column references)
- ✅ Documentation clearly explains trade-offs

### Negative
- ❌ More API surface to learn vs. a single `.Paginate()` method
- ❌ Composite cursor requires additional types (`CursorToken<T>`)

## Documentation Requirement
All pagination documentation MUST include the trade-off table above. Users must be able to make an informed choice.

## Reconsideration Criteria
If a single "smart" pagination strategy emerges that handles all cases (unlikely), consolidate. Otherwise, keep all three.

## References
- [FEATURE_MATRIX.md §11 — Pagination Analysis](../../FEATURE_MATRIX.md)
- `src/EricksonLopez.SqlBuilder/SelectQuery.cs` — `.Page()`, `.WindowPage()`, `.Seek()`
- [docs/Pagination.md](../Pagination.md)
