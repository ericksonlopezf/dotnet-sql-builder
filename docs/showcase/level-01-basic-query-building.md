# Level 01: Basic Query Building & Dialect Compilation

## 1. Declarative Strongly-Typed Query Construction
`EricksonLopez.SqlBuilder` provides a fluent, immutable query builder to construct SQL AST statements with compile-time safety and automatic parameterized bindings.

```csharp
using System;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.PostgreSql;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Construct strongly-typed immutable query AST
var query = Sql.From<User>()
    .Select("id", "email", "created_at")  // params string[] overload
    .Where(u => u.IsActive && u.CreatedAt >= DateTime.UtcNow.AddDays(-30))
    .OrderByDescending(u => u.CreatedAt)
    .Limit(25);  // Limit() is the correct API; Take() does not exist

var compiler = new PostgreSqlCompiler();
SqlResult result = query.Build(compiler);

Console.WriteLine(result.Sql);
// SELECT "Id", "Email", "CreatedAt" FROM "Users" WHERE "IsActive" = @p0 AND "CreatedAt" >= @p1 ORDER BY "CreatedAt" DESC LIMIT 25
```

---

## 2. Cross-Dialect Portability
The exact same immutable AST representation compiles natively into target SQL dialects without altering query construction logic:

```csharp
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Oracle;

// Compile to SQL Server (uses bracket escaping and TOP / OFFSET-FETCH)
var sqlServerCompiler = new SqlServerCompiler();
SqlResult sqlServerResult = query.Build(sqlServerCompiler);
Console.WriteLine(sqlServerResult.Sql);

// Compile to Oracle (uses Oracle identifier quoting and FETCH FIRST 25 ROWS ONLY)
var oracleCompiler = new OracleCompiler();
SqlResult oracleResult = query.Build(oracleCompiler);
Console.WriteLine(oracleResult.Sql);
```

---

## 3. Parameter Safety & Sanitization
Parameters are automatically extracted and encapsulated into an immutable parameter dictionary (`IReadOnlyDictionary<string, object?>`), eliminating SQL injection risks and enabling deterministic execution plan caching across database query engines.
