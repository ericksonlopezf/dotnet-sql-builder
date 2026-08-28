# AOT Audit Report — EricksonLopez.SqlBuilder

> **Purpose:** Documents the NativeAOT compatibility scope, verified limitations,
> and the concrete path to full AOT for each component.
> Aligned with [ADR-013](decisions/adr-013-aot-guarantees.md).
> Last audit: 2026-08-14

---

## Executive Summary

EricksonLopez.SqlBuilder is **conditionally NativeAOT compatible**. The core query building,
AST compilation, and Source-Generator-backed entity metadata paths are fully AOT-safe.
The Dapper `QueryAsync<T>` reflection mapper is the only non-AOT path, and a fully AOT-safe
alternative (`QueryAotAsync<T>`) is provided.

---

## Component Compatibility Table

| Component | Package | AOT Status | Mechanism | Notes |
|-----------|---------|-----------|-----------|-------|
| `SelectQuery<T>` / `InsertQuery<T>` / etc. | Core | ✅ Fully AOT | No reflection; `record` with `ImmutableArray` | |
| `SqlCompilerBase` / visitors | Core | ✅ Fully AOT | No reflection in compilation | |
| `SqlExpressionVisitor` | Core | ⚠️ First-call | `Expression.Compile()` on first WHERE | Cached after; not strict AOT |
| `SqlEntityCache<T>` (with `[SqlEntity]`) | Core + SrcGen | ✅ Fully AOT | Static generic initialization from SrcGen metadata | |
| `SqlEntityCache<T>` (without `[SqlEntity]`) | Core | ⚠️ Fallback | `typeof(T).Name` reflection fallback | Produces silent empty ColumnNames |
| `[SqlEntity]` Source Generator output | SrcGen | ✅ Fully AOT | Compile-time code generation | |
| `AotSqlRendererBase.RenderInsert<T>()` | Core | ✅ Fully AOT | `IStaticEntityMetadata<T>` from SrcGen | |
| `AotSqlRendererBase.RenderUpdate<T>()` | Core | ✅ Fully AOT | `IStaticEntityMetadata<T>` from SrcGen | |
| `BulkBuilder<T>` (AOT path) | Core + SrcGen | ✅ Fully AOT | `IBulkSerializer<T>` from SrcGen | |
| `QueryAotAsync<T>(Func<IDataReader, T>)` | Dapper | ✅ Fully AOT | User-supplied mapper delegate | |
| `QueryAsync<T>` (standard Dapper) | Dapper | ❌ Not AOT | Dapper reflection mapper | Use `QueryAotAsync<T>` instead |
| `DapperExtensions.RegisterCompiler<T>()` | Dapper | ✅ Fully AOT | `ConcurrentDictionary<Type, ISqlCompiler>` | Type lookup; no emit |
| `ParameterManager` | Core | ✅ Fully AOT | Dictionary; no reflection | |
| `SqlResilienceExtensions` | Dapper.Resilience | ✅ Fully AOT | Delegates to Dapper `ExecuteAsync` | |
| `UnitOfWork` | Dapper.UnitOfWork | ✅ Fully AOT | Standard `IDbTransaction` usage | |
| Roslyn Analyzers | Analyzers | ✅ Compile-time | Roslyn analysis; runs at build, not runtime | |
| `WindowBuilder<T>` | Core | ✅ Fully AOT | Expression tree to string (compile-time safe) | |
| `SqlBuilderInstrumentation` | OpenTelemetry | ✅ Fully AOT | `ActivitySource` - no reflection | |

---

## NativeAOT Test Requirements (Per ADR-013)

Each release must pass:

```bash
# 1. NativeAOT publish of Core + SourceGenerators
dotnet publish src/EricksonLopez.SqlBuilder.Benchmarks \
  -c Release -r win-x64 --self-contained \
  -p:PublishAot=true -p:UseMonoRuntime=false

# 2. Verify BulkBuilder<T> compiles in NativeAOT
# (BenchmarkDotNet CategoryA includes bulk benchmark — run under NativeAOT)

# 3. Verify QueryAotAsync<T> executes in NativeAOT integration test
# (test project: EricksonLopez.SqlBuilder.Integration.NativeAot.Tests)
```

> **Note:** Tests for NativeAOT are currently not part of the CI pipeline. Adding them is P1
> for the next release cycle. See TD-002 for `IsAotCompatible` metadata gap.

---

## AOT Compatibility Path per Scenario

### Scenario 1: Pure Query Building (No Execution)

```csharp
// ✅ Fully NativeAOT safe — no execution, no Dapper, no reflection
var query = Sql.From<Order>()
    .Where(o => o.Total > 100m)   // ⚠️ first call compiles expression (cached)
    .OrderBy(o => o.CreatedAt)
    .Limit(20);

var result = query.Build(new SqlServerCompiler());
// result.Sql and result.Parameters are ready
```

### Scenario 2: AOT Execution with Source Generators

```csharp
// ✅ Fully NativeAOT safe

// Entity:
[SqlEntity]
public partial class Order { ... }

// Query + Execution:
await connection.QueryAotAsync<Order>(
    Sql.From<Order>().Where(o => o.Active),
    Order.FromReader,   // ← Source Generator emits this
    compiler);
```

### Scenario 3: Standard Dapper (Not NativeAOT)

```csharp
// ❌ Not NativeAOT — uses Dapper reflection mapper
var orders = await connection.QueryAsync<Order>(
    Sql.From<Order>().Where(o => o.Active),
    compiler);
```

### Scenario 4: AOT Bulk Insert

```csharp
// ✅ Fully NativeAOT safe (with [SqlEntity] and SourceGenerators)
var result = new BulkBuilder<Order>()
    .WithStrategy(new SqlBulkCopyStrategy<Order>())
    .Insert(orders)
    .Build(compiler);
```

---

## Known AOT Gaps (Technical Debt)

| Gap | Severity | Status |
|-----|---------|--------|
| `Expression.Compile()` on first WHERE (TD-005) | ⚠️ Medium | Documented; not fixed |
| `SqlEntityCache<T>` fallback reflection (TD-003) | ⚠️ Medium | Needs `[RequiresDynamicCode]` guard |
| `IsAotCompatible = true` not in `.csproj` (TD-002) | ⚠️ Medium | Needs packaging fix |
| Dapper `QueryAsync<T>` (permanent limitation) | ❌ By Design | Use `QueryAotAsync<T>` |

---

## v2.0 Full AOT Target

The Source Generator will emit a full `IDataReader` mapper for each `[SqlEntity]`:

```csharp
// Generated by SourceGenerator (v2.0+):
public static Order FromReader(IDataReader reader)
{
    var parser = new Parser();
    return parser.Parse(reader);
}
```

This eliminates the need for users to pass the mapper manually:
```csharp
// v2.0 (automatic — no user mapper needed):
await connection.QueryAotAsync<Order>(query, compiler);
```

And enables setting `IsAotCompatible = true` on the Dapper package itself.

---

*This document must be updated after each change to Source Generator output, compiler visitor logic,
or Dapper extension method signatures.*
