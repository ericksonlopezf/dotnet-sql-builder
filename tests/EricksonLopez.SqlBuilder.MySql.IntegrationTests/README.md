# EricksonLopez.SqlBuilder.MySql.IntegrationTests

Integration test suite for MySQL / MariaDB using Testcontainers.

## Scope
- Executes live CRUD, bulk insertions, and transaction workflows against MySQL container instances.
- Verifies dialect-specific syntax against actual MySQL engine.

## Prerequisites
- Docker daemon running locally or in CI.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.MySql.IntegrationTests/EricksonLopez.SqlBuilder.MySql.IntegrationTests.csproj
```
