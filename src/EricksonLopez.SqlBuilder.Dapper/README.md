# EricksonLopez.SqlBuilder.Dapper

High-performance integration between `EricksonLopez.SqlBuilder` and `Dapper`.

## Purpose

While Dapper excels at object hydration and connection management, it lacks a structured, compile-safe mechanism for dynamic query composition. `EricksonLopez.SqlBuilder.Dapper` bridges this gap by marrying an immutable, strongly typed SQL AST with Dapper's fast query execution pipeline with zero intermediate allocation overhead.

## Architecture & Design Philosophy

- **Zero Additional Reflection:** Reuses Dapper's internal mapper caches and SqlBuilder's source-generated metadata.
- **Fluent Integration:** Seamlessly extends `IDbConnection` and `ISqlQuery` for natural query-and-execute workflows.
- **Multi-Mapping Beyond 7 Entities:** Provides `MultiMapBuilder<T>` supporting 8+ entity graphs.
- **Clean Execution Semantics:** Connects parameterized SQL queries directly to Dapper commands.

## Quick Example

```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;

var query = Sql.From<User>()
    .Where(u => u.IsActive)
    .OrderBy(u => u.LastName)
    .Paginate(1, 20);

var users = await connection.QueryAsync<User>(query, compiler);
```
