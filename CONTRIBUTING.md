# Contributing to EricksonLopez.SqlBuilder

Thank you for your interest in contributing to **EricksonLopez.SqlBuilder**! We welcome bug reports, feature requests, and pull requests.

## Prerequisites

- **.NET SDK 10.0.x** — version pinned in [`global.json`](global.json). Install from [dotnet.microsoft.com](https://dotnet.microsoft.com/download).
- **Docker** — required for running integration tests against PostgreSQL, SQL Server, MySQL, and Oracle via [Testcontainers](https://dotnet.testcontainers.org/). Not needed for SQLite or unit tests.

## Build Commands

Simulate the CI pipeline locally:

```bash
# Restore dependencies
dotnet restore dotnet-sql-builder.slnx

# Build in Release configuration
# (TreatWarningsAsErrors is relaxed locally to avoid blocking development)
dotnet build dotnet-sql-builder.slnx --no-restore --configuration Release -p:TreatWarningsAsErrors=false
```

## Test Commands

### Unit Tests

Unit tests do not require Docker and run in seconds:

```bash
dotnet test dotnet-sql-builder.slnx --no-build --configuration Release --filter "Category!=Integration"
```

### Integration Tests

Integration tests require Docker to be running. The CI pipeline pre-pulls images to speed up execution.

```bash
# SQLite (no Docker required)
dotnet test dotnet-sql-builder.slnx --configuration Release --filter "Category=Integration&Engine=SQLite"

# PostgreSQL (requires Docker)
docker pull postgres:16-alpine
dotnet test dotnet-sql-builder.slnx --configuration Release --filter "Category=Integration&Engine=PostgreSQL" -e TESTCONTAINERS_RYUK_DISABLED=true

# SQL Server (requires Docker)
docker pull mcr.microsoft.com/mssql/server:2022-latest
dotnet test dotnet-sql-builder.slnx --configuration Release --filter "Category=Integration&Engine=SqlServer" -e TESTCONTAINERS_RYUK_DISABLED=true

# MySQL (requires Docker)
docker pull mysql:8.3
dotnet test dotnet-sql-builder.slnx --configuration Release --filter "Category=Integration&Engine=MySQL" -e TESTCONTAINERS_RYUK_DISABLED=true

# MariaDB (requires Docker)
docker pull mariadb:11.3
dotnet test dotnet-sql-builder.slnx --configuration Release --filter "Category=Integration&Engine=MariaDB" -e TESTCONTAINERS_RYUK_DISABLED=true

# Oracle (requires Docker)
docker pull gvenzl/oracle-free:slim-faststart
dotnet test dotnet-sql-builder.slnx --configuration Release --filter "Category=Integration&Engine=Oracle" -e TESTCONTAINERS_RYUK_DISABLED=true
```

### Mutation Testing (Stryker)

The project uses [Stryker.NET](https://stryker-mutator.io/) for mutation testing with a strict **95% break threshold** (configured in [`stryker-config.json`](stryker-config.json)).

```bash
# Restore local tools first (includes dotnet-stryker 4.16.0)
dotnet tool restore

# Run mutation testing
dotnet stryker --config-file stryker-config.json
```

> **Note:** Stryker excludes `SourceGenerators`, `Analyzers`, and `Benchmarks` projects from mutation. See [ADR-001](docs/decisions/adr-001-stryker-source-generator-exclusion.md) for the rationale.

## Benchmark Policy

If you are modifying core AST compilation, dialect compilers, or Source Generators, run the benchmark suite to ensure no performance regressions:

```bash
dotnet run \
  --project benchmarks/EricksonLopez.SqlBuilder.Benchmarks/EricksonLopez.SqlBuilder.Benchmarks.csproj \
  --configuration Release \
  -- --job short --exporters json markdown
```

Benchmark results are uploaded as artifacts when the `benchmarks` job runs in CI (triggered on `main` push or manual dispatch).

## Branch Naming Convention

| Branch | Purpose |
|--------|---------|
| `main` | Stable. Always releasable. |
| `feature/*` | New features. |
| `release/*` | Release preparation branches. |
| `bugfix/*` or `hotfix/*` | Bug fixes. |

## Commit Convention

We follow [Conventional Commits](https://www.conventionalcommits.org/) informally. While not enforced by tooling, well-structured commit messages improve the auto-generated release notes:

```
feat(postgresql): add ON CONFLICT DO UPDATE support
fix(core): resolve null ref in WhereNode when predicate is null
docs: update packages TFM table
chore: bump Npgsql to 9.0.3
```

## Public API Changes

If your PR adds or removes public API surface (types, methods, properties), you **must** update `PublicAPI.Unshipped.txt` in the affected project. The `Microsoft.CodeAnalysis.PublicApiAnalyzers` package enforces this at build time and will fail CI if unshipped API changes are not declared.

## Pull Request Process

1. Ensure all unit and integration tests pass locally.
2. If adding new public APIs, update `PublicAPI.Unshipped.txt` in the affected project.
3. If changing core AST/compilers, run Stryker and verify mutation score stays above 95%.
4. If changing core/compilers, run benchmarks and confirm no regressions.
5. Follow the [PR template](.github/pull-request-template.md) checklist.
6. Wait for all CI checks to pass: Build, Unit Tests, Integration Tests, Stryker, SonarCloud.

Please review the [Code of Conduct](CODE_OF_CONDUCT.md) before participating.
