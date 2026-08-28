# AOT Roadmap — EricksonLopez.SqlBuilder

> **Purpose:** Concrete NativeAOT and trimming roadmap. This is the execution plan for
> achieving full `IsAotCompatible = true` across all packages.
> Aligned with [ADR-013](decisions/adr-013-aot-guarantees.md).
> Last audit: 2026-08-14

---

## Current AOT State (v1.1.x)

| Component | AOT Status | Blocker |
|-----------|-----------|---------|
| Core AST (`SelectQuery<T>` etc.) | ✅ Fully AOT | None |
| `SqlCompilerBase` + dialect visitors | ✅ Fully AOT | None |
| `SqlEntityCache<T>` (with `[SqlEntity]`) | ✅ Fully AOT | None |
| `SqlEntityCache<T>` (without `[SqlEntity]`) | ⚠️ Reflection fallback | TD-003 |
| `SqlExpressionVisitor` (first call) | ⚠️ `Expression.Compile()` | TD-005 |
| `ParameterManager` | ✅ Fully AOT | None |
| `AotSqlRendererBase.RenderInsert<T>` | ✅ Fully AOT | None |
| `AotSqlRendererBase.RenderUpdate<T>` | ✅ Fully AOT | None |
| `BulkBuilder<T>` (AOT path) | ✅ Fully AOT | None |
| `QueryAotAsync<T>(userMapper)` | ✅ Fully AOT | None |
| Dapper `QueryAsync<T>` | ❌ Not AOT | Dapper limitation (permanent) |
| Source Generator output | ✅ Compile-time | None |
| Roslyn Analyzers | ✅ Compile-time | None |
| `UnitOfWork` | ✅ Fully AOT | None |
| `SqlResilienceExtensions` | ✅ Fully AOT | None |
| `SqlBuilderInstrumentation` (OTel) | ✅ Fully AOT | None |
| `IsAotCompatible = true` in packages | ❌ Not set | TD-002 |
| `[RequiresDynamicCode]` on reflection fallback | ❌ Not set | TD-003 |

---

## Phase 1 — Declaration (v1.2.0) · P1

### AOT-001 — Add `IsAotCompatible = true` to All AOT-Safe Packages

**Files to update:**
```xml
<!-- src/EricksonLopez.SqlBuilder/EricksonLopez.SqlBuilder.csproj -->
<PropertyGroup>
  <IsAotCompatible>true</IsAotCompatible>
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
</PropertyGroup>
```

**Packages:**
- `EricksonLopez.SqlBuilder.Abstractions.csproj`
- `EricksonLopez.SqlBuilder.csproj`
- `EricksonLopez.SqlBuilder.SqlServer.csproj`
- `EricksonLopez.SqlBuilder.PostgreSql.csproj`
- `EricksonLopez.SqlBuilder.MySql.csproj`
- `EricksonLopez.SqlBuilder.Sqlite.csproj`
- `EricksonLopez.SqlBuilder.Oracle.csproj`

**Packages NOT to mark (have known limitations):**
- `EricksonLopez.SqlBuilder.Dapper.csproj` — `QueryAsync<T>` is not AOT

**Acceptance Criterion:** `dotnet publish -p:PublishAot=true` produces zero `ILLink` trim warnings
for the packages marked AOT-compatible.

---

### AOT-002 — Guard `SqlEntityCache<T>` Reflection Fallback

**File:** `src/EricksonLopez.SqlBuilder/SqlEntityCache.cs`

**Current state (problematic):**
```csharp
else
{
    TableName = type.Name.ToLower() + "s";   // reflection on type.Name — acceptable in AOT
    PropertyMap = new Dictionary<string, string>();
    ColumnNames = Array.Empty<string>();      // silently empty — wrong behavior
}
```

**Required fix:**
```csharp
#if NET5_0_OR_GREATER
[System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(
    "SqlEntityCache<T> reflection fallback is used because T does not implement ISqlEntity. " +
    "Add [SqlEntity] attribute and reference EricksonLopez.SqlBuilder.SourceGenerators for AOT-safe behavior.")]
[System.Diagnostics.CodeAnalysis.RequiresDynamicCode(
    "SqlEntityCache<T> fallback requires dynamic code. Use [SqlEntity] with SourceGenerators.")]
#endif
static SqlEntityCache()
{
    // ... existing logic with the else branch
}
```

Or alternatively (stricter):
```csharp
else
{
    throw new InvalidOperationException(
        $"Type '{typeof(T).Name}' must implement ISqlEntity. " +
        "Add [SqlEntity] attribute and reference EricksonLopez.SqlBuilder.SourceGenerators.");
}
```

**Decision:** Throw rather than silently produce empty columns. The silent fallback currently
produces structurally invalid SQL (no column list) with no diagnostic. The exception is safer.

**Acceptance Criterion:** `dotnet publish -p:PublishAot=true` on a project using `SqlEntityCache<T>`
without `[SqlEntity]` either fails with a clear error at publish-time or throws clearly at runtime.

---

### AOT-003 — Document and Attribute `SqlExpressionVisitor.Expression.Compile()`

**File:** `src/EricksonLopez.SqlBuilder/SqlExpressionVisitor.cs`

**Required:**
```csharp
/// <remarks>
/// First invocation of this visitor compiles the expression via <see cref="Expression.Compile()"/>.
/// This is not compatible with strict NativeAOT. Use <c>Sql.Raw(FormattableString)</c>
/// in NativeAOT scenarios requiring maximum AOT fidelity.
/// </remarks>
[System.Diagnostics.CodeAnalysis.RequiresDynamicCode(
    "Expression.Compile() is used on the first call to compile typed WHERE predicates. " +
    "This is not strictly NativeAOT safe. Use Sql.Raw() for NativeAOT-strict scenarios.")]
public override void Visit(ExpressionWhereNode node)
{
    // ...
}
```

