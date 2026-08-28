# EricksonLopez.SqlBuilder.SqlServer.IntegrationTests

Integration test suite for Microsoft SQL Server using Testcontainers (`mcr.microsoft.com/mssql/server:2022-latest`).

## Scope
- Executes live T-SQL CRUD, `SqlBulkCopyStrategy`, `OUTPUT` / `RETURNING` emulation, and transaction workflows against live SQL Server containers.
- Validates Polly transient error retries against simulated deadlocks (error 1205).

## Prerequisites
- Docker daemon running locally or in CI.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.SqlServer.IntegrationTests/EricksonLopez.SqlBuilder.SqlServer.IntegrationTests.csproj
```
