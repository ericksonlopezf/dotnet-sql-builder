# EricksonLopez.SqlBuilder.Oracle.IntegrationTests

Integration test suite for Oracle Database using Testcontainers (`gvenzl/oracle-free`).

## Scope
- Executes live CRUD, pagination (12c+ FETCH FIRST / 11g ROWNUM), and transaction workflows against Oracle Free container instances.
- Verifies Oracle-specific casing and parameter binding.

## Prerequisites
- Docker daemon running locally or in CI.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.Oracle.IntegrationTests/EricksonLopez.SqlBuilder.Oracle.IntegrationTests.csproj
```
