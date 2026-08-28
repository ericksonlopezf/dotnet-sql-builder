# EricksonLopez.SqlBuilder.UnitTests

Comprehensive unit and integration testing suite for the Core engine of `EricksonLopez.SqlBuilder`.

## Scope
- **Builders/**: Fluent query builders for `Select`, `Insert`, `Update`, `Delete`, `Window`, `Case`, and bulk DML operations.
- **Compilers/**: Base AST compilation visitors, parameter handling, and dialect-agnostic SQL expression parsing.
- **Core/**: Core memory management (`CompilationContext`, `StringBuilderPool`), AST partitioning, and multi-threaded concurrency stress tests (`ConcurrencyStressTests`).
- **Extensions/**: Pagination (Offset, Cursor Keyset, WindowPage), dynamic sorting, and entity diffing extensions.
- **Filters/**: Compile-time and runtime filter expressions.
- **Metadata/**: Entity metadata caching (`SqlEntityCache<T>`) and column selection engines.
- **Nodes/**: AST node visitors and immutability invariants.
- **Queries/**: Full end-to-end query generation scenarios.

## Execution
```bash
# Run all unit tests
dotnet test tests/EricksonLopez.SqlBuilder.UnitTests/EricksonLopez.SqlBuilder.UnitTests.csproj

# Run concurrency stress tests only
dotnet test tests/EricksonLopez.SqlBuilder.UnitTests/EricksonLopez.SqlBuilder.UnitTests.csproj --filter "FullyQualifiedName~ConcurrencyStressTests"
```
