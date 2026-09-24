# 06. Transactions and Bulk Operations

## Transactions

The core query builder package (`EricksonLopez.SqlBuilder`) **does not directly manage database connections or transactions** — its role is pure, zero-allocation SQL and parameter AST compilation. 

Execution and transaction management belong to the execution boundary:
- **ADO.NET / Native Dapper:** Manage `IDbTransaction` via connection lifecycle.
- **`EricksonLopez.SqlBuilder.Dapper`:** Direct extension methods over `IDbConnection` accept an optional `IDbTransaction? transaction` parameter, and `DapperConcurrencyExtensions` provides optimistic concurrency checks.

```csharp
await using var tx = await conn.BeginTransactionAsync();
try 
{
    var q1 = Sql.Insert(order).Build(compiler);
    await conn.ExecuteAsync(q1, tx);

    var q2 = Sql.Update<Customer>().Set(c => c.IsActive, true).Where(c => c.Id == 1).Build(compiler);
    await conn.ExecuteAsync(q2, tx);

    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
    throw;
}
```

## Bulk / Batch Operations

For high-throughput mass ingestion, the ecosystem provides dedicated provider-native bulk strategies that bypass individual row overhead:
- **SQL Server:** `SqlBulkCopyStrategy.BulkInsertAsync(connection, entities)` via `EricksonLopez.SqlBuilder.SqlServer`.
- **PostgreSQL:** `NpgsqlCopyStrategy.BulkInsertAsync(connection, entities)` via `EricksonLopez.SqlBuilder.PostgreSql` (binary `COPY`).
- **MySQL:** `MySqlBatchStrategy.BulkInsertAsync(connection, entities)` via `EricksonLopez.SqlBuilder.MySql`.
- **Oracle:** `OracleBulkCopyStrategy.BulkInsertAsync(connection, entities)` via `EricksonLopez.SqlBuilder.Oracle`.

For moderate workloads, parameterized multi-row batching or Dapper collection execution is supported across all dialects.
