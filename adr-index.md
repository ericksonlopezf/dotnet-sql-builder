# Architecture Decision Records — Index

> This directory contains all Architecture Decision Records (ADRs) for EricksonLopez.SqlBuilder.
> ADRs document **why** decisions were made, including decisions to **NOT** implement certain features.
> Every significant architectural decision requires an ADR before implementation begins.

## Status Legend

| Icon | Meaning |
|------|---------|
| ✅ Accepted | Decision is in effect; implementation complete or ongoing |
| 📋 Proposed | Decision drafted; pending final review or implementation |
| ⏸️ Deferred | Valid decision but implementation postponed to a future version |
| ❌ Rejected | Evaluated and intentionally discarded |
| 🔄 Superseded | Replaced by a newer ADR |

---

## Core Architecture

| ADR | Title | Status | Version |
|-----|-------|--------|---------|
| [ADR-001](./adr-001-stryker-source-generator-exclusion.md) | Stryker exclusion of Source Generator | ✅ Accepted | v1.0 |
| [ADR-006](./adr-006-source-generator-strategy.md) | Source Generator strategy (IIncrementalGenerator) | ✅ Accepted | v1.0 |
| [ADR-013](./adr-013-aot-guarantees.md) | AOT guarantees and NativeAOT scope | ✅ Accepted | v1.0 |
| [ADR-014](./adr-014-zero-allocation-benchmark-proof.md) | Zero-allocation claims require benchmark proof | ✅ Accepted | v1.0 |
| [ADR-017](./adr-017-immutable-ast-record-semantics.md) | Immutable AST via C# record semantics | ✅ Accepted | v1.0 |
| [ADR-024](./adr-024-no-automatic-query-caching.md) | No automatic query caching | ✅ Accepted | v1.0 |
| [ADR-041](./docs/decisions/adr-041-test-naming-osherove-ide1006.md) | Osherove test naming pattern and IDE1006 suppression | ✅ Accepted | v1.3 |

---

## Dialect & Package Architecture

| ADR | Title | Status | Version |
|-----|-------|--------|---------|
| [ADR-009](./adr-009-dialect-isolation-separate-packages.md) | Dialect isolation in separate packages | ✅ Accepted | v1.0 |
| [ADR-023](./adr-023-no-di-logging-core-dependencies.md) | No DI or logging as Core dependencies | ✅ Accepted | v1.0 |
| [ADR-028](./adr-028-oracle-pagination-strategy.md) | Oracle pagination strategy (FETCH FIRST / ROWNUM) | ✅ Accepted | v1.2 |
| [ADR-029](./adr-029-nulls-ordering-emulation.md) | NULLS FIRST / LAST emulation across dialects | ✅ Accepted | v1.2 |
| [ADR-030](./adr-030-packaging-independence-and-cpm.md) | Package independence and CPM governance | ✅ Accepted | v1.2 |

---

## Integration Packages

| ADR | Title | Status | Version |
|-----|-------|--------|---------|
| [ADR-002](./adr-002-dapper-integration-optional.md) | Dapper integration is optional | ✅ Accepted | v1.0 |
| [ADR-003](./adr-003-polly-not-core-dependency.md) | Polly is never a Core dependency | ✅ Accepted | v1.0 |
| [ADR-004](./adr-004-unitofwork-outside-core.md) | UnitOfWork outside Core (separate package) | ✅ Accepted | v1.0 |
| [ADR-005](./adr-005-multi-mapping-beyond-7-entities.md) | Multi-mapping >7 entities via fluent builder | ✅ Accepted | v1.0 |
| [ADR-015](./adr-015-resilience-integration-architecture.md) | Resilience integration architecture | ✅ Accepted | v1.0 |
| [ADR-016](./adr-016-transaction-retry-semantics.md) | Transaction + retry semantic correctness | ✅ Accepted | v1.0 |

---

## SQL Features

