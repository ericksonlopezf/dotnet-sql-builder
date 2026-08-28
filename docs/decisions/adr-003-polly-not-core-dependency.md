# ADR-003: Polly Must Never Be a Core Dependency

## Status
Accepted

## Context
Polly v8+ (`Microsoft.Extensions.Resilience`) is the standard resilience library for .NET. Database operations benefit from retry, timeout, and circuit breaker patterns.

## Problem
Making Polly a dependency of Core or Dapper packages would:
1. Add ~200KB+ to packages for users who don't need resilience
2. Couple SQL generation/execution to infrastructure concerns
3. Force a specific resilience framework on users who may use alternatives (Polly, custom, platform-level)
4. Conflict with teams that manage resilience at a higher layer (API gateway, service mesh)

## Options Considered
### Option A: Polly in Core
- Rejected: Violates single-responsibility; couples query building to resilience

### Option B: Polly in Dapper package
- Rejected: Not all Dapper users want Polly; adds unnecessary dependency

### Option C: Separate `EricksonLopez.SqlBuilder.Dapper.Resilience` package
- Chosen: Optional, clean separation

### Option D: Expose `Func<Task>` overloads that accept user-provided retry wrappers
- Viable alternative but less ergonomic than a dedicated package

## Decision
Resilience integration lives exclusively in `EricksonLopez.SqlBuilder.Dapper.Resilience`.

## Rationale
- Modularity: users opt-in to resilience
- No forced dependency on Polly
- Clean architecture: SQL building and execution are separate from retry semantics
- Users with existing Polly pipelines can integrate via the extension methods; users without Polly are not penalized

## Consequences
### Positive
- Core and Dapper packages have no Polly dependency
- Users choose their resilience strategy
- Easy to test without Polly

### Negative
- Users must install an additional package for retry behavior

## API Impact
```csharp
// Only available in EricksonLopez.SqlBuilder.Dapper.Resilience
await connection.ExecuteWithResilienceAsync(command, pipeline, cancellationToken);
await connection.QueryWithResilienceAsync<T>(query, pipeline, cancellationToken);
```

## Transaction + Retry Critical Note
This package MUST document: retry policies must wrap the entire transactional block (UoW creation + execute + commit), never individual commands within a transaction.

## Reconsideration Criteria
If .NET BCL adds native resilience primitives without requiring Polly, revisit adding thin built-in support to the Dapper package.
