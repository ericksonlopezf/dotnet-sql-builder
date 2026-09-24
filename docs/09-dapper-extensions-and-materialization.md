# 09. Dapper Extensions and Materialization

While the library produces strings and a `Dictionary<string, object>`, in production you generally do not want to execute purist ADO.NET every time.

The `EricksonLopez.SqlBuilder.Dapper` package provides extension methods that simplify the process:

```csharp
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.SqlServer;
using Microsoft.Data.SqlClient;

// Register the compiler once at application startup (accepts a factory delegate):
DapperExtensions.RegisterCompiler<SqlConnection>(() => new SqlServerCompiler());

// Execute directly on the IDbConnection — compiler is resolved automatically from the registry:
var query = Sql.From<Customer>().Where(c => c.IsActive);
var customers = await conn.QueryAsync<Customer>(query);
```

The Dapper extension internally maps the query parameters to Dapper's `DynamicParameters` without additional allocation overhead.
