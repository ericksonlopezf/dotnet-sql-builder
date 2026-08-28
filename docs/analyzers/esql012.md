# ESQL012 — Retry Pipeline Wraps Transaction Commit

## Summary

Placing a `CommitAsync()` call inside a Polly `ExecuteAsync` lambda is a dangerous anti-pattern. On retry, the commit will be re-attempted after the transaction has already been partially applied, which can cause **duplicate inserts**, **corrupted state**, or **phantom writes**.

## Rule Details

**Diagnostic ID:** ESQL012  
**Category:** Usage  
**Severity:** Warning  
**Analyzer:** `RetryInsideTransactionAnalyzer`

## Violation

```csharp
// ❌ ESQL012 warning — CommitAsync is inside the retry lambda
await pipeline.ExecuteAsync(async ct =>
{
    await connection.ExecuteAsync(insertQuery, ct);
    await unitOfWork.CommitAsync(ct);  // ← flagged: re-executed on retry
}, cancellationToken);
```

On retry, the `ExecuteAsync` lambda re-runs from the beginning, **but the previous partial execution is already committed or mid-commit**. This causes duplicate rows or data corruption.

## Fix

Wrap the **entire transactional unit** (begin → execute → commit) inside the retry lambda:

```csharp
// ✅ Correct — the entire transaction is inside the retry scope
await pipeline.ExecuteAsync(async ct =>
{
    await using var uow = await connection.BeginUnitOfWorkAsync(ct: ct);
    await connection.ExecuteAsync(insertQuery, uow, ct);
    await uow.CommitAsync(ct);  // ✅ safe — re-begins transaction on retry
}, cancellationToken);
```

If the first attempt fails, the pipeline creates a **brand-new transaction** on retry, preventing any partial state from leaking.

## Rationale

This rule enforces the safety contract documented in **ADR-016**. A retry policy is only safe when the entire operation — including the transaction boundary — is idempotent within the retry scope.

## See Also

- [ADR-016: Resilience Policy Boundaries](../../docs/adr/ADR-016.md)
- [EricksonLopez.SqlBuilder.Dapper.Resilience](../../src/EricksonLopez.SqlBuilder.Dapper.Resilience/README.md)
