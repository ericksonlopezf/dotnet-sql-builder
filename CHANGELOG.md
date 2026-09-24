# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2026-09-23

### Breaking Changes
- **PostgreSQL Dapper Bulk Operations CancellationToken Parameter (`BC-001`):**
  - **Change:** Added `CancellationToken cancellationToken = default` to public extension methods in `EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlDapperExtensions`:
    - `BulkCopyAsync<T>(this NpgsqlConnection, IEnumerable<T>, CancellationToken = default)`
    - `BulkCopyAsync<T>(this IDbConnection, IEnumerable<T>, CancellationToken = default)`
    - `BulkInsertUnnestAsync<T>(this IDbConnection, IEnumerable<T>, DbTransaction?, CancellationToken = default)`
    - `BulkInsertAsync(this IDbConnection, string, NpgsqlParameter[], IDbTransaction?, int?, CancellationToken = default)`
    - `BulkUpdateAsync(this IDbConnection, string, NpgsqlParameter[], IDbTransaction?, int?, CancellationToken = default)`
  - **Previous State:** Methods only accepted connection, parameters, and transaction/timeout arguments without cancellation tokens.
  - **Current State:** Methods accept a trailing `CancellationToken` parameter and invoke `cancellationToken.ThrowIfCancellationRequested()`.
  - **Impact:** Binary breaking change (ABI). Assemblies compiled against v1.0.0 calling these methods will fail with `System.MissingMethodException` at runtime until recompiled.
  - **Migration:** Recompile consuming projects against the updated package. When cancellation support is desired, pass a non-default `CancellationToken`.

- **ParameterManager AddNamed Collision Detection & Prefix Stripping (`BC-002`):**
  - **Change:** `ParameterManager.AddNamed` now strictly validates parameter registrations:
    - Throws `System.InvalidOperationException` when attempting to register an existing parameter name with a different value.
    - Strips leading dialect prefixes (`@`, `:`, `$`) via `name.TrimStart('@', ':', '$')`, preventing duplicate registration under both prefixed and unprefixed names.
    - Throws `System.ArgumentException` when `name` is null or whitespace.
  - **Previous State:** Silently overwrote parameter dictionary entries (last-write-wins) and treated `@p1` and `p1` as distinct keys.
  - **Current State:** Guardrail prevents silent parameter clobbering and normalizes parameter keys.
  - **Impact:** Runtime breaking change for query building flows that relied on overwriting named parameters.
  - **Migration:** Ensure named parameters within a query are uniquely named, or pass identical values when reusing parameter names.

