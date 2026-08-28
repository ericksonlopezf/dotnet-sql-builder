# Technical Debt Register — EricksonLopez.SqlBuilder

> Living document. All items are also summarized in [`master-feature-matrix.md`](master-feature-matrix.md).
> Updated: 2026-08-14 | Source: Full code-level audit

---

## Priority Scale

| Priority | Meaning |
|---------|---------|
| P1 | Correctness/safety risk — fix before next stable release |
| P2 | User-visible limitation or silent wrong behavior |
| P3 | Code quality / maintainability / future-proofing |

---

## Open Items

### TD-001 — `roadmap.md` Stale Entries · P1

**Status:** ✅ Fixed in this audit (roadmap.md updated)

**Description:**  
`roadmap.md` incorrectly listed `INSERT INTO … SELECT`, `CROSS APPLY`, and `OUTER APPLY`
as "deferred" features. All three are fully implemented in the codebase (v0.9.0/v1.0.0)
via `InsertQuery<T>.FromSelect()`, `SelectQuery<T>.CrossApply()`, `SelectQuery<T>.OuterApply()`.

**Fix applied:** roadmap.md updated with a "Shipped Features (Previously Stale)" section.

---

### TD-002 — `IsAotCompatible = true` Missing from NuGet Metadata · P1

**Area:** Packaging / all `.csproj` files

**Description:**  
All packages that are NativeAOT-compatible (Core, SourceGenerators, dialect packages that
don't use Dapper's reflection path) should declare `<IsAotCompatible>true</IsAotCompatible>`
in their `.csproj`. Without this, `dotnet publish -r native` with AOT will not benefit from
package-level trim analysis and may produce spurious warnings.

**Packages to update:**
- `EricksonLopez.SqlBuilder.csproj`
- `EricksonLopez.SqlBuilder.Abstractions.csproj`
- `EricksonLopez.SqlBuilder.SourceGenerators.csproj`
- All dialect compilers (`SqlServer`, `PostgreSql`, `MySql`, `Sqlite`, `Oracle`)

**Remediation:**
```xml
<PropertyGroup>
  <IsAotCompatible>true</IsAotCompatible>
</PropertyGroup>
```

---

### TD-003 — `SqlEntityCache<T>` Reflection Fallback Not Guarded · P1

**Area:** `src/EricksonLopez.SqlBuilder/SqlEntityCache.cs`

**Description:**  
When `T` does not implement `ISqlEntity` (i.e., no `[SqlEntity]` attribute and no Source Generator),
`SqlEntityCache<T>` falls back to:
```csharp
TableName = type.Name.ToLower() + "s";
ColumnNames = Array.Empty<string>();
```
This path uses `typeof(T).Name` at runtime. In NativeAOT, this is fine for `type.Name`, but the
`Activator.CreateInstance<T>()` call (if present) would be unsafe. More critically, this fallback
produces empty `ColumnNames` which silently generates incorrect SQL with no error.

**Remediation:**
1. Mark the fallback with `[RequiresUnreferencedCode("Use [SqlEntity] attribute with SourceGenerators for AOT-safe table mapping.")]`
2. Consider throwing a `NotSupportedException` instead of silently falling back — the silent behavior
   produces structurally invalid SQL (no columns).

---

### TD-004 — SQL Server `NULLS FIRST/LAST` Silently Ignored · P2

**Area:** `src/EricksonLopez.SqlBuilder.SqlServer/SqlServerCompiler.cs`

**Description:**  
`SqlServerVisitor.AppendNullsPosition()` is a NOP — it appends nothing when `NullsPosition.First`
or `NullsPosition.Last` is specified. This produces silently incorrect sort order on SQL Server when
a user explicitly requests `NULLS FIRST` or `NULLS LAST`.

**Current code:**
```csharp
protected override void AppendNullsPosition(NullsPosition nulls)
{
    // Append nothing — the column was already emitted; log diagnostic only
}
```

**Correct approach (SQL Server IIF emulation):**
SQL Server sorts NULLs last in ascending and first in descending by default. To control null position:
```sql
-- NULLS FIRST (ASC): inject IIF(col IS NULL, 0, 1), col ASC
-- NULLS LAST  (ASC): inject IIF(col IS NULL, 1, 0), col ASC
```

**Remediation:**
Since the column name is already written to `Context.Sql` by the time `AppendNullsPosition` is called,
the `OrderByNode` / `ThenByNode` compilation must be refactored to look ahead and inject the `IIF()`
expression **before** the column name. The cleanest fix is to handle this inside `CompileOrderBys()`
for the `SqlServerCompiler` override.

---

### TD-005 — `Expression.Compile()` on First WHERE Call · P2

**Area:** `src/EricksonLopez.SqlBuilder/SqlExpressionVisitor.cs`

