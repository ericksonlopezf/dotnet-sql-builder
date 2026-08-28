# EricksonLopez.SqlBuilder.Dapper.UnitTests

Unit test suite for `EricksonLopez.SqlBuilder.Dapper` and `EricksonLopez.SqlBuilder.Dapper.UnitOfWork`.

## Scope
- Validates Dapper asynchronous execution extensions (`QueryAsync`, `ExecuteAsync`, `ToPagedListAsync`, `BulkInsertAsync`, `BulkUpdateAsync`, `BulkDeleteAsync`).
- Tests Unit of Work and transaction boundary controls (`IUnitOfWork`, `ISavepoint`).
- Asserts parameter mapping and CancellationToken propagation.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.Dapper.UnitTests/EricksonLopez.SqlBuilder.Dapper.UnitTests.csproj
```
