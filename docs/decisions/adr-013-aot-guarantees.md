# ADR-013: AOT Guarantees and NativeAOT Compatibility Scope

## Status
Accepted

## Context
.NET NativeAOT requires: no runtime reflection, no `Type.GetType()`, no `Activator.CreateInstance()`, no IL emit, no dynamic code generation. The .NET 10 ecosystem has introduced `IsAotCompatible` metadata for packages.

## Scope Definition

### Fully AOT-Compatible (guaranteed)

| Component | Mechanism | Guarantee |
|-----------|----------|-----------|
| Core AST (records) | No reflection | ✅ Always |
| `SqlEntityCache<T>` | Static generic initialization | ✅ Always |
| `AotSqlRendererBase` | Source-generated metadata | ✅ Always |
| `BulkBuilder<T>` (AOT path) | `IStaticEntityMetadata<T>` from SrcGen | ✅ Always |
| `QueryAotAsync<T>` | User-provided `Func<IDataReader, T>` | ✅ Always |
| Source Generators | Compile-time code generation | ✅ Always |
| `ExecuteAsync` | No reflection in parameter passing | ✅ Always |

### Not AOT-Compatible (documented limitation)

| Component | Issue | Mitigation |
|-----------|-------|-----------|
| Dapper `QueryAsync<T>` | Reflection-based object mapper | Use `QueryAotAsync<T>` |
| `SqlExpressionVisitor` first call | `Expression.Compile()` at first use | Cached; subsequent calls are AOT-safe |
| Dapper `TypeHandler` | Uses `typeof(T)` at runtime | Acceptable; registration happens once at startup |

## Decision
Full NativeAOT compatibility requires this combination:
1. **Core package** (`EricksonLopez.SqlBuilder`)
2. **Source Generators** (`EricksonLopez.SqlBuilder.SourceGenerators`) — entity metadata
3. **`QueryAotAsync<T>`** — for query execution without Dapper reflection

The standard `QueryAsync<T>` in the Dapper package is **NOT NativeAOT-compatible** — this is Dapper's own limitation, not ours.

## AOT Test Plan
Each release must include:
- NativeAOT publish test for Core + SourceGenerators
- Verification that `BulkBuilder<T>` compiles cleanly in NativeAOT
- Verification that `QueryAotAsync<T>` executes in NativeAOT

## Future Path to Full AOT (v2.0)
Source Generator generates `IDataReader` mapper for each `[SqlEntity]` type, enabling fully AOT-compatible execution without relying on Dapper's reflection mapper.

## Consequences
### Positive
- Clear documented scope for NativeAOT
- Existing users not misled by overclaimed AOT support

### Negative
- Standard Dapper `QueryAsync<T>` path is not AOT — users must use `QueryAotAsync<T>` for NativeAOT scenarios

## API Impact
Documentation must clearly label:
- `QueryAsync<T>` — NOT NativeAOT compatible
- `QueryAotAsync<T>` — NativeAOT compatible (requires user mapper function)
- `BulkBuilder<T>` with SrcGen — NativeAOT compatible

## Reconsideration Criteria
When Dapper.AOT achieves full NativeAOT compatibility, evaluate whether to route through it instead of our custom `QueryAotAsync<T>` path.
