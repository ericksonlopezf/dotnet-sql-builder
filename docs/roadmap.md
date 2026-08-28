# ROADMAP — EricksonLopez.SqlBuilder
## Strategic Implementation Roadmap

> All architectural decisions are backed by ADRs in `decisions/`.
> Feature coverage is tracked in `master-feature-matrix.md`.
> This roadmap is the execution plan — not a marketing document.
> **Reality principle:** Only tasks traceable to actual code gaps are included.

---

## Version Strategy

| Version | Focus | Status |
|---------|-------|--------|
| **v0.6.0–v1.0.0** | Foundation, dialects, AOT, analyzers, UoW, resilience, bulk | ✅ Complete |
| **v1.1.0** | Phase 0 + Phase 3: Stabilization + AOT declaration (STAB-001–008) | ✅ Complete |
| **v1.2.0** | Phase 1 + Phase 2: SQL engine completion + advanced SQL (CORE-001–004, ADV-001–003) | ✅ Complete |
| **v1.3.0** | Phase 5 + Phase 6: Integration layer & safety analyzers (INT-001–003, SAFE-001–002) | ✅ Complete |
| **v1.4.0** | Phase 4: Performance benchmarks + allocation gates (PERF-001–007) | ✅ Complete |
| **v2.0.0** | Source-generated `IDataReader` mapper & inferred parser execution (AOT-001–007) | ✅ Complete |

---

## Phase 0 — Architectural Stabilization ✅ Complete
### Fix broken abstractions, packaging issues, and silent wrong behavior (ADR-031 to ADR-034)

---

### STAB-001 — Fix Oracle ROWNUM Pagination
```
ID: STAB-001
Title: Implement Oracle ROWNUM/FETCH FIRST pagination in OracleCompiler
Problem: OracleCompiler does not override CompileLimitOffset(). Oracle <12c has no
         LIMIT/OFFSET syntax; Oracle 12c+ uses FETCH FIRST. The fallback produces
         structurally incorrect SQL on Oracle without throwing, silently truncating results.
Current State: Base CompileLimitOffset() either emits nothing or emits LIMIT/OFFSET
               which Oracle does not understand.
Desired State: OracleCompiler.CompileLimitOffset() emits FETCH FIRST n ROWS ONLY
               (12c+, default) or ROWNUM-based wrapping (11g via DialectVersion flag).
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder.Oracle
Affected APIs: OracleCompiler.CompileLimitOffset()
Affected Tests: EricksonLopez.SqlBuilder.Oracle.UnitTests, Oracle.IntegrationTests
Performance Impact: Negligible
AOT Impact: None
Dialect Impact: Oracle only
Breaking Change: No (currently produces wrong SQL; now produces correct SQL)
Migration Required: No
Priority: P2
Estimated Complexity: Small (2-3 days)
Acceptance Criteria:
  - .Limit(20) on Oracle 12c+ emits FETCH FIRST 20 ROWS ONLY
  - .Limit(20).Offset(40) emits OFFSET 40 ROWS FETCH NEXT 20 ROWS ONLY
  - .Limit(20) with OracleDialectVersion.Oracle11g emits ROWNUM-based wrapper
  - Unit tests cover both 11g and 12c+ paths
  - Integration test validates actual Oracle pagination
```

---

### STAB-002 — Declare IsAotCompatible in All AOT-Safe Packages
```
ID: STAB-002
Title: Add IsAotCompatible = true to all NativeAOT-safe package .csproj files
Problem: No package declares IsAotCompatible. NuGet consumers and dotnet publish
         cannot know which packages participate in trim/AOT analysis.
Current State: None of the 15 packages declare IsAotCompatible.
Desired State: All packages that are AOT-safe declare it. The Dapper package
               explicitly declares false.
Dependencies: TD-003 (SqlEntityCache guard) should be fixed first to avoid false AOT claim
Affected Packages: Abstractions, Core, SqlServer, PostgreSql, MySql, Sqlite, Oracle,
                   Aot, SourceGenerators, Analyzers, UnitOfWork, Resilience, OpenTelemetry
Affected APIs: None (packaging only)
Affected Tests: CI NativeAOT publish gate (STAB-006)
Performance Impact: None
AOT Impact: Enables trim analysis at publish time
Dialect Impact: None
Breaking Change: No
Migration Required: No
Priority: P1
Estimated Complexity: Trivial (1 day)
Acceptance Criteria:
  - All identified AOT-safe packages have <IsAotCompatible>true</IsAotCompatible>
  - Dapper package has <IsAotCompatible>false</IsAotCompatible>
  - dotnet publish -p:PublishAot=true produces no ILLink warnings from these packages
```