- **Strict Identifier Syntax & Operator Validation (`BC-003`):**
  - **Change:** Introduced runtime validation via `SqlNamingHelper.ValidateIdentifier` and `SqlNamingHelper.ValidateOperator` across fluent builder entrypoints:
    - `SelectQuery<T>`: `.Select()`, `.Count()`, `.Sum()`, `.Avg()`, `.Min()`, `.Max()`, `.Where(col, op, val)`, `.WhereNull()`, `.WhereNotNull()`, `.OrderBy()`, `.OrderByDescending()`, `.Window()`, `.PageWindow()`.
    - `InsertQuery<T>`: `.Returning()`, `.OnConflict()`, `.Into()`.
    - `UpdateQuery<T>`: `.Returning()`, `.From()`, `.Join()`.
    - `DeleteQuery<T>`: `.Returning()`, `.Using()`, `.Join()`, `.Delete(tableName)`.
  - **Previous State:** Raw identifier strings were passed unchecked into the AST.
  - **Current State:** Identifiers containing whitespace or forbidden characters (`;`, `'`, `"`, `` ` ``, `/`, `\`, `-`, `\0`, `(`, `)`, `=`) throw `System.ArgumentException`. Operators must match approved SQL operators in `AllowedOperators`.
  - **Impact:** Runtime breaking change for consumers passing composite SQL fragments or subquery syntax to identifier parameters.
  - **Migration:** Use typed expression overloads or raw fragment methods (e.g. `.Where(FormattableString)`) instead of passing formatted expressions to identifier methods.

- **Negative Limit & Offset Invariant Guards (`BC-004`):**
  - **Change:** Enforced non-negative numeric constraints on query pagination:
    - `SelectQuery<T>.Limit(int)` and `.Offset(int)` throw `System.ArgumentOutOfRangeException` if argument is negative.
    - `SelectQuery<T>.Fetch(int)` throws `System.ArgumentOutOfRangeException` if argument is negative.
    - `LimitOffsetNode(int? limit, int? offset)` constructor validates non-negative bounds for limit and offset.
  - **Previous State:** Negative values were permitted and emitted directly into AST nodes.
  - **Current State:** Negative integers are rejected before AST insertion or compilation.
  - **Impact:** Runtime breaking change for code passing negative sentinel values (e.g., `-1` for unlimited).
  - **Migration:** Validate pagination inputs before calling `.Limit()` or `.Offset()`, or omit the clause when no limit or offset is required.

- **Dynamic Sorting Qualifier Limit & Alias Invariant (`BC-005`):**
  - **Change:** `DynamicSortingExtensions.OrderByDynamic<T>` enforces a maximum of two segments (`[alias.]property`) and validates table aliases against `^[a-zA-Z0-9_]+$`.
  - **Previous State:** Allowed arbitrary dot-delimited paths without qualifier count validation or alias format checking.
  - **Current State:** Throws `System.ArgumentException("Sort expression contains too many qualifiers.")` if more than one dot exists, and throws `System.ArgumentException("Invalid table alias in sort expression.")` for non-alphanumeric aliases.
  - **Impact:** Runtime breaking change for queries using multi-part qualified names (e.g. `database.schema.table.column`).
  - **Migration:** Format dynamic sort strings strictly as `propertyName` or `alias.propertyName`.

- **Multi-Projection AST Compilation Aggregation (`BC-006`):**
  - **Change:** `SqlCompilerBase.CompileSelect` now renders all projection nodes in the AST joined by commas (`SELECT a, b, c FROM ...`) instead of evaluating only the last projection node.
  - **Previous State:** Only the last `.Select()` node in the query node collection was compiled (`selectNodes[selectNodes.Count - 1]`).
  - **Current State:** All non-wildcard projection nodes are accumulated into the SQL projection list.
  - **Impact:** Behavioral change in generated SQL. Chained `.Select()` calls previously overwrote earlier selections; now they accumulate columns.
  - **Migration:** If replacing projections is intended, branch from a new `SelectQuery<T>` instance rather than appending `.Select()` to an existing query.

- **InsertQuery<T>.Values Exception Type Refinement (`BC-007`):**
  - **Change:** `InsertQuery<T>.Values(T entity)` now performs a safe cast (`entity as ISqlEntity`) and throws `System.InvalidOperationException` with an explicit diagnostic message if `T` does not implement `ISqlEntity`.
  - **Previous State:** Direct cast `(ISqlEntity)entity!` threw `System.InvalidCastException`.
  - **Current State:** Throws `System.InvalidOperationException($"Entity type '{typeof(T).Name}' does not implement ISqlEntity...")`.
  - **Impact:** Runtime exception type change.
  - **Migration:** Catch `System.InvalidOperationException` instead of `System.InvalidCastException`, and ensure entities are annotated with `[SqlEntity]`.

- **Soft Deprecation of `SelectQuery<T>.Fetch(int rows)` (`BC-008`):**
  - **Change:** Marked `SelectQuery<T>.Fetch(int rows)` with `[Obsolete("Use Limit(int) instead, which provides the identical functionality.", error: false)]`.
  - **Previous State:** Active public API without deprecation warnings.
  - **Current State:** Emits compiler warning `CS0618` during build.
  - **Impact:** Compile-time breaking change for projects configured with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` or with `CS0618` treated as an error.
  - **Migration:** Replace calls to `.Fetch(rows)` with `.Limit(rows)`.

- **Unified Ecosystem Versioning Across All Packages (`BC-009`):**
  - **Change:** Removed explicit `<Version>` overrides across individual `.csproj` files (`Aot`, `Dapper.Aot`, `Abstractions`, `SourceGenerators`, `PostgreSql`, `Dapper`, `Analyzers`) so that all 16 ecosystem packages centrally inherit `<VersionPrefix>2.0.0</VersionPrefix>` from `Directory.Build.props`.
  - **Previous State:** In the v1.0.0 release, package version skew existed where `EricksonLopez.SqlBuilder.Aot` and `EricksonLopez.SqlBuilder.Dapper.Aot` were published at `2.0.0` while other packages were published at `1.0.0`.
  - **Current State:** All 16 ecosystem packages are unified at version `2.0.0` via `Directory.Build.props`.
  - **Impact:** Eliminates version skew and package downgrade conflicts across the ecosystem.
  - **Migration:** Update all package references across consuming projects to version `2.0.0`.

- **SqlValidationException Base Type Hierarchy Change (`BC-010`):**
  - **Change:** `SqlValidationException` changed its base class from `System.Exception` to `EricksonLopez.SqlBuilder.Abstractions.Exceptions.SqlBuilderException`.
  - **Previous State:** Inherited directly from `System.Exception`.
  - **Current State:** Inherits from `SqlBuilderException` (which in turn inherits from `System.Exception`).
  - **Impact:** Metadata and reflection type hierarchy modification.
  - **Migration:** Reflection code inspecting base types should accommodate the new intermediate `SqlBuilderException` class.

### Deprecated
- **`SelectQuery<T>.Fetch(int rows)`:**
  - Deprecated in favor of `SelectQuery<T>.Limit(int limit)` to establish naming consistency across all query builders.

### Added
- **Core Domain Exceptions Hierarchy (`EricksonLopez.SqlBuilder.Abstractions.Exceptions`):**
  - Added base domain exception `SqlBuilderException`.
  - Added `SqlSafetyException` thrown for unsafe or destructive query operations (e.g. unconstrained mutations).
  - Added `SqlSyntaxException` thrown for AST construction or dialect compatibility violations.
- **Explicit Table Name SELECT Queries:**
  - Added `Sql.From<T>(string tableName)` overload allowing explicit table name specification without overriding entity metadata.
- **Bulk Operation Entity Source Introspection:**
  - Added `IEntitySource<out T>` interface to `EricksonLopez.SqlBuilder.Abstractions` allowing providers to inspect underlying entity collections.
- **Showcase & Samples:**
  - Added `Level11_ComprehensiveApiCoverage` sample covering full ecosystem API surface, CTE hierarchies, and cross-dialect execution patterns.
- **Oracle Native Bulk Copy:**
  - Implemented `OracleEntityDataReader<T>` in `EricksonLopez.SqlBuilder.Oracle` providing zero-reflection `IDataReader` streaming for bulk data loading via `Oracle.ManagedDataAccess.Client.OracleBulkCopy`.
- **Adversarial & Concurrency Security Test Suite:**
  - Added stress tests (`ConcurrencyStressTests`) verifying thread safety of query compilation under high thread contention.
  - Added adversarial security tests (`SqlInjectionRedTeamTests`, `QueryBuilderValidationTests`, `AdversarialSecurityAuditTests`) validating parameterization invariants and input boundaries.
  - Added property-based fuzzing tests (`PropertyBasedQueryFuzzingTests`) with FsCheck.
- **CI/CD Quality Gates:**
  - Added benchmark regression assertion script (`scripts/verify-benchmark-gate.ps1`) verifying zero-allocation hot paths and latency variance thresholds.

### Fixed
- **Keyword Escaping in Member Select Projections:**
  - `SqlCompilerVisitor` now automatically escapes reserved SQL and PostgreSQL keywords when resolving member expressions in `ExpressionSelectNode`.
- **Custom Column Mapping in Cursor Pagination:**
  - `CursorPaginationExtensions` now queries `SqlEntityCache<T>.PropertyMap` to honor `[Column("...")]` custom column mapping attributes instead of always using raw snake_case names.

## [1.0.0] - 2026-08-28

### Added
- **Core Engine & Abstractions (`EricksonLopez.SqlBuilder` & `EricksonLopez.SqlBuilder.Abstractions`):**
  - Immutable fluent SQL AST construction with C# record semantics.
  - Fluent query builders for `SELECT`, `INSERT`, `UPDATE`, and `DELETE` statements with interface-segregated builder contracts (`IDeleteFromBuilder<T>`, `IDeleteWhereBuilder<T>`, `IUpdateSetBuilder<T>`, `IUpdateWhereBuilder<T>`).
  - Double-dispatch typed visitor pattern (`ISqlVisitor`, `SqlVisitorBase`) for extensible AST rendering and dialect compilation.
  - Type handler abstraction (`ITypeHandler`) and concurrent parameter management (`IParameterManager`, `ParameterManager`).
  - Expression tree parser converting C# lambda predicates into parameterized SQL expressions.
  - Query fingerprinter (`IQueryFingerprinter`, `QueryFingerprinter`) for query normalization, hashing, and execution tracing.
- **Dialect Implementations (6 Independent Database Engines):**
  - `EricksonLopez.SqlBuilder.PostgreSql`: Full PostgreSQL dialect support, including `RETURNING`, `ON CONFLICT DO NOTHING / UPDATE`, array unnesting, binary `COPY`, and dialect-specific operators (`ILIKE`, regex matches).
  - `EricksonLopez.SqlBuilder.SqlServer`: Full SQL Server dialect support (T-SQL), including `OUTPUT` clause, `TOP` / `FETCH NEXT` pagination, table hints, and native `SqlBulkCopy` strategy.
  - `EricksonLopez.SqlBuilder.MySql`: MySQL dialect compiler with backtick escaping, `ON DUPLICATE KEY UPDATE` / `ON CONFLICT`, and `MySqlBatch` bulk execution.
  - `EricksonLopez.SqlBuilder.MariaDb`: Independent MariaDB dialect compiler with MariaDB-specific functions and syntax optimizations.
  - `EricksonLopez.SqlBuilder.Sqlite`: SQLite dialect compiler supporting `INSERT OR IGNORE`, `ON CONFLICT`, and standard ANSI limits.
  - `EricksonLopez.SqlBuilder.Oracle`: Oracle Database dialect compiler supporting Oracle 12c+ (`FETCH FIRST`) and Oracle 11g legacy pagination (`ROWNUM`), sequence integration, and Oracle-specific quotes.
- **Dapper & Data Access Integrations:**
  - `EricksonLopez.SqlBuilder.Dapper`: Seamless integration with Dapper for executing AST queries directly against `IDbConnection`.
  - Multi-mapping support for mapping 2 to 7 entity tuples and 8+ entity graphs via fluent mapping descriptors.
  - Optimistic concurrency control via `DbConcurrencyException` tracking expected rows affected.
  - NativeAOT Dapper integration (`EricksonLopez.SqlBuilder.Dapper.Aot`) utilizing compile-time delegate mappers without reflection.
- **NativeAOT Execution Engine (`EricksonLopez.SqlBuilder.Aot`):**
  - Zero-reflection query execution engine for trimming and NativeAOT compilation.
  - Compile-time generated `IStaticEntityMetadata<TEntity>` integration with static `FromReader` parser delegates.
- **Source Generators (`EricksonLopez.SqlBuilder.SourceGenerators`):**
  - `SqlEntityGenerator`: Generates zero-allocation static entity metadata, column tokens, and `IDataReader` parser delegates.
  - `FilterGenerator`: Generates strongly-typed filter extensions from entity definitions.
  - `MultiMapDescriptorGenerator`: Generates multi-mapping tuple unpackers at compile time.
- **Dedicated Pagination Ecosystem (`EricksonLopez.SqlBuilder.Pagination`):**
  - Keyset cursor pagination (`CursorPaginationExtensions`), Offset-based pagination, and Window function pagination.
  - Universal paginated results contract (`IPagedList<T>`, `PagedList<T>`, `CountedPagedList<T>`) supporting `net8.0`, `net9.0`, and `net10.0`.
- **Advanced SQL Query Features:**
  - Common Table Expressions (CTEs), Recursive CTEs, and CTE Materialization hints (`MATERIALIZED` / `NOT MATERIALIZED`).
  - Window Functions (`ROW_NUMBER()`, `RANK()`, `DENSE_RANK()`, `NTILE()`, `LAG()`, `LEAD()`) with `OVER (PARTITION BY ... ORDER BY ...)` and `FILTER (WHERE ...)` clauses.
  - Set Operations (`UNION`, `UNION ALL`, `INTERSECT`, `EXCEPT`).
  - Subqueries in `WHERE` (`EXISTS`, `NOT EXISTS`, `IN`, `NOT IN`) and Scalar Subqueries in `SELECT` and `FROM` (`LATERAL` / `CROSS APPLY`).
  - Case expressions builder (`CaseExpressionBuilder`).
- **Roslyn Safety Analyzers Suite (`EricksonLopez.SqlBuilder.Analyzers`):**
  - Comprehensive suite of compile-time safety and anti-pattern analyzers (`ESQL001` through `ESQL026`):
    - `ESQL001` & `ESQL003`: Guard against `DELETE` or `UPDATE` queries without `WHERE` clauses.
    - `ESQL002`: Detects SQL injection risks via raw string interpolation and concatenation.
    - `ESQL006`: Validates incompatible property types in `JOIN` conditions.
    - `ESQL007` & `ESQL010`: Performance warnings for unindexed ordering and leading wildcard `LIKE` queries.
    - `ESQL024`: Cartesian join detection (missing `ON` condition).
    - `ESQL026`: Flags deprecated generic `Sql.Merge<T>()` usage with error diagnostic.
- **Observability & Diagnostics (`EricksonLopez.SqlBuilder.OpenTelemetry`):**
  - OpenTelemetry database semantic conventions instrumentation (`ActivitySource` and `Meter`).
  - Query execution duration histograms and query counters.
- **Testing & Quality Assurance (`EricksonLopez.SqlBuilder.Testing`):**
  - Mock compiler (`MockSqlCompiler`), diagnostic activity scope (`DiagnosticActivityScope`), test data seeders, and snapshot comparison tools.

[Unreleased]: https://github.com/ericksonlopezf/dotnet-sql-builder/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/ericksonlopezf/dotnet-sql-builder/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/ericksonlopezf/dotnet-sql-builder/releases/tag/v1.0.0
