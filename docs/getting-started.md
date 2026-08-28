# Getting Started with EricksonLopez.SqlBuilder

EricksonLopez.SqlBuilder is an **immutable, AOT-first, strongly-typed SQL builder** for .NET. It compiles C# expressions into dialect-aware SQL with zero runtime reflection (when used with Source Generators).

## Installation

```bash
# Core package
dotnet add package EricksonLopez.SqlBuilder

# Dialect (choose one or more)
dotnet add package EricksonLopez.SqlBuilder.SqlServer
dotnet add package EricksonLopez.SqlBuilder.PostgreSql
dotnet add package EricksonLopez.SqlBuilder.MySql
dotnet add package EricksonLopez.SqlBuilder.Sqlite
dotnet add package EricksonLopez.SqlBuilder.Oracle

# Dapper integration (optional)
dotnet add package EricksonLopez.SqlBuilder.Dapper
```

## Define an Entity

### Option 1 — Source Generator (Recommended, AOT-safe)

Add the `[SqlEntity]` attribute and mark the class `partial`:

```csharp
using EricksonLopez.SqlBuilder.Annotations;

[SqlEntity("orders")]
public partial class Order
{
    public int Id { get; set; }
    public string CustomerId { get; set; } = "";
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
```

The Source Generator emits `IStaticEntityMetadata<Order>` — no runtime reflection.

### Option 2 — Manual (for existing types)

```csharp
public class Order : IStaticEntityMetadata<Order>
{
    public int Id { get; set; }
    public string Status { get; set; } = "";

    public static string TableName => "orders";
    // ... implement interface members
}
```

## Basic Queries

### SELECT

```csharp
var compiler = new SqlServerCompiler();

// SELECT * FROM [orders]
var all = Sql.From<Order>().Build(compiler);

// SELECT * FROM [orders] WHERE [status] = @p0
var active = Sql.From<Order>()
    .Where(o => o.Status == "active")
    .Build(compiler);

// SELECT * FROM [orders] ORDER BY [created_at] DESC LIMIT 20
var recent = Sql.From<Order>()
    .OrderByDesc(o => o.CreatedAt)
    .Limit(20)
    .Build(compiler);
```

### INSERT

```csharp
var order = new Order { CustomerId = "C42", Total = 99.99m, Status = "pending" };

// INSERT INTO [orders] ([customer_id], [total], [status]) VALUES (@p0, @p1, @p2)
var insert = Sql.Insert(order).Build(compiler);
```

### UPDATE

```csharp
// UPDATE [orders] SET [status] = @p0 WHERE [id] = @p1
var update = Sql.Update<Order>()
    .Set(o => o.Status, "shipped")
    .Where(o => o.Id == 42)
    .Build(compiler);
```

### DELETE

```csharp
// DELETE FROM [orders] WHERE [id] = @p0
var delete = Sql.Delete<Order>()
    .Where(o => o.Id == 42)
    .Build(compiler);
```

## Using with Dapper

```csharp
using EricksonLopez.SqlBuilder.Dapper;

// Register once at startup
DapperExtensions.RegisterCompiler<SqlConnection>(() => new SqlServerCompiler());

// Execute queries
var query = Sql.From<Order>().Where(o => o.Status == "pending");
var orders = await connection.QueryAsync<Order>(query, new SqlServerCompiler());

// Or use the auto-detected compiler from registration
var orders2 = await connection.QueryAsync<Order>(query);
```

## Multi-Dialect Support

The compiler is injected, making it trivial to target different databases:

```csharp
ISqlCompiler compiler = databaseType switch
{
    "sqlserver" => new SqlServerCompiler(),
    "postgresql" => new PostgreSqlCompiler(),
    "mysql"     => new MySqlCompiler(),
    "sqlite"    => new SqliteCompiler(),
    "oracle"    => new OracleCompiler(),
    _ => throw new NotSupportedException(databaseType)
};

var sql = Sql.From<Order>().Where(o => o.Id == 1).Build(compiler);
```

## Optimistic Concurrency

```csharp
// UPDATE [orders] SET [status] = @p0, [version] = [version] + 1
// WHERE [id] = @p1 AND [version] = @p2
var update = Sql.Update<Order>()
    .Set(o => o.Status, "shipped")
    .Where(o => o.Id == orderId)
    .WithConcurrencyToken(o => o.Version, currentVersion);

// Throws DbConcurrencyException if 0 rows affected
await connection.ExecuteWithConcurrencyCheckAsync<Order>(update, compiler);
```

## INSERT INTO ... SELECT

```csharp
var sourceQuery = Sql.From<Order>().Where(o => o.Status == "completed");
var archiveInsert = Sql.InsertFrom<ArchivedOrder>(sourceQuery, "id", "customer_id", "total");

var result = archiveInsert.Build(compiler);
// INSERT INTO [archived_orders] ([id], [customer_id], [total])
// SELECT * FROM [orders] WHERE [status] = @p0
```

## Design Principles

| Principle | Detail |
|-----------|--------|
| **Immutable** | Every builder method returns a new instance |
| **AOT-first** | Source Generators emit static metadata, no reflection at runtime |
| **Strongly-typed** | Expressions like `o => o.Status == "active"` are type-safe |
| **Modular** | Compiler, dialect, and Dapper integration are separate packages |
| **No magic** | No change tracking, no navigation properties, no auto-caching |

## Next Steps

- [Analyzers](./analyzers.md) — Roslyn diagnostics for query correctness
- [AOT / NativeAOT](./aot.md) — Publishing with NativeAOT
- [Performance](./performance.md) — Benchmark results vs raw ADO.NET
