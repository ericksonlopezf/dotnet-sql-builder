# ADR-014: Zero-Allocation Claims Require Benchmark Proof

## Status
Accepted

## Date
2026-08-12

## Context
"Zero-allocation" and "high performance" are common marketing claims in .NET library ecosystems. Without reproducible benchmark evidence, these claims are meaningless — and misleading if the hot path actually allocates.

## Problem
- Claiming zero-allocation without evidence creates credibility risk
- Benchmarks must be reproducible across CI environments
- "Zero-allocation" is a path-specific property — it only applies to specific code paths under specific conditions

## Decision

### Rule 1: No undocumented performance claims
Documentation and README may only claim "zero-allocation" or "high performance" for paths that have a corresponding BenchmarkDotNet report showing:
- `Allocated: 0 B`
- `Gen0: 0` (no GC pressure)

### Rule 2: Verified zero-allocation paths (as of v1.0)
| Path | Condition | Evidence |
|------|-----------|---------|
| `BulkBuilder<T>` with `IStaticEntityMetadata<T>` | AOT path, no expression compilation | Benchmark: `Build_Bulk_1000_AOT` |
| `AotSqlRendererBase.Render()` | StringBuilder pool, no LINQ | Benchmark: `Build_Select_AOT` |

### Rule 3: NOT zero-allocation (explicitly documented)
| Path | Reason |
|------|--------|
| First call to expression-based WHERE | `Expression.Compile()` allocates a delegate |
| `SqlExpressionVisitor` node traversal | AST node creation allocates |
| Dapper `QueryAsync<T>` materialization | Dapper uses reflection; allocates per row |

### Rule 4: Regression gate
Any release that degrades a previously verified zero-allocation path by >0 bytes fails the release benchmark gate.

### Rule 5: Benchmark location
All claims must reference a benchmark in `tests/EricksonLopez.SqlBuilder.Benchmarks/` with a committed baseline JSON in `benchmarks/baselines/`.

## Benchmark Acceptance Criteria

| Benchmark | Pass | Warn | Fail |
|-----------|------|------|------|
| `Build_Select_Simple` (allocated) | 0 B (cached) | > 0 B first run only | > 100 B/call |
| `Build_Bulk_1000_AOT` (allocated) | 0 B | — | Any allocation |
| `Build_Select_Simple` (time) | < 500 ns | 500 ns – 2 µs | > 2 µs |
| E2E vs raw Dapper overhead | < 10% | 10–25% | > 25% |
| Release regression threshold | < 5% | 5–10% | > 10% (blocks release) |

## Consequences

### Positive
- ✅ Credible performance claims backed by reproducible evidence
- ✅ Regression detection in CI
- ✅ Users can trust documented performance characteristics

### Negative
- ❌ Benchmarks require maintenance as code evolves
- ❌ CI benchmark environments vary; baselines may need environment-specific calibration

## Reconsideration Criteria
If BenchmarkDotNet adds automatic regression detection with configurable thresholds, integrate it into the CI pipeline directly.

## References
- [FEATURE_MATRIX.md §22 — Benchmark Matrix](../../FEATURE_MATRIX.md)
- `tests/EricksonLopez.SqlBuilder.Benchmarks/`
- [docs/Performance.md](../Performance.md)
- [ADR-006: Source Generator Strategy](./adr-006-source-generator-strategy.md)
