# ADR-051: Native AOT Trimming and Dynamic Expression Boundaries

## Status
Accepted — September 2026

## Date
2026-09-14

## Context
`EricksonLopez.SqlBuilder` targets high-performance .NET 8, 9, and 10 microservices where Native AOT compilation is a primary architectural goal.

However, two opposing requirements exist in modern .NET data access:
1. **Developer Ergonomics:** Fluent LINQ expression trees (`u => u.Age > 18 && u.IsActive`) provide natural and type-safe query construction, but extracting captured closure constants and property values at runtime requires evaluating `MemberExpression` and `ConstantExpression` via reflection.
2. **Strict Native AOT Safety:** The .NET IL linker (trimmer) removes unreferenced metadata and disables dynamic JIT code generation. Code paths using runtime reflection on arbitrary closures must be explicitly annotated to warn developers at compile time.

## Decision
Formally establish and document a dual-path architectural boundary between dynamic query composition and strict Native AOT execution:

### 1. Fluent Dynamic Expression Path (Annotated)
- Any API that compiles runtime expression trees into SQL parameters (`SelectQuery<T>.Build(ISqlCompiler)`, `InsertQuery<T>.Build(ISqlCompiler)`, `UpdateQuery<T>.Build(ISqlCompiler)`, `DeleteQuery<T>.Build(ISqlCompiler)`, and dynamic `AotQueryAsync(ISqlQuery, ...)`) is explicitly decorated with:
  ```csharp
  [RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() or pre-compiled results for NativeAOT strict paths.")]
  [RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() or pre-compiled results for NativeAOT strict paths.")]
  ```
- This ensures that developers targeting Native AOT receive immediate build-time diagnostics (`IL2026` / `IL3050`) rather than encountering silent trimming failures in production.

### 2. Strict Native AOT Path (Zero Reflection, 100% Trim-Safe)
The following execution paths are architecturally guaranteed to be 100% free of reflection, dynamic code generation, and trimming warnings:
- **Pre-Compiled Query Execution:** Executing pre-compiled `SqlResult` instances via `EricksonLopez.SqlBuilder.Aot` (`AotQueryExecutor`) or `EricksonLopez.SqlBuilder.Dapper.Aot` (`AotDapperExtensions.AotQueryAsync(IDbConnection, SqlResult, ...)`).
- **Source-Generated Entity Hydration:** Models annotated with `[SqlEntity]` generate static `FromReader(IDataReader)` and `GetReaderParser()` delegates at compile time, eliminating Dapper's runtime IL emit.
- **Source-Generated Filter Extensions:** `FilterGenerator` emits static extension methods (e.g. `.WhereIdEq(1)`) that format parameters directly without creating or evaluating `Expression<Func<T, bool>>` nodes.
- **Native Bulk Copy Strategies:** `SqlBulkCopyStrategy`, `NpgsqlCopyStrategy`, `MySqlBatchStrategy`, and `OracleBulkCopyStrategy` stream data using compile-time readers without reflection.

## Consequences
### Positive
- Total transparency and technical honesty across documentation and compiler warnings.
- Native AOT microservices achieve deterministic, zero-allocation runtime performance without unexpected reflection crashes.
- Developers targeting JIT runtimes continue to enjoy full, idiomatic LINQ expression trees without friction.

### Negative
- Developers compiling Native AOT applications must use pre-compiled queries, source-generated filters, or suppress IL2026/IL3050 if LINQ expressions are preserved via trimming root configurations.

## References
- [Native AOT Guarantees & Limits Guide](../aot.md)
- [ADR-006: Source Generator Strategy](./adr-006-source-generator-strategy.md)
- [ADR-043: Dapper.AOT Integration](./adr-043-dapper-aot-integration.md)