---

### STAB-003 — Guard SqlEntityCache<T> Reflection Fallback
```
ID: STAB-003
Title: Add [RequiresUnreferencedCode] / throw on SqlEntityCache<T> non-[SqlEntity] fallback
Problem: When T does not implement ISqlEntity, SqlEntityCache<T> silently falls back to:
         TableName = type.Name.ToLower() + "s"; ColumnNames = Array.Empty<string>().
         This produces structurally invalid SQL (no column list) with zero diagnostic.
Current State: Silent wrong behavior; no AOT attributes; no compile-time warning.
Desired State: Either throw InvalidOperationException at startup with a clear message,
               or annotate with [RequiresUnreferencedCode] so trim analysis catches it.
Dependencies: STAB-002 (needed before marking AOT-compatible)
Affected Packages: EricksonLopez.SqlBuilder (Core)
Affected APIs: SqlEntityCache<T> static constructor
Affected Tests: SqlEntityCacheTests.cs (must update tests for non-annotated types)
Performance Impact: None (startup-time only)
AOT Impact: Eliminates silent AOT violation
Dialect Impact: None
Breaking Change: Yes — any code using Sql.From<PlainClass>() without [SqlEntity] will break
Migration Required: Yes — add [SqlEntity] to all entity types or use Sql.From<T>("tableName")
Priority: P1
Estimated Complexity: Small (1-2 days including test updates)
Acceptance Criteria:
  - Using SqlEntityCache<T> where T does not implement ISqlEntity throws
    InvalidOperationException at first access with a message directing user to [SqlEntity]
  - Test: SqlEntityCacheTests covers the exception path
  - Alternatively: method annotated [RequiresUnreferencedCode] producing IL2026 at publish
```

---

### STAB-004 — Fix NULLS FIRST/LAST on MySQL and SQLite
```
ID: STAB-004
Title: MySQL and SQLite NULLS FIRST/LAST silently produce wrong sort order
Problem: On MySQL and SQLite, NullsPosition.First / NullsPosition.Last are silently
         ignored (NOP). A user who explicitly requests NULLS LAST gets default sort
         order with no warning. This is a silent correctness bug.
Current State: NOP behavior — no SQL emitted, no diagnostic.
Desired State: Option A — Emit a CASE WHEN col IS NULL THEN 0 ELSE 1 END expression
               (same strategy as SQL Server CASE WHEN approach) for both MY and LT.
               Option B — Throw NotSupportedException with a clear message.
               Recommendation: Option A (emulation) as it produces correct behavior.
Dependencies: STAB-001 (pattern established for SS)
Affected Packages: EricksonLopez.SqlBuilder.MySql, EricksonLopez.SqlBuilder.Sqlite
Affected APIs: MySqlCompiler, SqliteCompiler order by handling
Affected Tests: MySql.UnitTests, Sqlite.UnitTests — add NULLS FIRST/LAST test cases
Performance Impact: Negligible (one extra CASE WHEN per null-ordered column)
AOT Impact: None
Dialect Impact: MySQL, SQLite
Breaking Change: Yes — previously NOP becomes actual SQL; results change (correctly)
Migration Required: No (fixing silent wrong behavior)
Priority: P2
Estimated Complexity: Small (1-2 days)
Acceptance Criteria:
  - .OrderBy(x => x.DeletedAt, NullsPosition.Last) on MySQL produces
    CASE WHEN "deleted_at" IS NULL THEN 1 ELSE 0 END, "deleted_at" ASC
  - Same for SQLite
  - Unit tests pass for both ascending and descending with NULLS FIRST and NULLS LAST
```

---

