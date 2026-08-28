# Package Architecture Reference — EricksonLopez.SqlBuilder

> **Purpose:** Documents the NuGet package graph, dependency rules, and composition strategies.
> Aligned with [ADR-009](decisions/adr-009-dialect-isolation-separate-packages.md)
> and [ADR-003](decisions/adr-003-polly-not-core-dependency.md).
> Last audit: 2026-08-14

---

## Package Graph

```
EricksonLopez.SqlBuilder.Abstractions          (no runtime deps)
        │
        ▼
EricksonLopez.SqlBuilder                       (core AST + compilers)
        │
        ├──► EricksonLopez.SqlBuilder.SqlServer
        ├──► EricksonLopez.SqlBuilder.PostgreSql
        ├──► EricksonLopez.SqlBuilder.MySql
        ├──► EricksonLopez.SqlBuilder.Sqlite
        └──► EricksonLopez.SqlBuilder.Oracle
                │
                ▼ (optional)
        EricksonLopez.SqlBuilder.Dapper         (requires Dapper)
                │
                ├──► EricksonLopez.SqlBuilder.Dapper.UnitOfWork
                ├──► EricksonLopez.SqlBuilder.Dapper.Resilience   (requires Polly v8)
                └──► EricksonLopez.SqlBuilder.Dapper.MultiMap

EricksonLopez.SqlBuilder.SourceGenerators      (build-time only; no runtime dep)
EricksonLopez.SqlBuilder.Analyzers             (build-time only; Roslyn)
EricksonLopez.SqlBuilder.OpenTelemetry         (requires OpenTelemetry.Api)
EricksonLopez.SqlBuilder.Benchmarks            (internal; not shipped to NuGet)
```

---

## Package Descriptions

### `EricksonLopez.SqlBuilder.Abstractions`

- **Role:** Common interfaces, node types, and compiler contracts
- **Runtime deps:** None
- **Key types:** `ISqlQuery`, `IAstQuery`, `ISqlNode`, `ISqlCompiler`, `ISqlVisitor`, `ISqlRenderer`, `IParameterManager`, `ITypeHandler`, `ProviderCapability`
- **AOT:** ✅ Fully AOT compatible

---

### `EricksonLopez.SqlBuilder` (Core)

- **Role:** AST query builders, expression visitor, base compiler, parameter management
- **Runtime deps:** `EricksonLopez.SqlBuilder.Abstractions`, `System.Collections.Immutable`
- **Key types:** `SelectQuery<T>`, `InsertQuery<T>`, `UpdateQuery<T>`, `DeleteQuery<T>`, `MergeQuery<T>`, `RawQuery`, `SqlCompilerBase`, `SqlCompilerVisitor`, `SqlExpressionVisitor`, `AotSqlRendererBase`, `BulkBuilder<T>`, `WindowBuilder<T>`, `Sql` (entry point)
- **AOT:** ✅ Core + SrcGen path; ⚠️ `Expression.Compile()` on first WHERE

---

### `EricksonLopez.SqlBuilder.SqlServer`

- **Role:** T-SQL dialect compiler
- **Runtime deps:** `EricksonLopez.SqlBuilder`
- **Key types:** `SqlServerCompiler`, `SqlServerVisitor`, `SqlServerRenderer`
- **Special:** `OUTPUT INSERTED.*` / `OUTPUT DELETED.*`, `CROSS APPLY`, `OUTER APPLY`, `OFFSET … FETCH NEXT … ROWS ONLY`
- **AOT:** ✅ Fully AOT compatible

---

### `EricksonLopez.SqlBuilder.PostgreSql`

- **Role:** PostgreSQL dialect compiler
- **Runtime deps:** `EricksonLopez.SqlBuilder`
- **Key types:** `PostgreSqlCompiler`, `PostgreSqlVisitor`, `PostgreSqlRenderer`, `CopyNode`
- **Special:** `DISTINCT ON`, `RETURNING`, `ON CONFLICT … DO UPDATE`, `LATERAL`, `UNNEST`, `COPY FROM`
- **AOT:** ✅ Fully AOT compatible

---

### `EricksonLopez.SqlBuilder.MySql`

- **Role:** MySQL/MariaDB dialect compiler
- **Runtime deps:** `EricksonLopez.SqlBuilder`
- **Key types:** `MySqlCompiler`, `MySqlVisitor`, `MySqlRenderer`
- **Special:** `ON DUPLICATE KEY UPDATE`, backtick quoting
- **AOT:** ✅ Fully AOT compatible

---

### `EricksonLopez.SqlBuilder.Sqlite`

- **Role:** SQLite dialect compiler
- **Runtime deps:** `EricksonLopez.SqlBuilder`
- **Key types:** `SqliteCompiler`, `SqliteVisitor`, `SqliteRenderer`
- **Special:** `ON CONFLICT … DO UPDATE`, `RETURNING`
- **AOT:** ✅ Fully AOT compatible

---

### `EricksonLopez.SqlBuilder.Oracle`

- **Role:** Oracle Database dialect compiler
- **Runtime deps:** `EricksonLopez.SqlBuilder`
- **Key types:** `OracleCompiler`, `OracleVisitor`, `OracleRenderer`, `OracleParameterManager`
- **Special:** UPPERCASE identifiers, `:named` params, `RETURNING … INTO :out_col`, `MERGE INTO`
- **AOT:** ✅ Fully AOT compatible

