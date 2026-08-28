# COMPETITIVE MATRIX — EricksonLopez.SqlBuilder

> **Purpose:** Honest, audit-backed competitive analysis.
> Not a marketing document. Every claim is verifiable.
> Updated: 2026-08-14

---

## Comparison Candidates

| Library | Type | Focus | Version |
|---------|------|-------|---------|
| **EricksonLopez.SqlBuilder** | SQL Compiler / AST Builder | AOT-first, strongly typed, dialect-aware | v1.1.x |
| **SqlKata** | Query Builder | Cross-dialect, dynamic composition | v3.x |
| **Dapper** (raw) | Micro-ORM | SQL execution + mapping | v2.1.x |
| **EF Core** | Full ORM | LINQ-to-SQL, change tracking | v8/9/10 |
| **RepoDB** | Micro-ORM | Hybrid: raw SQL + entity ops | v1.13.x |
| **NHibernate** | Full ORM | DDD-focused, enterprise | v5.x |

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Supported and well-implemented |
| 🟡 | Partial / limited / caveats |
| ❌ | Not supported |
| 🚫 | Intentionally not supported (design decision) |
| ⚠️ | Implementation exists but has known bugs or limitations |

---

## 1. API Design & Type Safety

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| Strongly typed query construction | ✅ | ❌ (string-based) | ❌ | ✅ (LINQ) | 🟡 | ✅ (HQL/LINQ) |
| Compiler-verified column names | ✅ | ❌ | ❌ | ✅ | ❌ | ✅ |
| Immutable query objects | ✅ | ❌ (mutable builder) | N/A | ❌ | ❌ | ❌ |
| Safe query composition (no mutation) | ✅ | ❌ | N/A | 🟡 (IQueryable) | ❌ | ❌ |
| Expression Tree → SQL | ✅ | ❌ | ❌ | ✅ | 🟡 | ✅ |
| Raw SQL as escape hatch | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| SQL injection protection | ✅ | ✅ | 🟡 (manual) | ✅ | ✅ | ✅ |
| Fluent builder API | ✅ | ✅ | ❌ | ✅ | 🟡 | ❌ |
| Diagnostic-tagged queries | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `[SqlEntity]` compile-time metadata | ✅ | ❌ | ❌ | ✅ (data annot) | 🟡 | ✅ |

**Verdict:** ESQL has the strongest type safety without requiring a full ORM contract. SqlKata trades safety for flexibility (string-based columns). EF Core's type safety comes with the full ORM overhead. **Unique differentiator: immutable AST — no other library in this set has this.**

---

## 2. NativeAOT & Trim Compatibility

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| Core AOT-compatible | ✅ | ❌ | ❌ | ⚠️ (EF8+) | ❌ | ❌ |
| AOT query execution path | ✅ (AotQueryExecutor) | ❌ | ❌ | ❌ | ❌ | ❌ |
| Source Generator entity metadata | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| `IsAotCompatible = true` declared | ❌ (TD-002) | ❌ | ❌ | ✅ (partial) | ❌ | ❌ |
| Works in WASM AOT (Blazor) | 🟡 (see TD-005) | ❌ | ❌ | ❌ | ❌ | ❌ |
| Works in iOS / strict AOT | 🟡 (see TD-005) | ❌ | ❌ | ❌ | ❌ | ❌ |
| No IL Emit / Dynamic Method | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| No `Activator.CreateInstance` on critical path | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Trim-safe (verified) | 🟡 (partial) | ❌ | ❌ | 🟡 (partial) | ❌ | ❌ |

**Verdict:** ESQL is the only library in this set with an intentional, architectural NativeAOT strategy. The gaps (TD-002, TD-005) are known and have a concrete remediation plan. No competitor has an AOT-first execution path — **this is ESQL's most significant competitive moat.**

---

## 3. Dialect Support

