# API Reference — EricksonLopez.SqlBuilder

Comprehensive technical reference documentation modeled after **Microsoft Learn** standards, covering core entry points, query builders, compilers, executors, testing assertions, and extension points.

---

## Table of Contents

- [Entry Point: `Sql` Static Class](#entry-point-sql-static-class)
  - [`Sql.From<T>()`](#sqlfromt)
  - [`Sql.Insert<T>(T entity)`](#sqlinserttt-entity)
  - [`Sql.Update<T>()`](#sqlupdatet)
  - [`Sql.Delete<T>()`](#sqldeletet)
  - [`Sql.BulkInsert<T>(IEnumerable<T> entities)`](#sqlbulkinserttenumerablet-entities)
  - [`Sql.Raw(FormattableString query)`](#sqlrawformattablestring-query)
  - [`Sql.RegisterTypeHandler<T>(ITypeHandler handler)`](#sqlregistertypehandlertitypehandler-handler)
- [Query Builders & AST Nodes](#query-builders--ast-nodes)
  - [`SelectQuery<T>.Where(...)`](#selectquerytwhere)
  - [`SelectQuery<T>.Join<TTarget>(...)`](#selectquerytjointtarget)
  - [`SelectQuery<T>.SeekAfter(...) / SeekBefore(...)`](#selectquerytseekafter--seekbefore)
  - [`SelectQuery<T>.Build(ISqlCompiler compiler)`](#selectquerytbuildisqlcompiler-compiler)
  - [`InsertQuery<T>.OnConflict(...)`](#insertquerytonconflict)
  - [`InsertQuery<T>.Returning(...)`](#insertquerytreturning)
  - [`UpdateQuery<T>.WithConcurrencyToken(...)`](#updatequerytwithconcurrencytoken)
  - [`UpdateQuery<T>.ApplyDiff(T original, T modified)`](#updatequerytapplydifft-original-t-modified)
  - [`DeleteQuery<T>.WhereAll()`](#deletequerytwhereall)
- [Execution & Extensions](#execution--extensions)
  - [`ConnectionSqlExtensions.QueryPagedAsync<T>(...)`](#connectionsqlextensionsquerypagedasynct)
  - [`AotQueryExecutor.QueryAsync<T>(...)`](#aotqueryexecutorqueryasynct)
  - [`DapperExtensions.ExecuteWithConcurrencyCheckAsync(...)`](#dapperextensionsexecutewithconcurrencycheckasync)
  - [`DapperExtensions.QueryStreamAsync<T>(...)`](#dapperextensionsquerystreamasynct)
- [Dialect Compilers](#dialect-compilers)
  - [`ISqlCompiler`](#isqlcompiler)
  - [`PostgreSqlCompiler` / `SqlServerCompiler` / `SqliteCompiler` / `MySqlCompiler` / `MariaDbCompiler` / `OracleCompiler`](#concrete-compilers)
- [Observability & Diagnostics](#observability--diagnostics)
  - [`SqlBuilderDiagnostics` & `SqlBuilderInstrumentation`](#sqlbuilderdiagnostics--sqlbuilderinstrumentation)
- [Testing & Quality Verification](#testing--quality-verification)
  - [`QueryAssert.SqlMatches(...) / ParametersMatch(...)`](#queryassertsqlmatches--parametersmatch)
  - [`TestDataSeeder` & `SnapshotAssert`](#testdataseeder--snapshotassert)

---

## Entry Point: `Sql` Static Class

### `Sql.From<T>()`

Starts building a strongly-typed, immutable `SELECT` query based on entity metadata.

#### Signature
```csharp
public static SelectQuery<T> From<T>()
```

#### Type Parameters
- `T`: The entity type decorated with `[SqlEntity]`, mapping the table schema and column tokens.

#### Return Value
- `SelectQuery<T>`: A new, immutable query builder instance initialized with the `FROM` node for type `T`.

#### Exceptions
- `SqlValidationException`: Thrown if type `T` does not have mappable public properties or valid table metadata.

#### Remarks
Every method called on `SelectQuery<T>` returns a **new instance** using C# record semantics. The original instance is never mutated.

#### Basic Example
```csharp
var query = Sql.From<User>()
    .Where(u => u.IsActive);
```

#### Advanced Example
```csharp
var query = Sql.From<User>()
    .Select("id", "username", "email")  // params string[] — real overload
    .Where(u => u.IsActive && u.Role == "Admin")
    .OrderByDescending(u => u.CreatedAt)
    .Limit(25);
// Alternatively, anonymous object projection (single lambda):
// .Select(u => new { u.Id, u.Username, u.Email })
```

#### Best Practices
- Prefer strongly-typed expression lambdas (`u => u.IsActive`) over raw strings to ensure refactoring safety.
- Cache base queries and fork them for branch logic (`var admins = baseQuery.Where(u => u.Role == "Admin");`).

#### Performance
- Zero heap allocations when forking queries through structural AST node sharing (`ImmutableArray`).

#### Common Errors
- Invoking `query.Where(...)` without reassigning the returned instance (`query = query.Where(...)`).

#### When to Use
- Whenever composing `SELECT` queries across relational databases.

#### When NOT to Use
- For raw, non-relational commands or DDL operations (`CREATE TABLE`).

---

### `Sql.Insert<T>(T entity)`

Constructs an immutable `INSERT` query from a populated entity instance.

#### Signature
```csharp
public static InsertQuery<T> Insert<T>(T entity)
```

#### Parameters
- `entity` (`T`): The entity instance containing values to insert.

#### Return Value
- `InsertQuery<T>`: An immutable insert query builder supporting `OnConflict`, `Returning`, and compilation.

#### Exceptions
- `ArgumentNullException`: Thrown if `entity` is null.
- `SqlValidationException`: Thrown if no insertable columns are discovered on entity `T`.

#### Remarks
Properties decorated with `[DatabaseGenerated]` are omitted from the default column list to allow auto-incrementing identity generation.

#### Basic Example
```csharp
var user = new User { Username = "alice", Email = "alice@example.com" };
var query = Sql.Insert(user);
```

#### Advanced Example
```csharp
var query = Sql.Insert(user)
    .Returning(u => u.Id)
    .OnConflict(u => u.Username)
    .DoUpdate(u => new User { Email = user.Email });
```

#### Best Practices
- Combine with `.Returning(x => x.Id)` when requiring the generated primary key in PostgreSQL, SQLite, or SQL Server.

#### Performance
- Extracts values at zero-reflection overhead when using compile-time Source Generators.

#### Common Errors
- Manually supplying primary key values for identity columns without configuring dialect-specific overrides.

#### When to Use
- Single-entity inserts and upsert operations.

#### When NOT to Use
- Bulk ingestion of thousands of records (use `Sql.BulkInsert` or `BulkInsertAsync` instead).

---

### `Sql.Update<T>()`

Initializes a fluent, strongly-typed `UPDATE` query builder.

#### Signature
```csharp
public static IUpdateSetBuilder<T> Update<T>()
public static IUpdateSetBuilder<T> Update<T>(T entity)
```

#### Parameters
- `entity` (`T`, optional): When provided, non-key property values are used to populate initial `SET` clauses.

#### Return Value
- `IUpdateSetBuilder<T>`: Fluent interface enforcing at least one `SET` assignment before transitioning to `WHERE`.

#### Exceptions
- `SqlValidationException`: Thrown if no properties are assigned or if an unconstrained update is compiled without `.WhereAll()`.

#### Basic Example
```csharp
var query = Sql.Update<User>()
    .Set(u => u.IsActive, false)
    .Where(u => u.Id == 42);
```

#### Advanced Example
```csharp
var query = Sql.Update<User>()
    .Set(u => u.Email, "new@example.com")
    .SetRaw("login_count = login_count + 1")
    .WithConcurrencyToken(u => u.Version, currentVersion)
    .Where(u => u.Id == 42);
```

#### Best Practices
- Always enforce explicit `WHERE` conditions or use `WithConcurrencyToken` to prevent lost updates.

#### Performance
- Generates parameterized SQL statements, maximizing plan cache reuse on the database server.

#### Common Errors
- Omitting the `WHERE` clause, which triggers Roslyn analyzer `ESQL003` and runtime `SqlSafetyException`.

#### When to Use
- Granular updates and state-machine transitions.

#### When NOT to Use
- Complex mass transformations involving cross-table joins that are better expressed as stored procedures or batch jobs.

---

### `Sql.Delete<T>()`

Initializes an immutable `DELETE` query builder with compile-time safety guards.

#### Signature
```csharp
public static IDeleteFromBuilder<T> Delete<T>()
```

#### Return Value
- `IDeleteFromBuilder<T>`: Fluent builder requiring a `.Where()` predicate or explicit `.WhereAll()` declaration.

#### Exceptions
- `SqlSafetyException`: Thrown at compile/build time if no filter conditions or `.WhereAll()` are specified.

#### Basic Example
```csharp
var query = Sql.Delete<User>()
    .Where(u => u.Id == 10);
```

#### Advanced Example
```csharp
var query = Sql.Delete<AuditLog>()
    .Where(a => a.CreatedAt < DateTime.UtcNow.AddMonths(-6))
    .And(a => a.Severity == "DEBUG");
```

#### Best Practices
- Never use `.WhereAll()` in end-user facing endpoints without strict administrative authorization gates.

#### Performance
- Directly emits `DELETE FROM table WHERE ...` without prior object loading or entity hydration.

#### Common Errors
- Forgetting to call `.WhereAll()` when writing test cleanup fixtures, causing build-time analyzer failures.

#### When to Use
- Removing records based on business criteria.

#### When NOT to Use
- Physical table truncations requiring `TRUNCATE TABLE` privileges and log bypass.

---

### `Sql.BulkInsert<T>(IEnumerable<T> entities)`

Configures high-throughput batch insertion for large collections of records.

#### Signature
```csharp
public static BulkBuilder<T> BulkInsert<T>(IEnumerable<T> entities)
```

#### Parameters
- `entities`: Sequence of entities to ingest.

#### Return Value
- `BulkBuilder<T>`: Builder supporting batch size configuration, rule exclusions, and streaming execution.

#### Basic Example
```csharp
var query = Sql.BulkInsert(users);
```

#### Best Practices
- Use client-generated IDs (`UUIDv7` or sequential GUIDs) to maximize throughput and bypass sequence locks.

#### Performance
- Minimizes roundtrips by consolidating multiple rows into single batch commands or driver-native streams (`COPY`).

---

### `Sql.Raw(FormattableString query)`

Provides an escape hatch to execute vendor-specific SQL while guaranteeing parameterization against SQL injection.

#### Signature
```csharp
public static RawSelectNode Raw(FormattableString query)
```

#### Parameters
- `query`: Formattable interpolated string containing SQL and interpolated values.

#### Remarks
Values inside `{value}` brackets are **never** concatenated as raw strings; they are extracted as `@p0`, `@p1` parameters by `IParameterManager`.

#### Basic Example
```csharp
int minSalary = 80000;
var query = Sql.Raw($"SELECT * FROM employees WHERE salary >= {minSalary}");
```

#### Best Practices
- Use only when requiring vendor syntax not currently modeled by typed AST builders (e.g., PostgreSQL JSON operators).

#### Common Errors
- Passing a pre-concatenated regular string (`string.Format` or `"..." + x`) instead of an interpolated `FormattableString`, which bypasses parameterization.

---

### `SelectQuery<T>.CTE(string name, IAstQuery cteQuery)` / `.RecursiveCTE(string name, ISqlQuery body)`

Attaches a Common Table Expression (CTE) to the query for hierarchical or multi-stage composition.
The CTE is registered inline on the query builder and does **not** require a separate `Sql.With()` call.

> [!IMPORTANT]
> `Sql.With(...)` does NOT exist as a public static method. CTEs are composed through `SelectQuery<T>.CTE(name, query)` and `SelectQuery<T>.RecursiveCTE(name, body)`.

#### Signatures
```csharp
// Standard CTE
public SelectQuery<T> CTE(string name, IAstQuery cteQuery)
public SelectQuery<T> CTE(string name, IAstQuery cteQuery, MaterializationHint hint)

// Recursive CTE
public SelectQuery<T> RecursiveCTE(string name, ISqlQuery body)
public SelectQuery<T> RecursiveCTE(string name, ISqlQuery body, MaterializationHint hint)
```

#### Basic Example
```csharp
// WITH active_users AS (SELECT * FROM users WHERE is_active = @p0)
// SELECT * FROM orders ...
var activeUsers = Sql.From<User>().Where(u => u.IsActive);
var query = Sql.From<Order>()
    .CTE("active_users", activeUsers)
    .Select("id", "amount", "status");
```

#### Advanced Example: Recursive CTE
```csharp
var seriesBody = Sql.Raw("SELECT 1 AS n UNION ALL SELECT n + 1 FROM series WHERE n < 10");
var query = Sql.From<Order>()
    .RecursiveCTE("series", seriesBody)
    .Select("id");
```

#### Best Practices
- Use CTEs to avoid repetitive subqueries and improve readability of complex analytical reports.
- Use `MaterializationHint.Materialized` on PostgreSQL to force CTE evaluation as a fence (prevents optimizer from inlining it).

---

### `Sql.RegisterTypeHandler<T>(ITypeHandler<T> handler)`

Registers a custom type handler to serialize/deserialize complex domain types to database primitives.

#### Signature
```csharp
public static void RegisterTypeHandler<T>(ITypeHandler<T> handler)
```

#### Remarks
For projects using Dapper, use `DapperExtensions.RegisterTypeHandler<T>()` to atomically register the handler in both `SqlBuilder` and `Dapper`.

---

## Query Builders & AST Nodes

### `SelectQuery<T>.Where(...)`
Appends a boolean filtering predicate to the query AST. Supports lambda expressions, raw strings, and subqueries.

### `SelectQuery<T>.Join<TTarget>(...)`
Composes inner and left joins between entity `T` and target entity `TTarget` using strongly-typed join conditions.

### `SelectQuery<T>.SeekAfter(...) / SeekBefore(...)`
Implements Keyset (cursor) pagination using tuple comparison expressions (`(col1 > @p0 OR (col1 = @p0 AND col2 > @p1))`), providing constant $O(1)$ query complexity over arbitrarily deep datasets.

### `SelectQuery<T>.Build(ISqlCompiler compiler)`
Compiles the immutable AST into a concrete `SqlResult` containing the dialect-specific SQL string and extracted parameters.

### `InsertQuery<T>.OnConflict(...)`
Specifies conflict resolution target columns for upsert semantics (`ON CONFLICT ("id")`).

### `InsertQuery<T>.Returning(...)`
Configures dialect-native identity and column return clauses (`RETURNING` in PostgreSQL/SQLite/Oracle, `OUTPUT` in SQL Server).

### `UpdateQuery<T>.WithConcurrencyToken(...)`
Enforces optimistic concurrency verification by injecting version checks into the `SET` and `WHERE` clauses.

### `UpdateQuery<T>.ApplyDiff(T original, T modified)`
Calculates property deltas between two model instances and emits minimal `SET` clauses for modified columns only.

### `DeleteQuery<T>.WhereAll()`
Explicitly declares intentional unconstrained table deletion, safely overriding compile-time guards.

---

## Execution & Extensions

### `ConnectionSqlExtensions.QueryPagedAsync<T>(...)`
Executes an offset or cursor-based paginated query against an open `DbConnection`, returning a materialized `IPagedList<T>`.

### `AotQueryExecutor.QueryAsync<T>(...)`
High-performance execution engine designed for Native AOT. Materializes entities through direct `DbDataReader` delegates with zero runtime reflection.

### `DapperExtensions.ExecuteWithConcurrencyCheckAsync(...)`

Executes an `UPDATE` query configured with optimistic concurrency and verifies that exactly one row was updated.

#### Signature
```csharp
public static Task ExecuteWithConcurrencyCheckAsync(
    this IDbConnection connection,
    UpdateQuery<T> query,
    string entityName,
    IDbTransaction? transaction = null,
    CancellationToken cancellationToken = default);
```

#### Exceptions
- `DbConcurrencyException`: Thrown if `RowsAffected == 0`, indicating that another concurrent process modified or deleted the record.

---

### `DapperExtensions.QueryStreamAsync<T>(...)`

Streams query results as an `IAsyncEnumerable<T>`, materializing entities row-by-row without buffering the entire result set in memory.

#### Signature
```csharp
public static IAsyncEnumerable<T> QueryStreamAsync<T>(
    this IDbConnection connection,
    SelectQuery<T> query,
    IDbTransaction? transaction = null,
    CancellationToken cancellationToken = default);
```

#### Remarks
Ideal for processing massive datasets, ETL pipelines, and exporting data directly to output streams.

---

## Dialect Compilers

### `ISqlCompiler`

The core contract transforming an immutable Abstract Syntax Tree (AST) of `ISqlNode` elements into dialect-specific parameterized SQL.

#### Core Methods
```csharp
public interface ISqlCompiler
{
    SqlResult CompileSelect(IReadOnlyList<ISqlNode> nodes, IParameterManager parameterManager);
    SqlResult CompileInsert(IReadOnlyList<ISqlNode> nodes, IParameterManager parameterManager);
    SqlResult CompileUpdate(IReadOnlyList<ISqlNode> nodes, IParameterManager parameterManager);
    SqlResult CompileDelete(IReadOnlyList<ISqlNode> nodes, IParameterManager parameterManager);
    string EscapeIdentifier(string identifier);
    bool SupportsCapability(ProviderCapability capability);
}
```

### Concrete Compilers

| Compiler | Dialect Target | Identifier Quoting | Parameter Prefix | Upsert Syntax |
|---|---|---|---|---|
| `PostgreSqlCompiler` | PostgreSQL | `"column"` | `@p0` | `ON CONFLICT (...) DO UPDATE` |
| `SqlServerCompiler` | Microsoft SQL Server | `[column]` | `@p0` | `MERGE` / `OUTPUT` |
| `SqliteCompiler` | SQLite | `"column"` | `@p0` | `ON CONFLICT (...) DO UPDATE` |
| `MySqlCompiler` | MySQL | \`column\` | `@p0` | `ON DUPLICATE KEY UPDATE` |
| `MariaDbCompiler` | MariaDB | \`column\` | `@p0` | `ON DUPLICATE KEY UPDATE` |
| `OracleCompiler` | Oracle Database | `"column"` | `:p0` | `MERGE INTO` |

---

## Observability & Diagnostics

### `SqlBuilderDiagnostics` & `SqlBuilderInstrumentation`

Provides zero-overhead observability for distributed tracing and performance metrics conforming to OpenTelemetry database semantic conventions.

#### Diagnostic Properties
- `LoggerFactory`: Global `ILoggerFactory` instance for query debugging.
- `LogParameters`: Boolean flag controlling whether extracted parameter values are included in log entries or sanitized.
- `SlowQueryThresholdMs`: Threshold in milliseconds to flag slow-executing queries.
- `ActivitySource`: OpenTelemetry `ActivitySource` emitting `db.system`, `db.statement`, and `db.sql.tag` trace spans.
- `Meter`: OpenTelemetry `Meter` exposing query compilation counters and latency histograms.

---

## Testing & Quality Verification

### `QueryAssert.SqlMatches(...) / ParametersMatch(...)`

Testing utility enabling unit tests to assert compiled SQL statements (with automatic whitespace normalization) and parameter dictionary key-value pairs without fragile string regex matching. Supports engine-specific helpers such as `SqlMatchesPostgreSql`, `SqlMatchesSqlServer`, `SqlMatchesSqlite`, `SqlMatchesMySql`, and snapshot verification via `VerifySql`.

### `TestDataSeeder` & `SnapshotAssert`

- `TestDataSeeder.Customers(count)` / `TestDataSeeder.Generate(seed)`: Produces deterministic, reproducible entity collections for integration tests and benchmarks.
- `SnapshotAssert.MatchesSnapshot(actualSql, snapshotPath)`: Compares generated SQL against verified golden master files (`.sql`), catching unexpected dialect mutations during regressions.

