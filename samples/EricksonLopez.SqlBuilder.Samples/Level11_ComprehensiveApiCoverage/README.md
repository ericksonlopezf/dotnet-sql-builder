# Level 11: Comprehensive Public API Coverage Verification

> **Objective:** Exhaustive verification harness validating 100% of all public API methods, interfaces, and extension points across the entire `EricksonLopez.SqlBuilder` ecosystem.

---

## Overview

While Levels 01 through 10 demonstrate progressive architectural paradigms (from basic query generation to enterprise CQRS and multi-dialect compilation), **Level 11** acts as the definitive executable compliance harness. It guarantees that every single public method, overload, dialect-specific feature, test fixture helper, and AOT mapper is executed and verified at runtime.

---

## Verifications Performed

Level 11 executes 8 specialized verification suites:

### 1. Base CRUD & Fixture Lifecycle (`VerifyCrudTestsBaseMethodsAsync`)
- Verifies `CrudTestsBase` abstractions and concrete fixture instantiations.
- Tests query execution lifecycle across SELECT, INSERT, UPDATE, DELETE, soft-deletes, aggregation, grouping, pagination, and transaction rollbacks/commits.

### 2. Testing Utilities, Seeders & Object Mothers (`VerifyTestingSeedersAndMothers`)
- Generates reproducible datasets via `TestDataSeeder` (Customers, Addresses, Categories, Products, Users, Orders, OrderItems, Invoices, Payments).
- Validates typed builders (`CustomerBuilder`, `ProductBuilder`, etc.) and `ObjectMother` domain instances.

### 3. Fluent Query Assertions & Golden Master Snapshots (`VerifyQueryAndSnapshotAssertionsAsync`)
- Exercises `QueryAssert` fluent assertions:
  - Table name verification (`HasTable`)
  - Clause presence (`HasWhere`, `HasOrderBy`, `HasLimit`, `HasOffset`)
  - Parameter validation (`HasParameter`, `HasParameterWithValue`)
- Golden master SQL snapshot comparison against dialect baselines.

### 4. Domain & Entity Metadata Integrity (`VerifyDummyAndThreeColumnEntities`)
- Validates model mapping across edge-case schemas (`DummyEntity`, `ThreeColumnEntity`, custom primary keys, multi-column composite keys).
- Tests `ColumnToken` flags and metadata token resolution.

### 5. Native AOT & Dapper Engine Synergy (`VerifyAotAndDapperOperationsAsync`)
- Validates zero-reflection mapping via `IDataReaderMapper<T>` and `AotQueryExecutor`.
- Tests `DapperAotExtensions` and static reader parsers.

### 6. High-Throughput Bulk Operations & Rendering (`VerifyBulkAndRenderOperationsAsync`)
- Exercises batch ingestion pipelines, `BulkInsertAsync`, and `IEntitySource` streaming.
- Tests AST rendering pipelines and visitor tree traversals.

### 7. Dialect Compilers & AST Extension Nodes (`VerifyDialectAndAstExtensions`)
- Cross-compiles queries across all 6 supported SQL dialects:
  - `PostgreSqlCompiler`
  - `SqlServerCompiler`
  - `SqliteCompiler`
  - `MySqlCompiler`
  - `MariaDbCompiler`
  - `OracleCompiler`
- Validates dialect-specific grammar nodes (`LimitOffsetNode`, `ReturningNode`, `OnConflictNode`, `MergeNode`).

### 8. Keyset Pagination & OpenTelemetry Instrumentation (`VerifyPaginationAndInstrumentation`)
- Tests cursor pagination via `CursorPaginationExtensions` and seek operators (`SeekAfter`, `SeekBefore`).
- Exercises distributed tracing activity generation via `SqlBuilderDiagnostics.ActivitySource` and metrics emission via `SqlBuilderDiagnostics.Meter`.

---

## Running Level 11

Level 11 runs automatically as the final stage when executing the Showcase:

```bash
cd samples/EricksonLopez.SqlBuilder.Samples
dotnet run
```

Expected terminal output:
```text
=== LEVEL 11: COMPREHENSIVE API COVERAGE (212 METHODS) ===
=== LEVEL 11: [OK] ALL 212 API METHODS VERIFIED SUCCESSFULLY ===
```