| ADR | Title | Status | Version |
|-----|-------|--------|---------|
| [ADR-010](./docs/decisions/adr-010-bulk-api-architecture.md) | Bulk insert API (IBulkStrategy plugin model) | ✅ Accepted | v1.0 |
| [ADR-011](./docs/decisions/adr-011-raw-sql-escape-hatch-policy.md) | Raw SQL escape hatch policy | ✅ Accepted | v1.0 |
| [ADR-012](./docs/decisions/adr-012-pagination-strategy.md) | Pagination strategy (Offset + Window + Keyset + Composite Cursor) | ✅ Accepted | v1.0 |
| [ADR-020](./docs/decisions/adr-020-recursive-cte-support.md) | Recursive CTE support | ✅ Accepted | v1.0 |
| [ADR-021](./docs/decisions/adr-021-returning-output-clause-design.md) | RETURNING / OUTPUT clause design | ✅ Accepted | v1.0 |
| [ADR-022](./docs/decisions/adr-022-concurrency-token-update.md) | Concurrency token in UPDATE (optimistic locking) | ✅ Accepted | v1.0 |
| [ADR-018](./docs/decisions/adr-018-window-function-expression-support.md) | Window function typed expressions (FILTER deferred) | ✅ Accepted | v1.0 |
| [ADR-019](./docs/decisions/adr-019-cross-apply-lateral-join-deferred.md) | CROSS APPLY / LATERAL JOIN (typed outer-ref deferred) | ✅ Accepted | v1.0 |
| [ADR-031](./docs/decisions/adr-031-set-operations-all-modifiers.md) | Set operations ALL modifiers (INTERSECT ALL, EXCEPT ALL) | ✅ Accepted | v1.3 |
| [ADR-032](./docs/decisions/adr-032-null-safe-equality-predicates.md) | Null-safe equality predicates (IS DISTINCT FROM) | ✅ Accepted | v1.3 |
| [ADR-033](./docs/decisions/adr-033-projection-sentinels-nullif-coalesce.md) | Projection sentinels (NULLIF and multi-arg COALESCE) | ✅ Accepted | v1.3 |
| [ADR-034](./docs/decisions/adr-034-analytical-grouping-sets-rollup-cube.md) | Analytical grouping sets, rollup, and cube | ✅ Accepted | v1.3 |
| [ADR-035](./docs/decisions/adr-035-window-function-filter-clause.md) | Window function FILTER (WHERE ...) clause | ✅ Accepted | v1.3 |
| [ADR-036](./docs/decisions/adr-036-lateral-join-outer-reference-resolution.md) | Typed LATERAL JOIN with Sql.Outer<T> reference resolution | ✅ Accepted | v1.3 |
| [ADR-037](./docs/decisions/adr-037-cte-materialization-hints.md) | CTE Materialization hints (MATERIALIZED / NOT MATERIALIZED) | ✅ Accepted | v1.3 |
| [ADR-038](./docs/decisions/adr-038-opentelemetry-semantic-conventions.md) | OpenTelemetry database semantic conventions per dialect | ✅ Accepted | v1.3 |
| [ADR-039](./docs/decisions/adr-039-aot-sql-renderer-abstract-bulk-contract.md) | AotSqlRendererBase abstract bulk contract enforcement | ✅ Accepted | v1.3 |
| [ADR-040](./docs/decisions/adr-040-esql026-deprecated-merge-analyzer.md) | Roslyn Analyzer ESQL026 for deprecated generic MERGE | ✅ Accepted | v1.3 |
| [ADR-042](./docs/decisions/adr-042-scalar-subquery-in-select.md) | Scalar Subquery in SELECT clause | ✅ Accepted | v2.0 |
| [ADR-043](./docs/decisions/adr-043-dapper-aot-integration.md) | Dapper.AOT integration package strategy | ✅ Accepted | v2.0 |
| [ADR-046](./docs/decisions/adr-046-bulk-identity-retrieval-boundary.md) | Bulk identity retrieval boundary and client-generated keys strategy | ✅ Accepted | v2.0 |

---

## Intentionally NOT Implemented (Anti-Features)

