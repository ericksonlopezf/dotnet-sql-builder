# EricksonLopez.SqlBuilder.Analyzers.UnitTests

Unit test suite for Roslyn Analyzers and CodeFixes in `EricksonLopez.SqlBuilder.Analyzers`.

## Scope
- Validates 26 diagnostic rules (`SQL003`, `SQL004`, `SQL009`, `ESQL001`–`ESQL012`, `ESQL020`–`ESQL026`, `QueryPerformanceAnalyzer`).
- Uses `Microsoft.CodeAnalysis.Testing` to assert compile-time diagnostics and automated CodeFix transformations (e.g., `DELETE` without `WHERE`, obsolete `Sql.Merge`).

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.Analyzers.UnitTests/EricksonLopez.SqlBuilder.Analyzers.UnitTests.csproj
```
