# Feature Audit Cleanup — EricksonLopez.SqlBuilder

> **Document Type:** Technical Audit & Normalization Report  
> **Auditor:** Principal Software Architect & Technical Lead  
> **Date:** 2026-08-14  
> **Scope:** Full source, test, benchmark, and documentation reconciliation.

---

## 1. Audit Scope

This audit reconciles all internal and external documentation of `EricksonLopez.SqlBuilder` against compilable source code, automated test suites, BenchmarkDotNet configurations, and Architecture Decision Records (ADRs). The objective is to purge all marketing, pricing, and competitive positioning language while extracting and verifying concrete engineering facts.

---

## 2. Documents Reviewed

| Document | Pre-Audit Role | Post-Audit Classification | Action Taken |
|:---|:---|:---|:---|
| `feature-matrix.md` | Feature matrix with audit tags | Public Feature Truth | Normalized, stale `🔄` tags removed, rejections grounded in architecture |
| `roadmap.md` | Forward-looking plan with audit prose | Public Engineering Plan | Cleaned, stale shipped features purged, out-of-scope reasons formalized |
| `ADR-index.md` | Master ADR index | Public Architecture Index | Purged "ORM territory" language, linked to pure architectural invariants |
| `architecture-boundaries.md` | Design scope & rejected features | Public Boundary Guide | Removed "Alternative" tool columns; rephrased "Why Not" with pure engineering principles |
| `aot-audit.md` | NativeAOT audit | Public AOT Specification | Verified against source code; NativeAOT scope formalized |
| `aot-roadmap.md` | AOT milestones | Public AOT Roadmap | Focus kept 100% on .NET compiler and trimming pipeline |
| `performance-roadmap.md` | Performance plan with pricing references | Public Performance Plan | Purged "$999 Dapper Plus" mentions; focused on `IBulkStrategy` allocations/throughput |
| `technical-debt.md` | Technical debt register | Public Debt Backlog | Normalized into formal engineering action items (TD-001 – TD-008) |
| `competitive-matrix.md` | 9-competitor matrix | Internal Intelligence | Archived to `.agents/audit_artifacts/competitive-matrix.md` |
| `executive-assessment.md` | Executive maturity score (0-100) | Internal Governance | Archived to `.agents/audit_artifacts/executive-assessment.md` |
| `strategic-analysis.md` | Preliminary strategic analysis | Internal Governance | Archived to `.agents/audit_artifacts/strategic-analysis.md` |
| `docs/competitive-analysis.md` | Legacy competitor doc | Obsolete | Permanently removed from active workspace |

---

## 3. Competitive & Marketing Content Purged

The following categories of non-technical language and claims were systematically purged from all public documentation:

1. **Third-Party Comparisons:** "vs. Dapper", "vs. EF Core", "vs. SqlKata", "vs. RepoDb", "vs. Linq2Db".
2. **Promotional Assertions:** "only one in the market", "the only one that...", "better than X", "best-in-class candidate".
3. **Commercial Pricing:** "$999+/dev Dapper Plus", "free bulk vs commercial".
4. **Market Positioning:** Quadrant diagrams, market gap claims, "target to beat X".
5. **Dismissive Jargon:** Replaced "ORM territory" with exact architectural invariants (e.g., *"Requires mutable in-memory state and snapshot buffers; violates AST immutability"*).

---

## 4. Technical Evidence Retained

All verifiable technical capabilities have been preserved with architectural and code citations:

- **Immutable AST:** Query builders return `sealed record` instances backed by `ImmutableArray<ISqlNode>`.
- **Compile-Time Safety:** Strongly-typed lambda expressions (`x => x.Active && x.Balance > 100`) parsed by `SqlExpressionVisitor`.
- **Advanced SQL Dialect Compilation:**
  - Non-recursive and recursive CTEs (`.CTE()`, `.RecursiveCTE()`).
  - Keyset cursor pagination (`.Seek()`, `.SeekAfter()`, `.SeekBefore()`).
  - Window functions (`Window.RowNumber()`, `Rank()`, `Lag()`, `Lead()`, etc.).
  - Set operations (`UNION`, `UNION ALL`, `INTERSECT`, `EXCEPT`).
  - `INSERT INTO ... SELECT` (`InsertQuery<T>.FromSelect()`).
  - Joins: `CROSS APPLY` and `OUTER APPLY` (translated to `LATERAL` in PostgreSQL).
  - Optimistic locking concurrency tokens (`.WithConcurrencyToken()`).
  - Entity differential updates (`DiffUpdateExtensions.ApplyDiff()`).