### STAB-005 — Attribute Expression.Compile() with [RequiresDynamicCode]
```
ID: STAB-005
Title: Mark SqlExpressionVisitor expression compilation paths with AOT attributes
Problem: SqlExpressionVisitor.Visit() triggers Expression.Compile() on first call.
         This is not NativeAOT-safe in strict environments (iOS, WASM AOT, strict AOT).
         The methods are not annotated, so there is no compile-time warning.
Current State: No [RequiresDynamicCode] or [RequiresUnreferencedCode] on affected methods.
Desired State: All public entry points into SqlExpressionVisitor that trigger expression
               compilation are annotated with [RequiresDynamicCode].
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder (Core)
Affected APIs: SqlExpressionVisitor methods; SelectQuery<T>.Where(); UpdateQuery<T>.Set();
               DeleteQuery<T>.Where()
Affected Tests: No test changes; annotation only
Performance Impact: None
AOT Impact: Surfacing existing limitation as compile-time diagnostic
Dialect Impact: None
Breaking Change: No (annotation only; behavior unchanged)
Migration Required: No (recommendation: use Sql.Raw(FormattableString) in strict AOT)
Priority: P2
Estimated Complexity: Trivial (< 1 day)
Acceptance Criteria:
  - dotnet publish -p:PublishAot=true on a project using typed WHERE expressions
    produces an IL3050 warning (RequiresDynamicCode)
  - Documentation updated to recommend Sql.Raw(FormattableString) for strict AOT paths
```

---

### STAB-006 — Add CI NativeAOT Publish Gate
```
ID: STAB-006
Title: Add GitHub Actions workflow step for NativeAOT publish validation
Problem: There is no CI gate for NativeAOT compilation. A PR can introduce reflection
         silently and the AOT claim remains unverifiable.
Current State: No NativeAOT CI gate exists.
Desired State: CI workflow publishes the Benchmarks project with PublishAot=true and
               TreatWarningsAsErrors=true. Any ILLink warning fails the build.
Dependencies: STAB-002 (IsAotCompatible must be declared first)
Affected Packages: EricksonLopez.SqlBuilder.Benchmarks (as AOT test project)
Affected APIs: None
Affected Tests: CI configuration
Performance Impact: None (CI-only; ~5 min per run)
AOT Impact: Gate prevents AOT regressions
Dialect Impact: None
Breaking Change: No
Migration Required: No
Priority: P1
Estimated Complexity: Small (1 day)
Acceptance Criteria:
  - GitHub Actions workflow includes a NativeAOT publish step
  - CI fails if any new ILLink warning is introduced in Core/dialect packages
  - AOT binary executes successfully against a simple benchmark scenario
```

---

### STAB-007 — Fix External Pagination Project Reference
```
ID: STAB-007
Title: Resolve EricksonLopez.Pagination external project reference in core .csproj
Problem: EricksonLopez.SqlBuilder.csproj contains:
         <ProjectReference Include="..\..\..\dotnet-pagination\src\EricksonLopez.Pagination\…"
         This is a local file system path to a sibling repository. NuGet builds will fail
         unless the sibling repository is also present at the exact relative path.
Current State: External project reference in core package.
Desired State: Either convert to a NuGet package reference (PackageReference) or
               inline the required functionality.
Dependencies: Assess what EricksonLopez.Pagination provides (likely PaginationParameters)
Affected Packages: EricksonLopez.SqlBuilder (Core)
Affected APIs: PaginationExtensions.cs, CursorPaginationExtensions.cs
Affected Tests: All tests that use .Page() and .SeekAfter()
Performance Impact: None
AOT Impact: None
Dialect Impact: None
Breaking Change: Potentially if API surface changes
Migration Required: Depends on resolution approach
Priority: P1
Estimated Complexity: Medium (2-5 days depending on pagination API coupling)
Acceptance Criteria:
  - dotnet build in the SqlBuilder repository with no other sibling repositories present
    succeeds without errors
  - NuGet pack produces a valid package with correct dependencies
```

---

