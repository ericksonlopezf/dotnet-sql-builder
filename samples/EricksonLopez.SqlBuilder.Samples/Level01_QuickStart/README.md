# Level 1: Quick Start

## Overview

Level 1 demonstrates the fundamental workflow of **EricksonLopez.SqlBuilder**: setting up entity mappings, registering a database compiler with Dapper, and executing foundational CRUD operations (INSERT, SELECT, DELETE) against an in-memory SQLite database.

---

## Architectural Concepts

1. **Entity Metadata Mapping**:
   Classes represent relational tables using `[SqlEntity("tableName")]`. Primary keys or database-computed columns are annotated with `[DatabaseGenerated]` to inform the compiler to omit them from default `INSERT` column projections.
2. **Global Compiler Registration**:
   Through `DapperExtensions.RegisterCompiler<TConnection>()`, SqlBuilder seamlessly bridges into Dapper, allowing immutable query objects to be passed directly into `connection.QueryAsync<T>(query)` and `connection.ExecuteAsync(query)`.
3. **Immutable Query Composition**:
   Every query builder invocation (`Sql.From<T>()`, `Sql.Insert(entity)`, `Sql.Delete<T>()`) produces an immutable record representing an Abstract Syntax Tree (AST).

---

## Key APIs Covered

| API | Type | Responsibility |
|---|---|---|
| `[SqlEntity("tableName")]` | Attribute | Maps a C# record or class to a target database table |
| `[DatabaseGenerated]` | Attribute | Marks auto-increment / identity columns |
| `DapperExtensions.RegisterCompiler<TConn>()` | Extension Method | Associates a database connection type with its `ISqlCompiler` |
| `Sql.Insert<T>(entity)` | Entry Point | Initiates an `INSERT` query for an entity |
| `InsertQuery<T>.Returning(lambda)` | Builder Method | Appends dialect-specific `RETURNING` or `OUTPUT` clause |
| `Sql.From<T>()` | Entry Point | Starts an immutable `SELECT` query builder |
| `SelectQuery<T>.Where(predicate)` | Builder Method | Appends a strongly-typed boolean lambda filter |
| `Sql.Delete<T>()` | Entry Point | Starts a guarded `DELETE` query builder |
| `DeleteQuery<T>.Where(predicate)` | Builder Method | Scopes the deletion to matching records |

---

## Step-by-Step Implementation Guide

### 1. Entity Definition
```csharp
[SqlEntity("users")]
public partial class User
{
    [DatabaseGenerated]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

### 2. Register Compiler & Open Connection
```csharp
// Register SQLite dialect compiler globally for all SqliteConnection instances
DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
```

### 3. Insert with RETURNING Clause
```csharp
var newUser = new User { Name = "John Doe", Email = "john@example.com" };

// Sql.Insert infers column names from User metadata, excluding [DatabaseGenerated] Id
var insertQuery = Sql.Insert(newUser).Returning(u => u.Id);

// Execute via transparent Dapper extension method
var newId = (await connection.QueryAsync<int>(insertQuery)).Single();
Console.WriteLine($"[+] Inserted user with Id: {newId}");
```

### 4. Strongly-Typed SELECT Query
```csharp
var selectQuery = Sql.From<User>()
                     .Where(u => u.Id == newId);

var users = await connection.QueryAsync<User>(selectQuery);
var user = users.SingleOrDefault();
Console.WriteLine($"[+] Retrieved: {user?.Name} ({user?.Email})");
```

### 5. Safe Scoped DELETE Query
```csharp
var deleteQuery = Sql.Delete<User>()
                     .Where(u => u.Id == newId);

await connection.ExecuteAsync(deleteQuery);
Console.WriteLine("[+] User deleted from DB.");
```

---

## Execution Output

```text
=== LEVEL 1: QUICK START ===
[+] Inserted user with Id: 1
[+] Retrieved from DB: John Doe (john@example.com)
[+] User deleted from DB.
```

---

## Best Practices & Gotchas

> [!TIP]
> Always register your database compiler during application startup (e.g., in `Program.cs` or DI bootstrapping). Once registered, any standard ADO.NET connection can execute SqlBuilder queries directly.

> [!CAUTION]
> Never omit the `Where()` clause on `Sql.Delete<T>()` unless explicitly calling `WhereAll()`. SqlBuilder enforces this at compile-time via Roslyn analyzer `ESQL001`.