**Description:**  
On the first invocation of any typed `Where(x => ...)` expression, the `SqlExpressionVisitor`
calls `Expression.Compile()` to execute predicates. While the result is cached per expression
instance, this is not strictly NativeAOT safe in environments that forbid JIT compilation
(strict AOT, iOS, WASM AOT).

**Current mitigation:** ADR-013 documents this as a known limitation with a recommended workaround
(`Sql.Raw(FormattableString)`).

**Remediation for v2.0:**
Use a pre-compiled delegate approach: the Source Generator could emit pre-compiled expression evaluators
for `[SqlEntity]` types, eliminating the `Expression.Compile()` call for annotated types.

**Immediate action:**
1. Ensure this limitation is prominently documented in the NativeAOT section of the README
2. Add `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]` attributes to the affected methods

---

### TD-006 — Oracle ROWNUM Pagination Not Implemented · P2

**Area:** `src/EricksonLopez.SqlBuilder.Oracle/OracleCompiler.cs`

**Description:**  
`OracleCompiler` does not override `CompileLimitOffset()`. As a result, Oracle queries using
`.Limit()` / `.Offset()` fall through to the base implementation which may produce standard
`LIMIT/OFFSET` syntax — syntax that Oracle does not support in versions prior to 12c.

**Remediation:**
Override `OracleCompiler.CompileLimitOffset()`:

```csharp
// Oracle 12c+ (preferred):
// SELECT * FROM table FETCH FIRST 20 ROWS ONLY
// SELECT * FROM table OFFSET 40 ROWS FETCH NEXT 20 ROWS ONLY

// Oracle 11g (legacy):
// SELECT * FROM (SELECT t.*, ROWNUM AS rn FROM table t WHERE ROWNUM <= 60) WHERE rn > 40
```

Target: v1.4.0. Implement both `FETCH FIRST` (12c+) and `ROWNUM` (11g) paths with a dialect flag.

---

### TD-007 — `AotSqlRendererBase` Bulk Methods Should Be Abstract · P3

**Area:** `src/EricksonLopez.SqlBuilder/AotSqlRendererBase.cs`

**Description:**  
`RenderBulkInsert<T>()`, `RenderBulkUpdate<T>()`, `RenderBulkMerge<T>()`, `RenderBulkUpsert<T>()`,
and `RenderBulkInsertIgnore<T>()` all throw `NotSupportedException` in the base:

```csharp
public virtual BulkSqlResult RenderBulkInsert<T>(...) 
    => throw new NotSupportedException("Bulk operations are not yet implemented for this dialect.");
```

This is confusing for implementors — a dialect renderer that forgets to override will compile fine
but throw at runtime. The correct pattern is `abstract`.

**Remediation:**
```csharp
public abstract BulkSqlResult RenderBulkInsert<T>(...) where T : IStaticEntityMetadata<T>;
```

Dialect renderers that genuinely don't support bulk (e.g., SQLite) should override with their own
`NotSupportedException` + descriptive message. Breaking change: any external renderer implementations
must be updated. Schedule for v1.2.0.

---

### TD-008 — `MergeQuery<T>` Still Documented as Active Feature · P3

**Area:** `README.md`, main feature sections

**Description:**  
`MergeQuery<T>` is marked `[Obsolete]` in the source but is still listed as a primary feature
in the README. This creates a discoverability problem: users pick it up, see the obsolete warning,
and have no clear guidance on the recommended alternative per dialect:
- SQL Server: `Sql.Merge<T>()` (raw MergeQuery) or raw SQL
- PostgreSQL: `Sql.Insert<T>(...).OnConflict(...).DoUpdate(...)`
- MySQL: `Sql.Insert<T>(...).OnConflict(...).DoUpdate(...)`

**Remediation:**
1. Move `MergeQuery<T>` to a "Legacy / Escape Hatch" section in README
2. Add a code example for the recommended per-dialect upsert pattern
3. Add ESQL026 analyzer warning if `Sql.Merge<T>()` is used, recommending the `OnConflict` API

---

---

### TD-009 — External `EricksonLopez.Pagination` Project Reference · P1

**Area:** `src/EricksonLopez.SqlBuilder/EricksonLopez.SqlBuilder.csproj`

**Description:**  
The core package `.csproj` contains:
```xml
<ProjectReference Include="..\..\..\dotnet-pagination\src\EricksonLopez.Pagination\EricksonLopez.Pagination.csproj" />
```
This is a relative path to a sibling repository. `dotnet build` and `dotnet pack` fail unless
the sibling repository exists at the exact relative path.

**Remediation:**  
Convert to a `PackageReference` if the pagination package is published to NuGet, or inline
the required types (`PaginationParameters`, `PagedList<T>`) directly in the Core package.

---

### TD-010 — `InternalsVisibleTo` Duplicated 4× in Core `.csproj` · P3

**Area:** `src/EricksonLopez.SqlBuilder/EricksonLopez.SqlBuilder.csproj`

