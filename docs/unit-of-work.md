# Unit of Work — EricksonLopez.SqlBuilder

> **Package:** `EricksonLopez.SqlBuilder.Dapper.UnitOfWork` (planned v1.1)
> **ADR:** [ADR-004](decisions/adr-004-unitofwork-outside-core.md), [ADR-016](decisions/adr-016-transaction-retry-semantics.md)

---

## What Is the Unit of Work Pattern?

The Unit of Work (UoW) pattern groups multiple database operations into a single atomic transaction. All operations succeed together, or all roll back together.

In Dapper, this requires manually managing `IDbTransaction`. This package provides a correct, async-first implementation that eliminates common mistakes.

---

## Common Problems with Manual Transaction Management

| Problem | Consequence | Our Fix |
|---------|-------------|---------|
| Missing `await using` | Connection/transaction leak | `IAsyncDisposable` enforcement |
| Missing try/catch + rollback | Partial mutations on exception | Auto-rollback on `DisposeAsync` if not committed |
| Sync `Dispose()` only | Deadlocks in async code | `IAsyncDisposable` primary path |
| No CancellationToken at commit | Cannot cancel long commits | `CommitAsync(CancellationToken)` |
| No savepoints | Cannot partially roll back | Optional `ISavepoint` |
| Retry inside transaction | Data corruption | ELSB005 analyzer + documentation |

---

## API Reference

### Starting a Unit of Work

```csharp
// Method 1: Explicit (recommended for full control)
await using var uow = await connection.BeginUnitOfWorkAsync(
    IsolationLevel.ReadCommitted,
    cancellationToken);

await connection.ExecuteAsync(command1, uow.Transaction, cancellationToken);
await connection.ExecuteAsync(command2, uow.Transaction, cancellationToken);

await uow.CommitAsync(cancellationToken);
// If CommitAsync is not called before disposal → automatic rollback
```

```csharp
// Method 2: Functional scope (automatic commit on success)
await connection.WithUnitOfWorkAsync(async (uow, ct) =>
{
    await connection.ExecuteAsync(command1, uow.Transaction, ct);
    await connection.ExecuteAsync(command2, uow.Transaction, ct);
    // commits automatically if no exception thrown
}, cancellationToken);
```

### Using Savepoints

```csharp
await using var uow = await connection.BeginUnitOfWorkAsync();

await connection.ExecuteAsync(command1, uow.Transaction);

// Create a savepoint before risky operation
var sp = await uow.CreateSavepointAsync("before_risky");

try
{
    await connection.ExecuteAsync(riskyCommand, uow.Transaction);
}
catch (Exception)
{
    await sp.RollbackAsync(); // Roll back only to savepoint
}

await connection.ExecuteAsync(command2, uow.Transaction);
await uow.CommitAsync();
```

---

## Interface Definitions

```csharp
public interface IUnitOfWork : IAsyncDisposable
{
    IDbTransaction Transaction { get; }
    IsolationLevel IsolationLevel { get; }

    /// <summary>Commits the transaction. Must be called to persist changes.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Explicitly rolls back. Optional — disposal auto-rolls back if not committed.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates a named savepoint within the transaction.</summary>
    Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken ct = default);
}

public interface ISavepoint
{
    string Name { get; }
    Task RollbackAsync(CancellationToken cancellationToken = default);
    Task ReleaseAsync(CancellationToken cancellationToken = default);
}
```

---

## ⚠️ CRITICAL: Transaction + Retry Correctness

See [ADR-016](decisions/adr-016-transaction-retry-semantics.md) for full details.

### ❌ WRONG — Data Corruption Risk

```csharp
// DO NOT DO THIS
await using var uow = await connection.BeginUnitOfWorkAsync();

await pipeline.ExecuteAsync(async ct =>        // ← retry wraps individual command
    await connection.ExecuteAsync(cmd, uow.Transaction, ct));

await uow.CommitAsync();
// If cmd succeeds then retries, the operation runs multiple times!
```

### ✅ CORRECT

```csharp
// Retry wraps the ENTIRE transactional block
await pipeline.ExecuteAsync(async ct =>
{
    await using var uow = await connection.BeginUnitOfWorkAsync(ct: ct);
    await connection.ExecuteAsync(cmd1, uow.Transaction, ct);
    await connection.ExecuteAsync(cmd2, uow.Transaction, ct);
    await uow.CommitAsync(ct);
});
```

---

## Isolation Levels

| Level | Description | Use When |
|-------|-------------|---------|
| `ReadUncommitted` | Dirty reads allowed | Reporting only, no writes |
| `ReadCommitted` | Default for most DBs | General OLTP |
| `RepeatableRead` | Prevents non-repeatable reads | Financial calculations |
| `Serializable` | Strictest, slowest | Critical inventory, balances |
| `Snapshot` | MVCC-based (SQL Server, PG) | High concurrency reads with writes |

---

## DI Registration

```csharp
// In Program.cs / Startup
services.AddScoped<IUnitOfWork>(sp =>
{
    var connection = sp.GetRequiredService<IDbConnection>();
    return new UnitOfWork(connection);
});
```

**Important:** Always register as **Scoped**. Never Singleton (transaction shared across requests) or Transient (transaction lost between operations).

---

## Related Documents

- [ADR-004: UnitOfWork Outside Core](decisions/adr-004-unitofwork-outside-core.md)
- [ADR-016: Transaction + Retry Semantics](decisions/adr-016-transaction-retry-semantics.md)
- [Resilience.md](Resilience.md)
