# EricksonLopez.SqlBuilder Cookbook

Practical recipes for common database query and mutation scenarios using `EricksonLopez.SqlBuilder`.

---

## Recipe 1: Upsert / Conflict Handling

**Problem:**
Insert a record if it does not exist, or update it if a conflict occurs on key columns.

**Solution:**
Use `InsertQuery<T>.OnConflict()` with dialect-native actions.

**Code:**
```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.PostgreSql;

var query = Sql.Insert(new Employee { Id = 1, Name = "Alice", Department = "Engineering", Salary = 95000 })
    .OnConflict(e => e.Id)
    .DoUpdate(e => new Employee { Salary = 95000, Department = "Engineering" });

var result = query.Build(new PostgreSqlCompiler());
await connection.ExecuteAsync(result.Sql, result.Parameters);
```

**Best Practices:**
- Use dialect-native `OnConflict` for PostgreSQL, SQLite, and MySQL.
- For SQL Server and Oracle, use `Sql.Raw()` with `MERGE INTO` or atomic `IF NOT EXISTS` constructs.

---

## Recipe 2: Safe Deletion (Guarded DELETE)

**Problem:**
Safely delete records matching specific conditions while preventing accidental unconstrained table truncation.

**Solution:**
Use `Sql.Delete<T>()` with explicit predicates.

**Code:**
```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.SqlServer;

var deleteQuery = Sql.Delete<User>()
    .Where(u => u.Id == userId)
    .And(u => !u.IsActive);

var result = deleteQuery.Build(new SqlServerCompiler());
await connection.ExecuteAsync(result.Sql, result.Parameters);
```

**Explanation:**
`DeleteQuery<T>` protects against accidental whole-table deletion by requiring an explicit `.Where(...)` predicate or `.WhereAll()` declaration.

---

## Recipe 3: Partial Updates via Entity Diffing (Diff Updates)

**Problem:**
Given an original entity and a modified entity (e.g., from a web form or API request), update only the modified columns rather than issuing a full column write.

**Solution:**
Use the `ApplyDiff` extension method.

**Code:**
```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Sqlite;

var original = new Employee { Id = 1, Name = "Alice", Salary = 90000 };
var updated  = new Employee { Id = 1, Name = "Alice", Salary = 95000 };

var diffUpdate = Sql.Update<Employee>()
    .ApplyDiff(original, updated)
    .And(e => e.Id == original.Id);

var result = diffUpdate.Build(new SqliteCompiler());
```

**Explanation:**
`ApplyDiff` inspects entity properties, detects delta changes, and generates the precise `SET` assignments required (`SET salary = @p0`).

---

## Recipe 4: Bulk Ingestion

**Problem:**
Insert large volumes of rows efficiently without per-row round-trip overhead.

**Solution:**
Use `BulkBuilder<T>` with batching or native bulk strategies.

**Code:**
```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.MySql;

var logs = GetLogsToInsert(10000);

var bulkOperation = new BulkBuilder<LogEntry>()
    .WithBatchSize(1000)
    .Insert(logs);

var result = bulkOperation.Build(new MySqlCompiler());
```

**Best Practices:**
- Tune `BatchSize` considering the target engine parameter limit (e.g., SQL Server 2,100 parameters, SQLite 32,766).
- Use `SqlBulkCopyStrategy` (SQL Server) or `NpgsqlCopyStrategy` (PostgreSQL) for maximum throughput.
