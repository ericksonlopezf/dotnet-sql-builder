# ADR-010: Bulk Insert API Architecture

## Status
Accepted (IBulkStrategy implemented; native strategies deferred to v1.2)

## Date
2026-08-12

## Context
Bulk insert performance is one of the most common bottlenecks in data-intensive .NET applications. Different database providers have radically different optimal bulk mechanisms:

| Provider | Optimal Mechanism | Throughput |
|----------|-------------------|-----------|
| SQL Server | `SqlBulkCopy` | ~500K rows/sec |
| PostgreSQL | `COPY FROM STDIN` (Npgsql) | ~800K rows/sec |
| MySQL | `LOAD DATA INFILE` | ~400K rows/sec |
| SQLite | Multi-row INSERT in transaction | ~50K rows/sec |
| Oracle | `OracleBulkCopy` (`OracleBulkCopyStrategy`) | ~300K rows/sec |

The fallback (batched parameterized INSERT) achieves ~5K-50K rows/sec depending on batch size, which is acceptable for most workloads but not competitive with Dapper Plus for bulk-first scenarios.

## Problem
- A single `BulkInsert` API must work across all dialects transparently
- Provider-native APIs (e.g., `SqlBulkCopy`) require provider-specific assembly references
- The bulk API must be AOT-compatible (no reflection over entity properties at runtime)

## Options Considered

### Option A: Single batched-INSERT implementation for all dialects
- Rejected: leaves 10-100x performance on the table vs. Dapper Plus

### Option B: Hard-code provider detection at runtime (typeof(connection))
- Rejected: brittle, AOT-unfriendly (requires type inspection)

### Option C: Strategy pattern with `IBulkStrategy` interface
- **Chosen (Extensibility SPI)**: Maintained in `EricksonLopez.SqlBuilder.Dapper` for pluggable third-party strategies and fallbacks.

### Option D: Provider-specific static strategies (SqlBulkCopyStrategy, etc.)
- **Chosen (Performance Hot-Path)**: Implemented in each dialect package (`SqlServer`, `PostgreSql`, `MySql`, `Oracle`) to expose full engine capabilities without boxing overhead or intermediate shims.

## Decision

**Dual Architecture (v1.0):**

1. **Provider-Native High-Throughput Strategies (Direct Hot-Path):**
   Each dialect package ships a dedicated static class offering direct-path streaming using Source-Generated readers:
   - `EricksonLopez.SqlBuilder.SqlServer`: `SqlBulkCopyStrategy.BulkInsertAsync(...)` via `Microsoft.Data.SqlClient.SqlBulkCopy`.
   - `EricksonLopez.SqlBuilder.PostgreSql`: `NpgsqlCopyStrategy.BulkInsertAsync(...)` via binary `NpgsqlBinaryImporter COPY`.
   - `EricksonLopez.SqlBuilder.MySql`: `MySqlBatchStrategy.BulkInsertAsync(...)` via `MySqlBatch`.
   - `EricksonLopez.SqlBuilder.Oracle`: `OracleBulkCopyStrategy.BulkInsertAsync(...)` via `OracleBulkCopy`.

2. **Extensible Fallback Strategy SPI (`IBulkStrategy`):**
   `EricksonLopez.SqlBuilder.Dapper` maintains:
   ```csharp
   public interface IBulkStrategy
   {
       bool CanHandle(IDbConnection connection);
       Task<int> ExecuteAsync<T>(IDbConnection connection, IEnumerable<T> entities, ...);
   }
   ```
   Custom bulk providers can register via `DapperExtensions.RegisterBulkStrategy(strategy)`.

3. **Source Generator Role:**
   Source generators emit `GetReaderParser()` and entity metadata, allowing native strategies to stream entity properties directly into provider buffers without runtime reflection.

## Consequences

### Positive
- âœ… Transparent to callers â€” same `BulkInsertAsync` regardless of dialect
- âœ… Extensible â€” user can register custom strategies (e.g., for a custom DB driver)
- âœ… AOT-compatible â€” strategy implementations use Source Generator metadata
- âœ… Fallback guarantees correctness when no native strategy is registered

### Negative
- âŒ Native strategies require the corresponding provider package (e.g., `Npgsql`)
- âŒ Strategy registration must happen at startup â€” easy to forget
- âŒ Integration testing requires real database instances for each dialect

## Performance Impact
- Batched INSERT (current): ~5-50K rows/sec
- `SqlBulkCopy` (planned): ~500K rows/sec
- `COPY FROM STDIN` (planned): ~800K rows/sec

## Reconsideration Criteria
If EF Core 10+ ships a performant bulk insert API usable without a full DbContext, evaluate wrapping it as a strategy.

## References
- [FEATURE_MATRIX.md Â§10 â€” Bulk Operations Analysis](../master-feature-matrix.md)
- `src/EricksonLopez.SqlBuilder/BulkBuilder.cs`
- `src/EricksonLopez.SqlBuilder/Abstractions/IBulkStrategy.cs`
- [ADR-006: Source Generator Strategy](./adr-006-source-generator-strategy.md)
