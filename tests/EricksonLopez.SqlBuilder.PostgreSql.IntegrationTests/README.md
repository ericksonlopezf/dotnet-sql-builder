# EricksonLopez.SqlBuilder.PostgreSql.IntegrationTests

Integration test suite for PostgreSQL using Testcontainers (`postgres:latest`).

## Scope
- Executes live PostgreSQL CRUD, `NpgsqlCopy` binary bulk copy, multi-mapping, `DISTINCT ON`, and transaction workflows.
- Verifies positional parameter binding `$1..$N` against actual Postgres engine.

## Prerequisites
- Docker daemon running locally or in CI.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.PostgreSql.IntegrationTests/EricksonLopez.SqlBuilder.PostgreSql.IntegrationTests.csproj
```
