# EricksonLopez.SqlBuilder.SourceGenerators.UnitTests

Unit test suite for Roslyn Incremental Source Generators in `EricksonLopez.SqlBuilder.SourceGenerators`.

## Scope
- Validates code generation for `[SqlEntity]` annotated classes and records.
- Verifies generation of static metadata maps, column constants, and IDataReader hydration parsers.
- Uses snapshot tests (`Verify.Xunit`) to ensure determinism and syntax validity across various entity shapes (structs, nested classes, enums, Guids).

## Execution
```bash
dotnet test tests/EricksonLopez.SqlBuilder.SourceGenerators.UnitTests/EricksonLopez.SqlBuilder.SourceGenerators.UnitTests.csproj
```