### STAB-008 — Deprecate MergeQuery<T> from README and Add ESQL026 Analyzer
```
ID: STAB-008
Title: Remove MergeQuery<T> from primary README feature list and add analyzer warning
Problem: MergeQuery<T> is [Obsolete] in source but still listed as a featured capability
         in the README. This creates user confusion.
Current State: README shows MergeQuery<T> as a first-class feature.
               No analyzer warns when Sql.Merge<T>() is used.
Desired State: README moves MergeQuery<T> to "Legacy / Escape Hatch" section.
               ESQL026 analyzer warns on Sql.Merge<T>() usage with per-dialect alternatives.
Dependencies: None
Affected Packages: README.md, EricksonLopez.SqlBuilder.Analyzers
Affected APIs: MergeQuery<T>, ESQL026 new rule
Affected Tests: EricksonLopez.SqlBuilder.Analyzers.UnitTests — add ESQL026 tests
Performance Impact: None
AOT Impact: None
Dialect Impact: None
Breaking Change: No (warning, not error)
Migration Required: No
Priority: P3
Estimated Complexity: Small (1-2 days)
Acceptance Criteria:
  - README no longer presents MergeQuery<T> as a recommended feature
  - ESQL026 analyzer reports Warning when Sql.Merge<T>() is detected in code
  - Code fix suggests .OnConflict() for PG/SQLite/MySQL, Sql.Raw() for SS/Oracle
```

---

## Phase 1 — Core SQL Engine Completion
### Ensure all declared dialect support actually works correctly

---

### CORE-001 — Implement INTERSECT ALL / EXCEPT ALL in Set Operations
```
ID: CORE-001
Title: Expose INTERSECT ALL and EXCEPT ALL in SelectQuery<T>
Problem: The feature matrix lists INTERSECT ALL and EXCEPT ALL as not exposed.
         These are valid SQL standard operations supported by SS, PG, MY, LT, OR.
Current State: Only INTERSECT and EXCEPT (distinct) are exposed.
Desired State: .IntersectAll(query) and .ExceptAll(query) extension methods.
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder (Core)
Affected APIs: SelectQuery<T>.IntersectAll(), SelectQuery<T>.ExceptAll()
Affected Tests: QueryBuilderTests.cs, integration tests per dialect
Performance Impact: None
AOT Impact: None
Dialect Impact: Universal
Breaking Change: No (additive)
Migration Required: No
Priority: P3
Estimated Complexity: Trivial (1 day)
Acceptance Criteria:
  - q1.IntersectAll(q2) emits INTERSECT ALL
  - q1.ExceptAll(q2) emits EXCEPT ALL
  - Tests confirm correct dialect output
```

---

### CORE-002 — Add IS DISTINCT FROM / IS NOT DISTINCT FROM Predicate
```
ID: CORE-002
Title: Add IS DISTINCT FROM / IS NOT DISTINCT FROM as a typed expression operator
Problem: IS DISTINCT FROM (PG, LT) is not in the expression visitor or API.
         It is the null-safe equality operator, critical for NULL-safe comparison.
Current State: Not in API; must use Sql.Raw().
Desired State: Sql.IsDistinctFrom<T>(a, b) sentinel method in SqlExpressionVisitor.
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder (Core), EricksonLopez.SqlBuilder.Abstractions
Affected APIs: Sql.IsDistinctFrom, Sql.IsNotDistinctFrom
Affected Tests: SqlExpressionVisitorTests.cs
Performance Impact: None
AOT Impact: None
Dialect Impact: PG: native; LT: native; SS: emulated (IS NULL / <>) or throw; MY: emulated
Breaking Change: No (additive)
Migration Required: No
Priority: P3
Estimated Complexity: Small (2-3 days)
Acceptance Criteria:
  - .Where(x => Sql.IsDistinctFrom(x.Col, value)) emits IS DISTINCT FROM on PG/LT
  - SS and MY emit null-safe comparison emulation or throw NotSupportedException
  - Tests per dialect
```

---

