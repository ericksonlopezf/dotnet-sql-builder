# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
