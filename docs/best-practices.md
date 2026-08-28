# Best Practices

Architectural recommendations and guidelines to achieve maximum performance, security, and maintainability with **EricksonLopez.SqlBuilder**.

---

## 1. Query Security and Parameterization

- **Never concatenate strings into SQL queries:** Always use `FormattableString` or strongly-typed expression APIs. Roslyn analyzer `ESQL002` prevents unsafe string concatenation at compile time.
  ```csharp
  // ❌ Unsafe (prone to SQL injection):
  query.WhereRaw("id = " + userId);

  // ✅ Safe:
  query.Where($"id = {userId}");
  ```
- **Use `.WhereAll()` when an unconstrained operation is intentional:** Avoid suppressing `ESQL001`/`ESQL003` without explicit justification.

---

## 2. Immutability and Query Reuse

- Since all AST structures are immutable `record` instances, you can define shared base queries and safely fork them across multiple workflows or threads:
  ```csharp
  var baseQuery = Sql.From<Order>()
      .Where(o => o.TenantId == currentTenantId);

  var pendingOrders = baseQuery.Where(o => o.Status == "PENDING");
  var completedOrders = baseQuery.Where(o => o.Status == "COMPLETED");
  ```

---

## 3. Efficient Pagination

- **Use Keyset / Cursor Pagination (`SeekAfter` / `SeekBefore`) for large datasets:**
  `Limit(n).Offset(m)` suffers from $O(N)$ performance degradation on deep pages. `SeekAfter` leverages the B-Tree index to maintain $O(1)$ lookup complexity regardless of page depth.
  ```csharp
  var page = Sql.From<LogEntry>()
      .OrderBy(x => x.CreatedAt)
      .ThenBy(x => x.Id)
      .SeekAfter(new CursorKey("CreatedAt", lastDate), new CursorKey("Id", lastId))
      .Limit(50);
  ```

---

## 4. NativeAOT and Zero Reflection

- Decorate all entity models with `[SqlEntity]` to activate the incremental Roslyn Source Generator.
- In strict AOT environments (e.g. mobile, WASM, trimmed containers without JIT), use `AotQueryExecutor` and the generated static mapper `T.GetReaderParser()` to bypass standard Dapper reflection.

---

## 5. Resilience and Transaction Boundaries

- **Never wrap retry policies inside active database transactions (`IUnitOfWork`):**
  Rule `ESQL012` warns if a resilience pipeline wraps operations inside a transaction scope. When a transaction fails, the entire transaction must abort, roll back, and retry from the beginning.

---

## 6. Bulk Operations and Identity Retrieval (ADR-046)

- **For standard single-row inserts or moderate batches with database-generated IDs:**  
  Use the fluent `.Returning(x => x.Id)` API on `InsertQuery<T>`. It compiles to `OUTPUT inserted.Id` (SQL Server) or `RETURNING id` (PostgreSQL, SQLite, Oracle) natively.
- **For high-throughput bulk ingestion (`BulkInsertAsync`):**  
  Use **client-generated keys** (such as `UUIDv7`, sequential GUIDs, or Snowflake IDs). This enables the driver to utilize pure binary streaming (`COPY FROM STDIN`, `SqlBulkCopy`) at maximum throughput without sequence lock contention or parent-child roundtrip stalls.