| Dialect | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|---------|------|---------|--------|---------|--------|------------|
| SQL Server | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| PostgreSQL | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| MySQL / MariaDB | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| SQLite | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Oracle | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Dialect-aware compilation (AST) | ✅ | ✅ | ❌ | ✅ | 🟡 | ✅ |
| Separate package per dialect | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Identifier quoting per dialect | ✅ | ✅ | ❌ | ✅ | 🟡 | ✅ |
| FETCH NEXT / LIMIT/OFFSET emulation | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ |
| RETURNING / OUTPUT per dialect | ✅ | 🟡 | ❌ | 🟡 | ❌ | ❌ |
| ON CONFLICT / ON DUPLICATE KEY | ✅ | ✅ | ❌ | 🟡 | ❌ | ❌ |
| LATERAL JOIN / CROSS APPLY | ✅ | ❌ | ❌ | 🟡 | ❌ | ❌ |
| DISTINCT ON (PostgreSQL) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| COPY FROM STDIN (PostgreSQL) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Window functions | ✅ | ❌ | ❌ | 🟡 | ❌ | ❌ |
| Composite keyset cursor pagination | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

**Verdict:** ESQL and EF Core have the most comprehensive dialect support. ESQL's unique strengths are CROSS APPLY/LATERAL, composite keyset cursor pagination, and PostgreSQL-specific features (DISTINCT ON, COPY). SqlKata's dialect support is surface-level and lacks advanced features.

---

## 4. Roslyn Analyzers & Static Analysis

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| Custom Roslyn Analyzer package | ✅ (20+ rules) | ❌ | ❌ | ❌ | ❌ | ❌ |
| DELETE without WHERE → Error | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| UPDATE without WHERE → Error | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| SQL injection detection | ✅ (ESQL002/011) | ❌ | ❌ | ❌ | ❌ | ❌ |
| Retry inside transaction warning | ✅ (ESQL012) | ❌ | ❌ | ❌ | ❌ | ❌ |
| Dialect-incompatible API warning | ✅ (ESQL020) | ❌ | ❌ | ❌ | ❌ | ❌ |
| Code fixes (auto-repair) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

**Verdict:** Roslyn analyzers are **ESQL's unique feature** in this class of libraries. No other SQL builder or micro-ORM ships a production-quality Roslyn analyzer package. This is a strong differentiator especially in enterprise teams.

---

## 5. Performance & Allocation Profile

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| Zero-allocation AOT render path | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Zero-alloc `IDataReader` mapper (SrcGen) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| BenchmarkDotNet suite | ✅ | ❌ | ❌ | 🟡 (EF perf tests) | ❌ | ❌ |
| Allocation regression CI gate | ❌ (planned P1) | ❌ | ❌ | ✅ | ❌ | ❌ |
| `StringBuilder` pooling | ✅ (ObjectPool) | ❌ | N/A | ✅ | ❌ | ❌ |
| Compiled expression caching | ✅ | N/A | N/A | ✅ | N/A | ✅ |
| ImmutableArray vs mutable collections | ✅ | ❌ | N/A | ❌ | ❌ | ❌ |
| Thread-safe immutable AST (zero-lock sharing) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Measured overhead vs raw SQL | 🟡 (some benchmarks) | ❌ | ✅ | 🟡 | ❌ | ❌ |

**Verdict:** ESQL has the most deliberate performance architecture. The AOT render path (zero allocation) has no equivalent. The immutable AST enables thread-safe query sharing without locking. The gaps (allocation CI gate, full benchmark coverage) are in the roadmap.

---

## 6. Bulk Operations

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| `IBulkStrategy` plugin model | ✅ | ❌ | ❌ | ❌ | ✅ | ❌ |
| SQL Server SqlBulkCopy | ✅ | ❌ | ❌ | 🟡 | ✅ | ❌ |
| PostgreSQL COPY FROM STDIN | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Multi-row VALUES bulk insert | ✅ | ✅ | ❌ | ✅ | ✅ | ❌ |
| Bulk upsert strategy | ✅ | 🟡 | ❌ | ❌ | ✅ | ❌ |
| Column selection rules | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| AOT-compatible bulk | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |

