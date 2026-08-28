# Migration Guide: SqlKata → EricksonLopez.SqlBuilder

> **Target**: Developers migrating from [SqlKata](https://sqlkata.com/) v2.x  
> **Estimated effort**: 2–8 hours depending on query complexity  
> **Compatibility**: Full feature parity for 95% of SqlKata's common usage

---

## Why Migrate?

| Dimension | SqlKata | EricksonLopez.SqlBuilder |
|---|---|---|
| AOT / NativeAOT | ❌ Reflection-heavy | ✅ AOT-first, Source Generator |
| Type-safe predicates | ❌ String-based | ✅ Typed expression lambdas |
| Zero-allocation | ❌ Intermediate objects | ✅ `ImmutableArray`, `StringBuilder` reuse |
| Concurrency token | ❌ Not built-in | ✅ `WithConcurrencyToken()` |
| Window functions | ❌ Via raw SQL only | ✅ `Window.Rank<T>().PartitionBy(...).As("r")` |
| RETURNING/OUTPUT | ❌ Not built-in | ✅ `.Returning()` on INSERT/UPDATE/DELETE |
| Resilience (Polly) | ❌ Not provided | ✅ `SqlResilienceDefaults` + extensions |
| Unit of Work | ❌ Not provided | ✅ `IUnitOfWork` with auto-commit/rollback |
| Provider packages | SqlKata.Compilers | `EricksonLopez.SqlBuilder.{Provider}` |

---

## Package Mapping

```
SqlKata                                     → EricksonLopez.SqlBuilder.*
────────────────────────────────────────────────────────────────────────
SqlKata                                     → EricksonLopez.SqlBuilder
SqlKata.Execution (Dapper integration)      → EricksonLopez.SqlBuilder.Dapper
SqlKata.Compilers (SQL Server)              → EricksonLopez.SqlBuilder.SqlServer
SqlKata.Compilers (PostgreSQL)              → EricksonLopez.SqlBuilder.PostgreSql
SqlKata.Compilers (MySQL)                   → EricksonLopez.SqlBuilder.MySql
SqlKata.Compilers (SQLite)                  → EricksonLopez.SqlBuilder.Sqlite
SqlKata.Compilers (Oracle)                  → EricksonLopez.SqlBuilder.Oracle
— (not available)                           → EricksonLopez.SqlBuilder.Dapper.UnitOfWork
— (not available)                           → EricksonLopez.SqlBuilder.Dapper.Resilience
```

---

## Core Concept Differences

### 1. Entry Point

**SqlKata**
```csharp
var query = new Query("users");
```

**EricksonLopez.SqlBuilder**
```csharp
// Typed (recommended)
var query = Sql.From<User>();

// Untyped (for raw/dynamic scenarios)
var query = Sql.FromRaw("users");
```

### 2. Immutable vs Mutable Builders

SqlKata queries are **mutable**. EricksonLopez.SqlBuilder queries are **immutable** — every fluent call returns a new instance:

```csharp
// SqlKata (mutable — side effects)
var q = new Query("users");
q.Where("age", ">", 18);       // mutates q
q.Select("id", "name");        // mutates q

// EricksonLopez.SqlBuilder (immutable — no side effects)
var q = Sql.From<User>();
var q2 = q.Where(u => u.Age > 18);   // q is NOT modified
var q3 = q2.Select(u => new { u.Id, u.Name });

// Common mistake to avoid:
var q = Sql.From<User>();
q.Where(u => u.Age > 18);   // ❌ result is discarded! q is unchanged
```

### 3. Compilation

**SqlKata**
```csharp
var compiler = new SqlServerCompiler();
var result = compiler.Compile(query);
Console.WriteLine(result.Sql);
Console.WriteLine(result.Bindings);
```

**EricksonLopez.SqlBuilder**
```csharp
// Register the compiler once (startup)
DapperExtensions.RegisterCompiler<SqlConnection>(() => new SqlServerCompiler());

// Compile on the fly (via Dapper extensions)
await connection.QueryAsync<User>(query);

// Or compile manually
var compiler = new SqlServerCompiler();
var result = compiler.Compile(query);
Console.WriteLine(result.Sql);
Console.WriteLine(result.Parameters);   // Dictionary<string, object?>
```

---

## Query Clause Mapping

### SELECT

```csharp
// SqlKata
query.Select("id", "name", "email");
query.Select(new[] { "id", "name" });

// EricksonLopez.SqlBuilder — string columns
Sql.From<User>().Select("id", "name", "email");

// EricksonLopez.SqlBuilder — typed expression (recommended)
Sql.From<User>().Select(u => new { u.Id, u.Name, u.Email });

// EricksonLopez.SqlBuilder — raw SQL
Sql.From<User>().RawSelect($"id, UPPER(name) AS upper_name");
```

### WHERE

```csharp
// SqlKata — string-based
query.Where("age", ">", 18);
query.Where("status", "active");
query.WhereIn("id", new[] { 1, 2, 3 });
query.WhereNull("deleted_at");
query.WhereNotNull("email");
query.WhereBetween("age", 18, 65);
query.OrWhere("role", "admin");
query.WhereRaw("age > ? AND status = ?", 18, "active");

// EricksonLopez.SqlBuilder — typed expression (recommended)
Sql.From<User>()
   .Where(u => u.Age > 18)
   .Where(u => u.Status == "active")
   .Where(u => new[] { 1, 2, 3 }.Contains(u.Id))   // IN (1, 2, 3)
   .Where(u => u.DeletedAt == null)                 // IS NULL
   .Where(u => u.Email != null)                     // IS NOT NULL
   .Where(u => u.Age >= 18 && u.Age <= 65)          // BETWEEN alternative
   .RawWhere($"age > {18} AND status = {"active"}");

// OR WHERE (via multiple conditions)
Sql.From<User>()
   .Where(u => u.Age > 18 || u.Role == "admin");
```

### JOIN

```csharp
// SqlKata
query.Join("orders", "users.id", "=", "orders.user_id");
query.LeftJoin("orders", "users.id", "=", "orders.user_id");
query.RightJoin("orders", "users.id", "=", "orders.user_id");
query.CrossJoin("products");

// EricksonLopez.SqlBuilder
Sql.From<User>()
   .Join("orders", "users.id = orders.user_id")
   .LeftJoin("orders", "users.id = orders.user_id")
   .RawJoin($"RIGHT JOIN orders ON users.id = orders.user_id")
   .RawJoin($"CROSS JOIN products");

// Subquery join (NEW — not available in SqlKata without raw)
var sub = Sql.From<Order>().Where(o => o.Total > 100);
Sql.From<User>()
   .JoinSubquery(sub, "big_orders", "users.id = big_orders.user_id");

// LATERAL join (NEW — PostgreSQL/MySQL 8+)
Sql.From<User>()
   .LateralJoin(sub, "recent");
```

### ORDER BY

```csharp
// SqlKata
query.OrderBy("name");
query.OrderByDesc("created_at");
query.OrderByRaw("FIELD(status, 'active', 'pending', 'closed')");

// EricksonLopez.SqlBuilder
Sql.From<User>()
   .OrderBy(u => u.Name)
   .OrderByDescending(u => u.CreatedAt)
   .RawOrderBy($"FIELD(status, 'active', 'pending', 'closed')");
```

### GROUP BY / HAVING

```csharp
// SqlKata
query.GroupBy("department", "role");
query.Having("count(*)", ">", 5);
query.HavingRaw("COUNT(*) > 5");

// EricksonLopez.SqlBuilder
Sql.From<User>()
   .GroupBy(u => u.Department, u => u.Role)
   .RawHaving($"COUNT(*) > {5}");
```

### LIMIT / OFFSET

```csharp
// SqlKata
query.Limit(10).Offset(20);
query.ForPage(3, 10);   // page 3, 10 per page

// EricksonLopez.SqlBuilder
Sql.From<User>()
   .Limit(10)
   .Offset(20);

// Paginate helper
Sql.From<User>()
   .Paginate(page: 3, pageSize: 10);
```

### INSERT

```csharp
// SqlKata
new Query("users").AsInsert(new { Name = "Alice", Email = "alice@example.com" });

// EricksonLopez.SqlBuilder — typed
Sql.Insert<User>(new User { Name = "Alice", Email = "alice@example.com" });

// With RETURNING (PostgreSQL) — NEW
Sql.Insert<User>(new User { Name = "Alice" })
   .Returning(u => u.Id);
```

### UPDATE

```csharp
// SqlKata
new Query("users")
    .Where("id", 42)
    .AsUpdate(new { Name = "Alice", Email = "alice@example.com" });

// EricksonLopez.SqlBuilder — typed
Sql.Update<User>()
   .Set(u => u.Name, "Alice")
   .Set(u => u.Email, "alice@example.com")
   .Where(u => u.Id == 42);

// With concurrency token (NEW — not available in SqlKata)
Sql.Update<User>()
   .Set(u => u.Name, "Alice")
   .Where(u => u.Id == 42)
   .WithConcurrencyToken(u => u.Version, expectedValue: 5);
// Generates: SET name = @name, version = version + 1 WHERE id = @id AND version = @expectedVersion
```

### DELETE

```csharp
// SqlKata
new Query("users").Where("id", 42).AsDelete();

// EricksonLopez.SqlBuilder
Sql.Delete<User>().Where(u => u.Id == 42);

// With RETURNING (PostgreSQL)
Sql.Delete<User>()
   .Where(u => u.Id == 42)
   .Returning(u => u.Id);
```

---

## Execution with Dapper

### SqlKata Execution Layer

```csharp
// SqlKata
var db = new QueryFactory(connection, new SqlServerCompiler());
var users = db.Query("users").Where("active", true).Get<User>();
var user  = db.Query("users").Where("id", 42).First<User>();
```

### EricksonLopez.SqlBuilder.Dapper

```csharp
// Register once at startup
DapperExtensions.RegisterCompiler<SqlConnection>(() => new SqlServerCompiler());

// Anywhere in your code
var query = Sql.From<User>().Where(u => u.IsActive);

var users = await connection.QueryAsync<User>(query);
var user  = await connection.QueryFirstAsync<User>(
                Sql.From<User>().Where(u => u.Id == 42));
```

---

## Window Functions (New in EricksonLopez.SqlBuilder)

SqlKata has no typed window function API — you need raw SQL.

```csharp
// SqlKata (raw)
query.SelectRaw("ROW_NUMBER() OVER (PARTITION BY department ORDER BY salary DESC) AS rnk");

// EricksonLopez.SqlBuilder (typed)
Sql.From<Employee>()
   .Select(u => new { u.Id, u.Name })
   .Select(
       Window.Rank<Employee>()
             .PartitionBy(e => e.Department)
             .OrderByDescending(e => e.Salary)
             .As("rank"),
       Window.RowNumber<Employee>()
             .OrderBy(e => e.CreatedAt)
             .As("row_num"));
```

---

## LATERAL JOIN (New in EricksonLopez.SqlBuilder)

```csharp
// SqlKata (raw only)
query.JoinRaw("JOIN LATERAL (SELECT * FROM orders WHERE user_id = users.id LIMIT 5) o ON TRUE");

// EricksonLopez.SqlBuilder (typed)
var recentOrders = Sql.From<Order>().Where(o => o.UserId == 0).Limit(5);
Sql.From<User>()
   .LateralJoin(recentOrders, "recent_orders", "TRUE");
```

---

## Resilience (New — not in SqlKata)

```csharp
// Create a pipeline
var pipeline = SqlResilienceDefaults.Standard(SqlServerTransientErrorDetector.Default);

// Wrap the entire transactional unit
await pipeline.ExecuteAsync(async ct =>
{
    await using var uow = await connection.BeginUnitOfWorkAsync(ct: ct);
    await connection.ExecuteAsync(
        Sql.Update<User>().Set(u => u.Name, "Alice").Where(u => u.Id == 1),
        uow);
    await uow.CommitAsync(ct);
}, cancellationToken);
```

---

## Common Pitfalls

### 1. Immutability
```csharp
// ❌ Wrong — result is discarded
var q = Sql.From<User>();
q.Where(u => u.IsActive);   // returns new query, original q is unchanged!

// ✅ Correct
var q = Sql.From<User>().Where(u => u.IsActive);
```

### 2. Parameter Naming
- SqlKata uses `?` placeholders for most dialects
- EricksonLopez.SqlBuilder uses `@p0`, `@p1`, ... internally — never reference these in raw SQL; use `$"..."` interpolated strings instead

### 3. NULL comparisons
```csharp
// SqlKata
query.WhereNull("deleted_at");

// EricksonLopez.SqlBuilder — use C# null comparison in typed expression
Sql.From<User>().Where(u => u.DeletedAt == null);
// Generates: WHERE deleted_at IS NULL
```

### 4. IN with subqueries
```csharp
// SqlKata
query.WhereIn("department_id", new Query("departments").Select("id").Where("active", true));

// EricksonLopez.SqlBuilder
var departments = Sql.From<Department>().Select(d => d.Id).Where(d => d.IsActive);
Sql.From<User>().WhereExists(departments);  // or use subquery join
```

---

## Quick Reference Card

| SqlKata | EricksonLopez.SqlBuilder |
|---|---|
| `new Query("users")` | `Sql.From<User>()` |
| `.Select("id", "name")` | `.Select(u => new { u.Id, u.Name })` |
| `.Where("age", ">", 18)` | `.Where(u => u.Age > 18)` |
| `.WhereIn("id", ids)` | `.Where(u => ids.Contains(u.Id))` |
| `.WhereNull("col")` | `.Where(u => u.Col == null)` |
| `.WhereRaw("...")` | `.RawWhere($"...")` |
| `.OrderBy("name")` | `.OrderBy(u => u.Name)` |
| `.OrderByDesc("created_at")` | `.OrderByDescending(u => u.CreatedAt)` |
| `.GroupBy("dept")` | `.GroupBy(u => u.Department)` |
| `.Limit(10).Offset(20)` | `.Limit(10).Offset(20)` |
| `.ForPage(3, 10)` | `.Paginate(page: 3, pageSize: 10)` |
| `.Join("t", "a", "=", "b")` | `.Join("t", "a = b")` |
| `.LeftJoin(...)` | `.LeftJoin(...)` |
| `.SelectRaw("RANK() OVER...")` | `Window.Rank<T>().PartitionBy(...).As("r")` |
| `new Query().AsInsert(obj)` | `Sql.Insert<T>(entity)` |
| `new Query().AsUpdate(obj)` | `Sql.Update<T>().Set(...)` |
| `new Query().AsDelete()` | `Sql.Delete<T>()` |
| `compiler.Compile(q).Sql` | `compiler.Compile(q).Sql` |
| `db.Get<T>()` | `connection.QueryAsync<T>(query)` |
| `db.First<T>()` | `connection.QueryFirstAsync<T>(query)` |
| `db.Insert(obj)` | `connection.ExecuteAsync(Sql.Insert<T>(...))` |

---

## Getting Help

- 📖 [ADR Index](decisions/index.md) — Architecture Decision Records  
- 💬 Open an issue if you encounter a migration edge case  
- 🐛 Found a bug? Run `dotnet test` and include the failing test output  
