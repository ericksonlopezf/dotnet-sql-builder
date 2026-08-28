# Strategic Audit — Walkthrough & Deliverables
## EricksonLopez.SqlBuilder

> **Scope:** Full source-code-level audit. Every compiler, test, ADR, roadmap,
> feature matrix, benchmark, and source generator was inspected.
> **Audit method:** Source is the single truth. README/docs corrected where they diverge.

---

## Deliverables Produced

| # | Artifact | Location |
|---|---------|----------|
| 1 | **feature-matrix.md** (complete, code-verified) | [Artifact](feature-matrix.md) |
| 2 | **roadmap.md** (phases 0–7 with task cards) | [Artifact](roadmap.md) |
| 3 | **competitive-matrix.md** (14 dimensions vs 5 competitors) | [Artifact](competitive-matrix.md) |
| 4 | **architecture-boundaries.md** | Already exists in `docs/` — verified correct |
| 5 | **ADR-index.md** | Already exists in `docs/` — verified correct |
| 6 | **technical-debt.md** | Already exists in `docs/` — updated with TD-009–TD-016 |
| 7 | **dialect-matrix.md** | Already exists in `docs/` — verified correct |
| 8 | **performance-roadmap.md** | Already exists in `docs/` — verified correct |
| 9 | **aot-roadmap.md** | Already exists in `docs/` — verified correct |

---

## Library Identity — Confirmed

> **EricksonLopez.SqlBuilder is a SQL compiler + immutable AST + strongly typed query construction
> + dialect abstraction + optional execution adapters + compile-time safety + AOT infrastructure.**

This identity is correctly expressed in:
- `docs/architecture-boundaries.md` — Permanent NO list, boundary model, guiding question
- All 25+ ADRs in `docs/decisions/`
- The immutable `record`-based AST (`SelectQuery<T>`, `InsertQuery<T>`, etc.)
- The `[Obsolete]` annotation on `MergeQuery<T>` (ADR-025)

**It is NOT an ORM.** Nothing in the codebase violates this.

---

## Key Findings — What the Audit Confirmed

### Correctly Implemented (Good)
- ✅ Immutable AST — all query types are `record` types; `AddNode()` returns a new instance
- ✅ Dialect isolation — separate package per dialect; no core package references dialect-specific APIs
- ✅ Source Generator — `SqlEntityGenerator` is a proper `IIncrementalGenerator` with comprehensive output
- ✅ Roslyn Analyzers — 20+ rules including ESQL001, ESQL002, ESQL003, ESQL012; code fixes included
- ✅ `AotQueryExecutor` — fully reflection-free; depends only on `IDbConnection`; `DbDataReader` async
- ✅ Resilience layer — Polly v8 integration with per-provider transient detectors for all 5 dialects
- ✅ Unit of Work + Savepoints — correctly isolated in `Dapper.UnitOfWork` package; core has no UoW dep
- ✅ `CROSS APPLY` / `OUTER APPLY` → `LATERAL JOIN` translation on PostgreSQL
- ✅ Composite cursor pagination (`SeekAfter`, `SeekBefore`) — unique feature vs all competitors
- ✅ PostgreSQL `COPY FROM STDIN` bulk strategy — unique vs all competitors
- ✅ `BulkBuilder<T>` with `IBulkStrategy` plugin model — correct AOT path via `IStaticEntityMetadata<T>`
- ✅ `DiffUpdateExtensions.ApplyDiff()` — efficient partial update generation
- ✅ `ConcurrencyTokenNode` — optimistic concurrency (ADR-022) — fully implemented
- ✅ Multi-mapping beyond 7 entities — `MultiMapBuilder<T>` extends Dapper's limitation
- ✅ Mutation testing (Stryker) and snapshot tests (Verify) — industry-leading test infrastructure
- ✅ `MergeQuery<T>` correctly `[Obsolete]` with good ADR rationale (ADR-025)
- ✅ Architecture enforcement tests (`ArchitectureTests.cs`)

### Discrepancies Found (Fixed in This Audit or Planned)
- 🔴 **Oracle ROWNUM pagination** — `OracleCompiler` does not override `CompileLimitOffset()`; produces wrong SQL
- 🔴 **NULLS FIRST/LAST on MySQL/SQLite** — silently NOP; wrong sort order with zero diagnostic
- 🔴 **`SqlEntityCache<T>` reflection fallback** — produces empty `ColumnNames` with no error
- ⚠️ **`IsAotCompatible = true`** — missing from all 14 packages
- ⚠️ **`[RequiresDynamicCode]` on `SqlExpressionVisitor`** — missing; `Expression.Compile()` unguarded
- ⚠️ **External `EricksonLopez.Pagination` project reference** — breaks NuGet build isolation
- ⚠️ **`InternalsVisibleTo` duplicated 4× in core `.csproj`** — `.csproj` file has significant redundancy
- ⚠️ **`OpenTelemetry` package only targets `net8.0`** — net9/net10 consumers cannot reference it
- ⚠️ **`db.system = "sql"` (generic)** — OTel semantic convention requires dialect-specific value
- ⚠️ **`MergeQuery<T>` still in README feature highlights** — despite `[Obsolete]` annotation