**Acceptance Criterion:** The attribute propagates to all public methods that trigger expression
compilation. `PublishReadyToRun` and strict AOT publish produce a visible diagnostic, not a silent failure.

---

## Phase 2 — Source Generator Reader Mapper (v2.0.0) · P1

### AOT-004 — Generate Full `IDataReader` Mapper per `[SqlEntity]`

**Current state:** `QueryAotAsync<T>` requires the user to pass a `Func<IDataReader, T>`:
```csharp
await connection.QueryAotAsync<Order>(query, Order.FromReader, compiler);
```

**Target state (v2.0):** Source Generator emits the mapper automatically:
```csharp
// User code:
await connection.QueryAotAsync<Order>(query, compiler);
// No mapper argument — SrcGen handles it
```

**Implementation plan:**

1. Generator already emits `Order.FromReader(IDataReader reader)` via the `Parser` class (verified).
2. The `QueryAotAsync<T>` overload needs a version that resolves `T.FromReader` at compile time:

```csharp
// New overload (requires T : ISqlEntity):
public static Task<IEnumerable<T>> QueryAotAsync<T>(
    this IDbConnection connection,
    ISqlQuery query,
    ISqlCompiler compiler,
    CancellationToken cancellationToken = default)
    where T : class, ISqlEntity, new()
{
    return connection.QueryAotAsync<T>(query, T.GetReaderParser(), compiler, cancellationToken);
}
```

**Blocker:** `ISqlEntity` must expose `static abstract Func<IDataReader, T> GetReaderParser()` 
via C# 11 static abstract interface members. This requires .NET 7+ target.

**Target framework:** .NET 8+ (current minimum is already .NET 8).

**Acceptance Criterion:**
- Source Generator emits `static Func<IDataReader, T> GetReaderParser()` on all `[SqlEntity]` types
- New `QueryAotAsync<T>` overload (no mapper arg) compiles and runs under `PublishAot=true`
- Zero `ILLink` warnings

---

### AOT-005 — Mark `Dapper` Package `IsAotCompatible = false` and Document

**File:** `src/EricksonLopez.SqlBuilder.Dapper/EricksonLopez.SqlBuilder.Dapper.csproj`

```xml
<PropertyGroup>
  <!-- QueryAsync<T> uses Dapper's reflection mapper — not NativeAOT compatible -->
  <!-- Use QueryAotAsync<T> for NativeAOT scenarios -->
  <IsAotCompatible>false</IsAotCompatible>
</PropertyGroup>
```

Add XML doc on `QueryAsync<T>`:
```csharp
/// <remarks>
/// This method uses Dapper's reflection-based mapper and is NOT compatible with NativeAOT.
/// For NativeAOT scenarios, use <see cref="QueryAotAsync{T}(IDbConnection, ISqlQuery, Func{IDataReader, T}, ISqlCompiler, CancellationToken)"/>.
/// </remarks>
```

---

## Phase 3 — CI AOT Validation (v1.2.0) · P1

### AOT-006 — Add NativeAOT CI Test

**Current state:** There is no CI gate for NativeAOT compilation.

**Required GitHub Actions workflow step:**

```yaml
- name: NativeAOT Publish Test
  run: |
    dotnet publish src/EricksonLopez.SqlBuilder.Benchmarks \
      -c Release -r linux-x64 --self-contained \
      -p:PublishAot=true \
      -p:TreatWarningsAsErrors=true
  continue-on-error: false

- name: Verify AOT Binary Runs
  run: ./out/EricksonLopez.SqlBuilder.Benchmarks --filter *CategoryA* --list flat
```

**Acceptance Criterion:** CI pipeline fails if AOT publish introduces new `ILLink` warnings.

---

## Phase 4 — Trim Analysis (v1.2.0) · P2

### AOT-007 — Enable Trim Analyzer in All AOT Packages

```xml
<PropertyGroup>
  <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
  <SuppressTrimAnalysisWarnings>false</SuppressTrimAnalysisWarnings>
</PropertyGroup>
```

Run `dotnet publish -p:PublishTrimmed=true` and enumerate all `IL2026` / `IL2067` / `IL2091`
warnings. Suppress only with explicit `[UnconditionalSuppressMessage]` and documentation comment.

---

## AOT Feature Matrix (After Completion)

| Component | v1.1.x | v1.2.0 | v2.0.0 |
|-----------|--------|--------|--------|
| Core AST | ✅ | ✅ | ✅ |
| Dialect compilers | ✅ | ✅ | ✅ |
| `SqlExpressionVisitor` | ⚠️ | ⚠️ (documented) | ⚠️ |
| `SqlEntityCache<T>` fallback | ⚠️ | ✅ (throws) | ✅ |
| `IsAotCompatible` metadata | ❌ | ✅ | ✅ |
| `QueryAotAsync<T>` (user mapper) | ✅ | ✅ | ✅ |
| `QueryAotAsync<T>` (auto mapper) | ❌ | ❌ | ✅ |
| Dapper `QueryAsync<T>` | ❌ | ❌ | ❌ (permanent) |
| CI NativeAOT gate | ❌ | ✅ | ✅ |
| Trim analysis clean | ❌ | ✅ | ✅ |

---

*This roadmap supersedes the AOT section of ADR-013. ADR-013 remains the decision record;
this document is the execution plan.*
