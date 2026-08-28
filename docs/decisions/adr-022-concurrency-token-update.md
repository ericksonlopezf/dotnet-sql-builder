# ADR-022: Concurrency Token in UPDATE (Optimistic Locking)

## Status
Proposed (deferred to v1.2)

## Date
2026-08-12

## Context
Optimistic concurrency control prevents lost updates in concurrent write scenarios. The standard pattern is a "concurrency token" — a column whose value changes on every write (e.g., `version INT`, `row_version ROWVERSION`, `updated_at TIMESTAMP`).

```sql
-- Safe optimistic update:
UPDATE users
SET name = @name, version = version + 1
WHERE id = @id AND version = @expectedVersion
-- If 0 rows affected → someone else updated it → throw OptimisticConcurrencyException
```

## Problem
- Without built-in support, users must manually add the `AND version = @v` condition
- No consistent handling of "0 rows affected = concurrency conflict" across the library
- EF Core handles this automatically via `[ConcurrencyCheck]` / `IsRowVersion()`; our users expect something similar

## Options Considered

### Option A: No support — document the pattern manually
- Rejected: too common in DDD; manual implementation is error-prone

### Option B: `[ConcurrencyCheck]` attribute + Source Generator support
- **Chosen** (deferred): Generate the concurrency token condition automatically

### Option C: `.WithVersion(entity.Version)` fluent method on UpdateQuery
- Complementary to Option B; explicit API for cases without Source Generator

### Option D: Database-native row versioning (ROWVERSION, OID, xmin)
- Deferred: dialect-specific; requires separate ADR per dialect

## Decision

**Deferred to v1.2** due to:
1. Requires coordination between Source Generator and UpdateQuery
2. Exception type design (`ConcurrencyException`) needs library-wide discussion
3. Dialect differences for native row versioning are significant

**Planned implementation (v1.2):**

**Option A — Attribute-based (Source Generator path):**
```csharp
[SqlEntity]
public partial class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [ConcurrencyToken]  // Source Generator auto-adds WHERE version = @v to all updates
    public int Version { get; set; }
}

// Usage — version check is automatic:
await connection.ExecuteAsync(Sql.Update(user));
// → UPDATE users SET name = @name, version = version + 1
//   WHERE id = @id AND version = @version
```

**Option B — Explicit fluent API:**
```csharp
var query = Sql.Update<User>()
    .Set(u => u.Name, "Bob")
    .Where(u => u.Id == 42)
    .WithConcurrencyToken(u => u.Version, expectedVersion: 3);
// → UPDATE users SET name = @name WHERE id = @id AND version = 3
```

**Conflict detection:**
```csharp
var rowsAffected = await connection.ExecuteAsync(query);
if (rowsAffected == 0) throw new ConcurrencyConflictException(typeof(User), id: 42);
```

## Consequences

### Positive (when implemented)
- ✅ Lost-update prevention with minimal boilerplate
- ✅ Consistent with DDD aggregate pattern
- ✅ Source Generator integration means zero runtime overhead

### Negative (while deferred)
- ❌ Users must manually add concurrency WHERE condition
- ❌ No library-level `ConcurrencyConflictException` type

## Dialect Notes
- SQL Server `ROWVERSION` / `TIMESTAMP` — binary, auto-updated by server; read-back via `OUTPUT INSERTED.ts`
- PostgreSQL `xmin` — system column; free but not portable
- Standard approach: `INT version` + increment on every UPDATE (portable, explicit)

## Reconsideration Criteria
If multiple users report concurrency-related data corruption bugs caused by lack of support, reprioritize to v0.7.

## References
- [FEATURE_MATRIX.md §25 — Master Backlog P2](../../FEATURE_MATRIX.md)
- [ADR-007: No Change Tracking](./adr-007-no-change-tracking.md) — `ApplyDiff()` context
- [ADR-006: Source Generator Strategy](./adr-006-source-generator-strategy.md)