**Description:**  
The core `.csproj` has 4 separate `<ItemGroup>` blocks each duplicating the full set of
`<InternalsVisibleTo>` entries. This creates maintenance risk: future test project names
must be added in 4 places.

**Remediation:**  
Consolidate all `<InternalsVisibleTo>` entries into a single `<ItemGroup>`.

---

### TD-011 — Spanish Text in Core Package `Description` · P3

**Area:** `src/EricksonLopez.SqlBuilder/EricksonLopez.SqlBuilder.csproj:37`

**Description:**  
```xml
<!-- Legacy Spanish Description (translated): "The immutable NativeAOT-friendly SQL builder core." -->
<Description>Immutable AOT-friendly SQL Builder core package.</Description>
```
Package description previously contained Spanish text. Corrected to standard English.

**Remediation:**  
Update to English: `Immutable AOT-friendly SQL Builder core package.`

---

### TD-012 — OpenTelemetry Package Only Targets `net8.0` · P2

**Area:** `src/EricksonLopez.SqlBuilder.OpenTelemetry/EricksonLopez.SqlBuilder.OpenTelemetry.csproj`

**Description:**  
All other packages target `net8.0;net9.0;net10.0` or at minimum `net8.0;net10.0`.
The OpenTelemetry package only targets `net8.0`. Consumers on net9 or net10 get the net8
assembly, which may have compatibility issues with newer OTel SDK versions.

**Remediation:**  
Update to `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` and verify OTel SDK
compatibility for each TFM.

---

### TD-013 — OTel `db.system` Tag Uses Generic `"sql"` · P2

**Area:** `src/EricksonLopez.SqlBuilder.OpenTelemetry/SqlBuilderInstrumentation.cs:33`

**Description:**  
```csharp
activity.SetTag("db.system", "sql");
```
OTel semantic conventions (v1.28+) require `db.system` to be the specific DBMS name:
`mssql`, `postgresql`, `mysql`, `sqlite`, `oracle`. Using `"sql"` prevents dashboard
filters from working correctly.

**Remediation:**  
Pass the `ISqlCompiler` type to `StartQueryActivity()` and resolve `db.system` from
the compiler type name.

---

### TD-014 — No CI NativeAOT Publish Gate · P1

**Area:** GitHub Actions / CI pipeline

**Description:**  
There is no CI step that publishes with `PublishAot=true`. AOT regressions (new uses of
reflection, Activator, IL Emit) can be introduced without any CI failure.

**Remediation:**  
Add a CI job that publishes the Benchmarks project with `PublishAot=true -p:TreatWarningsAsErrors=true`.
See `docs/aot-roadmap.md § AOT-006` for the full workflow definition.

---

### TD-015 — NULLS FIRST/LAST Silently NOP on MySQL and SQLite · P2

**Area:** `src/EricksonLopez.SqlBuilder.MySql/MySqlCompiler.cs`,
`src/EricksonLopez.SqlBuilder.Sqlite/SqliteCompiler.cs`

**Description:**  
When `.OrderBy(x => x.Col, NullsPosition.First)` or `.OrderBy(x => x.Col, NullsPosition.Last)`
is called for MySQL or SQLite queries, the NullsPosition is silently ignored. The result is
that the sort order does not match the developer's intent with zero diagnostic.

SQL Server already implements the `CASE WHEN col IS NULL THEN 0 ELSE 1 END` emulation
(added during TD-004 remediation). MySQL and SQLite need the same.

**Remediation:**  
Override `AppendNullsPosition()` in `MySqlCompiler` and `SqliteCompiler` to emit the
`CASE WHEN` expression in the same pattern as `SqlServerCompiler`.

---

### TD-016 — `BulkBuilder<T>` Identity Retrieval After Insert Not Universal · P3

**Area:** `src/EricksonLopez.SqlBuilder/Builders/Bulk/BulkBuilder.cs`

**Description:**  
After a bulk insert, users commonly need the auto-generated primary keys (e.g., inserted IDs).
There is no standardized return mechanism in `BulkBuilder<T>` for identity retrieval.
SQL Server's `SqlBulkCopy` has a workaround (read back the identity range), PostgreSQL's
`COPY` does not expose inserted IDs, MySQL multi-row VALUES can use `LAST_INSERT_ID()`.

**Remediation:**  
Design and implement `IBulkStrategy<T>.GetInsertedIds()` or a post-insert reader on the
strategy. Schedule for v2.0 when the bulk strategy API can be revised.

---

## Resolved Items

| ID | Description | Resolution |
|----|-------------|-----------|
| TD-001 | roadmap.md stale entries | ✅ Updated roadmap.md (2026-08-14) |

---

## Tracking

> New technical debt items discovered during PR review must be added here with a priority
> before the PR is merged. Items at P1 must have a fix target version assigned.
