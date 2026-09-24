# EricksonLopez.SqlBuilder Cookbook

Practical, production-grade recipes for common database query, mutation, pagination, resilience, and architectural patterns using **EricksonLopez.SqlBuilder**.

Every recipe is derived directly from verified public APIs and demonstrated in the official executable Showcase (`samples/EricksonLopez.SqlBuilder.Samples`).

---

## Table of Contents

- [Recipe 1: Upsert / Conflict Handling](#recipe-1-upsert--conflict-handling)
- [Recipe 2: Safe Deletion & Guarded Truncation](#recipe-2-safe-deletion--guarded-truncation)
- [Recipe 3: Partial Updates via Entity Diffing](#recipe-3-partial-updates-via-entity-diffing)
- [Recipe 4: High-Throughput Bulk Data Ingestion](#recipe-4-high-throughput-bulk-data-ingestion)
- [Recipe 5: Keyset / Cursor Pagination for Deep Datasets](#recipe-5-keyset--cursor-pagination-for-deep-datasets)
- [Recipe 6: Analytical Queries with Window Functions](#recipe-6-analytical-queries-with-window-functions)
- [Recipe 7: Common Table Expressions (CTEs) & Modular Reports](#recipe-7-common-table-expressions-ctes--modular-reports)
- [Recipe 8: Reusable Dynamic Filters via Specification Pattern](#recipe-8-reusable-dynamic-filters-via-specification-pattern)
- [Recipe 9: Custom TypeHandlers for Domain Value Objects & JSON](#recipe-9-custom-typehandlers-for-domain-value-objects--json)
- [Recipe 10: Optimistic Concurrency & Resilient Retries](#recipe-10-optimistic-concurrency--resilient-retries)
- [Recipe 11: Zero-Reflection Native AOT Execution](#recipe-11-zero-reflection-native-aot-execution)
- [Recipe 12: Distributed Tracing & Query Observability](#recipe-12-distributed-tracing--query-observability)
- [Recipe 13: MariaDB Bulk Operations & ON DUPLICATE KEY UPDATE](#recipe-13-mariadb-bulk-operations--on-duplicate-key-update)
- [Recipe 14: Comprehensive Query Testing with QueryAssert & TestDataSeeder](#recipe-14-comprehensive-query-testing-with-queryassert--testdataseeder)

---

## Recipe 1: Upsert / Conflict Handling

### Problem
Insert an entity if it does not exist, or update specific columns if a conflict occurs on a primary key or unique constraint (`ON CONFLICT` / `MERGE`), in a portable and safe manner.

### Solution
Use `Sql.Insert<T>()` chained with `.OnConflict()` and `.DoUpdate()`. For dialects without native `ON CONFLICT` support (e.g., SQL Server and Oracle), use dedicated bulk copy strategies or locked transaction patterns.

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.PostgreSql;

public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public decimal Salary { get; set; }
}

public async Task UpsertEmployeeAsync(DbConnection connection, Employee emp)
{
    var compiler = new PostgreSqlCompiler();

    var query = Sql.Insert(emp)
        .OnConflict(e => e.Id)
        .DoUpdate(e => new Employee
        {
            Department = emp.Department,
            Salary = emp.Salary
        });

    SqlResult result = query.Build(compiler);
    await connection.ExecuteAsync(result.Sql, result.Parameters);
}
```

### Explanation
`InsertQuery<T>` generates the clause `ON CONFLICT ("id") DO UPDATE SET "department" = @p0, "salary" = @p1` for PostgreSQL and SQLite. Parameters are extracted automatically by `IParameterManager`.

### Best Practices
- Specify only mutable columns in `DoUpdate()`; do not overwrite primary keys or creation timestamps (`created_at`).
- For PostgreSQL and SQLite, always rely on native `OnConflict` expressions.

### Common Pitfalls
- Including the primary key in the `DoUpdate` projection, causing database constraint violations.
- Attempting to execute `OnConflict` against SQL Server or Oracle without using dialect-specific abstractions.

---

## Recipe 2: Safe Deletion & Guarded Truncation

### Problem
Prevent accidental deletion of an entire database table caused by inadvertently omitting a `WHERE` clause in destructive DML statements.

### Solution
`EricksonLopez.SqlBuilder` strictly requires a `Where` condition in `DeleteQuery<T>`. If intentional whole-table deletion is desired, `.WhereAll()` must be invoked explicitly. In addition, Roslyn Analyzer rule `ESQL001` validates this constraint at compile time.

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.SqlServer;

public class SessionToken
{
    public Guid TokenId { get; set; }
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public async Task PurgeExpiredSessionsAsync(DbConnection connection, int userId)
{
    var compiler = new SqlServerCompiler();

    // 1. Safe conditional deletion (WHERE required)
    var deleteQuery = Sql.Delete<SessionToken>()
        .Where(s => s.UserId == userId)
        .And(s => s.ExpiresAt < DateTime.UtcNow);

    var result = deleteQuery.Build(compiler);
    await connection.ExecuteAsync(result.Sql, result.Parameters);

    // 2. Intentional table-wide deletion (requires explicit declaration)
    var truncateQuery = Sql.Delete<SessionToken>()
        .WhereAll();

    var truncateResult = truncateQuery.Build(compiler);
    await connection.ExecuteAsync(truncateResult.Sql, truncateResult.Parameters);
}
```

### Explanation
Invoking `.Build()` on a `DeleteQuery<T>` without filters or `.WhereAll()` causes the library to throw a domain `SqlSafetyException`, aborting execution before any command is sent over the network.

### Best Practices
- Keep Roslyn safety analyzers (`ESQL001`, `ESQL003`) enabled as build errors in CI pipelines.
- Always prefer scoped filters by foreign keys or expiration dates.

### Common Pitfalls
- Catching generic `Exception` and ignoring `SqlSafetyException`, which signals a programming defect in query construction.

---

## Recipe 3: Partial Updates via Entity Diffing

### Problem
Update only the columns that actually changed between two entity snapshots (persisted state vs payload received in an API), avoiding redundant writes and concurrency collisions.

### Solution
Use the `.ApplyDiff(original, modified)` extension method on `UpdateQuery<T>`.

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;

public class UserProfile
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public int Version { get; set; }
}

public async Task UpdateProfileDeltasAsync(DbConnection connection, UserProfile original, UserProfile modified)
{
    var compiler = new SqliteCompiler();

    var updateQuery = Sql.Update<UserProfile>()
        .ApplyDiff(original, modified)
        .Where(u => u.Id == original.Id);

    var result = updateQuery.Build(compiler);
    await connection.ExecuteAsync(result.Sql, result.Parameters);
}
```

### Explanation
`ApplyDiff` compares public properties of both instances using cached metadata (`SqlEntityCache<T>`). If only `Bio` changed, it generates:
`UPDATE "user_profiles" SET "bio" = @p0 WHERE ("id" = @p1)`.

### Best Practices
- Combine `ApplyDiff` with an optimistic concurrency token (`.WithConcurrencyToken`) to guarantee the underlying record was not modified concurrently.

### Common Pitfalls
- Passing identical instances without verifying whether deltas exist, resulting in an `UPDATE` without `SET` clauses (throwing `SqlValidationException`).

---

## Recipe 4: High-Throughput Bulk Data Ingestion

### Problem
Insert thousands of rows simultaneously without incurring individual per-row round-trips or exceeding database parameter thresholds.

### Solution
Use `Sql.BulkInsert<T>()` alongside the execution infrastructure of `EricksonLopez.SqlBuilder.Dapper` or dialect-native streaming adapters (`BulkInsertAsync`, `SqlBulkCopyStrategy`, `NpgsqlCopyStrategy`, `OracleBulkCopyStrategy`).

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.PostgreSql;

public class MetricEntry
{
    public Guid Id { get; set; }
    public string SensorId { get; set; } = string.Empty;
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}

public async Task IngestMetricsAsync(Npgsql.NpgsqlConnection connection, IReadOnlyList<MetricEntry> metrics)
{
    // Native high-performance binary copy streaming for PostgreSQL
    await EricksonLopez.SqlBuilder.PostgreSql.NpgsqlCopyStrategy.BulkInsertAsync(
        connection, 
        metrics, 
        new EricksonLopez.SqlBuilder.Builders.Bulk.BulkOptions { BatchSize = 1000 }
    );
}
```

### Explanation
`BulkInsertAsync` calculates the parameter count per row and automatically partitions the collection into safe batches conforming to engine parameter limits (e.g., maximum 65,535 parameters in PostgreSQL or 2,100 in SQL Server).

### Best Practices
- Generate primary keys on the client (`UUIDv7` or sequential GUIDs) to prevent sequence lock contention or identity retrieval round-trips (ADR-046).
- Tune batch sizes between 500 and 2,000 items according to payload width.

### Common Pitfalls
- Exceeding database parameter ceilings when executing massive unbatched `INSERT INTO ... VALUES (...)` statements.

---

## Recipe 5: Keyset / Cursor Pagination for Deep Datasets

### Problem
Inefficient pagination on tables with millions of records. `OFFSET N` degrades performance to $O(N)$ because the database engine must scan and discard all preceding rows.

### Solution
Implement Keyset / Cursor pagination using `.SeekAfter()` or `.SeekBefore()` with deterministic B-Tree index ordering ($O(1)$ complexity).

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Pagination;
using EricksonLopez.SqlBuilder.SqlServer;

public class AuditLog
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public async Task<IReadOnlyList<AuditLog>> FetchNextLogPageAsync(
    DbConnection connection, 
    DateTime lastCreatedAt, 
    long lastId, 
    int pageSize = 50)
{
    var compiler = new SqlServerCompiler();

    var query = Sql.From<AuditLog>()
        .OrderBy(a => a.CreatedAt)
        .ThenBy(a => a.Id)
        .SeekAfter(
            new CursorKey("CreatedAt", lastCreatedAt),
            new CursorKey("Id", lastId)
        )
        .Limit(pageSize);

    SqlResult result = query.Build(compiler);
    return (await connection.QueryAsync<AuditLog>(result.Sql, result.Parameters)).ToList();
}
```

### Explanation
`SeekAfter` constructs an indexed tuple predicate: `(created_at > @p0 OR (created_at = @p0 AND id > @p1))`, allowing the storage engine to seek directly to the B-Tree position without reading intermediate rows.

### Best Practices
- Always include a unique column (such as `Id`) as the tiebreaker in composite cursor ordering.
- Ensure a matching composite index exists covering both the sort columns and cursor directions.

### Common Pitfalls
- Omitting the unique tiebreaker column, causing records with identical timestamps to be skipped or duplicated.

---

## Recipe 6: Analytical Queries with Window Functions

### Problem
Compute rankings, partitions, and moving averages over relational datasets without slow correlated subqueries.

### Solution
Use window functions built into `SelectQuery<T>` (`ROW_NUMBER()`, `RANK()`, `DENSE_RANK()`) with `.SelectWindow(...)` and `.Over(w => w.PartitionBy(...).OrderBy(...))`.

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.PostgreSql;

public class EmployeeSalary
{
    public int Id { get; set; }
    public string Department { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public int RankInDept { get; set; }
}

public async Task<IReadOnlyList<EmployeeSalary>> GetDepartmentRankingsAsync(DbConnection connection)
{
    var compiler = new PostgreSqlCompiler();

    var query = Sql.From<EmployeeSalary>()
        .Select(e => e.Id)
        .Select(e => e.Department)
        .Select(e => e.Salary)
        .Select(
            Window.RowNumber<EmployeeSalary>()
                  .PartitionBy(e => e.Department)
                  .OrderByDescending(e => e.Salary)
                  .As("RankInDept")
        );

    SqlResult result = query.Build(compiler);
    return (await connection.QueryAsync<EmployeeSalary>(result.Sql, result.Parameters)).ToList();
}
```

### Explanation
Generates `SELECT "id", "department", "salary", ROW_NUMBER() OVER(PARTITION BY "department" ORDER BY "salary" DESC) AS "RankInDept" FROM "employee_salaries"`, delegating the computation to the database engine in a single sequential scan.

### Best Practices
- Create indexes on the columns used in `PARTITION BY` and `ORDER BY` window specifications.

### Common Pitfalls
- Attempting to filter on window function results directly in a `WHERE` clause (disallowed by SQL standards; wrap with a CTE or subquery instead).

---

## Recipe 7: Common Table Expressions (CTEs) & Modular Reports

### Problem
Structure complex queries featuring multi-stage aggregations and pre-filtering while maintaining clean, maintainable code.

### Solution
Use `SelectQuery<T>.CTE(name, cteQuery)` to attach Common Table Expressions (`WITH alias AS (...)`) directly on the query builder and combine them into a main query.

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.SqlServer;

public class HighEarnerSummary
{
    public string Department { get; set; } = string.Empty;
    public int HighEarnerCount { get; set; }
}

public async Task<IReadOnlyList<HighEarnerSummary>> GetHighEarnerReportAsync(DbConnection connection, decimal salaryThreshold)
{
    var compiler = new SqlServerCompiler();

    // 1. Define isolated CTE
    var highEarnersCte = Sql.From<Employee>()
        .Where(e => e.Salary >= salaryThreshold);

    // 2. Compose main query consuming the CTE
    var mainQuery = Sql.From<Employee>()
        .CTE("HighEarners", highEarnersCte)
        .From("HighEarners")
        .Select("Department")
        .RawSelect($"COUNT(*) AS HighEarnerCount")
        .GroupBy("Department")
        .Having($"COUNT(*) >= 5");

    SqlResult result = mainQuery.Build(compiler);
    return (await connection.QueryAsync<HighEarnerSummary>(result.Sql, result.Parameters)).ToList();
}
```

### Explanation
The `.CTE()` method emits the `WITH` clause at the beginning of the SQL statement and transparently merges subquery parameters into the resulting `SqlResult`.

### Best Practices
- Use clear, descriptive CTE aliases and project only required columns to minimize tempdb/spill overhead.

### Common Pitfalls
- Parameter collision between main query and CTE (handled automatically by `IParameterManager`).

---

## Recipe 8: Reusable Dynamic Filters via Specification Pattern

### Problem
Allow application layers to compose multiple optional filters (search text, price ranges, status flags) without raw string concatenation or breaking query immutability.

### Solution
Implement the `ISqlFilter<T>` interface and apply filters dynamically using `.ApplyFilter()`.

### Complete Code
```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Filters;
using EricksonLopez.SqlBuilder.Sqlite;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class ProductSearchSpecification : ISqlFilter<Product>
{
    public string? SearchTerm { get; set; }
    public decimal? MinPrice { get; set; }
    public bool OnlyActive { get; set; } = true;

    public SelectQuery<Product> Apply(SelectQuery<Product> query)
    {
        if (OnlyActive)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
            query = query.Where(p => p.Name.Contains(SearchTerm));

        if (MinPrice.HasValue)
            query = query.Where(p => p.Price >= MinPrice.Value);

        return query;
    }
}

public SqlResult BuildCatalogQuery(ProductSearchSpecification spec)
{
    var compiler = new SqliteCompiler();
    
    var baseQuery = Sql.From<Product>()
        .ApplyFilter(spec)
        .OrderBy(p => p.Price);

    return baseQuery.Build(compiler);
}
```

### Explanation
Because `SelectQuery<T>` is an immutable record, each `query = query.Where(...)` statement yields a new query instance safely. The specification encapsulates domain filtering logic entirely outside of controllers or API endpoints.

### Best Practices
- Store specifications in the application or domain layer, completely decoupled from compiler choices.

### Common Pitfalls
- Forgetting to reassign the result of `query.Where(...)`, losing the applied filter due to record immutability.

---

## Recipe 9: Custom TypeHandlers for Domain Value Objects & JSON

### Problem
Map complex domain objects (such as tag collections, JSON metadata, or Strongly-Typed IDs) to and from database primitive columns without polluting the domain model.

### Solution
Implement `ITypeHandler<T>` and register it with both `SqlBuilder` and `Dapper` via `DapperExtensions.RegisterTypeHandler()`.

### Complete Code
```csharp
using System.Text.Json;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;

public class TagCollection : List<string> { }

public class TagCollectionTypeHandler : ITypeHandler<TagCollection>
{
    public object? SetValue(TagCollection? value)
    {
        return value == null ? null : JsonSerializer.Serialize(value);
    }

    public TagCollection? Parse(object? value)
    {
        if (value is string json && !string.IsNullOrWhiteSpace(json))
            return JsonSerializer.Deserialize<TagCollection>(json);
        return new TagCollection();
    }
}

// Application startup registration (Program.cs):
public static void ConfigureHandlers()
{
    var handler = new TagCollectionTypeHandler();
    // Dual registration for SqlBuilder (parameter generation) and Dapper (deserialization)
    DapperExtensions.RegisterTypeHandler<TagCollection>(handler);
}
```

### Explanation
`RegisterTypeHandler` coordinates parameter serialization in `SqlBuilder` (`@p0 = "[...]"`) with Dapper's deserialization pipeline when materializing `IEnumerable<T>`.

### Best Practices
- Ensure type handlers remain pure, stateless, and thread-safe.

### Common Pitfalls
- Registering the handler in Dapper but omitting it from `SqlBuilder`, causing the compiler to fail when converting the object to an ADO.NET parameter.

---

## Recipe 10: Optimistic Concurrency & Resilient Retries

### Problem
Prevent blind overwrites in concurrent environments and automatically recover from transient network drops or transient database deadlocks.

### Solution
Use `.WithConcurrencyToken()` on `UpdateQuery<T>` and execute via `ExecuteWithConcurrencyCheckAsync()` within an exponential backoff retry policy.

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.PostgreSql;
using Polly;

public class InventoryItem
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int Version { get; set; }
}

public async Task<bool> DecrementStockWithRetryAsync(DbConnection connection, ResiliencePipeline pipeline, int itemId, int count)
{
    var compiler = new PostgreSqlCompiler();

    return await pipeline.ExecuteAsync(async _ =>
    {
        // 1. Fetch current snapshot
        var item = await connection.QuerySingleAsync<InventoryItem>(
            Sql.From<InventoryItem>().Where(i => i.Id == itemId),
            compiler
        );

        if (item.Quantity < count)
            return false;

        // 2. Attempt update guarded by version token
        var updateQuery = Sql.Update<InventoryItem>()
            .Set(i => i.Quantity, item.Quantity - count)
            .WithConcurrencyToken(i => i.Version, item.Version)
            .Where(i => i.Id == itemId);

        // 3. Throws DbConcurrencyException if RowsAffected == 0 (concurrency collision)
        await connection.ExecuteWithConcurrencyCheckAsync<InventoryItem>(updateQuery, compiler);
        return true;
    });
}
```

### Explanation
`WithConcurrencyToken` automatically injects `"version" = "version" + 1` into `SET` and `"version" = @expected` into `WHERE`. If another transaction modified the row concurrently, `RowsAffected` is `0`, triggering `DbConcurrencyException` and invoking the retry loop.

### Best Practices
- Position the retry policy **outside** unit of work transactions, never inside (enforced by Roslyn rule `ESQL012`).

### Common Pitfalls
- Forgetting to increment the version field on entities lacking database-level concurrency triggers.

---

## Recipe 11: Zero-Reflection Native AOT Execution

### Problem
Deploy microservices in lightweight containers with Native AOT without trimming warnings (`IL2026`, `IL3050`) or missing JIT exceptions at runtime.

### Solution
Annotate entity classes with `[SqlEntity]`, enabling the Roslyn Source Generator to produce static reader mappers at compile time, and execute queries via `AotQueryExecutor`.

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Aot;
using EricksonLopez.SqlBuilder.PostgreSql;

[SqlEntity(TableName = "customers")]
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public async Task<IReadOnlyList<Customer>> FetchCustomersAotAsync(DbConnection connection)
{
    var compiler = new PostgreSqlCompiler();

    var query = Sql.From<Customer>()
        .Where(c => c.IsActive)
        .OrderBy(c => c.Name);

    SqlResult result = query.Build(compiler);

    // 100% reflection-free execution pipeline
    return await AotQueryExecutor.QueryAsync(
        connection, 
        result.Sql, 
        result.Parameters,
        reader => new Customer
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            IsActive = reader.GetBoolean(2)
        }
    );
}
```

### Explanation
The `SqlEntityGenerator` source generator produces `IStaticEntityMetadata<Customer>` at compile time. `AotQueryExecutor` invokes direct typed methods on `DbDataReader` without invoking `Type.GetType()` or `DynamicMethod`.

### Best Practices
- Enable `<PublishAot>true</PublishAot>` in `.csproj` to ensure the compilation pipeline catches any accidental trimming violations.

### Common Pitfalls
- Using classic reflection-based Dapper APIs in projects compiled with Native AOT.

---

## Recipe 12: Distributed Tracing & Query Observability

### Problem
Pinpoint performance bottlenecks, identify slow queries, and associate SQL statements with distributed OpenTelemetry traces without heavyweight profiling agents.

### Solution
Tag queries using `.WithTag()` and configure the native `SqlBuilderDiagnostics` instrumentation.

### Complete Code
```csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.OpenTelemetry;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

public static void ConfigureTelemetry(IServiceCollection services)
{
    services.AddOpenTelemetry()
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddSource(SqlBuilderDiagnostics.ActivitySource.Name)
                .AddConsoleExporter();
        });
}

public async Task<IReadOnlyList<Product>> GetFeaturedProductsAsync(DbConnection connection)
{
    var compiler = new SqliteCompiler();

    // Explicit tag attached for distributed tracing
    var query = Sql.From<Product>()
        .WithTag("catalog-featured-products")
        .Where(p => p.IsActive)
        .OrderByDescending(p => p.Price)
        .Limit(10);

    SqlResult result = query.Build(compiler);
    return (await connection.QueryAsync<Product>(result.Sql, result.Parameters)).ToList();
}
```

### Explanation
The tag `"catalog-featured-products"` is injected as a SQL comment `-- tag: catalog-featured-products` and recorded as the `db.sql.tag` attribute on the OpenTelemetry `ActivitySource`. The `Meter` records compiled query counts and execution durations.

### Best Practices
- Use structured tags (e.g., `[bounded-context].[feature].[action]`) for streamlined filtering in Jaeger, Zipkin, or Datadog.
- Configure `SqlBuilderDiagnostics.SlowQueryThresholdMs` to emit diagnostic warnings for expensive queries.

### Common Pitfalls
- Passing dynamic variable values (such as user IDs) in tag names, which leads to high metric cardinality.

---

## Recipe 13: MariaDB Bulk Operations & ON DUPLICATE KEY UPDATE

### Problem
Perform high-throughput bulk insertions and upserts specifically on MariaDB / MySQL servers using the dialect's native \`ON DUPLICATE KEY UPDATE\` semantics.

### Solution
Use \`MariaDbCompiler\` and \`MariaDbExtensions.BuildOnDuplicateKeyUpdate\` to generate valid backtick-quoted MariaDB queries with custom conflict update clauses.

### Complete Code
\`\`\`csharp
using System.Data.Common;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.MariaDb;
using Dapper;

public class ProductStock
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public async Task UpsertStockAsync(DbConnection connection, ProductStock stock)
{
    var compiler = new MariaDbCompiler();

    var query = Sql.Insert(stock);
    var compiled = query.Build(compiler);

    // Append MariaDB ON DUPLICATE KEY UPDATE clause
    var onDuplicateClause = MariaDbExtensions.BuildOnDuplicateKeyUpdate("quantity", "unit_price");
    var fullSql = $"{compiled.Sql} {onDuplicateClause}";

    await connection.ExecuteAsync(fullSql, compiled.Parameters);
}
\`\`\`

### Explanation
\`MariaDbCompiler\` renders MariaDB-compliant identifiers (using backticks \`\` `sku` \`\`) and parameter placeholders (\`@p0\`). \`BuildOnDuplicateKeyUpdate\` produces the canonical clause \`ON DUPLICATE KEY UPDATE \`quantity\` = VALUES(\`quantity\`), \`unit_price\` = VALUES(\`unit_price\`)\`.

### Best Practices
- Explicitly supply the column names to update to avoid touching audit fields such as \`created_at\`.
- For bulk operations exceeding 1,000 items, partition into batches to keep parameter count within MariaDB's server limits.

### Common Pitfalls
- Relying on PostgreSQL's \`ON CONFLICT\` syntax on MariaDB, which results in a syntax error.

---

## Recipe 14: Comprehensive Query Testing with QueryAssert & TestDataSeeder

### Problem
Verify that database query builders produce the exact expected SQL and parameter bindings across multiple database dialects without needing live database infrastructure in unit test suites.

### Solution
Leverage \`EricksonLopez.SqlBuilder.Testing\` utilities: \`TestDataSeeder\` for deterministic test records, \`QueryAssert.SqlMatches\` for whitespace-insensitive SQL comparison, and \`SnapshotAssert\` for regression detection.

### Complete Code
\`\`\`csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.Domain;
using EricksonLopez.SqlBuilder.Testing.Seeders;
using Xunit;

public class QueryCompilationTests
{
    [Fact]
    public void SelectActiveCustomers_ProducesNormalizedSql()
    {
        // 1. Seed deterministic test models
        var sampleCustomers = TestDataSeeder.Customers(count: 5);
        Assert.Equal(5, sampleCustomers.Count);

        // 2. Build immutable query
        var query = Sql.From<Customer>()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Limit(10);

        // 3. Compile against SQLite and SQL Server
        var sqliteResult = query.Build(new SqliteCompiler());
        var sqlServerResult = query.Build(new SqlServerCompiler());

        // 4. Assert with QueryAssert (whitespace & dialect normalization)
        QueryAssert.SqlMatches(
            "SELECT * FROM \"customers\" WHERE (is_active = @p0) ORDER BY \"name\" LIMIT 10",
            sqliteResult.Sql);

        QueryAssert.SqlMatches(
            "SELECT TOP 10 * FROM [customers] WHERE ([is_active] = @p0) ORDER BY [name]",
            sqlServerResult.Sql);
    }
}
\`\`\`

### Explanation
\`QueryAssert\` normalizes indentation, line breaks, and whitespace differences, comparing SQL AST output deterministically. \`TestDataSeeder\` uses seed-initialized generators to produce consistent data across test runs.

### Best Practices
- Use \`QueryAssert.SqlMatches\` to verify both standard and dialect-specific query structures.
- Store golden SQL baselines in \`.sql\` files and verify them with \`SnapshotAssert.MatchesSnapshot\`.

### Common Pitfalls
- Using fragile \`Assert.Equal(string, string)\` on SQL strings, which breaks whenever formatting or whitespace is adjusted.

