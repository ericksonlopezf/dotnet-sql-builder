# EricksonLopez.SqlBuilder.Aot.UnitTests

Unit test suite for Native AOT reflection-free execution in `EricksonLopez.SqlBuilder.Aot`.

## Scope
- Validates `AotQueryExecutor` asynchronous query execution over pure ADO.NET `DbCommand` and `DbDataReader`.
- Tests zero-allocation DML rendering pipelines and trimming compatibility.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.Aot.UnitTests/EricksonLopez.SqlBuilder.Aot.UnitTests.csproj
```
