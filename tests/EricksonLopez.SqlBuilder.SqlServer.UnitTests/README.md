# EricksonLopez.SqlBuilder.SqlServer.UnitTests

Unit test suite for `EricksonLopez.SqlBuilder.SqlServer`.

## Scope
- Validates Microsoft SQL Server (T-SQL) compilation via `SqlServerCompiler`.
- Tests square-bracket escaping `[col]`, `CROSS APPLY` / `OUTER APPLY`, `OFFSET ... FETCH NEXT`, and `NULLS FIRST/LAST` CASE-WHEN emulation.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.SqlServer.UnitTests/EricksonLopez.SqlBuilder.SqlServer.UnitTests.csproj
```