- **Source Generation & NativeAOT:**
  - Incremental code generator (`IIncrementalGenerator`) emitting `[SqlEntity]` metadata, ordinal-cached `Parser`, and property mappings.
  - Zero-reflection execution path via `QueryAotAsync<T>(mapper)`.
- **Reliability & Telemetry:**
  - Distributed tracing with `OpenTelemetry` (`ActivitySource`, `Meter`).
  - Polly v8 resilience pipelines with dialect-specific transient error classification.
  - Transaction and nested savepoint abstraction (`IUnitOfWork`, `ISavepoint`).
  - 21 Roslyn static safety analyzers (`ESQL001`–`ESQL025`).

---

## 5. Benchmarks Retained

BenchmarkDotNet benchmarks are maintained strictly as engineering baselines, following [ADR-014](file:///d:/DevData/ericksonlopez.dev/dotnet-sql-builder/docs/decisions/adr-014-zero-allocation-benchmark-proof.md):

- **Baseline:** Raw SQL string construction (`Baseline_RawString_*`).
- **Metrics Tracked:** Latency (median, P95), Memory allocations (Allocated Bytes, Gen0/Gen1/Gen2 collections).
- **Execution Scenarios:**
  - AST construction overhead.
  - SQL compilation cost across 5 dialects.
  - Single-row vs multi-row vs `IBulkStrategy` execution.
  - Reflection-based hydration vs Source-Generated ordinal-cached hydration.
  - Cold path vs warm cached path in expression parsing.

---

## 6. Three-Way Reconciliation Findings

Each capability was reconciled across Documentation, Source Code, Test Suite, and ADRs:

| Feature / Capability | Documentation | Source Code | Automated Tests | ADR / Design | Verified Status |
|:---|:---:|:---:|:---:|:---:|:---:|
| `CROSS APPLY` / `OUTER APPLY` | Marked `Implemented` | `SelectQuery.cs`, `SqlServerCompiler.cs`, `PostgreSqlCompiler.cs` | 100% Passing | ADR-019 (Typed deferred) | **VERIFIED IMPLEMENTED** |
| `INSERT INTO … SELECT` | Marked `Implemented` | `InsertQuery.cs`, `InsertSelectNode.cs` | 100% Passing | Accepted | **VERIFIED IMPLEMENTED** |
| Keyset / Cursor Pagination | Marked `Implemented` | `CompositeCursorNode.cs`, `SelectQuery.cs` | 100% Passing | ADR-012 | **VERIFIED IMPLEMENTED** |
| Diff-based UPDATE | Marked `Implemented` | `DiffUpdateExtensions.cs` | 100% Passing | Accepted | **VERIFIED IMPLEMENTED** |
| SQL Server `NULLS FIRST/LAST` | Marked `Partial` | `SqlServerCompiler.cs` line 49 (`AppendNullsPosition` NOP) | Tested as NOP | Planned v1.2 | **PARTIAL (Bug: Needs IIF)** |
| Oracle Limit/Offset Pagination | Marked `Planned` | `OracleCompiler.cs` line 262 (`OFFSET...FETCH NEXT`) | 100% Passing (12c+) | Planned (11g ROWNUM) | **IMPLEMENTED (12c+) / PARTIAL (11g)** |
| `SqlEntityCache<T>` Fallback | Marked `AOT-Safe` | `SqlEntityCache.cs` (returns empty `ColumnNames`) | Needs Guard | TD-003 | **PARTIAL / POTENTIAL BUG** |
| `SqlExpressionVisitor` Evaluation | Marked `Compile()` | `SqlExpressionVisitor.cs` (`ConditionalWeakTable` + reflection) | 100% Passing | TD-005 | **IMPLEMENTED (No Compile call)** |
| `<IsAotCompatible>` in `.csproj` | Claimed AOT | Absent in `.csproj` files | Build Passing | TD-002 | **MISSING METADATA** |
| `AotSqlRendererBase` Bulk Methods | Base virtuals | Throws `NotSupportedException` at runtime | Tested | TD-007 | **TECHNICAL DEBT (Needs abstract)** |
| `MergeQuery<T>` API | Legacy | Marked `[Obsolete]` | Tested | ADR-025 | **DEPRECATED / ESCAPE HATCH** |

---

## 7. Documentation Contradictions & Single Source of Truth

1. **`SqlExpressionVisitor` Mechanism:**
   - *Previous claim:* `Expression.Compile()` was called on the first `Where()` invocation.
   - *Code reality:* `SqlExpressionVisitor.cs` uses `ConditionalWeakTable<MemberInfo, Func<object, object?>>` and manual expression tree interpretation via `FieldInfo.GetValue` / `PropertyInfo.GetValue`.
   - *Resolution:* Documentation updated. `Expression.Compile()` is not invoked.
2. **Oracle Pagination Status:**
   - *Previous claim:* `OracleCompiler` did not override `CompileLimitOffset()` and produced invalid syntax.
   - *Code reality:* `OracleCompiler.cs` explicitly overrides `CompileLimitOffset()` emitting standard `OFFSET n ROWS FETCH NEXT n ROWS ONLY` (Oracle 12c+).
   - *Resolution:* Oracle 12c+ pagination is verified as implemented; legacy 11g `ROWNUM` subquery wrapping remains as a feature request.
3. **SQL Server `NULLS FIRST/LAST`:**
   - *Previous claim:* Supported via dialect visitor.
   - *Code reality:* `SqlServerVisitor.AppendNullsPosition()` is an explicit NOP.
   - *Resolution:* Classified as `PARTIAL / TD-004`. Requires `IIF(col IS NULL, 0, 1)` compilation fix.
4. **`MergeQuery<T>`:**
   - *Previous claim:* Promoted as an active feature in early READMEs.
   - *Code reality:* Attributed with `[Obsolete]` in codebase and rejected for cross-dialect abstraction by ADR-025.
   - *Resolution:* Demoted to Legacy / Escape Hatch in README and feature matrix.

---

## 8. Features Rejected by Architecture (Permanent Boundaries)

The following features are permanently excluded from the library scope per [architecture-boundaries.md](file:///d:/DevData/ericksonlopez.dev/dotnet-sql-builder/docs/architecture-boundaries.md):

| Feature | ADR | Technical & Architectural Justification |
|:---|:---:|:---|
| **Change Tracking** | ADR-007 | Requires mutable in-memory entity buffers; contradicts immutable AST and thread-safety invariants. |
| **Navigation Properties & Lazy Loading** | ADR-007 | Introduces implicit, non-deterministic query execution and N+1 database roundtrips. |
| **Identity Map / 1st-Level Cache** | ADR-007 | Introduces hidden mutable state with ambiguous lifecycles; thread-unsafe across concurrent requests. |
| **LINQ `IQueryable<T>` Provider** | ADR-008 | 50+ operators impossible to faithfully translate across SQL dialects; causes runtime translation exceptions and breaks NativeAOT trim safety. |
| **Database Migrations / DDL Diffing** | — | Schema lifecycle management is an operational concern distinct from query compilation. |
| **Automatic Query Result Caching** | ADR-024 | Cache invalidation is domain-dependent and cannot be soundly managed inside a query builder. |
| **Automatic Multi-Tenancy / Soft-Delete Filters** | — | Implicit filtering introduces hidden dependencies; predicates must remain explicit in AST construction. |
| **Core DI / Logging Framework Coupling** | ADR-023 | Core package must remain dependency-free and runtime-agnostic. OpenTelemetry handles observability. |
| **Generic Cross-Dialect MERGE Abstraction** | ADR-025 | Dialect semantics differ fundamentally; SQL Server MERGE exhibits concurrency anomalies under high load. |
| **Specification / Repository Patterns in Core** | ADR-026, ADR-027 | Application architecture concerns; users build repository abstractions on top of SqlBuilder. |

---

## 9. Resulting Normalized Backlog

The following backlog items form the active engineering roadmap:

```
[ENGINEERING BACKLOG]
├── P0 / P1 — Correctness & Packaging (v1.2.0)
│   ├── P1-F001: Add <IsAotCompatible>true</IsAotCompatible> to all AOT-ready .csproj files (TD-002)
│   ├── P1-F002: Add guard / throw explicit exception in SqlEntityCache<T> fallback path (TD-003)
│   ├── P1-F003: Fix SQL Server NULLS FIRST/LAST emulation via IIF() in CompileOrderBys (TD-004)
│   └── P1-F004: Add BenchmarkDotNet AOT & Bulk insert benchmark test cases (PERF-001, PERF-003)
│
├── P2 — SQL Engine Enhancements (v1.3.0)
│   ├── P2-F001: Window function typed FILTER clause (ADR-018)
│   ├── P2-F002: Typed LATERAL / CROSS APPLY with outer column reference resolution (ADR-019)
│   └── P2-F003: Oracle legacy 11g ROWNUM pagination mode (TD-006)
│
└── P3 — Architecture Cleanup & Full AOT (v2.0.0)
    ├── P3-F001: Make AotSqlRendererBase bulk methods abstract (TD-007)
    ├── P3-F002: Source Generator static abstract IDataReader mapper (AOT-004)
    └── P3-F003: Remove obsolete MergeQuery<T> API (TD-008 / ADR-025)
```
