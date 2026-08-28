# ADR-016: Transaction + Retry Semantic Correctness

## Status
Proposed

## Context
Combining retry policies with database transactions is a well-known source of data corruption and phantom duplicate records. If a transaction partially executes and the connection drops before commit, retrying the entire operation will execute the operations a second time — leading to duplicate inserts, double-counted updates, or inconsistent state.

## Problem
The `EricksonLopez.SqlBuilder.Dapper.Resilience` package introduces `ResiliencePipeline` integration. Without explicit guidance and tooling, developers will accidentally compose retry + transaction in the wrong order.

### Wrong Pattern (data corruption risk)

```csharp
// WRONG: retry wraps individual commands inside an existing transaction
await using var uow = await connection.BeginUnitOfWorkAsync();

await pipeline.ExecuteAsync(async ct =>
    await connection.ExecuteAsync(insertCommand, uow.Transaction, ct));

await pipeline.ExecuteAsync(async ct =>
    await connection.ExecuteAsync(updateCommand, uow.Transaction, ct));

await uow.CommitAsync();
// If insert succeeds, update fails transiently and is retried → insert may be committed by a
// concurrent commit, leaving inconsistent state
```

### Correct Pattern

```csharp
// CORRECT: retry wraps the ENTIRE transactional operation
await pipeline.ExecuteAsync(async ct =>
{
    await using var uow = await connection.BeginUnitOfWorkAsync(ct: ct);
    await connection.ExecuteAsync(insertCommand, uow.Transaction, ct);
    await connection.ExecuteAsync(updateCommand, uow.Transaction, ct);
    await uow.CommitAsync(ct);
    // If any step fails transiently, the entire unit is retried atomically
});
```

## Decision

1. **The `IUnitOfWork` interface must NOT accept a `ResiliencePipeline` parameter.** Providing this parameter would suggest incorrect usage.

2. **Roslyn Analyzer ELSB005** warns when a `ResiliencePipeline.ExecuteAsync` lambda contains a direct call to `CommitAsync()` without wrapping the full UoW lifecycle.

3. **Documentation in `Dapper.Resilience` package** must include prominent warning with correct and incorrect patterns.

4. **The `WithUnitOfWorkAsync` convenience method** MUST NOT accept a `ResiliencePipeline` parameter for the same reason.

## Consequences

### Positive
- Prevents a common and hard-to-debug data corruption pattern
- Analyzer provides early feedback

### Negative
- Developers must structure their code correctly
- Analyzer ELSB005 requires analysis of lambda call graphs (complexity)

### Positive (Side Effect)
- Forces explicit transaction lifetime management — makes code easier to audit

## Performance Impact
None — this is a design-time constraint.

## API Impact
```csharp
// IUnitOfWork does NOT have:
Task CommitAsync(ResiliencePipeline pipeline, CancellationToken ct); // ❌ Never add this

// Correct external usage:
await pipeline.ExecuteAsync(async ct =>
{
    await using var uow = await connection.BeginUnitOfWorkAsync(ct: ct);
    // ... operations ...
    await uow.CommitAsync(ct);
});
```

## Reconsideration Criteria
If Microsoft publishes a canonical pattern for transaction + retry in `Microsoft.Extensions.Resilience` that supersedes this guidance, align to it.