### CORE-003 — NULLIF / COALESCE as SELECT Projection Functions
```
ID: CORE-003
Title: Add NULLIF() and multi-argument COALESCE() to SELECT projections
Problem: COALESCE(col, fallback) exists as a WHERE expression sentinel.
         NULLIF is not available at all. Neither is accessible as a SELECT projection.
Current State: COALESCE is WHERE-only via Sql.Coalesce() sentinel.
               NULLIF does not exist.
Desired State: Both available in typed SELECT expressions.
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder (Core)
Affected APIs: Sql.NullIf<T>(), extended Sql.Coalesce<T>() multi-arg
Affected Tests: SqlExpressionVisitorTests.cs
Performance Impact: None
AOT Impact: None
Dialect Impact: Universal
Breaking Change: No (additive)
Migration Required: No
Priority: P3
Estimated Complexity: Small (2 days)
Acceptance Criteria:
  - Select(x => Sql.NullIf(x.Col, "")) emits NULLIF(col, @p0)
  - Select(x => Sql.Coalesce(x.Col, x.Col2, "default")) emits COALESCE(col, col2, @p0)
```

---

### CORE-004 — GROUPING SETS, ROLLUP, CUBE API
```
ID: CORE-004
Title: Expose GROUPING SETS, ROLLUP, CUBE in GroupBy API
Problem: GROUPING SETS, ROLLUP, CUBE are documented as "not in API; raw SQL only".
         These are essential for analytical reporting queries.
Current State: No API; users must use Sql.Raw().
Desired State: .GroupByRollup(cols[]), .GroupByCube(cols[]), .GroupingSets(groups[])
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder (Core), all dialect compilers
Affected APIs: SelectQuery<T>.GroupByRollup(), GroupByCube(), GroupingSets()
Affected Tests: Unit tests + integration tests per dialect
Performance Impact: None
AOT Impact: None
Dialect Impact: SS/PG/MY/OR: native; LT: throw NotSupportedException (SQLite lacks ROLLUP/CUBE)
Breaking Change: No (additive)
Migration Required: No
Priority: P3
Estimated Complexity: Medium (3-5 days)
Acceptance Criteria:
  - .GroupByRollup("col1", "col2") emits GROUP BY ROLLUP(col1, col2)
  - .GroupByCube(…) emits GROUP BY CUBE(…)
  - .GroupingSets(new[]{…}) emits GROUP BY GROUPING SETS ((…), (…))
  - SQLite throws NotSupportedException with clear message
```

---

## Phase 2 — Advanced SQL Features

---

### ADV-001 — Window Function FILTER (WHERE) Clause
```
ID: ADV-001
Title: Implement typed FILTER (WHERE …) clause on window functions (ADR-018)
Problem: ADR-018 defers this. Window functions like SUM(x) FILTER (WHERE status = 'active')
         OVER (PARTITION BY dept) are not expressible without raw SQL.
Current State: WindowBuilder has _filterExpression field (declared but not compiled).
Desired State: .Filter(x => x.Status == "active") on WindowBuilder emits FILTER (WHERE …)
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder (Core), all dialect compilers
Affected APIs: WindowBuilder<T>.Filter(Expression<Func<T,bool>>), WindowBuilder<T>.Filter(FormattableString)
Affected Tests: Unit tests, integration tests
Performance Impact: None
AOT Impact: Same as existing expression visitor
Dialect Impact: PG: native; SS: not supported natively (throw or raw); MY/LT: throw; OR: throw
Breaking Change: No (additive)
Migration Required: No
Priority: P2
Estimated Complexity: Medium (3-5 days)
Acceptance Criteria:
  - Window.Sum<Order, decimal>(o => o.Amount).Filter(o => o.Status == "active")
    emits SUM(amount) FILTER (WHERE status = @p0) OVER (…) on PostgreSQL
  - SS, MY, LT, OR throw NotSupportedException with message suggesting Sql.Raw()
  - WindowFunctionNode AST updated to store FilterNode
```

---

