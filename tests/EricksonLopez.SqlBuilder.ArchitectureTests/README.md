# EricksonLopez.SqlBuilder.ArchitectureTests

Architecture and structural boundary tests for the `EricksonLopez.SqlBuilder` solution using ArchUnitNET.

## Scope
- Enforces strict architectural layering and boundaries (e.g., Core has zero dependencies on Dapper, DI, or specific Dialects).
- Asserts that dialect packages do not reference each other.
- Verifies that Native AOT packages maintain zero reflection emit and trimming safety.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.ArchitectureTests/EricksonLopez.SqlBuilder.ArchitectureTests.csproj
```
