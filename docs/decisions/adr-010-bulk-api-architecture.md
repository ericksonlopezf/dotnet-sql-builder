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
| Oracle | `OracleBulkCopy` | ~300K rows/sec |

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
- **Chosen**: Open/closed — new strategies registered without modifying Core

### Option D: Provider-specific extension methods (SqlServerBulkInsert, etc.)
- Rejected: forces users to know which method to call; inconsistent API

## Decision

**Current implementation (v1.0):**
```csharp
// Core defines the abstraction
public interface IBulkStrategy
{
    ValueTask ExecuteAsync<T>(IDbConnection connection, IEnumerable<T> entities,
        BulkOptions options, CancellationToken ct) where T : ISqlEntity;
}

// DapperExtensions.BulkInsertAsync resolves strategy:
// 1. Check registry for connection type → use registered native strategy
// 2. Fall back to batched multi-row INSERT
```

**Future native strategies (v1.2):**
```
EricksonLopez.SqlBuilder.SqlServer  → registers SqlBulkCopyStrategy
EricksonLopez.SqlBuilder.PostgreSql → registers NpgsqlCopyStrategy
EricksonLopez.SqlBuilder.MySql      → registers MySqlBatchStrategy
```

**Strategy registration (per dialect package startup):**
```csharp
BulkStrategyRegistry.Register<SqlConnection>(new SqlBulkCopyStrategy());
```

**Source Generator role:** Generates `IStaticEntityMetadata<T>` which native strategies use to read column names and values without reflection.

## Consequences

### Positive
- ✅ Transparent to callers — same `BulkInsertAsync` regardless of dialect
- ✅ Extensible — user can register custom strategies (e.g., for a custom DB driver)
- ✅ AOT-compatible — strategy implementations use Source Generator metadata
- ✅ Fallback guarantees correctness when no native strategy is registered

### Negative
- ❌ Native strategies require the corresponding provider package (e.g., `Npgsql`)
- ❌ Strategy registration must happen at startup — easy to forget
- ❌ Integration testing requires real database instances for each dialect

## Performance Impact
- Batched INSERT (current): ~5-50K rows/sec
- `SqlBulkCopy` (planned): ~500K rows/sec
- `COPY FROM STDIN` (planned): ~800K rows/sec

## Reconsideration Criteria
If EF Core 10+ ships a performant bulk insert API usable without a full DbContext, evaluate wrapping it as a strategy.

## References
- [FEATURE_MATRIX.md §10 — Bulk Operations Analysis](../../FEATURE_MATRIX.md)
- `src/EricksonLopez.SqlBuilder/BulkBuilder.cs`
- `src/EricksonLopez.SqlBuilder/Abstractions/IBulkStrategy.cs`
- [ADR-006: Source Generator Strategy](./adr-006-source-generator-strategy.md)