**Verdict:** ESQL and RepoDB have the best bulk operation support. ESQL's plugin model (`IBulkStrategy`) is unique and extensible without taking a dependency on the bulk library itself. PostgreSQL COPY FROM STDIN in ESQL has no equivalent in other query builders.

---

## 7. Resilience & Transaction Management

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| Polly v8 integration | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Per-provider transient error detectors | ✅ (5 providers) | ❌ | ❌ | ❌ | 🟡 | ❌ |
| IUnitOfWork + ISavepoint | ✅ | ❌ | ❌ | ✅ | 🟡 | ✅ |
| Retry-safe transaction semantics (ESQL012) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Pre-configured resilience presets | ✅ (Standard/Aggressive/Conservative) | ❌ | ❌ | ❌ | ❌ | ❌ |

**Verdict:** Only ESQL provides out-of-the-box Polly v8 integration with per-provider transient error detection and ESQL012 to guard against the dangerous retry-inside-transaction pattern.

---

## 8. Observability

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| OpenTelemetry ActivitySource | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| OTel Meter (query counter) | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Query tagging (propagates to OTel) | ✅ | ❌ | ❌ | ✅ (query tags) | ❌ | ❌ |
| Slow query detection | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ |
| Parameter masking | ✅ (default) | ❌ | ❌ | ✅ | ❌ | ❌ |
| Structured logging support | ✅ (via OTel) | ❌ | 🟡 | ✅ | ❌ | ❌ |

---

## 9. ORM Features (Deliberately Excluded)

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| Change tracking | 🚫 | 🚫 | 🚫 | ✅ | ❌ | ✅ |
| Navigation properties | 🚫 | 🚫 | 🚫 | ✅ | ❌ | ✅ |
| Lazy loading | 🚫 | 🚫 | 🚫 | ✅ | ❌ | ✅ |
| Identity map / first-level cache | 🚫 | 🚫 | 🚫 | ✅ | ❌ | ✅ |
| Database migrations | 🚫 | 🚫 | 🚫 | ✅ | ❌ | ✅ |
| LINQ IQueryable provider | 🚫 | 🚫 | 🚫 | ✅ | ❌ | ✅ |

**Note:** For ESQL, these are intentional design decisions, not gaps. See `architecture-boundaries.md`.

---

## 10. Ecosystem & Multi-Database Portability

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| Multi-database single codebase | ✅ | ✅ | 🟡 | ✅ | ✅ | ✅ |
| Compiler resolved at runtime | ✅ (RegisterCompiler<TConn>) | ✅ | N/A | ✅ | ✅ | ✅ |
| Per-dialect separate package | ✅ | ❌ | N/A | ✅ | ❌ | ❌ |
| Minimal package footprint | ✅ | ✅ | ✅ | ❌ (large) | ✅ | ❌ |
| DI integration | 🚫 (by design) | ✅ | 🟡 | ✅ | ✅ | ✅ |
| Source Generator dependency | Optional | ❌ | ❌ | ✅ | ❌ | ❌ |

---

## 11. Testing & Quality Infrastructure

| Capability | ESQL | SqlKata | Dapper | EF Core | RepoDB | NHibernate |
|-----------|------|---------|--------|---------|--------|------------|
| Unit test coverage | ✅ (extensive) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Snapshot tests (Verify) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Mutation testing (Stryker) | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| Integration tests per dialect | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| BenchmarkDotNet benchmarks | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ |
| Public API surface tracking | ✅ (Microsoft.CodeAnalysis.PublicApiAnalyzers) | ❌ | ❌ | ✅ | ❌ | ❌ |
| Architecture enforcement (tests) | ✅ (ArchitectureTests.cs) | ❌ | ❌ | ❌ | ❌ | ❌ |

**Verdict:** ESQL's testing infrastructure is best-in-class. Snapshot tests, mutation testing (Stryker), and architecture enforcement tests are unique among OSS SQL builders.

