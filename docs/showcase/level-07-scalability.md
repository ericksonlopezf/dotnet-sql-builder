# Level 07: Scalability

**Context:** Horizontal scaling patterns, cursor/seek pagination, window-based pagination, bulk throughput optimization.

---

## Topics Covered

| # | Feature | API |
|---|---------|-----|
| 1 | Keyset pagination (Seek) | `CursorPaginationExtensions.Seek<T>(col, lastValue, asc, limit)` |
| 2 | Composite keyset (SeekAfter) | `SeekAfter(params CursorKey[])` |
| 3 | Composite keyset backward (SeekBefore) | `SeekBefore(params CursorKey[])` |
| 4 | Window-page pagination | `WindowPage(pageNumber, pageSize, col, desc)` |
| 5 | Offset-based pagination | `Paginate(page, size)` / `QueryPagedAsync<T>` |
| 6 | BulkInsert throughput | `Sql.BulkInsert<T>(entities)` tuning |
| 7 | Streaming | `ToStreamAsync` for zero-buffering |
| 8 | Parallel query execution | Task.WhenAll with multiple queries |
| 9 | Dynamic sorting | `OrderByDynamic<T>(colName, descending)` |

---

## Pagination Performance Guide

| Strategy | Best For | Consistent after Delete? | Deep Page Support |
|----------|----------|--------------------------|-------------------|
| `Paginate(page, size)` | Reports, admin UIs | ❌ No (OFFSET shifts) | ❌ Degrades |
| `Seek<T>(column, lastValue)` | API pagination (single key) | ✅ Yes | ✅ O(1) |
| `SeekAfter(CursorKey[])` | Complex sort keys | ✅ Yes | ✅ O(1) |
| `WindowPage(num, size, col)` | Reports with stable page #s | ⚠️ Partial | ✅ Consistent |

---

## CursorKey Constructor

```csharp
// CursorKey(string ColumnName, object? Value, bool IsDescending = false)
new CursorKey("id", lastId)                         // ascending
new CursorKey("created_at", lastDate, true)         // descending key
```

---

## Multi-Key Seek (Tie-Breaking)

```csharp
// Forward: WHERE (col1 > @p0 OR (col1 = @p0 AND col2 > @p1)) ORDER BY col1, col2
query.SeekAfter(
    new CursorKey("priority", lastPriority),
    new CursorKey("id", lastId))
    .Limit(pageSize)

// Backward: WHERE (col1 < @p0 OR (col1 = @p0 AND col2 < @p1))
query.SeekBefore(
    new CursorKey("priority", lastPriority),
    new CursorKey("id", lastId))
    .Limit(pageSize)
```

---

## Reference

**Source:** [`Level07_Scalability/ScalabilitySample.cs`](../../samples/EricksonLopez.SqlBuilder.Samples/Level07_Scalability/ScalabilitySample.cs)
