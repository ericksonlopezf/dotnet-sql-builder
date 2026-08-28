# 06. Transactions and Bulk Operations

## Transactions

EricksonLopez.SqlBuilder **does not directly manage database connections or transactions**. Its sole role is to generate SQL and its parameters (Dictionary). Transactions must be handled at the execution point (for example, with pure Dapper or ADO.NET).

```csharp
using var tx = await conn.BeginTransactionAsync();
try 
{
    var q1 = Sql.Insert(order).Build(compiler);
    await conn.ExecuteAsync(q1.Sql, q1.Parameters, tx);

    var q2 = Sql.Update<Customer>().Set(c => c.IsActive, true).Where(c => c.Id == 1).Build(compiler);
    await conn.ExecuteAsync(q2.Sql, q2.Parameters, tx);

    await tx.CommitAsync();
}
catch
{
    await tx.RollbackAsync();
}
```

## Bulk / Batch Operations

The ecosystem does not require complex builders for batch; it is enough to iterate the SQL generation or (even better) use the parameter array capabilities that engines like Postgres and SQL Server support.

For real mass insert (Bulk Insert), the native BulkCopy tool or Dapper `ExecuteAsync(sql, objectList)` extensions are recommended.
