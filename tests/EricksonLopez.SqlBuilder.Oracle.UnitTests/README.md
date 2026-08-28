# EricksonLopez.SqlBuilder.Oracle.UnitTests

Unit test suite for `EricksonLopez.SqlBuilder.Oracle`.

## Scope
- Validates Oracle Database compilation via `OracleCompiler`.
- Tests double-quote escaping, Oracle 12c+ `FETCH FIRST / OFFSET FETCH` pagination, legacy Oracle 11g `ROWNUM` subquery wrapping, and dialect restrictions.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.Oracle.UnitTests/EricksonLopez.SqlBuilder.Oracle.UnitTests.csproj
```
