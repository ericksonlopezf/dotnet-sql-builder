# Level 0: Conceptual Foundation

## What is EricksonLopez.SqlBuilder?

`EricksonLopez.SqlBuilder` is an **immutable, type-safe, NativeAOT-ready SQL query builder** for modern .NET (8.0 / 9.0 / 10.0).

It provides a clean, fluent abstraction to construct SQL queries without relying on fragile string concatenation or heavy ORM overhead. Every query is represented as an immutable Abstract Syntax Tree (AST) that compiles to dialect-specific SQL at execution time.

---

## The Problem It Solves

### Raw Dapper (fragile)
```csharp
// ❌ SQL injection risk, no type safety, hard to maintain
var sql = "SELECT * FROM users WHERE status = '" + status + "' AND age > " + minAge;
var users = await conn.QueryAsync<User>(sql);
```

### EF Core (too heavy)
```csharp
// ❌ Full ORM overhead, migration complexity, change tracking side-effects
var users = await ctx.Users
    .Where(u => u.Status == status && u.Age > minAge)
    .ToListAsync();
```

### EricksonLopez.SqlBuilder (correct approach)
```csharp
// ✅ Type-safe, parameterized, dialect-aware, immutable, zero allocation
var query = Sql.From<User>()
    .Where(u => u.Status == status && u.Age > minAge)
    .OrderBy(u => u.Name)
    .Limit(10);

var users = await connection.QueryAsync<User>(query);
```

---

## Why It Exists

| Pain Point | EricksonLopez.SqlBuilder Solution |
|---|---|
| SQL injection via string concat | Parameterized AST — never string concat |
| Hard-coded column names | Strongly-typed expression trees |
| Vendor lock-in | 6 dialect compilers (PostgreSQL, SQL Server, MySQL, MariaDB, SQLite, Oracle) |
| Reflection overhead at runtime | NativeAOT + Source Generators = zero reflection |
| Dapper incompatibility | First-class Dapper integration |
| Untestable raw SQL | `QueryAssert` + `SnapshotAssert` testing utilities |
| Mutable query state (SqlKata) | Fully immutable — thread-safe, composable |

---

## Core Architectural Pillars

### 1. Immutable AST
Every method call returns a **new** independent query instance. Queries can be safely shared across threads and reused as base queries.

```csharp
// Base query — immutable, reusable
var baseQuery = Sql.From<Product>().Where(p => p.IsActive);

// Derived queries — do not mutate baseQuery
var featured = baseQuery.Where(p => p.IsFeatured).Limit(5);
var byCategory = baseQuery.Where(p => p.CategoryId == 3);
```

### 2. Type-Safe Expression Trees
Use strongly-typed C# lambda expressions to build filters without magic strings:

```csharp
// Translates to: WHERE is_active = @p0 AND age >= @p1
Sql.From<User>().Where(u => u.IsActive && u.Age >= 18);
```

### 3. Multi-Dialect Compilation
The same AST compiles to any supported dialect:

```csharp
var query = Sql.From<User>().Where(u => u.Id == 1).Limit(1);

var pgSql    = query.Build(new PostgreSqlCompiler());   // LIMIT 1 syntax
var msSql    = query.Build(new SqlServerCompiler());    // TOP 1 syntax
var sqlite   = query.Build(new SqliteCompiler());       // LIMIT 1 syntax
var oracleSql = query.Build(new OracleCompiler());      // FETCH FIRST 1 ROWS ONLY
```

### 4. NativeAOT & Zero Reflection
Annotating entities with `[SqlEntity]` triggers a Roslyn Source Generator that produces all metadata at **compile time**:

```csharp
[SqlEntity("users")]
public partial class User
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
// The source generator emits: GetColumnNames(), GetValues(), GetTableName(), etc.
```

### 5. Dapper Synergy
Seamless integration with Dapper — queries are passed directly to Dapper extension methods:

