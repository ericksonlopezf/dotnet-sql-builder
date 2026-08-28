# ADR-011: Raw SQL Escape Hatch Policy

## Status
Accepted

## Date
2026-08-12

## Context
No query builder can express 100% of valid SQL. Power users need a way to write raw SQL for dialect-specific features (e.g., `AT TIME ZONE`, `JSON_VALUE`, `PIVOT`, stored procedure calls, hints). This creates an inherent tension between safety (parameterized SQL) and flexibility (raw strings).

## Problem
- A raw `string` overload enables SQL injection if the user concatenates user input
- Without any escape hatch, the library becomes a blocker for advanced users
- The `+` concatenation pattern (`"WHERE id = " + userId`) is the #1 SQL injection vector in .NET

## Options Considered

### Option A: No raw SQL — only typed builder API
- Rejected: blocks legitimate power user scenarios; library becomes unusable for stored procs, table hints, complex expressions

### Option B: Raw `string` only (no safety)
- Rejected: SQL injection risk; contradicts library's safety positioning

### Option C: `FormattableString` interpolation (safe) + `[Obsolete]` string overload
- **Chosen**: Provides safety by default; migration path via deprecation warning

### Option D: Named parameter DSL (`Sql.Raw("WHERE id = @id", new { id = value })`)
- Partially adopted as the `parameters` overload; deprecated in favor of `FormattableString`

## Decision

**Primary (safe) API:**
```csharp
// Holes become named parameters — CANNOT inject SQL
var query = Sql.Raw($"WHERE created_at > {cutoffDate} AND tenant_id = {tenantId}");
// Produces: WHERE created_at > @p0 AND tenant_id = @p1

// Direct Where escape hatch using FormattableString on SelectQuery:
var safeWhere = Sql.From<Product>()
    .Where($"price * tax_rate > {maxPrice} AND category_id = {categoryId}");
// Produces: SELECT ... FROM Products WHERE price * tax_rate > @p0 AND category_id = @p1
```

**Deprecated API (kept for migration only):**
```csharp
[Obsolete("Use FormattableString overload to prevent SQL injection. This overload will be removed in v2.0.")]
var query = Sql.Raw("WHERE created_at > @date", new { date = cutoffDate });
```

**Security guarantee:** `FormattableString` holes are unconditionally converted to named parameters. There is no code path that injects hole values directly into the SQL string.

**Roslyn Analyzer (ESQL004):** Warns when the `string` overload is used. Severity: Warning (not Error, to allow gradual migration).

## Consequences

### Positive
- ✅ `FormattableString` overload is safe by construction — no injection possible
- ✅ `[Obsolete]` warning guides users to the safe API
- ✅ Analyzer catches string overload usage in CI
- ✅ Power users retain full SQL expressiveness

### Negative
- ❌ `FormattableString` syntax (`$"..."`) may be unfamiliar to some users
- ❌ Deprecated string overload adds API surface that will need to be removed

## Migration Guide
```csharp
// Before (unsafe):
Sql.Raw("WHERE id = " + userId);  // ❌ SQL injection possible
Sql.Raw($"WHERE id = {userId}");  // ❌ Still a string — same issue

// After (safe):
Sql.Raw($"WHERE id = {userId}");  // ✅ FormattableString overload — parameter
```

> **Note:** The syntax looks identical, but the compiler picks the `FormattableString` overload when a `$"..."` literal is passed directly. If you assign to `string` first, it falls back to the unsafe overload.

## Reconsideration Criteria
If C# ships a `[InterpolatedStringHandler]` that makes the distinction clearer to users, evaluate removing the `string` overload entirely (v2.0 breaking change).

## References
- `src/EricksonLopez.SqlBuilder/Sql.cs` — `Sql.Raw` overloads
- `src/EricksonLopez.SqlBuilder.Analyzers/UnsafeStringConcatenationAnalyzer.cs` — ESQL002 (string concatenation)
- [ADR-013: AOT Guarantees](./adr-013-aot-guarantees.md)
