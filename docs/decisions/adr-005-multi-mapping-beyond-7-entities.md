# ADR-005: Multi-Mapping Beyond 7 Entities via Fluent Builder

## Status
Proposed

## Context
Dapper provides typed `Query<T1, T2, ..., T7, TReturn>` overloads for multi-mapping (splitting a flat JOIN result into multiple objects). Beyond 7 type parameters, Dapper requires the untyped `Query<TReturn>(sql, types: Type[], map: Func<object[], TReturn>)` overload — losing compile-time type safety and AOT compatibility.

## Problem
Multi-JOIN queries mapping more than 7 entities need a type-safe, AOT-compatible solution that doesn't require runtime `Type[]` arrays or object casting.

## Options Considered

### Option A: Add generic overloads T8, T9... up to TN
- Rejected: Generic arity explosion; C# has practical limits; overload resolution becomes complex

### Option B: Wrap the `Type[]` Dapper overload in a helper
- Rejected: Still unsafe (casts to `object[]`), still not AOT-compatible

### Option C: Fluent `MultiMapBuilder<TReturn>` API
- Chosen: Type-safe, composable, maps well to Source Generator approach

### Option D: Source Generator generates typed N-ary mappers at compile time
- Chosen as complement to Option C — generates `MultiMapDescriptor<T1..TN>` based on usage

## Decision

**2-7 entities:** Provide standard Dapper wrapper extensions in `EricksonLopez.SqlBuilder.Dapper`:
```csharp
await connection.QueryAsync<User, Order, User>(
    query,
    (user, order) => { user.Order = order; return user; },
    splitOn: "OrderId");
```

**8+ entities:** Fluent `MultiMapBuilder<TReturn>` in `EricksonLopez.SqlBuilder.Dapper.MultiMap`:
```csharp
var result = await connection.MultiMapAsync<User>(
    query,
    map => map
        .Split<Order>(on: "OrderId")
        .Split<Product>(on: "ProductId")
        .Split<Category>(on: "CategoryId")
        .Into((user, order, product, category) =>
        {
            user.Order = order;
            order.Product = product;
            product.Category = category;
            return user;
        }));
```

## AOT Strategy
Source Generator analyzes `MultiMapBuilder<TReturn>` usage and generates typed `MultiMapDescriptor<T1..TN>` at compile time.
- No runtime `Type[]` construction
- No runtime casting
- Compile-time `splitOn` validation (planned Roslyn analyzer)

## Consequences
### Positive
- Type-safe beyond 7 entities
- AOT-compatible via Source Generator
- Readable fluent API

### Negative
- Requires additional `Dapper.MultiMap` package
- Source Generator complexity

## Performance Impact
2-7 path: same as raw Dapper (wrapper overhead negligible)
8+ path with Source Generator: potentially faster than `Type[]` path (no runtime type dispatch)

## AOT Impact
8+ path: ✅ AOT-safe via generated descriptors
2-7 path: ❌ Inherits Dapper's reflection limitations (use `QueryAotAsync` for AOT)

## Reconsideration Criteria
If Dapper adds native 8+ typed support, deprecate this package.
