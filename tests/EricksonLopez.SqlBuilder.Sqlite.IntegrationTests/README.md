# EricksonLopez.SqlBuilder.Sqlite.IntegrationTests

Integration test suite for SQLite.

## Scope
- Executes live SQLite operations against in-memory (`:memory:`) and local file database instances.
- Verifies dialect constraints and batch operations without requiring external container dependencies.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.Sqlite.IntegrationTests/EricksonLopez.SqlBuilder.Sqlite.IntegrationTests.csproj
```
