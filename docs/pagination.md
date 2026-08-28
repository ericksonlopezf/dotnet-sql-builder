# Pagination — EricksonLopez.SqlBuilder

> **ADR:** [ADR-012](decisions/adr-012-pagination-strategy.md)

---

## Overview

EricksonLopez.SqlBuilder provides three pagination strategies as first-class citizens:
1. **Offset pagination** — simple, universal, O(n) at depth
2. **Window-based pagination** — ROW_NUMBER() based, dialect-specific
3. **Seek (keyset) pagination** — O(1) constant performance, ideal for APIs

No opinion on which to use — both are documented with trade-offs.

---

## Strategy Comparison

| Dimension | Offset | Window | Seek |
|-----------|--------|--------|------|
| Performance at depth | O(n) degrades | O(n) but more stable | O(1) constant |
| Data consistency | Low (rows shift on insert) | Medium | High (stable cursor) |
| Random page access | ✅ ("jump to page 50") | ✅ | ❌ (forward only) |
| SQL complexity | Simple | Medium | Medium |
| Use case | Admin UIs, small datasets | Large sorted datasets | APIs, infinite scroll |
| All DB support | ✅ | ✅ (w/ emulation) | ✅ (with indexes) |

---

## 1. Offset Pagination (`.Page()`)

```csharp
// Using PaginationParameters (from EricksonLopez.Pagination)
var query = Sql.From<User>()
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Page(PaginationParameters.Create(pageNumber: 2, pageSize: 20));

// Or via Dapper extension
var query = Sql.From<User>()
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name);

var pagedQuery = query.Paginate(pageNumber: 2, pageSize: 20);

// PostgreSQL generates:
// SELECT * FROM users WHERE is_active = @p0
// ORDER BY name LIMIT 20 OFFSET 20

// SQL Server generates:
// SELECT * FROM users WHERE is_active = @p0
// ORDER BY name OFFSET 20 ROWS FETCH NEXT 20 ROWS ONLY
```

### Paginated Result with Count

```csharp
var result = await query.ToPagedListAsync(
    connection, pageNumber: 2, pageSize: 20);
// Result: IPagedList<User> with items + total count (two queries)
```

---

## 2. Window-Based Pagination (`.WindowPage()`)

Best for SQL Server scenarios with complex ORDER BY that benefits from ROW_NUMBER():

```csharp
var query = Sql.From<User>()
    .Where(u => u.IsActive)
    .WindowPage(pageNumber: 3, pageSize: 25, orderByColumn: "created_at", descending: true);

// Generates:
// WITH paged AS (
//   SELECT *, ROW_NUMBER() OVER (ORDER BY created_at DESC) AS __rn
//   FROM users WHERE is_active = @p0
// )
// SELECT * FROM paged WHERE __rn BETWEEN 51 AND 75
```

---

## 3. Seek (Keyset) Pagination (`.Seek()`)

Ideal for APIs and infinite scroll where you page forward through large datasets:

```csharp
// First page
var query = Sql.From<Post>()
    .OrderByDescending(p => p.CreatedAt)
    .ThenByDescending(p => p.Id)
    .Seek(afterId: null, pageSize: 25);

// Subsequent page (pass last item's cursor values)
var query = Sql.From<Post>()
    .OrderByDescending(p => p.CreatedAt)
    .ThenByDescending(p => p.Id)
    .Seek(afterCreatedAt: lastItem.CreatedAt, afterId: lastItem.Id, pageSize: 25);

// Generates:
// SELECT * FROM posts
// WHERE (created_at < @p0 OR (created_at = @p0 AND id < @p1))
// ORDER BY created_at DESC, id DESC
// LIMIT 25
```

### Seek Pagination Best Practices

1. **Always include a unique tie-breaker** — never sort by non-unique column alone
2. **Create a composite index** that matches your ORDER BY
3. **Encode cursor for APIs** — return base64-encoded cursor in API response

```csharp
// Opaque API cursor
var cursor = Convert.ToBase64String(
    JsonSerializer.SerializeToUtf8Bytes(new { p.CreatedAt, p.Id }));
// Return as: { "nextCursor": "eyJDcmVhdGVkQXQi..." }
```

---

## 4. Composite Cursor (Planned v1.x)

Multi-column seek cursor with type safety:

```csharp
// Create cursor from last item
var cursor = PageCursor.After(lastPost, p => p.CreatedAt, p => p.Id);

var query = Sql.From<Post>()
    .OrderByDescending(p => p.CreatedAt)
    .ThenByDescending(p => p.Id)
    .SeekAfter(cursor); // type-safe, generates correct WHERE

// Composite cursor handles NULL values and ties correctly
```

---

## NULL Ordering (Planned v1.x)

```csharp
// NULLS LAST (PostgreSQL native, emulated on SQL Server)
.OrderBy(u => u.DeletedAt, NullsPosition.Last)

// PostgreSQL: ORDER BY deleted_at NULLS LAST
// SQL Server: ORDER BY IIF(deleted_at IS NULL, 1, 0), deleted_at
```

---

## Dynamic Sorting

For API-driven sorting (e.g., from query string), use `DynamicSortingExtensions`:

```csharp
// Safe: allowlist validation prevents injection
var query = Sql.From<User>()
    .Where(u => u.IsActive)
    .ApplySort(request.SortBy, allowedColumns: new[] { "name", "email", "created_at" });
```

---

## Related Documents

- [ADR-012: Pagination Strategy](decisions/adr-012-pagination-strategy.md)
- [Cookbook.md](Cookbook.md) — examples
