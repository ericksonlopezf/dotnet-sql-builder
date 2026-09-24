# Level 2: Full Configuration

## Overview

Level 2 explores enterprise configuration, diagnostics, custom data serialization, and advanced extension methods in **EricksonLopez.SqlBuilder**. You will learn how to configure query logging, handle complex JSON value objects via `ITypeHandler`, and use dynamic ordering and pagination.

---

## Architectural Concepts

1. **Decoupled Diagnostics**:
   `SqlBuilderDiagnostics` centralizes query tracing, logging of parameters, slow query threshold monitoring, and OpenTelemetry instrumentation without binding to concrete logging frameworks.
2. **Dual-Layer Type Handlers**:
   Complex value objects (e.g., JSON documents stored in `TEXT` or `JSONB` columns) require coordinated serialization across both SqlBuilder's parameter extraction and Dapper's reader materialization. SqlBuilder provides `ITypeHandler` to handle both.
3. **Dynamic Identifiers & Safe Sorting**:
   User-driven sorting criteria (e.g., clicking table headers in UI) can introduce SQL injection risks. SqlBuilder provides `OrderByDynamic(columnName, descending)` which sanitizes column names against the entity's verified metadata map.

---

## Key APIs Covered

| API | Type | Responsibility |
|---|---|---|
| `SqlBuilderDiagnostics.LoggerFactory` | Property | Injects an `ILoggerFactory` for query and error logging |
| `SqlBuilderDiagnostics.LogParameters` | Property | Controls whether parameter values are logged or masked for security |
| `SqlBuilderDiagnostics.SlowQueryThresholdMs` | Property | Execution time threshold (ms) above which warnings are logged |
| `ITypeHandler` / `SqlMapper.TypeHandler<T>` | Interface | Converts complex C# types to database parameters and parses DB values |
| `Sql.RegisterTypeHandler<T>(handler)` | Method | Registers a type handler for AST parameter serialization |
| `SelectQuery<T>.OrderByDynamic(col, desc)` | Method | Safely appends an `ORDER BY` clause using a string column name |
| `SelectQuery<T>.Limit(n).Offset(skip)` | Methods | Configures standard offset-based pagination |

---

## Step-by-Step Implementation Guide

### 1. Complex JSON Type Mapping
```csharp
public class Metadata { public string CreatedBy { get; set; } = string.Empty; }

public class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>, ITypeHandler
{
    public override T Parse(object value)
    {
        if (value is T tValue) return tValue;
        var json = value?.ToString();
        return string.IsNullOrEmpty(json) ? default! : JsonSerializer.Deserialize<T>(json)!;
    }

    public override void SetValue(IDbDataParameter parameter, T value)
    {
        parameter.Value = value == null ? DBNull.Value : JsonSerializer.Serialize(value);
        parameter.DbType = DbType.String;
    }

    void ITypeHandler.SetValue(IDbDataParameter parameter, object? value) => SetValue(parameter, (T)value!);
    object? ITypeHandler.Parse(Type destinationType, object? value) => Parse(value!);
}
```

### 2. Diagnostics & TypeHandler Registration
```csharp
// Configure query observability
SqlBuilderDiagnostics.LoggerFactory = LoggerFactory.Create(b => b.AddConsole());
SqlBuilderDiagnostics.LogParameters = true;
SqlBuilderDiagnostics.SlowQueryThresholdMs = 50;

// Register type handlers for both Dapper and SqlBuilder
var jsonHandler = new JsonTypeHandler<Metadata>();
SqlMapper.AddTypeHandler(jsonHandler);
Sql.RegisterTypeHandler<Metadata>(jsonHandler);
```

### 3. Executing Queries with Dynamic Sorting
```csharp
var query = Sql.From<Product>()
    .OrderByDynamic("Name", descending: true)
    .Limit(10)
    .Offset(0);

var products = await connection.QueryAsync<Product>(query);
```

---

## Execution Output

```text
=== LEVEL 2: FULL CONFIGURATION ===
[+] Product inserted with AOT Builder (excluding generated, ignoring nulls).
[+] Product retrieved: Laptop - Price: 2000 - Created by: System
```
