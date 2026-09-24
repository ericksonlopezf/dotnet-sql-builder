# Migration Guide — EricksonLopez.SqlBuilder

Practical guide for migrating existing data access code from **Entity Framework Core**, **SqlKata**, and **Raw Dapper** to **EricksonLopez.SqlBuilder**, as well as upgrading across major library versions.

---

## Table of Contents

- [1. Migrating from Entity Framework Core](#1-migrating-from-entity-framework-core)
- [2. Migrating from SqlKata](#2-migrating-from-sqlkata)
- [3. Migrating from Raw SQL Strings & Dapper](#3-migrating-from-raw-sql-strings--dapper)
- [4. Modernizing to Current Idiomatic SqlBuilder Patterns](#4-modernizing-to-current-idiomatic-sqlbuilder-patterns)

---

## 1. Migrating from Entity Framework Core

### Architectural Contrast

| Dimension | Entity Framework Core | EricksonLopez.SqlBuilder |
|---|---|---|
| **Paradigm** | Full ORM with Change Tracker | Immutable AST Query Builder |
| **Memory Allocation** | High (snapshot buffers, change tracking state) | Zero to minimal (pure records, ImmutableArray) |
| **Native AOT** | Requires heavy trimming configurations and compiled models | 100% Native AOT ready via compile-time Source Generators |
| **Query Predictability** | Complex LINQ-to-SQL translation with potential N+1 | Explicit, deterministic SQL compiled directly to target dialect |
| **Bulk Operations** | Heavy save loops or third-party extensions | Built-in native high-speed bulk ingestion pipelines |

### Code Comparison

#### EF Core (Before):
```csharp
// EF Core context query
var users = await dbContext.Users
    .Where(u => u.IsActive && u.Role == "Admin")
    .OrderByDescending(u => u.CreatedAt)
    .Take(20)
    .AsNoTracking()
    .ToListAsync();

// EF Core update
var user = await dbContext.Users.FindAsync(id);
if (user != null)
{
    user.Email = newEmail;
    await dbContext.SaveChangesAsync();
}
```

#### SqlBuilder (After):
```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;

// SqlBuilder query
var query = Sql.From<User>()
    .Where(u => u.IsActive && u.Role == "Admin")
    .OrderByDescending(u => u.CreatedAt)
    .Limit(20);

var users = (await connection.QueryAsync<User>(query, compiler)).ToList();

// SqlBuilder targeted update (zero prior load overhead)
var updateQuery = Sql.Update<User>()
    .Set(u => u.Email, newEmail)
    .Where(u => u.Id == id);

await connection.ExecuteAsync(updateQuery, compiler);
```

---

## 2. Migrating from SqlKata

### Key Differences
- **Immutability**: SqlKata queries are **mutable** objects. Invoking `.Where()` alters the query instance in-place, causing severe concurrency bugs if queries are shared across threads. In `SqlBuilder`, all queries are **immutable records**; every method returns a new copy.
- **Type Safety**: SqlKata relies heavily on untyped magic strings (`Where("is_active", true)`). `SqlBuilder` uses strongly-typed C# expressions (`Where(u => u.IsActive)`).

### Code Comparison

#### SqlKata (Before — Prone to Cross-Thread Mutation):
```csharp
// ❌ SqlKata modifies 'baseQuery' in place!
var baseQuery = new Query("users").Where("tenant_id", 100);

var admins = baseQuery.Where("role", "Admin"); 
// 'baseQuery' now has the Admin filter permanently attached!
```

#### SqlBuilder (After — Pure & Thread-Safe):
```csharp
// ✅ SqlBuilder returns new immutable instances:
var baseQuery = Sql.From<User>()
    .Where(u => u.TenantId == 100);

var admins = baseQuery.Where(u => u.Role == "Admin");
var members = baseQuery.Where(u => u.Role == "Member");
// 'baseQuery' remains completely untouched and safe for reuse.
```

---

## 3. Migrating from Raw SQL Strings & Dapper

### Key Differences
- **SQL Injection Prevention**: Raw SQL string concatenation easily introduces SQL injection vulnerabilities. `SqlBuilder` generates fully parameterized queries (`@p0`, `@p1`) automatically.
- **Schema Refactoring**: Changing a property name in C# with `SqlBuilder` automatically updates the query via compiler symbols, whereas raw SQL strings fail silently until execution.

### Code Comparison

#### Raw SQL & Dapper (Before — Fragile & Prone to Concatenation):
```csharp
// ❌ Vulnerable to SQL injection if interpolated incorrectly:
string sql = $"SELECT * FROM users WHERE status = '{status}' AND age >= {minAge}";
var users = await connection.QueryAsync<User>(sql);
```

#### SqlBuilder (After — Parameterized & Refactoring-Safe):
```csharp
// ✅ Safe, strongly-typed and automatically parameterized:
var query = Sql.From<User>()
    .Where(u => u.Status == status && u.Age >= minAge);

var users = await connection.QueryAsync<User>(query, compiler);
```

---

## 4. Modernizing to Current Idiomatic SqlBuilder Patterns

### Modern Architecture & Best Practices

When transitioning legacy code to the current `EricksonLopez.SqlBuilder` architecture, adhere to the following modern design patterns:

1. **Static Query Entry Points**:
   - Rather than instantiating ad-hoc query builders, use the standardized fluent entry points: `Sql.From<T>()`, `Sql.Insert<T>()`, `Sql.Update<T>()`, and `Sql.Delete<T>()`.
2. **Standardized Paged Result**:
   - Leverage `EricksonLopez.SqlBuilder.Pagination` which provides standardized `.ToPagedListAsync()` and keyset cursor pagination directly over `SelectQuery` ASTs.
3. **Mandatory Guarded Deletions**:
   - Unconstrained deletions without a `Where` clause are flagged by analyzer `ESQL002` (and rejected at runtime unless explicitly opted into) to prevent unintended table truncation.
4. **Compile-Time Source Generators & NativeAOT**:
   - Annotate entities with `[SqlEntity]` to unlock compile-time metadata generation and zero-reflection `AotQueryExecutor` support.
