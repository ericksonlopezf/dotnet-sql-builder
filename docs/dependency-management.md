# Dependency Management

This document describes the Central Package Management (CPM) configuration used in this repository and provides a reference table of all pinned NuGet dependency versions.

---

## Central Package Management (CPM)

This repository uses **Central Package Management** via [`Directory.Packages.props`](../Directory.Packages.props), enabled by:

```xml
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
```

**What CPM means for contributors:**

- All `<PackageReference>` entries in individual `.csproj` files **must not** include a `Version` attribute — the version is defined once, centrally, in `Directory.Packages.props`
- To add a new NuGet dependency: add a `<PackageVersion>` entry to `Directory.Packages.props` first, then reference it in the `.csproj`
- Per-project overrides are permitted using `VersionOverride="x.y.z"` on the `<PackageReference>` element (used sparingly — see PostgreSQL note below)

---

## Pinned Dependency Versions

All versions are sourced directly from [`Directory.Packages.props`](../Directory.Packages.props).

### Core Runtime Dependencies

| Package | Version | Used By |
|---------|---------|---------|
| `Dapper` | 2.1.35 | `SqlBuilder.Dapper` |
| `System.Collections.Immutable` | 10.0.10 | Core AST (`SelectQuery<T>`, `WhereNode`, etc.) |
| `System.Text.Json` | 8.0.5 | Pagination cursor encoding |
| `System.Diagnostics.DiagnosticSource` | 8.0.1 | OpenTelemetry ActivitySource |
| `OpenTelemetry` | 1.17.0 | `SqlBuilder.OpenTelemetry` |
| `OpenTelemetry.Api` | 1.17.0 | `SqlBuilder.OpenTelemetry` |
| `OpenTelemetry.Extensions.Hosting` | 1.17.0 | Sample projects |
| `OpenTelemetry.Exporter.Console` | 1.17.0 | Sample projects |
| `Microsoft.Extensions.Resilience` | 9.0.0 | Resilience helper classes in `SqlBuilder.Dapper` (wraps Polly v8) |
| `Microsoft.Extensions.Logging.Abstractions` | 8.0.2 | Resilience error detectors |
| `Microsoft.Extensions.Hosting` | 10.0.11 | Sample projects |

### Database Drivers

| Package | Version | Used By |
|---------|---------|---------|
| `Npgsql` | 9.0.3 | `SqlBuilder.PostgreSql` |
| `Microsoft.Data.SqlClient` | 6.0.2 | `SqlBuilder.SqlServer` |
| `Microsoft.Data.Sqlite` | 9.0.5 | `SqlBuilder.Sqlite` |
| `MySqlConnector` | 2.4.0 | `SqlBuilder.MySql` |
| `Oracle.ManagedDataAccess.Core` | 23.26.300 | `SqlBuilder.Oracle` |

### Roslyn / Source Generator Tools

| Package | Version | Used By |
|---------|---------|---------|
| `Microsoft.CodeAnalysis.CSharp` | 4.10.0 | `SqlBuilder.Analyzers`, `SqlBuilder.SourceGenerators` |
| `Microsoft.CodeAnalysis.CSharp.Workspaces` | 4.10.0 | `SqlBuilder.Analyzers` |
| `Microsoft.CodeAnalysis.Analyzers` | 3.3.4 | `SqlBuilder.Analyzers`, `SqlBuilder.SourceGenerators` |
| `Microsoft.CodeAnalysis.PublicApiAnalyzers` | 3.3.4 | All non-test, non-generator source projects |
| `Microsoft.CodeAnalysis.Common` | 4.8.0 | Analyzer testing |
| `Microsoft.SourceLink.GitHub` | 8.0.0 | All projects (global in `Directory.Build.props`) |
| `PolySharp` | 1.14.1 | `SqlBuilder.Analyzers`, `SqlBuilder.SourceGenerators` (polyfills for older runtime targets) |

### Testing Infrastructure

