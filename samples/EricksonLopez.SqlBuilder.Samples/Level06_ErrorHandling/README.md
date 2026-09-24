# Level 6: Error Handling & Resilience

## Overview

Level 6 demonstrates production resilience patterns: transient error handling with exponential backoff retries, optimistic concurrency checking via `WithConcurrencyToken`, catching `DbConcurrencyException`, safe mutations with `RETURNING`, and error diagnostics.

---

## Resilience Architecture

1. **Transient vs Permanent Error Classification**:
   Transient exceptions (e.g., deadlock victims, network dropouts) warrant retry with jitter; constraint violations or syntax errors must fail fast.
2. **Optimistic Concurrency Control**:
   Append `WithConcurrencyToken(e => e.Version)` to updates. SqlBuilder increments the version column and ensures `RowsAffected == 1`. If zero rows are affected, `ExecuteWithConcurrencyCheckAsync` throws `DbConcurrencyException`.
3. **Atomic Generated Values**:
   Use dialect `RETURNING` clauses to retrieve server-assigned values (e.g., identity keys, default timestamps) in the same execution turn.

---

## Code Examples

### Optimistic Concurrency Check
```csharp
var updateQuery = Sql.Update<VersionedItem>()
    .Set(i => i.Name, "Updated Item")
    .WithConcurrencyToken(i => i.Version, currentVersion)
    .Where(i => i.Id == itemId);

// Throws DbConcurrencyException if another process modified the row
await connection.ExecuteWithConcurrencyCheckAsync(updateQuery, "VersionedItem");
```

### Retry Policy
```csharp
int maxRetries = 3;
for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        await connection.ExecuteAsync(query);
        break;
    }
    catch (SqliteException ex) when (IsTransient(ex) && attempt < maxRetries)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100));
    }
}
```

---

## Execution Output

```text
=== LEVEL 6: ERROR HANDLING, RESILIENCE, AND CONCURRENCY ===
[+] 1. Execution with Retry and Exponential Backoff
[+] 2. WithConcurrencyToken — Optimistic Concurrency (int auto-increment)
[+] 3. ExecuteWithConcurrencyCheckAsync — Throws DbConcurrencyException
    [!] DbConcurrencyException caught: Optimistic concurrency conflict detected for entity 'VersionedItem'.
[+] 4. WithConcurrencyToken — With explicit token (Guid pattern demo)
[+] 5. RETURNING clause in INSERT and UPDATE
[+] 6. SqlBuilderDiagnostics — Error observability
```
