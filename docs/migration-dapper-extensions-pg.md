# Migration Guide: `dapper-extensions-pg` to `dotnet-sql-builder`

The standalone `dapper-extensions-pg` repository has been deprecated as 100% of its capabilities and responsibilities are now natively supported in `dotnet-sql-builder`.

This guide details how to migrate existing code.

## 1. Pagination

`dotnet-sql-builder` provides the equivalent extension methods in the `EricksonLopez.SqlBuilder.Dapper` namespace.

### Namespace Change
```diff
- using EricksonLopez.DapperExtensions.PostgreSQL.Pagination;
+ using EricksonLopez.SqlBuilder.Dapper;
```

### Code Migration
The pagination parameter object uses standard factory initialization.

```diff
- var page = await connection.QueryPagedAsync<ProductDto>(
+ var page = await connection.QueryPagedRawAsync<ProductDto>(
      sql: "...", countSql: "...",
-     pagination: PaginationParameters.Of(page: 1, pageSize: 20),
+     parameters: PaginationParameters.Create(page: 1, pageSize: 20),
      param: new { Active = true });
```

*(Note: `QueryPagedMultipleAsync` maintains the identical name and signature, updating only the pagination parameter type).*

## 2. Bulk Operations with UNNEST

The PostgreSQL package in `dotnet-sql-builder` supports bulk insertions via `UNNEST`.

### Namespace Change
```diff
- using EricksonLopez.DapperExtensions.PostgreSQL.Bulk;
+ using EricksonLopez.SqlBuilder.PostgreSql;
```

### Code Migration
The `BulkParameters<T>` class structure is identical.

```csharp
// No logic changes required.
var parameters = BulkParameters.From(products)
    .Add("Ids", p => p.Id, NpgsqlDbType.Uuid)
    .Build();

await connection.BulkInsertAsync(sql, parameters);
```

> **Performance Note:** `dotnet-sql-builder` also offers `BulkCopyAsync()` leveraging PostgreSQL binary `COPY` protocol (significantly faster than UNNEST) and `BulkInsertUnnestAsync()` which auto-infers parameters for types implementing `ISqlEntity`.

## 3. Transactions (Unit of Work)

The transactional delegate execution pattern maps directly.

### Namespace Change
```diff
- using EricksonLopez.DapperExtensions.PostgreSQL.Transactions;
+ using EricksonLopez.SqlBuilder.PostgreSql;
```

### Code Migration
```csharp
// Signature is identical.
await connection.ExecuteInTransactionAsync(async trx =>
{
    // Transactional operations
});
```

## 4. JSONB Type Handlers

JSONB serialization with `System.Text.Json` was centralized in the Dapper integration package for cross-dialect reuse.

### Namespace Change
```diff
- using EricksonLopez.DapperExtensions.PostgreSQL.TypeHandlers;
+ using EricksonLopez.SqlBuilder.Dapper;
```

### Code Migration
The `NpgsqlTypeHandlerRegistrar` class was renamed to `PostgreSqlTypeHandlerRegistrar` for architectural consistency.

```diff
- NpgsqlTypeHandlerRegistrar.RegisterJsonbHandler<MyEntity>();
+ PostgreSqlTypeHandlerRegistrar.RegisterJsonbHandler<MyEntity>();
```

## 5. Package References (NuGet)

Remove legacy package references and add `EricksonLopez.SqlBuilder.PostgreSql`:

```xml
<!-- REMOVE -->
<PackageReference Include="EricksonLopez.DapperExtensions.PostgreSQL" Version="..." />

<!-- ADD -->
<PackageReference Include="EricksonLopez.SqlBuilder.PostgreSql" Version="..." />
```