---

### `EricksonLopez.SqlBuilder.Dapper`

- **Role:** Dapper integration — execution extensions and compiler registry
- **Runtime deps:** `EricksonLopez.SqlBuilder`, `Dapper`
- **Key types:** `DapperExtensions` (static), `SqlBuilderDiagnostics`, `QueryMetrics`
- **AOT:** ⚠️ `QueryAsync<T>` is NOT AOT; `QueryAotAsync<T>` IS AOT

---

### `EricksonLopez.SqlBuilder.Dapper.UnitOfWork`

- **Role:** Transaction management via `IUnitOfWork`
- **Runtime deps:** `EricksonLopez.SqlBuilder.Dapper`
- **Key types:** `UnitOfWork`, `IUnitOfWork`, `ISavepoint`, `UnitOfWorkExtensions`
- **AOT:** ✅ Fully AOT compatible

---

### `EricksonLopez.SqlBuilder.Dapper.Resilience`

- **Role:** Polly v8 integration for transient fault handling
- **Runtime deps:** `EricksonLopez.SqlBuilder.Dapper`, `Microsoft.Extensions.Resilience` (Polly v8)
- **Key types:** `SqlResilienceExtensions`, `ISqlTransientErrorDetector`, `SqlServerTransientErrorDetector`, `PostgreSqlTransientErrorDetector`, `MySqlTransientErrorDetector`, `SqlResilienceDefaults`
- **AOT:** ✅ Fully AOT compatible

---

### `EricksonLopez.SqlBuilder.Dapper.MultiMap`

- **Role:** Multi-result-set mapping beyond Dapper's 7-type limit
- **Runtime deps:** `EricksonLopez.SqlBuilder.Dapper`
- **Key types:** `MultiMapBuilder<T1, T2, ...>`
- **AOT:** ⚠️ Depends on Dapper's internal reflection multi-map

---

### `EricksonLopez.SqlBuilder.SourceGenerators`

- **Role:** Incremental Roslyn source generator for AOT-safe entity metadata
- **Runtime deps:** None (build-time tool package)
- **Key types:** `SqlEntityGenerator` (IIncrementalGenerator)
- **Output:** `ISqlEntity`, `IEntityMetadataProvider<T>`, `IBulkSerializer<T>` implementations
- **ADR:** ADR-001 (Stryker exclusion), ADR-006 (strategy)

---

### `EricksonLopez.SqlBuilder.Analyzers`

- **Role:** 21 Roslyn diagnostic analyzers for compile-time SQL safety
- **Runtime deps:** None (analyzer package)
- **Key rules:** ESQL001–ESQL012, ESQL020–ESQL025, SQL003/004/009
- **ADR:** Each rule has an entry in `docs/decisions/index.md`

---

### `EricksonLopez.SqlBuilder.OpenTelemetry`

- **Role:** OTel integration for distributed tracing of SQL operations
- **Runtime deps:** `OpenTelemetry.Api`, `EricksonLopez.SqlBuilder`
- **Key types:** `SqlBuilderInstrumentation`
- **AOT:** ✅ Fully AOT compatible

---

## Dependency Rules (Enforced)

| Rule | Description |
|------|-------------|
| Core must not depend on Dapper | ADR-002 |
| Core must not depend on Polly | ADR-003 |
| Core must not depend on DI (`IServiceCollection`, `ILogger`) | ADR-023 |
| Each dialect package must only depend on Core, not other dialects | ADR-009 |
| Source Generators must have no runtime dependency | ADR-006 |
| Analyzers must have no runtime dependency | — |
| OpenTelemetry package must not be required by Core | — |

---

## Minimum Composition (Typical App)

```xml
<!-- Application targeting SQL Server: -->
<PackageReference Include="EricksonLopez.SqlBuilder.SqlServer" Version="1.1.*" />
<PackageReference Include="EricksonLopez.SqlBuilder.Dapper" Version="1.1.*" />
<PackageReference Include="EricksonLopez.SqlBuilder.SourceGenerators" Version="1.1.*" 
                  PrivateAssets="all" />
<PackageReference Include="EricksonLopez.SqlBuilder.Analyzers" Version="1.1.*" 
                  PrivateAssets="all" />

<!-- Optional: -->
<PackageReference Include="EricksonLopez.SqlBuilder.Dapper.UnitOfWork" Version="1.1.*" />
<PackageReference Include="EricksonLopez.SqlBuilder.Dapper.Resilience" Version="1.1.*" />
<PackageReference Include="EricksonLopez.SqlBuilder.OpenTelemetry" Version="1.1.*" />
```

---

## Package Naming Convention

`EricksonLopez.SqlBuilder[.Subdomain[.Feature]]`

- `EricksonLopez.SqlBuilder` — core namespace root
- `EricksonLopez.SqlBuilder.<Dialect>` — per-database provider
- `EricksonLopez.SqlBuilder.Dapper` — execution adapter root
- `EricksonLopez.SqlBuilder.Dapper.<Feature>` — additive Dapper extension

---

*Update this document when new packages are added or dependency rules change.*
