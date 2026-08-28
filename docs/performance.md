# Performance Benchmarks

EricksonLopez.SqlBuilder is designed for high-performance SQL generation with a focus on minimal allocations and Native AOT compatibility.

> [!WARNING]
> The benchmark results in this document are **illustrative targets**, not outputs from an authoritative BenchmarkDotNet run committed to this repository. The actual results will vary by hardware, .NET version, and workload. To obtain accurate, reproducible results, run the benchmark suite locally (see below).

## Running Benchmarks

The `EricksonLopez.SqlBuilder.Benchmarks` project uses [BenchmarkDotNet](https://benchmarkdotnet.org/).

```bash
# Run from the repository root — Release configuration required
dotnet run \
  --project src/EricksonLopez.SqlBuilder.Benchmarks/EricksonLopez.SqlBuilder.Benchmarks.csproj \
  --configuration Release \
  -- --job short --exporters json markdown
```

Benchmark results are uploaded as workflow artifacts when the `benchmarks` job runs in CI (on `main` push or manual dispatch via `workflow_dispatch` with `run_benchmarks=true`).

## Design Goals

The core query compiler is designed to achieve:

- **Zero allocations** on repeated identical query paths (enabled by immutable AST reuse and `ImmutableArray` nodes)
- **Compile-time entity metadata** via Source Generators — no `System.Reflection` at runtime
- **AOT safety** — no `Emit`, no `DynamicMethod`, no dynamic proxy

These goals are validated by the benchmark suite and enforced by [ADR-014](decisions/adr-014-zero-allocation-benchmark-proof.md), which requires benchmark proof before any zero-allocation claim is published.

## Key Performance Factors

| Factor | Mechanism |
|--------|-----------|
| Zero reflection | `[SqlEntity]` entities use compile-time generated metadata (`IStaticEntityMetadata<T>`) |
| Immutable AST | Query nodes (`SelectQuery<T>`, `WhereNode`, etc.) are immutable records — safe to cache and reuse |
| `ImmutableArray` | Core AST uses `System.Collections.Immutable.ImmutableArray<T>` for structural sharing |
| AOT execution | `AotQueryExecutor` bypasses Dapper entirely — fully reflection-free hot path |

See [ADR-013](decisions/adr-013-aot-guarantees.md) for the full specification of AOT guarantees and [ADR-014](decisions/adr-014-zero-allocation-benchmark-proof.md) for the policy on zero-allocation claims.

