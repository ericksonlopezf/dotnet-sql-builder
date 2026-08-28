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
| [ADR-001](./decisions/adr-001-stryker-source-generator-exclusion.md) | Stryker exclusion of Source Generator | ✅ Accepted | v1.0 |
| [ADR-006](./decisions/adr-006-source-generator-strategy.md) | Source Generator strategy (IIncrementalGenerator) | ✅ Accepted | v1.0 |
| [ADR-013](./decisions/adr-013-aot-guarantees.md) | AOT guarantees and NativeAOT scope | ✅ Accepted | v1.0 |
| [ADR-014](./decisions/adr-014-zero-allocation-benchmark-proof.md) | Zero-allocation claims require benchmark proof | ✅ Accepted | v1.0 |
| [ADR-017](./decisions/adr-017-immutable-ast-record-semantics.md) | Immutable AST via C# record semantics | ✅ Accepted | v1.0 |
| [ADR-024](./decisions/adr-024-no-automatic-query-caching.md) | No automatic query caching | ✅ Accepted | v1.0 |
| [ADR-041](./decisions/adr-041-test-naming-osherove-ide1006.md) | Osherove test naming pattern and IDE1006 suppression | ✅ Accepted | v1.3 |

---

## Dialect & Package Architecture

| ADR | Title | Status | Version |
|-----|-------|--------|---------|
| [ADR-009](./decisions/adr-009-dialect-isolation-separate-packages.md) | Dialect isolation in separate packages | ✅ Accepted | v1.0 |
| [ADR-023](./decisions/adr-023-no-di-logging-core-dependencies.md) | No DI or logging as Core dependencies | ✅ Accepted | v1.0 |
| [ADR-028](./decisions/adr-028-oracle-pagination-strategy.md) | Oracle pagination strategy (FETCH FIRST / ROWNUM) | ✅ Accepted | v1.2 |
| [ADR-029](./decisions/adr-029-nulls-ordering-emulation.md) | NULLS FIRST / LAST emulation across dialects | ✅ Accepted | v1.2 |
| [ADR-030](./decisions/adr-030-packaging-independence-and-cpm.md) | Package independence and CPM governance | ✅ Accepted | v1.2 |

---

## Integration Packages

| ADR | Title | Status | Version |
|-----|-------|--------|---------|
| [ADR-002](./decisions/adr-002-dapper-integration-optional.md) | Dapper integration is optional | ✅ Accepted | v1.0 |
| [ADR-003](./decisions/adr-003-polly-not-core-dependency.md) | Polly is never a Core dependency | ✅ Accepted | v1.0 |
| [ADR-004](./decisions/adr-004-unitofwork-outside-core.md) | UnitOfWork outside Core (separate package) | ✅ Accepted | v1.0 |
| [ADR-005](./decisions/adr-005-multi-mapping-beyond-7-entities.md) | Multi-mapping >7 entities via fluent builder | ✅ Accepted | v1.0 |
| [ADR-015](./decisions/adr-015-resilience-integration-architecture.md) | Resilience integration architecture | ✅ Accepted | v1.0 |
| [ADR-016](./decisions/adr-016-transaction-retry-semantics.md) | Transaction + retry semantic correctness | ✅ Accepted | v1.0 |

---

## SQL Features

| ADR | Title | Status | Version |
|-----|-------|--------|---------|
| [ADR-010](./decisions/adr-010-bulk-api-architecture.md) | Bulk insert API (IBulkStrategy plugin model) | ✅ Accepted | v1.0 |
| [ADR-011](./decisions/adr-011-raw-sql-escape-hatch-policy.md) | Raw SQL escape hatch policy | ✅ Accepted | v1.0 |
| [ADR-012](./decisions/adr-012-pagination-strategy.md) | Pagination strategy (Offset + Window + Keyset) | ✅ Accepted | v1.0 |
| [ADR-020](./decisions/adr-020-recursive-cte-support.md) | Recursive CTE support | ✅ Accepted | v1.0 |
| [ADR-021](./decisions/adr-021-returning-output-clause-design.md) | RETURNING / OUTPUT clause design | ✅ Accepted | v1.0 |
| [ADR-022](./decisions/adr-022-concurrency-token-update.md) | Concurrency token in UPDATE (optimistic locking) | ✅ Accepted | v1.0 |
| [ADR-018](./decisions/adr-018-window-function-expression-support.md) | Window function typed expressions (FILTER deferred) | ✅ Accepted | v1.0 |
| [ADR-019](./decisions/adr-019-cross-apply-lateral-join-deferred.md) | CROSS APPLY / LATERAL JOIN (typed outer-ref deferred) | ✅ Accepted | v1.0 |

---

## Intentionally NOT Implemented (Anti-Features)

| ADR | Feature Rejected | Reason |
|-----|-----------------|--------|
| [ADR-007](./decisions/adr-007-no-change-tracking.md) | Change tracking | Requires mutable state — contradicts immutable AST |
| [ADR-007](./decisions/adr-007-no-change-tracking.md) | Navigation properties (lazy loading) | Unpredictable query generation; N+1 trap; defeats predictability |
| [ADR-007](./decisions/adr-007-no-change-tracking.md) | Identity map / first-level cache | Hidden state; thread safety; non-obvious lifetime |
| [ADR-008](./decisions/adr-008-no-linq-iqueryable-provider.md) | LINQ `IQueryable<T>` provider | Impossible to guarantee AOT safety and predictable SQL translation |
| [ADR-024](./decisions/adr-024-no-automatic-query-caching.md) | Automatic query caching | Hidden state; cache invalidation; memory leak risk |
| [ADR-023](./decisions/adr-023-no-di-logging-core-dependencies.md) | Built-in DI container integration | Unnecessary coupling; forces framework on all users |
| [ADR-003](./decisions/adr-003-polly-not-core-dependency.md) | Polly as Core dependency | Optional concern; forces resilience on users who don't need it |
| [ADR-023](./decisions/adr-023-no-di-logging-core-dependencies.md) | ILogger in Core | Forces logging framework; users should choose their stack |
| [ADR-025](./decisions/adr-025-no-generic-merge-abstraction.md) | Generic cross-dialect MERGE abstraction | SQL Server MERGE has correctness/concurrency bugs; abstraction would hide them |
| [ADR-026](./decisions/adr-026-no-specification-pattern-in-core.md) | Specification pattern in Core | App-layer pattern; adapter package may be added separately |
| [ADR-027](./decisions/adr-027-no-repository-pattern.md) | Repository pattern implementation | App-layer architecture; users build repositories on top of SqlBuilder |

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
| SQL003 | `SELECT *` usage → Warning | ✅ Implemented (legacy prefix) |
| SQL004 | Redundant WHERE condition → Warning | ✅ Implemented (legacy prefix) |
| SQL009 | Missing column reference → Warning | ✅ Implemented (legacy prefix) |
