# EricksonLopez.SqlBuilder.PostgreSql.UnitTests

Unit test suite for `EricksonLopez.SqlBuilder.PostgreSql`.

## Scope
- Validates PostgreSQL compilation via `PostgreSqlCompiler`.
- Tests double-quote escaping, positional numbering `$1..$N`, `DISTINCT ON`, `CROSS/LEFT JOIN LATERAL`, window `FILTER (WHERE ...)`, CTE hints `MATERIALIZED`/`NOT MATERIALIZED`, and `ON CONFLICT DO UPDATE/NOTHING`.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.PostgreSql.UnitTests/EricksonLopez.SqlBuilder.PostgreSql.UnitTests.csproj
```
