# Analyzer Rules Reference — EricksonLopez.SqlBuilder

> **Purpose:** Definitive reference for all 21 Roslyn analyzer rules shipped with
> `EricksonLopez.SqlBuilder.Analyzers`. Includes severity, trigger conditions,
> compliant patterns, and suppression guidance.
> Last audit: 2026-08-14

---

## Rule Summary

| Rule ID | Description | Severity | Category |
|---------|-------------|----------|---------|
| [ESQL001](#esql001) | DELETE without WHERE | Error | Safety |
| [ESQL002](#esql002) | Raw SQL string concatenation | Error | Security |
| [ESQL003](#esql003) | UPDATE without WHERE | Error | Safety |
| [ESQL004](#esql004) | Query performance concern | Warning | Performance |
| [ESQL005](#esql005) | Dapper compiler misconfiguration | Warning | Usage |
| [ESQL006](#esql006) | Missing ON condition in JOIN | Warning | Safety |
| [ESQL007](#esql007) | Potential missing index hint | Info | Performance |
| [ESQL008](#esql008) | Large OFFSET value | Warning | Performance |
| [ESQL009](#esql009) | LIKE leading wildcard | Warning | Performance |
| [ESQL010](#esql010) | LIKE wildcard usage concern | Warning | Performance |
| [ESQL011](#esql011) | `Sql.Raw(string)` unsafe overload | Warning | Security |
| [ESQL012](#esql012) | Retry pipeline inside transaction | Warning | Correctness |
| [ESQL020](#esql020) | Dialect-specific API + incompatible compiler | Warning | Correctness |
| [ESQL021](#esql021) | `[SqlEntity]` without Source Generator | Warning | Usage |
| [ESQL022](#esql022) | Type mapping registration issue | Warning | Usage |
| [ESQL023](#esql023) | Synchronous SQL call on UI thread | Warning | Performance |
| [ESQL024](#esql024) | Cartesian product (missing join condition) | Warning | Safety |
| [ESQL025](#esql025) | SqlKata API detected (migration code fix) | Info | Migration |
| [SQL003](#sql003) | `SELECT *` usage | Warning | Performance |
| [SQL004](#sql004) | Redundant WHERE condition | Warning | Correctness |
| [SQL009](#sql009) | Missing column reference | Warning | Correctness |

---

## Rule Details

### ESQL001

**DELETE without WHERE clause**  
**Severity:** Error  

**Trigger:** A `DeleteQuery<T>` chain reaches `.Build()` without calling `.Where()`, `.WhereAll()`, `.WhereExists()`, or `.WhereNotExists()`.

**Non-compliant:**
```csharp
var query = Sql.Delete<User>().Build(compiler);  // ESQL001: deletes all rows!
```

**Compliant:**
```csharp
var query = Sql.Delete<User>()
    .Where(u => u.Id == id)
    .Build(compiler);

// Or if full-table delete is intentional:
var query = Sql.Delete<User>().WhereAll().Build(compiler);
```

**Suppression:** `#pragma warning disable ESQL001` — requires code review sign-off.

---

### ESQL002

**Raw SQL string concatenation**  
**Severity:** Error  

**Trigger:** String concatenation or interpolation is used inside `Sql.Raw()` without parameterization.

**Non-compliant:**
```csharp
var query = Sql.Raw($"SELECT * FROM users WHERE name = '{name}'");  // SQL injection!
```

**Compliant:**
```csharp
var query = Sql.Raw($"SELECT * FROM users WHERE name = {name}");  // parameterized
```

---

### ESQL003

**UPDATE without WHERE clause**  
**Severity:** Error  

**Trigger:** `UpdateQuery<T>` builds without any WHERE condition.

**Non-compliant:**
```csharp
Sql.Update<User>().Set<bool>(u => u.IsActive, false).Build(compiler);
```

**Compliant:**
```csharp
Sql.Update<User>().Set<bool>(u => u.IsActive, false).Where(u => u.Id == id).Build(compiler);
```

---

### ESQL008

**Large OFFSET value**  
**Severity:** Warning  

**Trigger:** `.Offset(n)` where `n` exceeds a threshold (default: 10,000).

**Context:** Large OFFSETs cause full index scans and degrade performance at scale.

**Compliant alternative:**
```csharp
// Use keyset/cursor pagination instead:
Sql.From<Order>()
   .Where(o => o.Id > lastId)
   .OrderBy(o => o.Id)
   .Limit(20);
```

---

### ESQL009

**LIKE leading wildcard**  
**Severity:** Warning  

**Trigger:** `.Where(x => x.Name.Contains("search"))` produces `LIKE '%search%'` — the leading `%` prevents index use.

**Compliant (if full-text is needed):**
```csharp
// Use full-text search or restrict to suffix match:
.Where(x => x.Name.StartsWith("search"))  // LIKE 'search%' — index-friendly
```

---

### ESQL012

**Retry pipeline inside transaction**  
**Severity:** Warning  

**Trigger:** `ResiliencePipeline.ExecuteAsync(...)` call detected inside the scope of an `IUnitOfWork` or `IDbTransaction` parameter.

**Context:** Retrying inside a transaction after a transient error leaves the transaction in an unknown state and can cause data corruption.

**Non-compliant:**
```csharp
await using var uow = await connection.BeginUnitOfWorkAsync();
await pipeline.ExecuteAsync(async ct =>          // ESQL012: retry inside transaction!
{
    await connection.ExecuteAsync(query, uow, ct);
}, cancellationToken);
await uow.CommitAsync();
```

**Compliant:**
```csharp
await pipeline.ExecuteAsync(async ct =>           // retry wraps the entire unit of work
{
    await using var uow = await connection.BeginUnitOfWorkAsync(ct: ct);
    await connection.ExecuteAsync(query, uow, ct);
    await uow.CommitAsync(ct);
}, cancellationToken);
```

---

### ESQL020

**Dialect-specific API + incompatible compiler**  
**Severity:** Warning  

**Trigger:** A method decorated with `[RequiresCapability(ProviderCapability.X)]` is called,
and the registered compiler for the connection does not declare support for that capability.

**Example:**
```csharp
// [RequiresCapability(ProviderCapability.Apply | ProviderCapability.Lateral)]
query.CrossApply(subquery, "sub");  // ESQL020: MySqlCompiler does not support Apply
```

---

### ESQL021

**`[SqlEntity]` without Source Generator**  
**Severity:** Warning  

**Trigger:** A type is decorated with `[SqlEntity]` but `EricksonLopez.SqlBuilder.SourceGenerators`
is not referenced in the project.

**Fix:** Add the Source Generator package reference.

---

### ESQL025

**SqlKata API detected (migration code fix)**  
**Severity:** Info  

**Trigger:** Detected use of `new Query(...)`, `.WhereLike(...)`, or other SqlKata-specific APIs.

**Context:** Provides a code fix to migrate from SqlKata's string-based API to the typed EricksonLopez API.

---

### SQL003

**`SELECT *` usage**  
**Severity:** Warning  

**Trigger:** Query does not call `.Select(...)` — will emit `SELECT *`.

**Context:** `SELECT *` is brittle (column order/type changes), transfers unnecessary data, and
prevents covering index use.

**Compliant:**
```csharp
Sql.From<User>().Select(u => new { u.Id, u.Email })  // explicit columns
```

---

## Suppression Policy

- **ESQL001 / ESQL003 suppressions** require a code review comment explaining why full-table operation is intended.
- **ESQL002 suppressions** are not allowed in production code — raw SQL injection must be parameterized.
- All other suppressions should be accompanied by `// NOSONAR` equivalent comment documenting the reason.

---

## Custom Severity Configuration (`.editorconfig`)

```ini
[*.cs]
dotnet_diagnostic.ESQL001.severity = error
dotnet_diagnostic.ESQL002.severity = error
dotnet_diagnostic.ESQL003.severity = error
dotnet_diagnostic.ESQL008.severity = warning
dotnet_diagnostic.SQL003.severity = suggestion   # downgrade if SELECT * is acceptable in your context
```

---

*This document must be updated when analyzer rules are added, modified, or removed.*
