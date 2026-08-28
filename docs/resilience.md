# Resilience — EricksonLopez.SqlBuilder

> **Package:** `EricksonLopez.SqlBuilder.Dapper.Resilience` (**planned** — not yet published)
> **ADR:** [ADR-003](decisions/adr-003-polly-not-core-dependency.md), [ADR-016](decisions/adr-016-transaction-retry-semantics.md)

> [!IMPORTANT]
> **Current status:** The `EricksonLopez.SqlBuilder.Dapper.Resilience` package is not yet available on NuGet. The extension methods `QueryWithResilienceAsync` and `ExecuteWithResilienceAsync` shown in this document represent the planned API surface. **Use the Polly-direct pattern shown below until the package ships.**

---

## Overview

The `Dapper.Resilience` package provides optional Polly v8 (`Microsoft.Extensions.Resilience`) integration for executing SQL queries with retry, timeout, and circuit breaker policies.

**Polly is NEVER a dependency of Core or `EricksonLopez.SqlBuilder.Dapper`.**

---

## Why Resilience Matters for Database Operations

Transient database failures are common in production:
- Network hiccups
- Connection pool exhaustion
- Database deadlocks
- Azure SQL throttling
- PostgreSQL serialization failures

Retrying these operations correctly prevents unnecessary failures.

---

## Installation

```bash
dotnet add package EricksonLopez.SqlBuilder.Dapper.Resilience
dotnet add package Microsoft.Extensions.Resilience
```

---

## Quick Start

### Configure a Pipeline

```csharp
var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder()
            .Handle<SqlException>(SqlTransientErrors.IsSqlServerTransient)
            .Handle<TimeoutException>(),
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        BaseDelay = TimeSpan.FromMilliseconds(500)
    })
    .AddTimeout(TimeSpan.FromSeconds(30))
    .Build();
```

### Execute with Resilience (Planned Extension API)

> [!NOTE]
> The following API is the planned interface for `EricksonLopez.SqlBuilder.Dapper.Resilience`. **It is not yet available.**

```csharp
// [PLANNED] Query
var users = await connection.QueryWithResilienceAsync<User>(
    query, pipeline, cancellationToken);

// [PLANNED] Execute (INSERT/UPDATE/DELETE)
var rows = await connection.ExecuteWithResilienceAsync(
    command, pipeline, cancellationToken);
```

### Current Workaround — Polly Direct

Until the package ships, wrap the execution call directly:

```csharp
// ✅ Works today with EricksonLopez.SqlBuilder.Dapper
var users = await pipeline.ExecuteAsync(
    async ct => (IEnumerable<User>)await connection.QueryAsync<User>(query, cancellationToken: ct),
    cancellationToken);

---

## Transient Error Detection

Use the built-in detector helpers:

```csharp
// SQL Server
.Handle<SqlException>(SqlTransientErrors.IsSqlServerTransient)

// PostgreSQL
.Handle<NpgsqlException>(PostgreSqlTransientErrors.IsTransient)

// MySQL
.Handle<MySqlException>(MySqlTransientErrors.IsTransient)

// SQLite
.Handle<SqliteException>(SQLiteTransientErrors.IsTransient)
```

### Transient Error Codes Reference

| Provider | Transient Codes |
|----------|----------------|
| SQL Server | 1205 (deadlock), 40613, 40197, 40501, 49918 |
| Azure SQL | 40613, 40197, 40501, 49918, 4221 |
| PostgreSQL | 40001 (serialization failure), 40P01 (deadlock) |
| MySQL | 1213 (deadlock), 1205 (lock wait timeout) |
| SQLite | SQLITE_BUSY (5), SQLITE_LOCKED (6) |
| Oracle | ORA-00060 (deadlock), ORA-08177 (can't serialize) |

---

## ⚠️ CRITICAL: Transactions and Retry

> **Never** retry individual SQL commands inside an active transaction.
> **Always** retry the entire transactional block as a unit.

### ❌ WRONG

```csharp
await using var uow = await connection.BeginUnitOfWorkAsync();

// Retrying inside a transaction can duplicate operations!
await pipeline.ExecuteAsync(async ct =>
    await connection.ExecuteAsync(insertCmd, uow.Transaction, ct));

await uow.CommitAsync();
```

### ✅ CORRECT

```csharp
await pipeline.ExecuteAsync(async ct =>
{
    await using var uow = await connection.BeginUnitOfWorkAsync(ct: ct);
    await connection.ExecuteAsync(insertCmd, uow.Transaction, ct);
    await connection.ExecuteAsync(updateCmd, uow.Transaction, ct);
    await uow.CommitAsync(ct);
});
```

**Why:** If `insertCmd` succeeds but `updateCmd` fails transiently on the 2nd attempt, the retry creates a new transaction and re-runs `insertCmd` — which may insert a duplicate row.

---

## Using with DI / ResiliencePipelineProvider

```csharp
// Registration
services.AddResiliencePipeline("sql-default", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder()
                .Handle<SqlException>(SqlTransientErrors.IsSqlServerTransient),
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        })
        .AddTimeout(TimeSpan.FromSeconds(30));
});

// Usage in service
public class OrderRepository
{
    private readonly IDbConnection _connection;
    private readonly ResiliencePipeline _pipeline;

    public OrderRepository(
        IDbConnection connection,
        ResiliencePipelineProvider<string> pipelines)
    {
        _connection = connection;
        _pipeline = pipelines.GetPipeline("sql-default");
    }

    public Task<IEnumerable<Order>> GetActiveOrdersAsync(CancellationToken ct)
    {
        var query = Sql.From<Order>().Where(o => o.IsActive);
        // [PLANNED] When Dapper.Resilience ships:
        // return _connection.QueryWithResilienceAsync<Order>(query, _pipeline, ct);
        
        // Current workaround:
        return _pipeline.ExecuteAsync(
            async ct => (IEnumerable<Order>)await _connection.QueryAsync<Order>(query, cancellationToken: ct),
            ct).AsTask();
    }
}
```

---

## Resilience Strategy Guidelines

| Strategy | Use For | Avoid For |
|---------|---------|----------|
| Retry | Transient network/DB errors | Business logic errors, auth failures |
| Timeout | Long-running queries | Quick CRUD |
| Circuit Breaker | Repeated failures | Single-use operations |
| Hedging | Read queries (acceptable duplicate) | Write queries |

---

## Observability Integration

When combined with OpenTelemetry:

```csharp
// Resilience retry attempts appear as events on the db.query span
// Enable via standard Polly telemetry
services.AddResiliencePipelineTelemetry();
```

---

## Related Documents

- [ADR-003: Polly Not Core Dependency](decisions/adr-003-polly-not-core-dependency.md)
- [ADR-016: Transaction + Retry Semantics](decisions/adr-016-transaction-retry-semantics.md)
- [UnitOfWork.md](UnitOfWork.md)
