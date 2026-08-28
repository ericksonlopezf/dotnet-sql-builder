# EricksonLopez.SqlBuilder.Sqlite.UnitTests

Unit test suite for `EricksonLopez.SqlBuilder.Sqlite`.

## Scope
- Validates SQLite SQL compilation via `SqliteCompiler`.
- Tests double-quote identifier escaping, `LIMIT ... OFFSET`, `INSERT OR REPLACE` / `INSERT OR IGNORE`, and `NULLS FIRST/LAST` CASE-WHEN emulation.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.Sqlite.UnitTests/EricksonLopez.SqlBuilder.Sqlite.UnitTests.csproj
```
