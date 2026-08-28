# EricksonLopez.SqlBuilder.MySql.UnitTests

Unit test suite for `EricksonLopez.SqlBuilder.MySql`.

## Scope
- Validates MySQL/MariaDB SQL compilation and formatting via `MySqlCompiler`.
- Tests backtick identifier escaping, `NULLS FIRST/LAST` CASE-WHEN emulation, `ON DUPLICATE KEY UPDATE` UPSERT rendering, and set operations.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.MySql.UnitTests/EricksonLopez.SqlBuilder.MySql.UnitTests.csproj
```
