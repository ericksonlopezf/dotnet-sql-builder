# FAQ & Troubleshooting

Quick reference guide to resolve common questions, debug issues, and apply best practices when using **EricksonLopez.SqlBuilder**.

---

## 1. Frequently Asked Questions (FAQ)

### How does EricksonLopez.SqlBuilder differ from SqlKata?
- **Immutable AST:** `SelectQuery<T>`, `InsertQuery<T>`, etc. are immutable C# `record` types. Sharing query instances across threads is 100% thread-safe without requiring `.Clone()`.
- **Static Typing & Compile-Time Safety:** Strongly-typed expressions (`Where(x => x.Active)`) instead of fragile magic strings.
- **First-Class NativeAOT:** Zero-reflection support through incremental Roslyn Source Generators and `IDataReaderMapper<T>`.
- **Built-in Roslyn Analyzers:** Compile-time guards (e.g. `ESQL001` for `DELETE` without `WHERE` and `ESQL003` for `UPDATE` without `WHERE`).

### Why does my query not mutate when calling builder methods?
All queries are **immutable**. Every method call returns a **new instance**.
```csharp
// ❌ Incorrect (base query remains unchanged):
var query = Sql.From<User>();
query.Where(u => u.IsActive); // Returns a new instance that is discarded

// ✅ Correct:
var query = Sql.From<User>()
    .Where(u => u.IsActive);
```

### How do I choose the correct compiler for my database engine?
Instantiate or register the compiler matching your target database provider:
- SQL Server: `new SqlServerCompiler()` from `EricksonLopez.SqlBuilder.SqlServer`
- PostgreSQL: `new PostgreSqlCompiler()` from `EricksonLopez.SqlBuilder.PostgreSql`
- MySQL / MariaDB: `new MySqlCompiler()` / `new MariaDbCompiler()`
- SQLite: `new SqliteCompiler()`
- Oracle: `new OracleCompiler()`

```csharp
var compiler = new PostgreSqlCompiler();
SqlResult result = query.Build(compiler);

string sql = result.Sql;
IReadOnlyDictionary<string, object?> parameters = result.Parameters;
```

---

## 2. Troubleshooting & Diagnostics

### ESQL001: "DELETE query without a WHERE clause"
- **Cause:** Attempted to compile or build a `Sql.Delete<T>()` query without specifying a `Where` clause.
- **Resolution:**
  1. Add a `.Where(x => ...)` or `.Where(FormattableString)` clause.
  2. If a full table truncate/delete is intentional, call `.WhereAll()` to explicitly declare that intent.

### ESQL003: "UPDATE query without a WHERE clause"
- **Cause:** Attempted to execute an unconstrained update query.
- **Resolution:** Add `.Where(x => ...)` or explicitly call `.WhereAll()`.

### ESQL012: "Retry policy detected inside Unit of Work"
- **Cause:** A retry policy was placed inside an active `IUnitOfWork` transaction scope. Automatic retries within an active transaction can cause state inconsistencies or broken connection states.
- **Resolution:** Apply resilience policies outside the `BeginUnitOfWorkAsync` boundary, retrying the entire unit of work from the start upon encountering transient errors.

### Why do I get a runtime error in NativeAOT when using `Where(x => ...)`?
- **Cause:** Arbitrary LINQ expression tree compilation requires runtime JIT if entities are not registered with the `[SqlEntity]` Source Generator.
- **Resolution:**
  1. Decorate entity classes with `[SqlEntity]` and emit static metadata via `EricksonLopez.SqlBuilder.SourceGenerators`.
  2. Alternatively, use `Sql.Raw()` or `Where($"Column = {value}")`, which are 100% JIT-free and NativeAOT safe.

---

## 3. Escape Hatch: Safe Raw SQL

If you require vendor-specific SQL syntax not modeled by typed builders:
```csharp
// Immune to SQL injection via FormattableString parameter extraction:
var query = Sql.From<Product>()
    .Where($"price * tax_rate > {maxAllowed}")
    .OrderBy($"created_at DESC");
```
Interpolated values are automatically extracted into parameterized SQL parameters (`@p0`, `@p1`, etc.).
