# Level 5: Batch Processing & Streaming

## Overview

Level 5 demonstrates high-throughput data operations: bulk insertions, zero-reflection Native AOT execution with `QueryAotAsync`, sequential stream processing for large objects (`SequentialAccess`), cancellation tokens, keyset cursor pagination, and window paging.

---

## High-Performance APIs

| API | Performance Benefit |
|---|---|
| `Sql.BulkInsert<T>(entities)` | Emits multi-row `INSERT INTO ... VALUES (...), (...)` batches |
| `AotQueryExecutor.QueryAsync<T>` | Zero heap allocations from reflection; uses compile-time mapper delegates |
| `CommandBehavior.SequentialAccess` | Streams large text/blob columns directly from the network buffer |
| `QueryStreamAsync<T>()` | Returns `IAsyncEnumerable<T>` for pipeline processing without buffer bloat |
| `SeekAfter(...) / SeekBefore(...)` | $O(1)$ Keyset pagination using clustered index ordering |

---

## Core Code Examples

### Bulk Insertion
```csharp
var bulkQuery = Sql.BulkInsert(new[]
{
    new Job { Name = "Data Sync", Status = "Queued" },
    new Job { Name = "Report Generation", Status = "Queued" }
});
await connection.ExecuteAsync(bulkQuery);
```

### Keyset (Cursor) Pagination
```csharp
// Keyset pagination avoids the O(N) cost of high OFFSET values
var seekQuery = Sql.From<LogEntry>()
    .SeekAfter(lastSeenTimestamp, lastSeenId)
    .OrderBy(l => l.Timestamp)
    .ThenBy(l => l.Id)
    .Limit(50);
```

---

## Execution Output

```text
=== LEVEL 5: PROCESSING ===
[+] 1. BulkInsertAsync — Bulk entity insertion
[+] 2. Sql.BulkInsert<T> — Bulk INSERT via InsertQuery
[+] 3. QueryAotAsync — Zero-reflection read for NativeAOT
[+] 4. QueryFirstOrDefaultAotAsync — First AOT result
[+] 5. QuerySequentialAsync — For LOB columns (text/blob)
[+] 6. CancellationToken — Operation cancellation
[+] 7. QueryStreamAsync — Streaming without in-memory buffering
[+] 8. BulkDeleteAsync — Bulk deletion
[+] 9. SeekAfter / SeekBefore — Composite keyset cursor pagination
[+] 10. WindowPage — ROW_NUMBER window-based pagination
[+] 11. Seek<T>() — Single-key cursor pagination
[+] 12. OrderByDynamic — String-based dynamic sorting
```