### ADV-002 — Typed LATERAL JOIN with Outer Column References (ADR-019)
```
ID: ADV-002
Title: Implement full typed LATERAL JOIN with outer-reference resolution
Problem: Current LateralJoin() on PostgreSQL accepts a subquery but cannot reference
         columns from the outer query using typed expressions (Sql.Outer<T>(c => c.Id)).
         The deferred typed outer-reference in the AST is not implemented.
Current State: LateralJoin(subquery, alias, onCondition) works with raw string on condition.
Desired State: Sql.Outer<TEntity>(c => c.Id) sentinel in WHERE of subquery resolves to
               outer.column correctly in the compiled SQL.
Dependencies: ADV-001 would benefit from same compiler machinery
Affected Packages: EricksonLopez.SqlBuilder (Core), EricksonLopez.SqlBuilder.PostgreSql
Affected APIs: New Sql.Outer<T>() sentinel; LateralJoin typed condition
Affected Tests: Unit + PG integration tests
Performance Impact: None
AOT Impact: Same as existing expression visitor
Dialect Impact: PostgreSQL primary; SS CROSS/OUTER APPLY benefiting
Breaking Change: No (additive)
Migration Required: No
Priority: P2
Estimated Complexity: Large (1-2 weeks; AST changes required)
Acceptance Criteria:
  - Sql.From<Customer>().LateralJoin("top_orders",
      Sql.From<Order>().Where(o => o.CustomerId == Sql.Outer<Customer>(c => c.Id)).Limit(3))
    emits correct LATERAL JOIN with outer-table column reference
  - Type-safety verified: renaming CustomerId causes compile error
  - PG integration test passes
```

---

### ADV-003 — Materialized/NOT MATERIALIZED CTE Hints (PostgreSQL)
```
ID: ADV-003
Title: Add MATERIALIZED / NOT MATERIALIZED hints to CTE for PostgreSQL
Problem: PostgreSQL 12+ supports MATERIALIZED and NOT MATERIALIZED hints on CTEs
         to control optimizer behavior. These are not in the API.
Current State: No hint API.
Desired State: .CTE("name", subquery, MaterializationHint.Materialized)
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder.PostgreSql
Affected APIs: SelectQuery<T>.CTE() overload with MaterializationHint enum
Affected Tests: PG unit tests, PG integration tests
Performance Impact: None
AOT Impact: None
Dialect Impact: PostgreSQL only; other dialects ignore hint
Breaking Change: No (additive)
Migration Required: No
Priority: P3
Estimated Complexity: Small (1-2 days)
Acceptance Criteria:
  - CTE("name", subquery, MaterializationHint.Materialized) emits WITH name AS MATERIALIZED (…)
  - Other dialects silently ignore the hint (no throw)
```

---

## Phase 3 — Native AOT Full Path

---

### AOT-001–AOT-007
> See `docs/aot-roadmap.md` for the complete AOT execution plan.
> These tasks are fully specified in the AOT-ROADMAP document.

**Summary of AOT tasks in v1.2.0:**
- AOT-001: Declare `IsAotCompatible` (= STAB-002)
- AOT-002: Guard `SqlEntityCache<T>` fallback (= STAB-003)
- AOT-003: Attribute `Expression.Compile()` (= STAB-005)
- AOT-006: CI NativeAOT gate (= STAB-006)
- AOT-007: Enable trim analyzer in all AOT packages

**Summary of AOT tasks in v2.0.0:**
- AOT-004: Source Generator emits `GetReaderParser()` static abstract member
- AOT-005: `QueryAotAsync<T>` overload with no mapper argument
- Full `IsAotCompatible = true` for all packages without `[RequiresDynamicCode]` violations

---

## Phase 4 — Performance

---

### PERF-001–PERF-007
> See `docs/performance-roadmap.md` for the complete performance execution plan.
> These tasks are fully specified in the PERFORMANCE-ROADMAP document.

**Summary of critical performance tasks (P1):**
- PERF-001: Add AOT path benchmarks (RenderInsert, RenderUpdate)
- PERF-002: Measure `Expression.Compile()` first-call cost
- PERF-003: Add bulk insert benchmark suite
- PERF-007: Add allocation regression CI gates

**Summary of P2 performance tasks:**
- PERF-004: `ImmutableArray` vs `List<T>` AST node allocation profile
- PERF-005: `StringBuilder` pool in `CompilationContext`
- PERF-006: Investigation of opt-in compiled SQL cache

---

## Phase 5 — Integration Layer ✅ Complete
### OpenTelemetry multi-targeting, OTel semantic conventions, and abstract bulk renderers (ADR-038, ADR-039)

---

### INT-001 — OpenTelemetry Package — Add net9.0 and net10.0 Targets