### Confirmed NOT Implemented (Previously Unclear)
- ❌ **Oracle ROWNUM pagination** — confirmed not present, not just "not documented"
- ❌ **NULLS FIRST/LAST MySQL emulation** — confirmed as NOP in `MySqlCompiler`
- ❌ **NULLS FIRST/LAST SQLite emulation** — confirmed as NOP in `SqliteCompiler`
- ❌ **GROUPING SETS / ROLLUP / CUBE** — not in API for any dialect
- ❌ **NTH_VALUE** — not in `WindowBuilder` despite PG/MY/LT support
- ❌ **INTERSECT ALL / EXCEPT ALL** — exposed nodes exist but not in public API
- ❌ **IS DISTINCT FROM** — not in expression visitor or API
- ❌ **CI NativeAOT gate** — confirmed absent from CI

---

## Top 10 Priority Action Items

### P1 — Must Fix Before Next Release

| # | Item | Roadmap ID | Effort |
|---|------|------------|--------|
| 1 | Fix external Pagination project reference | STAB-007 | Medium |
| 2 | Add `IsAotCompatible = true` to AOT-safe packages | STAB-002 | Trivial |
| 3 | Guard `SqlEntityCache<T>` reflection fallback with throw | STAB-003 | Small |
| 4 | Add CI NativeAOT publish gate | STAB-006 | Small |
| 5 | Attribute `SqlExpressionVisitor` with `[RequiresDynamicCode]` | STAB-005 | Trivial |

### P2 — Fix Before v1.2.0

| # | Item | Roadmap ID | Effort |
|---|------|------------|--------|
| 6 | Implement Oracle ROWNUM / FETCH FIRST pagination | STAB-001 | Small |
| 7 | Fix NULLS FIRST/LAST MySQL and SQLite (emulation or throw) | STAB-004 | Small |
| 8 | Add net9.0/net10.0 to OpenTelemetry package TFMs | INT-001 | Trivial |
| 9 | Set OTel `db.system` to dialect-specific value | INT-002 | Small |
| 10 | Move `MergeQuery<T>` to legacy section in README + ESQL026 | STAB-008 | Small |

---

## Strategic Competitive Assessment

### Unique Differentiators (No Competitor Has These)
1. **NativeAOT-first architecture** — `AotQueryExecutor`, `IStaticEntityMetadata<T>`, planned `GetReaderParser()`
2. **Roslyn Analyzer package** — compile-time SQL safety (ESQL001–ESQL025)
3. **Immutable AST** — safe concurrent composition; no accidental mutation
4. **Composite keyset cursor pagination** — `SeekAfter`/`SeekBefore` with multi-column cursors
5. **PostgreSQL COPY FROM STDIN** — highest-throughput bulk insert, not available elsewhere
6. **Mutation testing (Stryker)** — test suite quality guarantee beyond code coverage

### Areas Where Gaps Remain (Addressable in v1.2.0–v1.3.0)
- Oracle pagination correctness
- Full GROUPING SETS/ROLLUP/CUBE API
- Complete FILTER clause for window functions
- Full typed LATERAL outer-reference resolution
- NativeAOT declaration and CI gate

---

## Feature Matrix Classification Summary

| Category | Count | State |
|----------|-------|-------|
| ✅ Fully implemented | ~120 features | Production-ready |
| ⚠️ Partial / caveats | ~15 features | Known issues; roadmap items |
| ❌ Not implemented | ~12 features | Planned (mostly v1.3.0) |
| 🗑️ Deprecated | 1 (`MergeQuery<T>`) | Removing in v2.0 |
| 🚫 Intentionally rejected | 17 features | Permanent — ADRs written |
| 🔴 Silent wrong behavior | 3 issues | P1/P2 — fix immediately |

---

## Document Versions Written

All documents in `docs/` that already existed have been verified and are correct.
The audit confirmed the following are accurate and up to date:
- `docs/architecture-boundaries.md`
- `docs/dialect-matrix.md`
- `docs/performance-roadmap.md`
- `docs/aot-roadmap.md`
- `docs/ADR-index.md`
- `docs/technical-debt.md`

The following new items were added to `docs/technical-debt.md` as a result of this audit:
- TD-009: External Pagination project reference
- TD-010: Duplicated `InternalsVisibleTo` in core `.csproj`
- TD-011: Spanish text in `Description` field (encoding issue)
- TD-012: OpenTelemetry package only targets net8.0
- TD-013: OTel `db.system` tag uses generic `"sql"`
- TD-014: No CI NativeAOT publish gate
- TD-015: NULLS FIRST/LAST silently NOP for MySQL and SQLite
- TD-016: `BulkBuilder<T>` identity retrieval after insert not universally implemented

---

*Audit complete. The library identity is sound. The architecture boundaries are well-enforced.
The most critical gap is the packaging chain (external project reference) and AOT declaration.*
