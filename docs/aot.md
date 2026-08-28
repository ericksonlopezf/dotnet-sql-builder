# NativeAOT Compatibility Guarantees & Limits

EricksonLopez.SqlBuilder is strictly designed to support NativeAOT execution out of the box, fulfilling a core promise of high-performance microservices.

## The Guarantee

All core SQL compilation logic (e.g., `SqlCompilerBase`, `UpdateQuery`, `SelectQuery`, builders, visitors) operates strictly on deterministic Expression Trees (transformed by Source Generators) or explicit abstractions (`ISqlNode`). There are zero occurrences of `System.Reflection.Emit`, `MakeGenericType`, or `MakeGenericMethod` during query compilation.

**Hard guarantees** (architecturally enforced):
- Construction of an AST via `Sql.From<T>()`, `Sql.Insert()`, `Sql.Update()`, `Sql.Delete()` → **0 reflection**
- Compilation of the AST via any `ISqlCompiler.Compile(query)` → **0 reflection**
- Mapping of query results via source-generated `GetReaderParser()` → **0 reflection**
- Bulk serialization via native strategy (`SqlBulkCopyStrategy`, `NpgsqlCopyStrategy`, `MySqlBatchStrategy`) → **0 reflection in hot path**

**Soft guarantees** (require correct configuration):
- `AotQueryExecutor` + `[SqlEntity]` decorated types → **fully NativeAOT compatible end-to-end execution**

## Per-Package AOT Compatibility

| Package | AOT Compatible | Notes |
|---------|:--------------:|-------|
| `EricksonLopez.SqlBuilder` | ✅ | Zero reflection in core. `IsAotCompatible=true` |
| `EricksonLopez.SqlBuilder.Abstractions` | ✅ | Interfaces + struct-based types only |
| `EricksonLopez.SqlBuilder.SqlServer` | ✅ | Compiler is trim-safe |
| `EricksonLopez.SqlBuilder.PostgreSql` | ✅ | Compiler is trim-safe |
| `EricksonLopez.SqlBuilder.MySql` | ✅ | Compiler is trim-safe |
| `EricksonLopez.SqlBuilder.MariaDb` | ✅ | Compiler is trim-safe |
| `EricksonLopez.SqlBuilder.Sqlite` | ✅ | Compiler is trim-safe |
| `EricksonLopez.SqlBuilder.Oracle` | ⚠️ **NOT AOT-safe** | `Oracle.ManagedDataAccess.Core` driver uses reflection internally. This is a third-party driver limitation, not a framework limitation. |
| `EricksonLopez.SqlBuilder.Aot` | ✅ | Purpose-built for AOT via `AotQueryExecutor` |
| `EricksonLopez.SqlBuilder.Dapper` | ⚠️ Partial | Dapper's core result mapping uses `Emit`. Use `Dapper.AOT` interceptors or the `.Aot` package for full AOT safety |
| `EricksonLopez.SqlBuilder.Dapper.Aot` | ✅ | Dapper.AOT interceptors; zero runtime reflection |
| `EricksonLopez.SqlBuilder.SourceGenerators` | ✅ | Build-time only; not deployed |
| `EricksonLopez.SqlBuilder.Analyzers` | ✅ | Build-time only; not deployed |
| `EricksonLopez.SqlBuilder.Pagination` | ✅ | Pure AST extensions; no reflection |
| `EricksonLopez.SqlBuilder.OpenTelemetry` | ✅ | ActivitySource + Meter are trim-safe |

## Limits and Constraints

### 1. Source Generators are Mandatory for Full AOT

To successfully compile an application using NativeAOT, you **must** use the `EricksonLopez.SqlBuilder.SourceGenerators` package.
The Source Generators analyze your `[SqlEntity]` models and generate explicit `GetValues()`, `TableName`, and column mapping methods. Without these, the core engine attempts a Reflection fallback to map properties, which causes NativeAOT compilation warnings (IL2026/IL3050) and may fail at runtime.

### 2. Oracle Database — NOT AOT Compatible

> [!CAUTION]
> The `EricksonLopez.SqlBuilder.Oracle` package wraps `Oracle.ManagedDataAccess.Core`, which relies on internal reflection to establish connections and map types. **NativeAOT publishing with the Oracle dialect is not supported.** This is a driver-level constraint outside the framework's control.
>
> For Oracle workloads requiring AOT, investigate the experimental `Oracle.ManagedDataAccess.OpenTelemetry` or Devart's Oracle driver, but note that neither has official NativeAOT support as of 2026.

### 3. Dapper Multi-Mapping (8+ entities)

NativeAOT compatibility strictly limits ORM mappings. Dapper itself relies on `System.Reflection.Emit`. Therefore, to maintain AOT safety, you should use interceptors like `Dapper.AOT` or rely on explicitly typed data readers (e.g., `SqlDataReader.GetOrdinal()`) instead of Dapper's dynamic APIs. The `SqlBuilder` integrates safely with `Dapper.AOT`.

For 8+ entity multi-mapping: use the source-generated `MultiMapDescriptor<T>` instead of the reflection-based `DapperMultiMappingExtensions` overloads.

### 4. Expression Trees Limits

Expression Trees created at runtime (e.g., `Expression.Lambda(...)`) that invoke methods dynamically might trigger NativeAOT warnings if those methods are pruned by the linker. Always use lambda syntax directly in the code (e.g., `x => x.Id == 5`) so the compiler can statically analyze the references.

### 5. Streaming Queries (`QueryStreamAsync`)

`DapperExtensions.QueryStreamAsync<T>()` requires `DbConnection` (not `IDbConnection`) and is AOT-compatible when using the Dapper.AOT interceptors. Standard Dapper streaming uses `Emit`-based mapping; use `AotConnectionExtensions.AotQueryAsync<T>()` + manual batching for fully AOT-safe streaming.

