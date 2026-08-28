# ADR-015: Resilience Integration Architecture

## Status
Proposed (implementation deferred to v1.1 — Phase 2.2)

## Date
2026-08-12

## Context
Database operations fail transiently: connection timeouts, transient lock contention, network blips, SQL Server failover. Polly v8 (`Microsoft.Extensions.Resilience`) is the de facto standard for handling these scenarios in .NET.

The question is: where and how does resilience integrate with EricksonLopez.SqlBuilder?

## Problem
- Resilience concerns (retry, timeout, circuit breaker) are cross-cutting but must not contaminate Core or Dapper packages
- Transient error detection is provider-specific (SQL Server error codes ≠ PostgreSQL error codes)
- Retry inside a transaction is a data corruption hazard (see ADR-016)
- Users who already use Polly via ASP.NET Core `AddResiliencePipeline` should be able to reuse their pipeline

## Options Considered

### Option A: Embed retry logic in `DapperExtensions.ExecuteAsync`
- Rejected: Polly becomes a transitive dependency of the Dapper package; violates ADR-003

### Option B: Dedicated `EricksonLopez.SqlBuilder.Dapper.Resilience` package
- **Chosen**: Optional, additive, follows the modularity philosophy

### Option C: Document how to use Polly directly (no integration package)
- Rejected: Users repeatedly write the same boilerplate; transient error detection is hard to get right per provider

### Option D: `IDbConnection` decorator that injects resilience transparently
- Rejected: Connection lifecycle complexity; proxy breaks AOT

## Decision

**Package:** `EricksonLopez.SqlBuilder.Dapper.Resilience`

**Dependencies:** `EricksonLopez.SqlBuilder.Dapper` + `Microsoft.Extensions.Resilience` (Polly v8)

**Core abstractions:**
```csharp
// Transient error detection per provider
public interface ISqlTransientErrorDetector
{
    bool IsTransient(Exception exception);
}

// Pre-configured detectors
SqlServerTransientErrorDetector.Default  // error codes: 1205, 1222, 40197, 40501, 40613, 49918
PostgreSqlTransientErrorDetector.Default // SQLSTATE: 40001 (serialization_failure), 08006
MySqlTransientErrorDetector.Default      // error codes: 1213 (deadlock), 2006, 2013
```

**Extension methods:**
```csharp
// Execute with resilience pipeline
await connection.ExecuteWithResilienceAsync(query, pipeline, cancellationToken);

// Query with resilience pipeline
var results = await connection.QueryWithResilienceAsync<User>(query, pipeline, ct);

// Built-in pipeline factory (opinionated defaults)
var pipeline = SqlResilienceDefaults.Standard(detector: SqlServerTransientErrorDetector.Default);
// → 3 retries, exponential backoff (1s, 2s, 4s), 30s timeout
```

**Critical constraint (from ADR-016):**
Resilience pipelines MUST wrap the entire transactional unit — not individual statements inside a transaction.

```csharp
// ✅ Correct — retry wraps the full unit of work
await pipeline.ExecuteAsync(async ct =>
{
    await using var uow = await connection.BeginUnitOfWorkAsync(ct: ct);
    await connection.ExecuteAsync(insertQuery, uow, ct);
    await uow.CommitAsync(ct);
}, cancellationToken);

// ❌ Wrong — retry inside a transaction (Roslyn ESQL005 warns on this)
await using var uow = await connection.BeginUnitOfWorkAsync();
await pipeline.ExecuteAsync(async ct =>
{
    await connection.ExecuteAsync(insertQuery, uow, ct);
}, cancellationToken);
await uow.CommitAsync();
```

**Roslyn Analyzer (ESQL005):** Warns when `ResiliencePipeline.ExecuteAsync` wraps a call that has a `IDbTransaction` / `IUnitOfWork` parameter — indicating retry inside a transaction.

## Consequences

### Positive
- ✅ Correct transient error detection per provider (hard to get right manually)
- ✅ Polly v8 integration with minimal boilerplate
- ✅ Analyzer (ESQL005) catches the retry-inside-transaction anti-pattern
- ✅ `Microsoft.Extensions.Resilience` pipeline compatibility — users can inject their own

### Negative
- ❌ Adds Polly v8 as a direct dependency (acceptable — it's an opt-in package)
- ❌ Integration testing requires real DB connections to test retry behavior
- ❌ Circuit breaker state is per-pipeline instance — users must understand pipeline lifecycle

## Implementation Notes (v1.1)
- Start with SQL Server + PostgreSQL transient detectors
- MySQL + SQLite detectors can be contributed by community
- Oracle detector deferred (complex error code hierarchy)

## Reconsideration Criteria
If .NET ships a native resilience primitive that replaces Polly without the ~200KB overhead, evaluate replacing the dependency.

## References
- [ADR-003: Polly Not a Core Dependency](./adr-003-polly-not-core-dependency.md)
- [ADR-016: Transaction + Retry Semantics](./adr-016-transaction-retry-semantics.md)
- [docs/Resilience.md](../Resilience.md)
- [FEATURE_MATRIX.md §9 — Polly / Resilience Analysis](../../FEATURE_MATRIX.md)
