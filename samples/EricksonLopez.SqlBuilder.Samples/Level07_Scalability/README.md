# Level 7: Scalability & Performance

## Overview

Level 7 covers scalability invariants, thread-safety guarantees of immutable query objects, comparing pagination strategies (Offset vs. Keyset vs. Window), and the "compile once, execute many times" optimization pattern.

---

## Scalability Invariants

1. **Immutable Thread Safety**:
   Because query builders do not mutate internal state, a single query template can be shared across multiple threads or concurrent requests without locks.
2. **Pagination Scalability Matrix**:

| Strategy | Performance on Page 1 | Performance on Page 10,000 | Memory Usage |
|---|---|---|---|
| **Limit / Offset** | Fast ($O(K)$) | Slow ($O(N+K)$ scan) | Low |
| **Keyset (`SeekAfter`)** | Fast ($O(1)$) | Fast ($O(1)$ index seek) | Minimal |
| **WindowPage (`ROW_NUMBER`)** | Moderate | Fast with index | Moderate |

---

## Compile Once, Execute Many Pattern

```csharp
// Compile the AST once into an immutable SqlResult template
var query = Sql.From<Metric>().Where(m => m.NodeId == 1).Limit(10);
SqlResult compiled = query.Build(new SqliteCompiler());

// Re-execute compiled SQL with new parameters without paying AST traversal cost
for (int i = 0; i < 3; i++)
{
    await connection.QueryAsync<Metric>(compiled.Sql, compiled.Parameters);
}
```

---

## Execution Output

```text
=== LEVEL 7: SCALABILITY, PAGINATION AND PERFORMANCE ===
[+] 1. Thread Safety — Immutable queries shared across threads
[+] 2. Offset Pagination — Limit / Offset
[+] 3. PaginationParameters.Page() — Typed pagination
[+] 4. Cursor Pagination with Seek<T> (keyset)
[+] 5. Composite Cursor Pagination — SeekAfter / SeekBefore
[+] 6. WindowPage — ROW_NUMBER() pagination (better for deep pages)
[+] 7. OrderByDynamic — Dynamic sorting (SQL injection safe)
[+] 8. QueryPagedRawAsync — Pagination with raw SQL
[+] 9. Optimization — Compile query once and reuse
```