| Package | Version | Used By |
|---------|---------|---------|
| `xunit` | 2.9.3 | All test projects |
| `xunit.runner.visualstudio` | 2.8.2 | All test projects |
| `Microsoft.NET.Test.Sdk` | 17.12.0 | All test projects |
| `coverlet.collector` | 6.0.4 | Unit test coverage |
| `coverlet.msbuild` | 6.0.4 | Coverage MSBuild integration |
| `Testcontainers` | 4.4.0 | Integration test base |
| `Testcontainers.PostgreSql` | 4.4.0 | PostgreSQL integration tests |
| `Testcontainers.MsSql` | 4.4.0 | SQL Server integration tests |
| `Testcontainers.MySql` | 4.4.0 | MySQL integration tests |
| `Testcontainers.Oracle` | 4.4.0 | Oracle integration tests |
| `AwesomeAssertions` | 9.5.0 | Assertion library |
| `NSubstitute` | 5.3.0 | Mocking |
| `Bogus` | 35.6.1 | Fake data generation |
| `AutoFixture` | 4.18.1 | Test fixture auto-generation |
| `FsCheck` | 3.0.0-rc3 | Property-based testing |
| `FsCheck.Xunit` | 3.0.0-rc3 | FsCheck xUnit integration |
| `Verify.Xunit` | 31.12.5 | Snapshot testing |
| `NetArchTest.Rules` | 1.3.2 | Architecture enforcement tests |
| `TngTech.ArchUnitNET.xUnit` | 0.13.3 | Architecture unit tests |
| `Microsoft.Extensions.ObjectPool` | 8.0.2 | Test infrastructure |
| `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit` | 1.1.2 | Analyzer unit tests |
| `Microsoft.CodeAnalysis.CSharp.CodeFix.Testing.XUnit` | 1.1.2 | Code-fix unit tests |
| `Microsoft.CodeAnalysis.CSharp.SourceGenerators.Testing.XUnit` | 1.1.2 | Source generator tests |

### Benchmark & Comparison (Internal Only)

| Package | Version | Used By |
|---------|---------|---------|
| `BenchmarkDotNet` | 0.14.0 | `SqlBuilder.Benchmarks` |
| `SqlKata` | 4.0.1 | Benchmarks (competitor comparison) |
| `SqlKata.Execution` | 4.0.1 | Benchmarks |
| `Dapper.SqlBuilder` | 2.0.78 | Benchmarks |
| `RepoDb` | 1.13.1 | Benchmarks |
| `SqlSugarCore` | 5.1.4.198 | Benchmarks |
| `Microsoft.EntityFrameworkCore` | 10.0.10 | Benchmarks |

### Cloud / Integration

| Package | Version | Used By |
|---------|---------|---------|
| `Azure.Identity` | 1.12.1 | Sample projects |
| `EricksonLopez.SharedKernel` | 2.0.0 | Sample and test projects |
| `EricksonLopez.Result` | 1.1.0 | `SqlBuilder.Abstractions` (Result pattern dependency) |

---

## Notes

### Npgsql Version

`SqlBuilder.PostgreSql` uses Npgsql `9.0.3` as pinned in `Directory.Packages.props`. Any per-project `VersionOverride` would be documented in the individual `.csproj`.

### Pre-release Packages

Some testing packages (`AwesomeAssertions`, `FsCheck`) use pre-release or release-candidate versions. These are scoped exclusively to test projects and have no impact on production NuGet packages.

### Updating Dependencies

To update a dependency version:

1. Change the `Version` attribute in [`Directory.Packages.props`](../Directory.Packages.props)
2. Run `dotnet restore dotnet-sql-builder.slnx` to update lock files
3. Run unit and integration tests to verify no regressions

Dependabot is configured to propose NuGet updates weekly (grouped by ecosystem). See [`docs/ci-cd.md`](ci-cd.md#5-dependency-scanning-dependabot) for details.

---

## See Also

- [`Directory.Packages.props`](../Directory.Packages.props) — Authoritative version source
- [`Directory.Build.props`](../Directory.Build.props) — Global MSBuild configuration
- [`docs/build.md`](build.md) — Build system documentation
- [`docs/ci-cd.md`](ci-cd.md) — CI pipeline and Dependabot configuration
