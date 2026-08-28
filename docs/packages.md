# NuGet Packages and Compatibility

This document describes the complete ecosystem of NuGet packages produced by this repository, their target frameworks, and dependency relationships.

## Package Ecosystem

The repository is modularized into granular packages following a **pay-for-play** dependency model: users take only the packages they actually need. See [`docs/architecture.md`](architecture.md) for the architectural justification (ADR-009).

---

## Published Packages

### Core Packages

| Package Name | Description | Target Frameworks | AOT Safe |
|---|---|---|:---:|
| `EricksonLopez.SqlBuilder` | Core fluent query builder, AST generator, dialect configuration APIs | `net8.0`, `net9.0` | ✅ |
| `EricksonLopez.SqlBuilder.Abstractions` | Interfaces (`ISqlCompiler`, `ISqlNode`), attributes (`[SqlEntity]`, `[DatabaseGenerated]`), shared types | `net8.0`, `net9.0` | ✅ |

### Dialect Compilers

Each package provides `ISqlCompiler` implementations for a specific database engine. Install only the dialect(s) your application targets.

| Package Name | Engine | Key Features | Target Frameworks |
|---|---|---|---|
| `EricksonLopez.SqlBuilder.SqlServer` | SQL Server / Azure SQL | `SqlBulkCopyStrategy`, `SqlBulkMergeStrategy`, `OUTPUT` clause | `net8.0`, `net9.0` |
| `EricksonLopez.SqlBuilder.PostgreSql` | PostgreSQL | `NpgsqlCopyStrategy`, `NpgsqlBulkMergeStrategy`, `RETURNING`, `ON CONFLICT` | `net8.0`, `net9.0` |
| `EricksonLopez.SqlBuilder.MySql` | MySQL | `MySqlBatchStrategy`, `MySqlBulkMergeStrategy`, `ON DUPLICATE KEY UPDATE` | `net8.0`, `net9.0` |
| `EricksonLopez.SqlBuilder.MariaDb` | MariaDB | Dedicated MariaDB dialect compiler inheriting MySQL AST visitor | `net8.0`, `net9.0` |
| `EricksonLopez.SqlBuilder.Sqlite` | SQLite | Lightweight, zero external driver dependency | `net8.0`, `net9.0` |
| `EricksonLopez.SqlBuilder.Oracle` | Oracle | `Oracle.ManagedDataAccess.Core` driver, FETCH FIRST & ROWNUM pagination | `net8.0`, `net9.0` |

### Integration, Execution & Pagination Packages

| Package Name | Description | Target Frameworks | AOT Safe |
|---|---|---|:---:|
| `EricksonLopez.SqlBuilder.Dapper` | Extension methods: `connection.QueryAsync(builder)`, multi-mapping 2–7, bulk API | `net8.0`, `net9.0` | ⚠️ Dapper uses reflection |
| `EricksonLopez.SqlBuilder.Dapper.Aot` | Dapper.AOT & NativeAOT reflection-free execution extensions over DbConnection | `net8.0`, `net9.0` | ✅ |
| `EricksonLopez.SqlBuilder.Aot` | Reflection-free `AotQueryExecutor` for NativeAOT-compatible execution | `net8.0`, `net9.0` | ✅ |
| `EricksonLopez.SqlBuilder.Pagination` | Offset and keyset cursor pagination extension methods for SelectQuery AST | `net8.0`, `net9.0`, `net10.0` | ✅ |
| `EricksonLopez.SqlBuilder.OpenTelemetry` | OpenTelemetry `ActivitySource` tracing integration | `net8.0`, `net9.0` | ✅ |

### Developer Tools & Analyzers

| Package Name | Description | Target Frameworks |
|---|---|---|
| `EricksonLopez.SqlBuilder.Analyzers` | Roslyn analyzers enforcing SQL safety best practices (ESQL rules) | `netstandard2.0` |
| `EricksonLopez.SqlBuilder.SourceGenerators` | Zero-reflection metadata generation for NativeAOT and materialization | `netstandard2.0` |

---

## Internal-Only Packages

| Package Name | Purpose |
|---|---|
| `EricksonLopez.SqlBuilder.Testing` | Shared test infrastructure: mock compiler, SQL assertion helpers, Testcontainers setup, DDL scripts |
| `EricksonLopez.SqlBuilder.Benchmarks` | BenchmarkDotNet performance suite — not a library |

---

## Target Framework Compatibility Matrix

| Package | netstandard2.0 | net8.0 | net9.0 | net10.0 |
|---|:---:|:---:|:---:|:---:|
| `EricksonLopez.SqlBuilder` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.Abstractions` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.SqlServer` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.PostgreSql` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.MySql` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.MariaDb` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.Sqlite` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.Oracle` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.Dapper` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.Dapper.Aot` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.Aot` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.Pagination` | — | ✅ | ✅ | ✅ |
| `EricksonLopez.SqlBuilder.OpenTelemetry` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.Testing` | — | ✅ | ✅ | — |
| `EricksonLopez.SqlBuilder.Analyzers` | ✅ | — | — | — |
| `EricksonLopez.SqlBuilder.SourceGenerators` | ✅ | — | — | — |

---

## Dependency Management

This repository uses **Central Package Management (CPM)** via [`Directory.Packages.props`](../Directory.Packages.props). All third-party NuGet dependencies are pinned globally to ensure version consistency across the entire solution.

### Key Direct Dependencies (Pinned Versions)

| Package | Version | Used By |
|---|---|---|
| `Dapper` | 2.1.35 (Overridden in Dapper csproj) | `SqlBuilder.Dapper` |
| `Npgsql` | 9.0.3 (Overridden to 9.0.2 in PostgreSql csproj) | `SqlBuilder.PostgreSql` |
| `Microsoft.Data.SqlClient` | 6.0.2 | `SqlBuilder.SqlServer` |
| `Microsoft.Data.Sqlite` | 9.0.5 | `SqlBuilder.Sqlite` |
| `MySqlConnector` | 2.4.0 | `SqlBuilder.MySql` |
| `Oracle.ManagedDataAccess.Core` | 23.26.300 | `SqlBuilder.Oracle` |
| `OpenTelemetry.Api` | 1.17.0 | `SqlBuilder.OpenTelemetry` |
| `System.Collections.Immutable` | 10.0.10 | Core AST |
| `BenchmarkDotNet` | 0.14.0 | Benchmarks only |
| `Testcontainers.*` | 4.4.0 | Integration tests only |
| `xunit` | 2.9.3 | Test projects |

---

## Public API and Breaking Changes

- **Public API Monitoring:** The repository uses `Microsoft.CodeAnalysis.PublicApiAnalyzers`. Any changes to the public API surface must be explicitly declared in `PublicAPI.Unshipped.txt` in the respective project. Undeclared API changes will fail the build.
- **Breaking Changes:** No breaking changes are introduced without a major version bump. Semantic Versioning (`VersionPrefix` in `Directory.Build.props`) governs the version scheme.
- **Package Validation:** `<EnablePackageValidation>true</EnablePackageValidation>` is enabled globally, comparing each release package against its shipped baseline to detect API surface changes automatically.
