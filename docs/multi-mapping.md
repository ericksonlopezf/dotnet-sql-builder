# Multi-Mapping — EricksonLopez.SqlBuilder

> **Package:** `EricksonLopez.SqlBuilder.Dapper` (2-7 entities) + `EricksonLopez.SqlBuilder.Dapper.MultiMap` (8+ entities, v1.x)
> **ADR:** [ADR-005](decisions/adr-005-multi-mapping-beyond-7-entities.md)

---

## What Is Multi-Mapping?

Multi-mapping allows a single JOIN query to populate multiple related objects from a flat result set. Instead of executing N+1 queries, you execute one JOIN and split the result columns into multiple entities.

```sql
SELECT u.id, u.name, o.id AS order_id, o.total FROM users u
INNER JOIN orders o ON o.user_id = u.id
WHERE u.is_active = true
```

This single query populates both `User` and `Order` objects.

---

## Dapper's Limitation

Dapper provides generic overloads up to `Query<T1, T2, T3, T4, T5, T6, T7, TReturn>`.

Beyond 7 entities, you must use:
```csharp
connection.Query<TReturn>(sql, types: new[] { typeof(T1), typeof(T2), ..., typeof(T8) },
    map: objects => { /* unsafe object[] casting */ });
```

This is:
- ❌ Not type-safe (manual casting to `object[]`)
- ❌ Not AOT-compatible (`Type[]` runtime dispatch)
- ❌ Verbose and error-prone

---

## 2-7 Entity Mapping (Dapper package)

Standard Dapper multi-mapping wrapped with `ISqlQuery` support:

```csharp
// 2 entities
var users = await connection.QueryAsync<User, Order, User>(
    Sql.From<User>().Join<Order>((u, o) => u.Id == o.UserId),
    (user, order) =>
    {
        user.LatestOrder = order;
        return user;
    },
    splitOn: "order_id");

// 3 entities
var result = await connection.QueryAsync<User, Order, Product, User>(
    query,
    (user, order, product) =>
    {
        order.Product = product;
        user.Order = order;
        return user;
    },
    splitOn: "order_id,product_id");
```

### `splitOn` Convention

`splitOn` specifies the column(s) that mark the start of each new entity in the flat result set.
- Default: `"Id"` (Dapper's default)
- Multiple: comma-separated `"OrderId,ProductId,CategoryId"`

---

## 8+ Entity Mapping (Dapper.MultiMap package)

For queries with 8 or more entities, use the fluent `MultiMapBuilder<TReturn>`:

```csharp
var result = await connection.MultiMapAsync<User>(
    query,
    map => map
        .Split<Order>(on: "order_id")
        .Split<Product>(on: "product_id")
        .Split<Category>(on: "category_id")
        .Split<Supplier>(on: "supplier_id")
        .Split<Warehouse>(on: "warehouse_id")
        .Split<Region>(on: "region_id")
        .Split<Country>(on: "country_id")
        .Into((user, order, product, category, supplier, warehouse, region, country) =>
        {
            product.Category = category;
            product.Supplier = supplier;
            supplier.Warehouse = warehouse;
            warehouse.Region = region;
            region.Country = country;
            order.Product = product;
            user.Order = order;
            return user;
        }));
```

### Source Generator (AOT Path, v1.0)

Annotate your multi-map usage to enable compile-time descriptor generation:

```csharp
// With [GenerateMultiMap] attribute, Source Generator emits
// MultiMapDescriptor<User, Order, Product, Category, ...> at compile time
[GenerateMultiMap(typeof(User), typeof(Order), typeof(Product), typeof(Category))]
public partial class OrderRepository { }
```

---

## When to Use Multi-Mapping vs QueryMultiple

| Scenario | Use |
|----------|-----|
| Single JOIN, related objects | Multi-mapping |
| Multiple independent queries | `QueryMultipleAsync` |
| N:M without aggregation | Multi-mapping + deduplication |
| Deep hierarchies (5+ levels) | Consider `QueryMultipleAsync` |
| Performance-critical paths | Benchmark both; JOIN may cause row explosion |

### Row Explosion Warning

```sql
-- If User has 10 Orders and each Order has 5 Products:
-- This JOIN returns 50 rows, not 10
SELECT u.*, o.*, p.*
FROM users u
INNER JOIN orders o ON o.user_id = u.id
INNER JOIN products p ON p.order_id = o.id
```

For N:M scenarios, prefer `QueryMultipleAsync` (two separate queries) over a JOIN that multiplies rows.

---

## QueryMultipleAsync Example

For independent aggregation:

```csharp
var query1 = Sql.From<User>().Where(u => u.IsActive);
var query2 = Sql.From<Order>().Where(o => o.Status == "pending");

// Execute both in one round-trip (not yet implemented — future feature)
// Currently: execute separately with transaction
using var tx = connection.BeginTransaction();
var users = await connection.QueryAsync<User>(query1, tx);
var orders = await connection.QueryAsync<Order>(query2, tx);
```

---

## AOT Compatibility

| Path | AOT Safe? |
|------|----------|
| 2-7 via Dapper wrappers | ❌ (Dapper reflection) — use `QueryAotAsync` for AOT |
| 8+ `MultiMapBuilder` | ✅ (with Source Generator descriptors, v1.0) |

---

## Related Documents

- [ADR-005: Multi-Mapping Beyond 7 Entities](decisions/adr-005-multi-mapping-beyond-7-entities.md)
- [ADR-013: AOT Guarantees](decisions/adr-013-aot-guarantees.md)
