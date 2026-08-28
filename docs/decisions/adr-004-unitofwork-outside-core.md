# ADR-004: UnitOfWork Belongs Outside Core

## Status
Accepted

## Context
The Unit of Work (UoW) pattern provides transactional boundaries for grouping multiple database operations atomically. Many Dapper users implement it inconsistently, leading to connection leaks, missing rollbacks, and incorrect async disposal.

## Problem
Where should UoW live?

- UoW has no dependency on query building logic
- UoW manages connection and transaction lifecycle — not SQL text generation
- Including UoW in Core would tie transaction management to the SQL building library
- Including UoW in the Dapper package would add lifecycle semantics to a stateless extension package

## Options Considered

### Option A: UoW in Core
- Rejected: Core generates SQL, not transactions. This mixes concerns.

### Option B: UoW in `EricksonLopez.SqlBuilder.Dapper`
- Rejected: The Dapper package is stateless (extension methods only). UoW introduces lifecycle (IAsyncDisposable). Mixing stateless and stateful in one package complicates versioning and testing.

### Option C: Separate `EricksonLopez.SqlBuilder.Dapper.UnitOfWork` package
- Chosen: Clean separation. UoW can be used independently of the full SqlBuilder query building API.

### Option D: No UoW package — users implement their own
- Rejected: The pattern is complex to implement correctly (async disposal, auto-rollback, savepoints). Providing a correct implementation adds real value.

## Decision
`EricksonLopez.SqlBuilder.Dapper.UnitOfWork` is a standalone package. It depends on:
- `System.Data.Common` (IDbConnection, IDbTransaction)
- `EricksonLopez.SqlBuilder.Dapper` (optional — for `ExecuteAsync` extensions)

## Interface Design

```csharp
public interface IUnitOfWork : IAsyncDisposable
{
    IDbTransaction Transaction { get; }
    IsolationLevel IsolationLevel { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken ct = default);
}
```

## Critical Semantic Rule (ADR-016 cross-reference)
Retry policies MUST NOT wrap individual commands inside a transaction.
They MUST wrap the entire transactional block: BeginUoW → Execute → Commit.

## Consequences
### Positive
- UoW is independently versionable
- Clean single responsibility
- Can be used without full SqlBuilder

### Negative
- One more package to install

## API Impact
```csharp
// Primary: explicit
await using var uow = await connection.BeginUnitOfWorkAsync(IsolationLevel.ReadCommitted, ct);
await connection.ExecuteAsync(command, uow.Transaction, ct);
await uow.CommitAsync(ct);

// Convenience: functional scope
await connection.WithUnitOfWorkAsync(async (uow, ct) =>
{
    await connection.ExecuteAsync(command, uow.Transaction, ct);
}, ct);
```

## Reconsideration Criteria
If the Dapper ecosystem standardizes a UoW interface, align to it instead.
