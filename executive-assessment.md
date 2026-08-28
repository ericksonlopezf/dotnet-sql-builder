# Final Executive Assessment — EricksonLopez.SqlBuilder
## Strategic Audit Conclusion

> Written from the position of a Principal Architect with full repository access.
> Based on direct source code inspection, not documentation claims.
> Opinionated. Not inflated. Not written to please.

---

## Current Maturity Scores (0–100)

| Dimension | Score | Assessment |
|-----------|:-----:|------------|
| **Architecture** | 82 | Immutable AST is correct and defensible. Dialect isolation (ADR-009) is executed properly. The core design philosophy is coherent. Deductions: external project reference (TD-009), missing AOT declaration (TD-002), duplicated `.csproj` entries (TD-010). |
| **API** | 74 | Fluent API is discoverable and consistent. Expression-based WHERE/JOIN/SET is excellent. Gaps: NULLIF, IS DISTINCT FROM, GROUPING SETS, FILTER on window functions, NTH_VALUE not accessible. `WindowBuilder<T>` is good but incomplete. `MergeQuery<T>` is correctly deprecated but still in README prominently. |
| **SQL Coverage** | 71 | Core CRUD, CTEs, set operations, window functions, pagination (keyset, window, offset) are excellent. Gaps: GROUPING SETS/ROLLUP/CUBE not in API (raw SQL only), NTH_VALUE missing, FILTER on window functions deferred, IS DISTINCT FROM/NULLIF missing, INTERSECT ALL/EXCEPT ALL not exposed. |
| **Dialects** | 76 | 5 dialects implemented. SS, PG, MY, LT are good. Oracle has a critical correctness bug (ROWNUM pagination). MySQL and SQLite have a silent NULLS FIRST/LAST NOP. Overall dialect coverage is the broadest in its class. |
| **AOT** | 55 | Core AST is AOT-safe. `AotQueryExecutor` exists and is correct. BUT: `IsAotCompatible = true` not declared, `[RequiresDynamicCode]` missing on expression visitor, `SqlEntityCache<T>` fallback produces empty columns silently, no CI gate. The *architecture* is AOT-first but the *declaration* and *guard rails* are missing. |
| **Performance** | 63 | `ObjectPool<StringBuilder>` in use. `ImmutableArray<ISqlNode>` prevents mutation overhead. AOT render path (`AotSqlRendererBase`) is zero-allocation by design. But: most performance targets in performance-roadmap.md say "Unknown — needs benchmark." No allocation regression CI gate. The *design* is performance-conscious; the *proof* is incomplete. |
| **Dapper Integration** | 84 | Best-in-class Dapper integration. `QueryAsync`, `ExecuteAsync`, `QueryAotAsync`, `IAsyncEnumerable`, multi-mapping 8+, type handler dual-registration, connection-type compiler resolution. The only deduction is that `QueryAsync<T>` is not AOT-compatible (Dapper's limitation, correctly documented). |
| **Testing** | 88 | Stryker mutation testing, Verify snapshot tests, architecture tests, extensive unit tests, integration tests per dialect. `PublicAPI.Shipped.txt` + `PublicAPI.Unshipped.txt` tracking. This is genuinely best-in-class for the .NET OSS SQL builder space. |
| **Documentation** | 65 | ADRs are comprehensive and well-written. ROADMAP, DIALECT-MATRIX, PERFORMANCE-ROADMAP, AOT-ROADMAP, ARCHITECTURE-BOUNDARIES are excellent strategic documents. Deductions: README still lists `MergeQuery<T>` as a feature despite `[Obsolete]`, some README claims unverified against code, no public API reference docs, no cookbook/example repository. |
| **Security** | 78 | `FormattableString` parameterization is correct. ESQL002/011 guard raw SQL. Dynamic ordering resolves through `PropertyMap` (injection-safe). Gaps: PII in OTel activity statements not scrubbed, `Sql.Raw(string)` still accessible without compiler error (only warning). |
| **Developer Experience** | 73 | Roslyn analyzers (20+ rules) are a **class-leading differentiator** no competitor has. Typed query construction eliminates typo bugs. `Window.Rank<T>().PartitionBy(e => e.Dept).As("rank")` is ergonomic. Deductions: ESQL026 missing for `Sql.Merge<T>()` usage, documentation gap means discoverability is lower than the API quality deserves. |
| **Ecosystem Readiness** | 58 | External project reference to `EricksonLopez.Pagination` breaks NuGet build isolation — this is a P1 blocker for public release. No `IsAotCompatible` means `.NET 9+` tooling won't flag AOT issues. OTel only targets net8.0. Despite having 15 packages, the release chain is not fully independent. |

**Weighted Overall Score: 72/100**

---

## Strategic Position

> **Emerging → Production Ready (Transition)**

The core is production-ready for motivated adopters who understand the library's philosophy. The packaging chain (external project reference) and missing AOT metadata are blocking factors for general production adoption. Once those P1 items are resolved (STAB-002, STAB-003, STAB-006, STAB-007), this library crosses into **Production Ready** territory.

The testing infrastructure and ADR discipline are already at **Mature** level — significantly ahead of where the adoption number would suggest.

---

## Top 10 Priority Actions

These are ordered by: (blocking risk × strategic impact) — not by effort.

| # | Action | Why It's Priority #1-10 | Roadmap ID |
|---|--------|--------------------------|------------|
| **1** | Fix external `EricksonLopez.Pagination` project reference | **Breaks NuGet builds for anyone without the sibling repo. This is a P1 blocker for the entire release chain.** | STAB-007 |
| **2** | Add `IsAotCompatible = true` to all AOT-safe packages | Without this, `dotnet publish -p:PublishAot=true` produces no warnings from the library even when issues exist. The AOT-first claim is unenforceable without this declaration. | STAB-002 |
| **3** | Guard `SqlEntityCache<T>` reflection fallback with explicit `throw` | Currently produces empty `ColumnNames` silently, generating structurally invalid SQL. This is the worst category of bug: **silent wrong behavior**. | STAB-003 |
| **4** | Fix Oracle ROWNUM pagination in `OracleCompiler` | Oracle is the only dialect with a correctness bug that produces wrong query results on real databases. `LIMIT/OFFSET` on Oracle <12c fails at runtime. | STAB-001 |
| **5** | Fix NULLS FIRST/LAST NOP on MySQL and SQLite | Same category as above — silent wrong sort order. `NullsPosition.First` being silently ignored is a correctness trap. Emulate with `CASE WHEN` as SQL Server does. | STAB-004 |
| **6** | Add CI NativeAOT publish gate | Without this, every PR can introduce AOT regressions with no detection. The AOT-first claim must be enforced by CI, not by aspiration. | STAB-006 |
| **7** | Add `[RequiresDynamicCode]` to `SqlExpressionVisitor` | Makes the `Expression.Compile()` limitation visible at publish-time rather than as a runtime failure on iOS/WASM. | STAB-005 |
| **8** | Extend OpenTelemetry package to net9.0 / net10.0 | Consumers on net9/net10 cannot use the OTel package today. | INT-001 |
| **9** | Set OTel `db.system` to dialect-specific value | `"sql"` is not a valid OTel semantic convention value. All dashboards that filter on `db.system` will not capture ESQL queries. | INT-002 |
| **10** | Move `MergeQuery<T>` out of README feature section + add ESQL026 | Reduces user confusion about deprecated APIs. ESQL026 directs users to the correct per-dialect upsert path. | STAB-008 |

---

## Top 10 Things NOT to Build

These are permanent rejections backed by architectural reasoning. Each has (or should have) an ADR.

### 1. Change Tracking (ADR-007)
**Never build.** Change tracking requires a mutable snapshot buffer per entity — architecturally incompatible with an immutable AST and concurrent sharing. Every framework that implements change tracking (EF Core, NHibernate) requires a *session* or *context* that owns entity lifetime. SqlBuilder's design makes this impossible to add without a fundamental rewrite. **Users who need change tracking already have EF Core.**

### 2. LINQ `IQueryable<T>` Provider (ADR-008)
**Never build.** `IQueryable<T>` has 50+ standard operators. Supporting all of them while maintaining NativeAOT safety, dialect correctness, and deterministic SQL output is impossible. Every IQueryable provider in .NET history has at least one operator that silently fetches all rows into memory. The abstraction leaks. **Users who need IQueryable have EF Core.**

### 3. Generic Cross-Dialect MERGE Abstraction (ADR-025)
**Never build.** SQL Server MERGE has *documented concurrency bugs* that produce duplicates even inside explicit transactions. PostgreSQL's `ON CONFLICT` is safe. MySQL's `ON DUPLICATE KEY` uses different conflict detection semantics. A unified `Sql.Upsert<T>()` that works across all dialects would either be the MySQL least-common-denominator (useless) or would require per-dialect parameters (defeating the abstraction). The correct answer is dialect-specific APIs already implemented.

### 4. DI / `IServiceCollection` Auto-Registration (ADR-023)
**Never build.** Adding `services.AddSqlBuilder()` to Core introduces `Microsoft.Extensions.DependencyInjection` as a Core dependency. This breaks: (a) Pure ADO.NET consumers who don't use Microsoft DI, (b) NativeAOT scenarios where source-generated DI is preferred, (c) minimal API scenarios. **Users add compilers to DI in one line themselves.** There is no complexity this solves that isn't already solved.

### 5. Automatic Query Caching (ADR-024)
**Never build.** Cache invalidation requires knowing when the schema or data changes. A query builder cannot know this without being an ORM. An opt-in `SqlCompilerCache` utility (PERF-006) is the correct approach — the user decides the eviction policy. Automatic caching would introduce hidden mutable state incompatible with the thread-safety guarantee of the immutable AST.

### 6. Navigation Properties / Lazy Loading (ADR-007)
**Never build.** Navigation properties require object graph tracking and implicit query generation. `order.Customer` triggering a `SELECT * FROM customers WHERE id = @p0` during object traversal is the N+1 problem by design. This is not a missing feature — it is the wrong abstraction for a SQL compiler. **Users who need this have EF Core.**

### 7. Database Migrations
**Never build.** Migrations are a completely separate tool category. They track schema versions, generate DDL, handle rollback plans, and integrate with deployment pipelines. SqlBuilder is a DML compiler. Mixing DDL lifecycle management into a DML query builder produces a confused tool that does neither well. Flyway, DbUp, EF Core Migrations, and Roundhouse all do this better than any query builder could.

### 8. Automatic Retry of Mutations (ADR-016)
**Never build** automatic retry around mutations. `INSERT`, `UPDATE`, `DELETE` are not idempotent. A transient network failure after the server commits but before the client receives the ACK means the operation succeeded — retrying it produces a duplicate. ESQL012 exists precisely to prevent users from wiring Polly around individual mutations. **Retry belongs around the entire `BeginTransaction → Execute → Commit` unit, not individual statements.**

### 9. IL Emit / Dynamic Proxies
**Never build.** `Reflection.Emit`, `DynamicMethod`, and `ProxyGenerator` patterns are permanently excluded. These break NativeAOT at the architectural level — there is no workaround. Source Generators are the correct answer for any dynamic code generation need. **If it requires IL emit, it does not belong in this library.**

### 10. Implicit Business Logic (Soft Delete, Multi-Tenancy Filters, Audit Fields)
**Never build.** Global soft-delete filters, automatic tenant ID injection, and audit field population (`CreatedAt = DateTime.UtcNow`) are business logic — not SQL compiler concerns. Libraries that embed this require users to configure a thread-local context or ambient state. This creates hidden coupling and breaks both testability and composability. **The correct approach is explicit: `.Where(x => !x.IsDeleted)`, `.Where(x => x.TenantId == currentTenantId)`, `.Set(x => x.UpdatedAt, DateTime.UtcNow)`. One line each. No magic.**

---

## 10 Biggest Architectural Risks

### Risk 1: External Project Reference Breaks Release Independence (P1)
The `EricksonLopez.Pagination` sibling repository reference means SqlBuilder cannot be built, tested, or published without that sibling being present at the exact relative path. This is a release-blocking risk that affects every CI pipeline and every contributor's local setup.

### Risk 2: AOT Claim Without AOT Gate (P1)
The "AOT-first" identity is the library's strongest differentiator. Without a CI NativeAOT publish gate, this claim is aspirational rather than enforced. A single PR that adds reflection to Core can silently break the AOT guarantee.

### Risk 3: Silent Wrong Behavior on Oracle and NULL Ordering (P1/P2)
Oracle ROWNUM pagination and MySQL/SQLite NULLS FIRST/LAST are silent correctness bugs — they produce wrong results with no exception, no diagnostic, and no warning. These erode trust when discovered in production. Silent wrong behavior is categorically worse than a `NotSupportedException`.

### Risk 4: `SqlEntityCache<T>` Fallback Producing Empty ColumnNames (P1)
If a developer uses `Sql.From<PlainClass>()` without `[SqlEntity]`, they get `ColumnNames = Array.Empty<string>()` and structurally invalid SQL. The error will manifest far from the root cause (no columns in SELECT/INSERT is a query execution error, not a compilation error). This needs to throw immediately at `SqlEntityCache<T>` initialization.

### Risk 5: Expression Compilation Not Guarded for Strict AOT (P2)
`SqlExpressionVisitor.Expression.Compile()` will fail silently in iOS publish or strict WASM AOT modes. The missing `[RequiresDynamicCode]` attribute means users discover this at publish time, not at code authoring time. For a library claiming AOT-first identity, this is a significant trust gap.

### Risk 6: Package Proliferation Without Clear Install Guidance (P2)
15+ packages require clear guidance on which combinations to install. A developer who needs SQL Server + Dapper + OTel needs 4+ packages. Without a "Getting Started" decision tree in the README, the first 5 minutes of experience is a package management puzzle.

### Risk 7: Stale FEATURE-MATRIX / ROADMAP Drift (P3)
The existing feature-matrix.md (before this audit) listed features without code verification. As the codebase evolves, documents drift from reality. Without automated doc-verification (e.g., `dotnet-script` that reads public API and compares to FEATURE-MATRIX), this drift will recur.

### Risk 8: `AotSqlRendererBase` Bulk Methods as `virtual` Throws (P3)
A dialect renderer that forgets to override bulk methods compiles without error but throws at runtime. The correct pattern is `abstract`. Until this is fixed, any new dialect implementation is a hidden runtime failure waiting to happen.

### Risk 9: Competitive Convergence on Key Differentiators (P3)
NativeAOT, Roslyn analyzers, and immutable AST are ESQL's moats. If Dapper.AOT matures into full reflection-free execution, or if EF Core ships a slim query builder mode, the differentiation narrows. The library must keep shipping meaningful AOT improvements to stay ahead.

### Risk 10: OTel `db.system = "sql"` Breaks Production Observability (P2)
Dashboard filters in Datadog, Grafana, New Relic, and OTEL Collector all filter on `db.system`. Using `"sql"` means all ESQL queries are invisible to standard database query dashboards. Users will think SqlBuilder doesn't produce traces — when in fact the traces are uncategorized.

---

## 10 Biggest Competitive Advantages

### 1. Immutable AST — Thread-Safe Query Sharing Without Locks
No other library in this class has immutable query objects. `SelectQuery<T>` can be stored in a static field, shared across threads, and composed into new queries without defensive copying or lock acquisition. This is a genuinely novel design choice with real production benefits.

### 2. Roslyn Analyzers — Compile-Time SQL Safety
ESQL001 (DELETE without WHERE → Error), ESQL002 (SQL injection via string concat), ESQL012 (retry inside transaction), ESQL020 (dialect-incompatible API) — these are unique in the .NET SQL builder space. No other SQL builder or micro-ORM ships a production-quality analyzer package. This is a team-safety multiplier.

### 3. NativeAOT Architecture — The Only Library With an AOT Path
`AotQueryExecutor` + `IStaticEntityMetadata<T>` + `BulkBuilder<T>` on the AOT path = reflection-free end-to-end execution. No competitor has this. As .NET 10 pushes AOT into mainstream usage and Blazor WASM adoption grows, this will become an increasingly strong differentiator.

### 4. Composite Keyset Cursor Pagination
`.SeekAfter(CursorKey[])` with multi-column composite cursors (e.g., `(created_at, id)` after a specific row) is uniquely implemented. SqlKata, Dapper, EF Core, and RepoDB all lack this. Cursor pagination is the correct pagination strategy for large datasets and real-time feeds — this is a high-value differentiator.

### 5. PostgreSQL COPY FROM STDIN Bulk Strategy
The highest-throughput PostgreSQL bulk insert mechanism is implemented as a first-class `IBulkStrategy`. No other .NET query builder provides this. At 100k+ rows, COPY is 10–50x faster than multi-row VALUES. This is a production-critical feature for analytics and data pipeline workloads.

### 6. Strongly Typed Query Construction That Actually Compiles
`.Where(x => x.CustomerName.StartsWith("Smith"))` → `WHERE customer_name LIKE 'Smith%'`. Renaming `CustomerName` to `Name` in C# causes a **compile error** in the query. No string-based query builder (`SqlKata`, `Dapper.SqlBuilder`) offers this. It's the difference between discovering a bug at compile time versus at 3am in production.

### 7. Per-Dialect Separate Packages — Install Only What You Need
Users who only target PostgreSQL install exactly 3 packages: `Abstractions`, `Core`, `PostgreSql`. No SQL Server code ships in their binary. This reduces:
- Package footprint
- AOT trim surface
- Potential transitive dependency conflicts

No competitor has this level of package granularity with correct dependency direction.

### 8. Polly v8 Resilience with Per-Provider Transient Error Detection
Pre-built transient error detectors for all 5 dialects (SQL Server, PostgreSQL, MySQL, SQLite, Oracle) + the ESQL012 analyzer that prevents the dangerous "retry inside transaction" anti-pattern. No other SQL library in .NET provides this level of resilience infrastructure.

### 9. Mutation Testing (Stryker) + Snapshot Tests
The test infrastructure quality is objectively best-in-class. Stryker kills mutants that standard coverage metrics miss. Verify snapshot tests catch SQL output regressions even when code coverage doesn't. This means the library's correctness guarantees are more robust than stated coverage numbers suggest.

### 10. `IUnitOfWork` + `ISavepoint` With Correct Semantics
Savepoints (`CREATE SAVEPOINT name`, `ROLLBACK TO SAVEPOINT`, `RELEASE SAVEPOINT`) are correctly implemented with a no-op fallback for non-`DbTransaction` connections. This is often missing from micro-ORM transaction abstractions. Combined with ADR-016 (retry must wrap the entire transactional unit), the transaction semantics are formally correct.

---

## Final Recommendation — 5-Year Architect View

> **If I were the architect responsible for EricksonLopez.SqlBuilder for the next 5 years:**

### What I Would Build

**1. Complete the AOT declaration and CI gate immediately** — before any feature work. The "AOT-first" label is the library's primary competitive claim. It must be enforced by tooling, not stated in prose. Every week without a CI NativeAOT gate is a week of potential regression accumulation.

**2. Resolve the packaging chain** — the external `EricksonLopez.Pagination` project reference is a release-blocking architectural defect. I would inline the required types or convert to a NuGet package reference in the next sprint, nothing else.

**3. In v2.0, generate `GetReaderParser()` via Source Generator** — this eliminates the last user-facing friction in the AOT path. Today: `await conn.QueryAotAsync<Order>(query, Order.FromReader, compiler)`. After AOT-004: `await conn.QueryAotAsync<Order>(query, compiler)`. This makes the AOT path a first-class default, not an afterthought.

**4. Implement GROUPING SETS, ROLLUP, CUBE** — these are analytical SQL features that have no workaround (raw SQL with type erasure is the only option today). This is CORE SQL functionality missing from the API.

**5. Add the Window Function FILTER clause** — it's already architecturally scaffolded (`_filterExpression` in `WindowBuilder<T>`). It's one of the most commonly needed advanced analytical SQL constructs.

**6. Write a public benchmark against SqlKata, Dapper raw, and EF Core compiled queries** — and publish results. ESQL's performance story is architecturally strong but unproven publicly. A BenchmarkDotNet comparison would convert skeptical adopters.

**7. Ship a "Getting Started" cookbook** — the gap between the library's quality and its discoverability is large. A well-structured cookbook covering 10 real-world scenarios (CRUD, pagination, bulk, multi-tenant, CQRS, AOT setup) would 10x adoption.

### What I Would Redesign

**1. `SqlEntityCache<T>` without `[SqlEntity]`** — change from silent empty fallback to explicit `throw InvalidOperationException`. Silent wrong behavior is categorically worse than loud failure. This is a breaking change that should be in v1.2.0.

**2. OTel `db.system` tag** — change from generic `"sql"` to dialect-specific value (`"mssql"`, `"postgresql"`, etc.). This makes ESQL traces visible in all standard observability platforms. One-line fix with immediate production impact.

**3. The `AotSqlRendererBase` bulk method signatures** — from `virtual throw NotSupportedException` to `abstract`. This forces dialect renderer implementors to make an explicit choice rather than inheriting silent failure.

**4. The core `.csproj` structure** — consolidate 4× duplicated `InternalsVisibleTo` blocks, fix the Spanish description, and add `IsAotCompatible`. These are 15-minute fixes that should have been done before any v1.x release.

### What I Would Remove

**1. `MergeQuery<T>` from the public API surface in v2.0** — it's already `[Obsolete]`. The v2.0 release should be the hard removal date. Oracle and SQL Server users should use `Sql.Raw(FormattableString)` with full MERGE syntax.

**2. The `Sql.Raw(string)` overload without the `FormattableString` guardrail** — or at minimum promote ESQL011 from Warning to Error. The `FormattableString` overload is the correct API. The string overload is a compatibility concession that should be made harder to use, not easier.

**3. Redundant package relationships** — audit whether `EricksonLopez.SqlBuilder.Testing` should become a shipped package (making it easier for users to write SqlBuilder-aware tests) rather than an internal-only package.

### What I Would Permanently Refuse to Build

> This list must be defended by ADRs, not just said. ADR-007, ADR-008, ADR-023, ADR-024, ADR-025, ADR-016 already cover most of these.

1. **Change tracking** — forever. If you need change tracking, use EF Core. No version of SqlBuilder should track entity state.

2. **LINQ IQueryable provider** — forever. The SQL translation would be incomplete, the AOT guarantees would be broken, and the maintenance cost would consume the entire engineering capacity.

3. **Automatic global filters** (soft-delete, tenant) — forever. Implicit predicates that attach to every query are a correctness trap and a testability nightmare. The explicit `.Where()` is one line.

4. **Migrations** — forever. Wrong tool category. Use DbUp, Flyway, or EF Core Migrations.

5. **DI registration in Core** — forever. `AddSqlBuilder()` introduces framework coupling. Users add one compiler singleton to DI in their startup code. There is nothing to abstract.

6. **Generic MERGE across all dialects** — forever. The SQL Server MERGE bug is real and documented by Microsoft. An abstraction that hides a correctness hazard is worse than no abstraction.

7. **IL Emit, DynamicMethod, ProxyGenerator** — forever. These kill NativeAOT at the architectural level. Source Generators are the answer.

8. **Automatic query result caching** — forever. Cache invalidation without a full ORM's schema awareness is impossible to do correctly. An opt-in utility (PERF-006) is acceptable; implicit caching in the compiler is not.

9. **Repository pattern implementation** — forever. Build SqlBuilder; users build repositories on top of SqlBuilder. That is the correct layering.

10. **Distributed transactions (MSDTC/XA)** — forever. These are infrastructure-level concerns, not library concerns, and they are not NativeAOT compatible.

---

## The One-Paragraph Summary

EricksonLopez.SqlBuilder is a genuinely well-designed library with an architecturally coherent identity, a disciplined ADR record, class-leading testing infrastructure, and unique differentiators that no competitor currently matches — specifically: Roslyn analyzers, NativeAOT execution path, immutable AST composition, and composite keyset cursor pagination. Its weaknesses are primarily *declaration gaps* (AOT metadata not declared, CI gate absent), *packaging fragility* (external project reference), and *three silent correctness bugs* (Oracle pagination, MySQL/SQLite NULL ordering, `SqlEntityCache<T>` fallback). None of these are architectural flaws — they are fixable in one sprint. Once resolved, this library crosses cleanly into Production Ready territory. The biggest risk to its long-term position is not technical — it is the discoverability gap between the quality of the implementation and the quality of the public-facing documentation and examples. Fix the P1 bugs, declare the AOT guarantees, resolve the packaging chain, and write the cookbook. The architecture is already there.

---

*Assessment produced by full source-code audit. 2026-08-14.*
