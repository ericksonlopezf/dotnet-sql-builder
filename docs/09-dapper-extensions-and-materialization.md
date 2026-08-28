# 09. Dapper Extensions and Materialization

While the library produces strings and a `Dictionary<string, object>`, in production you generally do not want to execute purist ADO.NET every time.

The `EricksonLopez.SqlBuilder.Dapper` package provides extension methods that simplify the process:

```csharp
using EricksonLopez.SqlBuilder.Dapper;

// You no longer need .Build(compiler). You simply pass the connection and the compiler:
var result = await conn.QueryAsync(
    Sql.From<Customer>().Where(c => c.IsActive),
    compiler
);

// Or, if you use dependency injection for the compiler:
var result = await conn.QueryAsync(query);
```

The Dapper extension internally maps the parameter `Dictionary` to Dapper's `DynamicParameters` without additional allocation cost.
