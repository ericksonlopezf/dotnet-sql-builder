# Level 01: Basic Query Building & Dialect Compilation

## 1. Declarative Query Construction
`EricksonLopez.SqlBuilder` provides a fluent, immutable builder interface to construct SQL statements with compile-time safety and automatic parameter bindings.

```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.PostgreSql;

var query = SqlQuery.Select("id", "email", "created_at")
    .From("users")
    .Where("is_active", Op.Equals, true)
    .Where("created_at", Op.GreaterThanOrEqual, DateTime.UtcNow.AddDays(-30))
    .OrderByDesc("created_at")
    .Limit(25)
    .Build(PostgreSqlDialect.Instance);

Console.WriteLine(query.Sql);
// SELECT id, email, created_at FROM users WHERE is_active = @p0 AND created_at >= @p1 ORDER BY created_at DESC LIMIT 25
```

---

## 2. Cross-Dialect Portability
The same AST representation compiles natively into target SQL dialects without changing query construction logic:

```csharp
// Compile to SQL Server
var sqlServerQuery = query.Compile(SqlServerDialect.Instance);
// Output uses OFFSET ... FETCH NEXT:
// SELECT id, email, created_at FROM users WHERE is_active = @p0 AND created_at >= @p1 ORDER BY created_at DESC OFFSET 0 ROWS FETCH NEXT 25 ROWS ONLY

// Compile to Oracle
var oracleQuery = query.Compile(OracleDialect.Instance);
// Output uses Oracle FETCH FIRST:
// SELECT id, email, created_at FROM users WHERE is_active = :p0 AND created_at >= :p1 ORDER BY created_at DESC FETCH FIRST 25 ROWS ONLY
```

---

## 3. Parameter Safety & Sanitization
Parameters are automatically encapsulated in strongly typed parameter maps with exact type mappings, mitigating SQL injection risks and enabling deterministic execution plan caching across database query engines.