```csharp
var query = Sql.From<User>().Where(u => u.Id == userId);
var user = await connection.QuerySingleOrDefaultAsync<User>(query);
```

---

## Advantages

| Advantage | Detail |
|---|---|
| **Safety** | All parameters are always parameterized — SQL injection impossible |
| **Composability** | Queries can be composed, extended, and reused like values |
| **Testability** | `QueryAssert.SqlMatches()` verifies SQL output deterministically |
| **Multi-dialect** | PostgreSQL, SQL Server, MySQL, MariaDB, SQLite, Oracle |
| **NativeAOT** | Publish as native binaries with zero reflection at runtime |
| **Dapper compatible** | Drop-in for existing Dapper codebases |
| **Performance** | Minimal allocations, `StringBuilderPool`, AOT readers |
| **Pagination** | Offset, keyset (cursor), and window-based pagination built-in |
| **OpenTelemetry** | Distributed tracing and metrics out of the box |
| **Type safety** | Compile-time errors for column names via expression trees |

---

## Disadvantages & Limitations

| Limitation | Detail |
|---|---|
| **No change tracking** | Unlike EF Core, you must write UPDATE queries explicitly |
| **No lazy loading** | All eager loading via explicit JOINs |
| **No migrations** | DDL (schema creation) must be managed separately |
| **Learning curve** | Requires understanding of the AST model |
| **Raw SQL occasionally needed** | Complex database-specific functions may require `Sql.Raw()` |

---

## Comparison With Alternatives

| Feature | EricksonLopez.SqlBuilder | Dapper (raw) | EF Core | SqlKata |
|---|---|---|---|---|
| Type safety | ✅ Full | ❌ None | ✅ Full | ⚠️ Partial |
| Immutability | ✅ Yes | N/A | ❌ Mutable | ❌ Mutable |
| NativeAOT | ✅ Full | ✅ Yes | ⚠️ Partial | ❌ No |
| Multi-dialect | ✅ 6 dialects | ❌ Manual | ✅ Via provider | ✅ Multiple |
| Dapper compat | ✅ Native | ✅ Native | ❌ No | ⚠️ Partial |
| SQL injection | ✅ Impossible | ⚠️ Manual | ✅ Safe | ✅ Safe |
| Pagination | ✅ Built-in | ❌ Manual | ✅ Built-in | ⚠️ Partial |
| OpenTelemetry | ✅ Built-in | ❌ Manual | ✅ Via EF Core | ❌ No |
| Testing utils | ✅ QueryAssert | ❌ None | ⚠️ Limited | ❌ None |
| Bulk operations | ✅ Built-in | ❌ Manual | ⚠️ Via EF Core | ❌ No |

---

## Package Architecture

```
EricksonLopez.SqlBuilder                ← Core query builder (required)
EricksonLopez.SqlBuilder.Abstractions   ← Contracts and nodes
EricksonLopez.SqlBuilder.SourceGenerators ← Code generation for NativeAOT

Dialect compilers (choose one):
├── EricksonLopez.SqlBuilder.PostgreSql
├── EricksonLopez.SqlBuilder.SqlServer
├── EricksonLopez.SqlBuilder.MySql
├── EricksonLopez.SqlBuilder.MariaDb
├── EricksonLopez.SqlBuilder.Sqlite
└── EricksonLopez.SqlBuilder.Oracle

Integration packages (optional):
├── EricksonLopez.SqlBuilder.Dapper       ← Dapper extensions
├── EricksonLopez.SqlBuilder.Dapper.Aot   ← AOT-safe Dapper extensions
├── EricksonLopez.SqlBuilder.Pagination   ← Pagination helpers
├── EricksonLopez.SqlBuilder.Aot          ← Zero-reflection renderers
└── EricksonLopez.SqlBuilder.OpenTelemetry ← Tracing/metrics
```

---

## Proceed to Level 1 →

[Level 1: Quick Start](../Level01_QuickStart/README.md) — installation, minimal configuration, and your first functional query.
