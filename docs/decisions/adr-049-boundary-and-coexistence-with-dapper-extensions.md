# ADR-049: Architectural Boundary and Coexistence with EricksonLopez.DapperExtensions

## Status
Accepted

## Date
2026-09-04

## Context
Across the EricksonLopez ecosystem, persistence concerns are divided into query building and execution runtime. Over time, an overlap emerged:
- `EricksonLopez.SqlBuilder` is primarily an immutable AST SQL compiler, dialect renderer, and query construction engine. However, execution adapters in `EricksonLopez.SqlBuilder.Dapper` and dialect-specific bulk extensions in `EricksonLopez.SqlBuilder.PostgreSql` (such as `BulkParameters`, `TransactionExtensions`, and `PostgreSqlDapperExtensions`) began replicating infrastructure concerns.
- `EricksonLopez.DapperExtensions` exists under the philosophy of *"Raw SQL, Managed Infrastructure"*, providing enterprise `IUnitOfWork`, `ISavepoint`, dialect-native bulk throughput (`UNNEST`, `SqlBulkCopy`, `INSERT ALL`), Native AOT `MultiMapBuilder`, reactive `IAsyncEnumerable<T>` streaming, Polly v8 resilience pipelines, and OpenTelemetry instrumentation.

An architectural audit of enterprise consumers (`OpusHydra` with 34 BCs and `JeiyelFE26` DGII Fiscal Core) confirmed that production systems standardize directly on `EricksonLopez.DapperExtensions` for runtime persistence infrastructure.

In accordance with:
- **Principle 3**: *Composition over monoliths.*
- **Principle 14**: *Every abstraction must justify its existence.*
- **Rule 20 & 22**: *Internal Platform Libraries & Implementation Seniority.*

We must formally demarcate the boundary between both packages.

## Decision
We formally establish the architectural boundary between `EricksonLopez.SqlBuilder` and `EricksonLopez.DapperExtensions`:

1. **`EricksonLopez.SqlBuilder` is the Sole Owner of SQL AST Compilation & Query Building**:
   - Query AST construction: `SelectQuery<T>`, `InsertQuery<T>`, `UpdateQuery<T>`, `DeleteQuery<T>`.
   - Dialect query compilation and rendering (PostgreSQL, SQL Server, MySQL, SQLite, Oracle).
   - Roslyn Analyzers enforcing SQL safety (e.g. `DELETE`/`UPDATE` without `WHERE`, injection detection).
   - Source generators for `[SqlEntity]` metadata and compile-time AOT renderers.
   - Output: Immutable `SqlResult` (`string Sql`, `IReadOnlyDictionary<string, object> Parameters`).
   - `SqlBuilder` MUST NOT manage database connection lifecycles, transaction nesting, or connection pooling.

2. **`EricksonLopez.DapperExtensions` is the Sole Owner of Data Access Runtime & Execution Infrastructure**:
   - Transactional Boundaries: `IUnitOfWork` and `ISavepoint` with nested transaction and savepoint rollback semantics.
   - Dialect-Native Bulk Ingestion: PostgreSQL typed array `UNNEST`, SQL Server `SqlBulkCopy`, Oracle `INSERT ALL`, and parameter-safe batching.
   - Graph Hydration: Zero-allocation `MultiMapBuilder` and compile-time `IDataReaderMapper<T>` for 1:N relational mapping.
   - Streaming: Reactive `IAsyncEnumerable<T>` streaming over unbuffered `CommandDefinition`.
   - Resilience & Observability: Polly v8 execution strategies wrapping Unit of Work, plus OpenTelemetry and HealthChecks.

3. **`EricksonLopez.SqlBuilder.Dapper` is a Thin Bridge Adapter**:
   - Its sole purpose is allowing consumers of `SqlBuilder` to execute a compiled `ISqlQuery` or `SqlResult` on an existing `IDbConnection` or `IDbTransaction`.
   - It MUST NOT duplicate Unit of Work, savepoint logic, or dialect bulk runtimes.
   - The historical migration note (`migration-dapper-extensions-pg.md`) claiming that `dapper-extensions-pg` was deprecated into `SqlBuilder` is formally superseded.

## Consequences

### Positive
- Eradicates duplicate implementations of `BulkParameters` and transaction management.
- Re-aligns `SqlBuilder` with its core identity as a compiler and AST generator rather than an ORM.
- Clarifies dependencies for consuming repositories across `OpusHydra`, `JeiyelFE26`, and lagging consumers.

### Negative
- Applications utilizing both fluent query building and managed transaction infrastructure will reference both packages, which is the intended modular composition model.

## References
- [Architecture Boundaries Document](../architecture-boundaries.md)
- [ADR-002: Dapper Integration Is Optional](./adr-002-dapper-integration-optional.md)
- [ADR-004: UnitOfWork Belongs Outside Core](./adr-004-unitofwork-outside-core.md)
- [`EricksonLopez.DapperExtensions` Architecture Guide](https://github.com/ericksonlopezf/dotnet-dapper-extensions/blob/main/docs/architecture.md)
