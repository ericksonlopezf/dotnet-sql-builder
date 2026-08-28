# ADR-021: RETURNING / OUTPUT Clause Design

## Status
Accepted (implemented in v1.1)

## Date
2026-08-12

## Context
Inserting a row and immediately reading back generated values (auto-increment IDs, server-side defaults like `GETUTCDATE()`, computed columns) is a universal requirement. Each database dialect implements this differently.

## Problem
Without `RETURNING` / `OUTPUT`, the common workaround is:
1. `INSERT INTO table (...) VALUES (...)`
2. `SELECT LAST_INSERT_ID()` / `SCOPE_IDENTITY()` / sequence query

This requires two round trips and is non-atomic (if another insert occurs between the two statements, LAST_INSERT_ID() may be ambiguous without proper connection isolation).

## Dialect Differences

| Dialect | Syntax | Returns |
|---------|--------|---------|
| PostgreSQL | `INSERT ... RETURNING id, created_at` | Result set |
| SQL Server | `INSERT ... OUTPUT INSERTED.id, INSERTED.created_at` | Result set |
| MySQL | No RETURNING (use `LAST_INSERT_ID()`) | — |
| SQLite 3.35+ | `INSERT ... RETURNING id` | Result set |
| Oracle | `INSERT ... RETURNING id INTO :v_id` | OUT parameter |

## Options Considered

### Option A: No RETURNING support — document LAST_INSERT_ID() workaround
- Rejected: too common a requirement; two round trips violate atomicity guarantees

### Option B: Database-agnostic `.Returning()` that maps to each dialect's mechanism
- **Chosen**: Single API, per-dialect SQL emission

### Option C: Dialect-specific `.ReturningPostgreSql()`, `.OutputSqlServer()` methods
- Rejected: forces users to write dialect-conditional code

## Decision

**Fluent API (uniform across all query types):**
```csharp
// InsertQuery
var query = Sql.Insert(new User { Name = "Alice" })
    .Returning(u => u.Id)
    .Returning(u => u.CreatedAt);

// UpdateQuery
var query = Sql.Update<User>()
    .Set(u => u.Name, "Bob")
    .Where(u => u.Id == 42)
    .Returning(u => new { u.Id, u.UpdatedAt });
```

**Dialect emission:**
```sql
-- PostgreSQL
INSERT INTO "users" ("name") VALUES (@p0) RETURNING "id", "created_at"

-- SQL Server
INSERT INTO [users] ([name]) OUTPUT INSERTED.[id], INSERTED.[created_at] VALUES (@p0)

-- SQLite 3.35+
INSERT INTO "users" ("name") VALUES (@p0) RETURNING "id", "created_at"

-- MySQL — throws NotSupportedException (use LAST_INSERT_ID() via separate query)
-- Oracle — throws NotSupportedException (use RETURNING INTO OUT parameter via Sql.Raw)
```

**Key design decisions:**
1. `MySqlCompiler.Visit(ReturningNode)` → throws `NotSupportedException` with guidance
2. `OracleCompiler.Visit(ReturningNode)` → throws `NotSupportedException` with guidance
3. The `NotSupportedException` message includes the correct workaround for that dialect

**Dapper execution:**
```csharp
var id = await connection.QuerySingleAsync<int>(query);
// query.Build(pgCompiler).Sql = "INSERT ... RETURNING id"
```

## Consequences

### Positive
- ✅ Single `.Returning()` call works for PostgreSQL, SQL Server, SQLite
- ✅ Atomic insert + read in one round trip
- ✅ Expression-based — compile-time column name validation
- ✅ Clear `NotSupportedException` guides users to correct workaround on MySQL/Oracle

### Negative
- ❌ MySQL does not support `RETURNING` — must use `LAST_INSERT_ID()` separately
- ❌ Oracle's `RETURNING INTO` requires OUT parameters — different execution model
- ❌ Multi-row insert RETURNING is only PostgreSQL-native (SQL Server requires OUTPUT INTO a temp table)

## Reconsideration Criteria
If MySQL 9+ adds `RETURNING` clause support, implement `MySqlCompiler.Visit(ReturningNode)`.

## References
- [FEATURE_MATRIX.md §4 — Complete Feature Discovery](../../FEATURE_MATRIX.md)
- `src/EricksonLopez.SqlBuilder.Abstractions/Nodes/InsertNodes.cs` — `ReturningNode`
- `src/EricksonLopez.SqlBuilder/InsertQuery.cs` — `.Returning()` methods
- `src/EricksonLopez.SqlBuilder.PostgreSql/PostgreSqlCompiler.cs`
- `src/EricksonLopez.SqlBuilder.SqlServer/SqlServerCompiler.cs`
