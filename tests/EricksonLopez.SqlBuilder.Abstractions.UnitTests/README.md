# EricksonLopez.SqlBuilder.Abstractions.UnitTests

Unit test suite for the `EricksonLopez.SqlBuilder.Abstractions` project.

## Scope
- Validates the core AST node hierarchy (`ISqlNode`, `SelectNode`, `InsertNode`, `UpdateNode`, `DeleteNode`, `CteNode`, `WindowFunctionNode`, `JoinNode`, etc.).
- Verifies record immutability, copy semantics, and structural equality.
- Tests core interfaces and contract guarantees without runtime dependencies.

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.Abstractions.UnitTests/EricksonLopez.SqlBuilder.Abstractions.UnitTests.csproj
```
