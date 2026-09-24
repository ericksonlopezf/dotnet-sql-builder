# EricksonLopez.SqlBuilder.Benchmarks

BenchmarkDotNet performance and regression testing suite for **EricksonLopez.SqlBuilder**.

[![CI](https://img.shields.io/github/actions/workflow/status/ericksonlopezf/dotnet-sql-builder/ci.yml?branch=main&label=CI)](https://github.com/ericksonlopezf/dotnet-sql-builder/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](../../LICENSE)
[![Target Framework](https://img.shields.io/badge/.NET-net10.0-blue.svg)](https://dotnet.microsoft.com)

This project evaluates the AST compilation latency, memory allocation invariants, and materialization throughput of `EricksonLopez.SqlBuilder` against raw string formatting, Dapper, SqlKata, RepoDb, and EF Core.

---

## Running Benchmarks Locally

Execute benchmarks in `Release` configuration from the repository root:

```bash
# Run all benchmarks with short job for rapid developer feedback
dotnet run \
  --project benchmarks/EricksonLopez.SqlBuilder.Benchmarks/EricksonLopez.SqlBuilder.Benchmarks.csproj \
  --configuration Release \
  --framework net10.0 \
  -- --job short --filter "*" --memory

# Run a specific benchmark category (e.g., AST compilation)
dotnet run \
  --project benchmarks/EricksonLopez.SqlBuilder.Benchmarks/EricksonLopez.SqlBuilder.Benchmarks.csproj \
  --configuration Release \
  --framework net10.0 \
  -- --job short --filter "*SelectBenchmark*"
```

---

## Automated Quality Gates

In pull requests, GitHub Actions executes [`benchmark-regression-gate.yml`](../../.github/workflows/benchmark-regression-gate.yml) and evaluates results with [`scripts/verify-benchmark-gate.ps1`](../../scripts/verify-benchmark-gate.ps1) against `benchmarks/results/baseline.json`:

1. **Zero Heap Allocation Invariant**: Core AST compilation and query combinators must allocate **0 B** on hot paths.
2. **Latency Regression Limit**: Mean execution time must not regress by more than **5%** compared to baseline.

---

## Documentation & References

- [Performance Guide & Benchmark Specs](../../docs/performance.md)
- [Architecture & Design Invariants](../../docs/architecture.md)
- [Best Practices](../../docs/best-practices.md)
- [Getting Started](../../docs/getting-started.md)
- [Contributing Guide](../../CONTRIBUTING.md)
- [Security Policy](../../SECURITY.md)
- [Support Guidelines](../../SUPPORT.md)
- [MIT License](../../LICENSE)