```
ID: INT-001
Title: Add net9.0 and net10.0 to OpenTelemetry package TFMs
Problem: EricksonLopez.SqlBuilder.OpenTelemetry only targets net8.0.
         Consumers on net9.0 or net10.0 cannot reference it without downgrading.
Current State: <TargetFramework>net8.0</TargetFramework>
Desired State: <TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>
Dependencies: Verify OTel SDK compatibility with net9/net10
Affected Packages: EricksonLopez.SqlBuilder.OpenTelemetry
Affected APIs: None
Affected Tests: CI multi-TFM build
Performance Impact: None
AOT Impact: None
Dialect Impact: None
Breaking Change: No (additive)
Migration Required: No
Priority: P2
Estimated Complexity: Trivial (< 1 day)
Acceptance Criteria:
  - Package builds and tests pass on net8.0, net9.0, net10.0
```

---

### INT-002 — OTel db.system Semantic Attribute Per Dialect

```
ID: INT-002
Title: Set OTel db.system tag to dialect-correct value per connection type
Problem: SqlBuilderInstrumentation.StartQueryActivity() sets db.system = "sql" generically.
         OTel semantic conventions require values like "mssql", "postgresql", "mysql", "sqlite".
Current State: db.system = "sql" (generic) in all cases.
Desired State: Dialect-aware db.system resolved from compiler type.
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder.OpenTelemetry
Affected APIs: SqlBuilderInstrumentation.StartQueryActivity()
Affected Tests: Unit tests for tag correctness
Performance Impact: Negligible (one type check per query)
AOT Impact: None
Dialect Impact: All dialects
Breaking Change: No (tag value changes; existing consumers may need filter updates)
Migration Required: Optional — update OTel dashboards if filtering on db.system
Priority: P2
Estimated Complexity: Small (1-2 days)
Acceptance Criteria:
  - Activity started for SQL Server query has db.system = "mssql"
  - Activity started for PostgreSQL query has db.system = "postgresql"
  - Activity started for MySQL query has db.system = "mysql"
  - Activity started for SQLite query has db.system = "sqlite"
  - Activity started for Oracle query has db.system = "oracle"
```

---

### INT-003 — BulkBuilder<T> Abstraction: Make Base Methods abstract

```
ID: INT-003
Title: Convert AotSqlRendererBase bulk methods from virtual to abstract (TD-007)
Problem: RenderBulkInsert<T>(), RenderBulkUpdate<T>(), RenderBulkMerge<T>(),
         RenderBulkUpsert<T>(), and RenderBulkInsertIgnore<T>() throw
         NotSupportedException in the base. Dialect implementors that forget to override
         compile successfully but throw at runtime.
Current State: virtual throws NotSupportedException.
Desired State: abstract — dialect renderers that don't support bulk must override with
               their own NotSupportedException + descriptive message.
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder (Core), all dialect renderer packages
Affected APIs: AotSqlRendererBase, SqlServerRenderer, PostgreSqlRenderer, MySqlRenderer,
               SqliteRenderer, OracleRenderer
Affected Tests: Unit tests for each renderer
Performance Impact: None
AOT Impact: None
Dialect Impact: All dialects must be updated
Breaking Change: Yes — external renderer implementations must override abstract methods
Migration Required: Yes — any external AotSqlRendererBase subclass must add overrides
Priority: P3
Estimated Complexity: Small (1-2 days)
Acceptance Criteria:
  - AotSqlRendererBase bulk methods are abstract
  - All 5 dialect renderers implement them (throwing NotSupportedException with message)
  - SqlServer and PostgreSql renderers implement actual bulk logic
  - Unit tests verify abstract contract is enforced
```

---

## Phase 6 — Developer Safety & Analyzers ✅ Complete
### Roslyn analyzers and compile-time diagnostics (ADR-040)

---

### SAFE-001 — Add ESQL026 Analyzer for Sql.Merge<T>() Usage (= STAB-008)
> See STAB-008 above.

---

