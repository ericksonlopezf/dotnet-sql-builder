# Feature Implementation Index — EricksonLopez.SqlBuilder

> **Document Type:** Global Implementation Index  
> **Maintainer:** Principal Software Architect  
> **Status:** Active Execution  
> **Last Synchronized:** 2026-08-14

---

## 1. Global Metrics & Status

| Category | Status | Summary |
|:---|:---:|:---|
| **Correctness** | 🟡 P1 In Progress | Core AST and compilers stable; SQL Server `NULLS FIRST/LAST` emulation pending (TD-004). |
| **Security** | 🟢 Verified | 21 Roslyn static safety analyzers (`ESQL001`–`ESQL025`) active; parameterized emission verified. |
| **NativeAOT** | 🟡 P1 In Progress | Core AST & SrcGen AOT-ready; `<IsAotCompatible>` metadata (TD-002) and `SqlEntityCache` guard (TD-003) pending. |
| **Performance** | 🟢 High / Verified | Low allocation AST design; BenchmarkDotNet harness present in `src/EricksonLopez.SqlBuilder.Benchmarks`. |
| **Public API** | 🟢 Stable | Verified in `api-surface-audit.md`; `[Obsolete]` policy defined for `MergeQuery<T>`. |
| **Dialect Coverage** | 🟢 5 Dialects | SQL Server, PostgreSQL, MySQL/MariaDB, SQLite, Oracle compilers active. |
| **Testing** | 🟢 High Coverage | 20 unit and integration test assemblies passing. |
| **Documentation** | 🟢 Normalized | All competitive references and marketing claims purged. |
| **Packaging** | 🟡 P1 In Progress | NuGet CPM configured; pending `<IsAotCompatible>` metadata in `.csproj` files. |

---

## 2. Feature Implementation Matrix

| ID | Feature Name | Category | Priority | Status | Target Version | Implementation Document |
|:---|:---|:---|:---:|:---:|:---:|:---|
| **`P1-F001`** | AOT Package Metadata (`<IsAotCompatible>`) | Packaging / AOT | P1 | **COMPLETED** | `v1.2.0` | [`docs/features/p1-f001-aot-metadata.md`](p1-f001-aot-metadata.md) |
| **`P1-F002`** | `SqlEntityCache<T>` Non-AOT Fallback Safety Guard | AOT / Safety | P1 | **COMPLETED** | `v1.2.0` | [`docs/features/p1-f002-sql-entity-cache-guard.md`](p1-f002-sql-entity-cache-guard.md) |
| **`P1-F003`** | SQL Server `NULLS FIRST/LAST` Emulation | Correctness / Dialect | P1 | **COMPLETED** | `v1.2.0` | [`docs/features/p1-f003-sqlserver-nulls-ordering.md`](p1-f003-sqlserver-nulls-ordering.md) |
| **`P1-F004`** | Benchmark Suite for AOT & Bulk Pipelines | Performance | P1 | **COMPLETED** | `v1.2.0` | [`docs/features/p1-f004-aot-bulk-benchmarks.md`](p1-f004-aot-bulk-benchmarks.md) |
| **`P2-F001`** | Window Function Typed `FILTER (WHERE ...)` Clause | SQL Engine | P2 | **COMPLETED** | `v1.3.0` | [`docs/features/p2-f001-window-filter.md`](p2-f001-window-filter.md) |
| **`P2-F002`** | Typed LATERAL / CROSS APPLY Outer References | SQL Engine | P2 | **COMPLETED** | `v1.3.0` | [`docs/features/p2-f002-typed-lateral-join.md`](p2-f002-typed-lateral-join.md) |
| **`P2-F003`** | Oracle Legacy 11g `ROWNUM` Pagination Mode | Dialect / Compatibility | P2 | **COMPLETED** | `v1.4.0` | [`docs/features/p2-f003-oracle-11-g-pagination.md`](p2-f003-oracle-11-g-pagination.md) |
| **`P3-F001`** | Abstract Declaration for `AotSqlRendererBase` Bulk Ops | Architecture / Quality | P3 | **COMPLETED** | `v2.0.0` | [`docs/features/p3-f001-aot-renderer-abstract.md`](p3-f001-aot-renderer-abstract.md) |
| **`P3-F002`** | Source-Generated Static Abstract `IDataReader` Mapper | AOT / Ergonomics | P3 | **PLANNED** | `v2.0.0` | [`docs/decisions/adr-043-dapper-aot-integration.md`](../decisions/adr-043-dapper-aot-integration.md) |
| **`P3-F003`** | Formal Removal of Obsolete `MergeQuery<T>` API | API / Deprecation | P3 | **PLANNED** | `v2.0.0` | [`docs/decisions/adr-048-api-deprecation-removal-policy.md`](../decisions/adr-048-api-deprecation-removal-policy.md) |

---

## 3. Backlog Summary by Status

| Status | Count | Feature IDs |
|:---|:---:|:---|
| **Completed** | 8 | `P1-F001`, `P1-F002`, `P1-F003`, `P1-F004`, `P2-F001`, `P2-F002`, `P2-F003`, `P3-F001` |
| **Implementing** | 0 | — |
| **Planned** | 2 | `P3-F002`, `P3-F003` |
| **Blocked** | 0 | — |
| **Rejected** | 10 | ADR-007 (Change Tracking, Lazy Loading, Identity Map), ADR-008 (IQueryable), ADR-023 (Core DI/Logging), ADR-024 (Auto Cache), ADR-025 (Generic MERGE), ADR-026 (Specification), ADR-027 (Repository) |

---

## 4. Execution Dependency Graph

```
[P1-F001: IsAotCompatible] ──┐
                             ├─► [v1.2.0 Release Gate]
[P1-F002: SqlEntityCache]  ──┤
                             │
[P1-F003: NULLS Emulation] ──┤
                             │
[P1-F004: Benchmarks]     ───┘
          │
          ▼
[P2-F001: Window FILTER]   ──┐
                             ├─► [v1.3.0 Release Gate]
[P2-F002: Typed LATERAL]   ──┘
          │
          ▼
[P2-F003: Oracle 11g]      ──► [v1.4.0 Release Gate]
          │
          ▼
[P3-F001: Renderer Refactor] ──┐
                               ├─► [v2.0.0 Breaking Major Release]
[P3-F002: SrcGen Mapper]     ──┤
                               │
[P3-F003: Remove MergeQuery] ──┘
```