| ADR | Feature Rejected | Reason |
|-----|-----------------|--------|
| [ADR-007](./docs/decisions/adr-007-no-change-tracking.md) | Change tracking | Requires mutable state — contradicts immutable AST |
| [ADR-007](./docs/decisions/adr-007-no-change-tracking.md) | Navigation properties (lazy loading) | Unpredictable query generation; N+1 trap; defeats predictability |
| [ADR-007](./docs/decisions/adr-007-no-change-tracking.md) | Identity map / first-level cache | Hidden state; thread safety; non-obvious lifetime |
| [ADR-008](./docs/decisions/adr-008-no-linq-iqueryable-provider.md) | LINQ `IQueryable<T>` provider | Impossible to guarantee AOT safety and predictable SQL translation |
| [ADR-024](./docs/decisions/adr-024-no-automatic-query-caching.md) | Automatic query caching | Hidden state; cache invalidation; memory leak risk |
| [ADR-023](./docs/decisions/adr-023-no-di-logging-core-dependencies.md) | Built-in DI container integration | Unnecessary coupling; forces framework on all users |
| [ADR-003](./docs/decisions/adr-003-polly-not-core-dependency.md) | Polly as Core dependency | Optional concern; forces resilience on users who don't need it |
| [ADR-023](./docs/decisions/adr-023-no-di-logging-core-dependencies.md) | ILogger in Core | Forces logging framework; users should choose their stack |
| [ADR-025](./docs/decisions/adr-025-no-generic-merge-abstraction.md) | Generic cross-dialect MERGE abstraction | SQL Server MERGE has correctness/concurrency bugs; abstraction would hide them |
| [ADR-026](./docs/decisions/adr-026-no-specification-pattern-in-core.md) | Specification pattern in Core | App-layer pattern; adapter package may be added separately |
| [ADR-027](./docs/decisions/adr-027-no-repository-pattern.md) | Repository pattern implementation | App-layer architecture; users build repositories on top of SqlBuilder |
| [ADR-045](./docs/decisions/adr-045-rejection-of-braces-column-expansion.md) | Braces column expansion shorthand `{col1, col2}` | Zero functional gain; violates AOT-first and type safety principles |

---

## Roslyn Analyzer Decisions

| Rule | Feature | Status |
|------|---------|--------|
| ESQL001 | DELETE without WHERE → Error | ✅ Implemented |
| ESQL002 | Raw SQL string concatenation → Error | ✅ Implemented |
| ESQL003 | UPDATE without WHERE → Error | ✅ Implemented |
| ESQL004 | Query performance concern → Warning | ✅ Implemented |
| ESQL005 | Dapper compiler misconfiguration → Warning | ✅ Implemented |
| ESQL006 | Missing ON condition in JOIN → Warning | ✅ Implemented |
| ESQL007 | Potential missing index hint → Info | ✅ Implemented |
| ESQL008 | Large OFFSET value → Warning | ✅ Implemented |
| ESQL009 | LIKE leading wildcard → Warning | ✅ Implemented |
| ESQL010 | LIKE wildcard usage concern → Warning | ✅ Implemented |
| ESQL011 | `Sql.Raw(string)` unsafe overload → Warning | ✅ Implemented |
| ESQL012 | Retry pipeline inside transaction → Warning | ✅ Implemented |
| ESQL020 | Dialect-specific API + incompatible compiler → Warning | ✅ Implemented |
| ESQL021 | `[SqlEntity]` without Source Generator → Warning | ✅ Implemented |
| ESQL022 | Type mapping registration issue → Warning | ✅ Implemented |
| ESQL023 | Synchronous SQL call on UI thread → Warning | ✅ Implemented |
| ESQL024 | Cartesian product (missing join condition) → Warning | ✅ Implemented |
| ESQL025 | SqlKata API detected (migration code fix) → Info | ✅ Implemented |
| ESQL026 | Deprecated generic `Sql.Merge<T>()` detected → **Error** (v2.0) | ✅ Implemented |
| SQL003 | `SELECT *` usage → Warning | ✅ Implemented (legacy prefix) |
| SQL004 | Redundant WHERE condition → Warning | ✅ Implemented (legacy prefix) |
| SQL009 | Missing column reference → Warning | ✅ Implemented (legacy prefix) |