### SAFE-002 — Analyzer Coverage for NTH_VALUE Missing Support
```
ID: SAFE-002
Title: Add documentation/diagnostic for NTH_VALUE not being in the API
Problem: NTH_VALUE(col, n) is supported by PG, MY, LT but not exposed in WindowBuilder.
         Developers may not discover the gap until they hit a raw SQL workaround.
Current State: No NTH_VALUE in API; no diagnostic.
Desired State: Add Window.NthValue<T,TKey>(selector, n) to WindowBuilder, or
               add an ESQL diagnostic suggesting raw SQL for NTH_VALUE.
Dependencies: None
Affected Packages: EricksonLopez.SqlBuilder (Core) or Analyzers
Priority: P3
Estimated Complexity: Small (API addition) or Trivial (doc only)
Acceptance Criteria:
  - NTH_VALUE is accessible via typed API for PG/MY/LT
  - SS and OR throw NotSupportedException
```

---

## Phase 7 — Never (Permanently Out of Scope)

> These will **never** be implemented. See ADRs in `docs/decisions/`.

| Feature | Reason | ADR |
|---------|--------|-----|
| Change tracking | Requires mutable in-memory state; violates AST immutability | ADR-007 |
| LINQ IQueryable provider | Impossible to guarantee AOT safety and predictable SQL | ADR-008 |
| Navigation properties | Introduces implicit query generation (N+1 trap) | ADR-007 |
| Migration engine | Out of scope for an AST compilation engine | — |
| Automatic DI integration | Unnecessary coupling; blocks framework-agnostic usage | ADR-023 |
| Automatic query caching | Cache invalidation cannot be implicitly guaranteed | ADR-024 |
| Soft delete global filter | Implicit business logic; explicit predicates required | — |
| Multi-tenancy global filter | Implicit hidden dependency | — |
| Generic cross-dialect MERGE abstraction | Semantic differences make safe abstraction impossible | ADR-025 |
| Automatic retry of mutations | Non-idempotent + retry = duplicate data | ADR-016 |

---

## Roadmap ↔ Feature Matrix ↔ ADR Traceability

| Roadmap Item | FM Section | ADR | Package | Tests |
|--------------|------------|-----|---------|-------|
| STAB-001 (Oracle pagination) | §5 Pagination | TD-006 | Oracle | Oracle.UnitTests |
| STAB-002 (IsAotCompatible) | §13 AOT | TD-002 | All | CI |
| STAB-003 (Cache guard) | §13 AOT | TD-003 | Core | SqlEntityCacheTests |
| STAB-004 (NULLS MY/LT) | §5 ORDER BY | TD-015 | MySql, Sqlite | MY/LT.UnitTests |
| STAB-005 (Expression attr) | §13 AOT | TD-005 | Core | — |
| STAB-006 (CI gate) | §13 AOT | ADR-013 | CI | — |
| STAB-007 (Pagination ref) | §21 Packaging | TD-009 | Core | All |
| STAB-008 (MergeQuery) | §12 MERGE | ADR-025 | Analyzers | Analyzers.UnitTests |
| CORE-001 (INTERSECT ALL) | §7 Set Ops | — | Core | QueryBuilderTests |
| CORE-002 (IS DISTINCT FROM) | §3 WHERE | — | Core | ExpressionVisitorTests |
| CORE-003 (NULLIF/COALESCE) | §3 WHERE | — | Core | ExpressionVisitorTests |
| CORE-004 (GROUPING SETS) | §2 SELECT | — | Core | QueryBuilderTests |
| ADV-001 (FILTER clause) | §8 Window | ADR-018 | Core | WindowBuilderTests |
| ADV-002 (LATERAL outer-ref) | §4 JOINs | ADR-019 | Core, PG | PG.UnitTests |
| ADV-003 (Materialized CTE) | §7 CTEs | — | PostgreSql | PG.UnitTests |
| INT-001 (OTel TFMs) | §19 OTel | — | OpenTelemetry | — |
| INT-002 (db.system tag) | §19 OTel | — | OpenTelemetry | OTel tests |
| INT-003 (Abstract bulk) | §17 Bulk | ADR-010 | Core + dialects | All renderer tests |
| SAFE-001 (ESQL026) | §18 Analyzers | ADR-025 | Analyzers | Analyzers.UnitTests |

---

*No orphan roadmap item. No ADR without traceability. Every item references source, package, tests, and architecture.*