---

## 12. Summary Comparison Table

| Dimension | Winner(s) | ESQL Position |
|-----------|----------|---------------|
| Type Safety | ESQL, EF Core | **Best-in-class for query builders** |
| NativeAOT | ESQL | **Unique — no competitor has an AOT path** |
| Roslyn Analyzers | ESQL | **Unique — no other SQL builder has this** |
| Dialect Coverage (breadth) | ESQL, EF Core | Tied |
| Dialect Coverage (depth/correctness) | ESQL | Best for PG/LT/SS advanced features |
| Bulk Operations | ESQL, RepoDB | ESQL has broadest plugin model |
| ORM Features | EF Core, NHibernate | ESQL intentionally avoids this |
| Resilience | ESQL | **Unique — Polly v8 integration with retry safety** |
| Observability | ESQL, EF Core | Competitive |
| Testing Infrastructure | ESQL | **Best-in-class** |
| Immutable AST | ESQL | **Unique** |
| Community/Adoption | EF Core | ESQL: emerging library |
| Documentation | EF Core | ESQL: improving |
| DI Integration | EF Core, others | ESQL: intentionally avoided |

---

## 13. When to Choose ESQL vs Competitors

### Choose ESQL when:
- You need **NativeAOT / Blazor WASM** compatibility
- You need **SQL Server + PostgreSQL** in the same codebase with dialect-specific optimization
- You want **compile-time SQL correctness guarantees** (no typo bugs from string columns)
- You need **Roslyn analyzer protection** (team safety for junior developers)
- You need **complex pagination**: composite cursor, window pagination
- You need **advanced PostgreSQL features**: DISTINCT ON, COPY, LATERAL JOIN
- You need a **strongly typed, testable, composable** query layer for CQRS
- You have a **high-allocation-sensitive** workload (AOT render path)

### Choose SqlKata when:
- You need **rapid prototyping** with maximum flexibility
- You are comfortable with **string-based column references** (no type safety needed)
- You need **very simple** cross-dialect query building without boilerplate

### Choose Dapper (raw) when:
- Your team is **SQL experts** who prefer to write raw SQL
- You need **absolute maximum performance** with minimal overhead
- You want **simple mapping** without any query builder overhead

### Choose EF Core when:
- You need **full ORM**: change tracking, navigation properties, migrations
- Your domain model drives the database schema
- You are building standard CRUD applications with low performance requirements
- You are targeting SQL Server primarily

### Choose RepoDB when:
- You need **bulk operations** without EF Core overhead
- You want a hybrid between Dapper and EF Core
- You don't need dialect-specific advanced features

---

## 14. Honest ESQL Weaknesses (v1.1.x)

| Gap | Impact | Timeline |
|-----|--------|---------|
| `IsAotCompatible = true` not declared (TD-002) | NuGet AOT metadata missing | v1.2.0 |
| Oracle ROWNUM pagination broken (TD-006) | Wrong results on Oracle <12c | v1.4.0 |
| NULLS FIRST/LAST silently NOP on MySQL/SQLite | Silent wrong sort order | v1.2.0 |
| External pagination project reference (TD-009) | Build fails without sibling repo | v1.2.0 |
| `Expression.Compile()` not attributed (TD-005) | Silent AOT failure in strict environments | v1.2.0 |
| No CI NativeAOT gate | Regressions can be introduced silently | v1.2.0 |
| GROUPING SETS / ROLLUP / CUBE not in API | Analytical queries need raw SQL | v1.3.0 |
| NTH_VALUE not in WindowBuilder | Minor gap | v1.3.0 |
| No INTERSECT ALL / EXCEPT ALL | Minor gap | v1.3.0 |
| DI integration intentionally excluded | May frustrate framework-first teams | Never (by design) |

---

*Competitive analysis based on public GitHub source code review as of 2026-08-14.*
*Re-audit when major new versions of competitor libraries are released.*
