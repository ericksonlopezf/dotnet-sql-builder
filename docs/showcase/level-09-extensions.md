# Level 09: Extensions

**Context:** Official extension packages and their APIs: `SqlResult` utilities, `GetFingerprint`, `ProjectTo<T>`, `ToResultAsync`, `Sql.Raw`, OpenTelemetry, Dapper MultiMap.

---

## Extension Packages

| Package | Key API | Purpose |
|---------|---------|---------|
| `EricksonLopez.SqlBuilder.Dapper` | `QueryAsync<T>`, `ExecuteAsync`, `BulkInsertAsync`, `ToStreamAsync`, `ToResultAsync` | Execute queries via Dapper |
| `EricksonLopez.SqlBuilder.OpenTelemetry` | `SqlBuilderDiagnostics.ActivitySource` | OTel tracing |
| `EricksonLopez.SqlBuilder.Aot` | `AotQueryExecutor.QueryAsync<T>` | Zero-reflection Native AOT |
| `EricksonLopez.SqlBuilder.Dapper.MultiMap` | `QueryMultiMapAsync<T1,T2,...>` | JOIN result projection |

---

## SqlResult Utilities

```csharp
var result = Sql.From<User>().Where(u => u.Id == 1).Build(compiler);

// SQL fingerprint (stable identifier for monitoring)
string fingerprint = result.GetFingerprint();

// Projecting to different DTO
var projected = result.ProjectTo<UserDto>();
```

## ToResultAsync — Full Result with Metadata

```csharp
// Returns SqlExecutionResult { Rows, Duration, RowsAffected }
var execResult = await connection.ToResultAsync<User>(query, compiler);
Console.WriteLine($"Rows: {execResult.Rows.Count}, Time: {execResult.Duration.TotalMs}ms");
```

## Sql.Raw — Escape Hatch

```csharp
// For dialect-specific raw SQL fragments not modeled in the AST
var rawQuery = Sql.Raw($"SELECT * FROM users WHERE {columnName} = {userId}");
// Parameters are still parameterized via FormattableString holes
```

## OpenTelemetry Integration

```csharp
// ActivitySource is exposed for custom instrumentation
using var activity = SqlBuilderDiagnostics.ActivitySource
    .StartActivity("custom-query");

var result = await connection.QueryAsync<User>(query, compiler);
```

---

## Reference

**Source:** [`Level09_Extensions/ExtensionsSample.cs`](../../samples/EricksonLopez.SqlBuilder.Samples/Level09_Extensions/ExtensionsSample.cs)
