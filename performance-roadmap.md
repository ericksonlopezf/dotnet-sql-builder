# Performance Roadmap — EricksonLopez.SqlBuilder

> **Purpose:** Concrete, actionable performance work items organized by impact and dependency.
> All claims must be validated by BenchmarkDotNet. No performance "improvement" is accepted
> without a benchmark proving it. Aligned with [ADR-014](decisions/adr-014-zero-allocation-benchmark-proof.md).
> Last audit: 2026-08-14

---

## Performance Philosophy

> Every performance claim requires a BenchmarkDotNet benchmark with `[MemoryDiagnoser]`.
> "Faster" means: lower median latency + lower allocations vs. baseline.
> Baseline is always **raw SQL string construction** (zero framework overhead).

---

## Current Benchmark Coverage

### Verified in `CategoryABenchmarks.cs`

| Benchmark | Status |
|-----------|--------|
| `Baseline_RawString_SimpleSelect` | ✅ Baseline |
| `Baseline_RawString_ComplexSelect` | ✅ Baseline |
| `Baseline_RawString_Insert` | ✅ Baseline |
| `Baseline_RawString_Update` | ✅ Baseline |
| `Baseline_RawString_Delete` | ✅ Baseline |
| `SqlBuilder_SimpleSelect` | ✅ |
| `SqlBuilder_ComplexSelect` | ✅ |
| `SqlBuilder_SelectWithPagination` | ✅ |
| `SqlBuilder_SelectWithGroupBy` | ✅ |
| `SqlBuilder_SelectWithCte` | ✅ |
| `SqlBuilder_InsertSingleEntity` | ✅ |
| `SqlBuilder_UpdateWithWhere` | ✅ |
| `SqlBuilder_DeleteWithWhere` | ✅ |
| `SqlBuilder_RawQuery` | ✅ |
| `SqlBuilder_KeysetPagination_SingleKey` | ✅ |
| `SqlBuilder_CompositeCursorPagination` | ✅ |
| `SqlBuilder_CaseExpression` | ✅ |

### Benchmark Coverage

| Benchmark Suite | Status | Focus |
|-----------------|--------|-------|
| `CategoryABenchmarks` | ✅ Complete | SELECT, INSERT, UPDATE, DELETE, Raw, Keyset, Composite Seek, Case, AST ImmutableArray vs List (PERF-004) |
| `AotExecutionBenchmarks` | ✅ Complete | AOT SELECT, Metadata extraction, Raw Formattable, stackalloc DML RenderInsert/Update (PERF-001) |
| `SqlExpressionVisitorBenchmarks` | ✅ Complete | AST instantiation, AST visitor, Cold vs Warm `Expression.Compile()` (PERF-002) |
| `CategoryBBenchmarks` | ✅ Complete | Complex CTEs, multi-JOINs, dialect comparisons |
| `CategoryCBenchmarks` | ✅ Complete | Dapper vs AotQueryExecutor execution comparisons |

---

## Performance Work Items

### PERF-001 — Add AOT Path Benchmarks · P1

**Problem:** `AotSqlRendererBase.RenderInsert<T>()` and `RenderUpdate<T>()` are not benchmarked.
The NativeAOT claim needs measurement proof.

**Task:**
```csharp
[Benchmark]
[BenchmarkCategory("CategoryB", "AOT", "Insert")]
public SqlResult AotPath_Insert()
{
    var entity = new Order { Id = 1, TotalAmount = 100m, Status = "active" };
    Span<bool> mask = stackalloc bool[5]; // all columns
    mask.Fill(true);
    return Compiler.RenderInsert<Order>(entity, mask);
}

[Benchmark]
[BenchmarkCategory("CategoryB", "AOT", "Update")]
public SqlResult AotPath_Update()
{
    var entity = new Order { Id = 1, TotalAmount = 150m };
    Span<bool> setMask = stackalloc bool[5];
    Span<bool> whereMask = stackalloc bool[5];
    setMask[3] = true;   // TotalAmount
    whereMask[0] = true; // Id
    return Compiler.RenderUpdate<Order>(entity, setMask, whereMask);
}
```

**Acceptance Criterion:** AOT path allocates ≤20% of the visitor-based path for INSERT/UPDATE.

---

### PERF-002 — Measure `Expression.Compile()` First-Call Cost · P1

**Problem:** The first call to any typed `Where()` expression invokes `Expression.Compile()`.
This cost is unknown and must be quantified before the v2.0 AOT roadmap can be planned.

**Task:** Add a benchmark measuring:
1. Cold first-call cost (compiling the expression)
2. Warm call cost (expression already cached)
3. Comparison vs. raw string WHERE

**Acceptance Criterion:** Document the measured overhead. If cold call > 10µs, add a
`[BenchmarkCategory("ColdPath")]` warning in CI.

---

### PERF-003 — Bulk Insert Benchmark Suite · P1

**Problem:** `BulkBuilder<T>` is a key component for high-throughput scenarios but lacks baseline benchmarks.

**Task:**
```csharp
[Benchmark]
[BenchmarkCategory("CategoryC", "Bulk")]
[Params(100, 1000, 10000)]
public void BulkInsert_SqlBulkCopy(int rowCount)
{
    var orders = GenerateOrders(rowCount);
    _ = new BulkBuilder<Order>()
        .WithStrategy(SqlBulkCopyStrategy)
        .Insert(orders)
        .Build(Compiler);
}

[Benchmark]
[BenchmarkCategory("CategoryC", "Bulk")]
public void BulkInsert_MultiRowValues_1000()
{
    var orders = GenerateOrders(1000);
    _ = Sql.Insert<Order>(null!).Bulk(orders).Build(Compiler);
}
```

**Acceptance Criterion:** Bulk strategies measurably outperform single-row batching at >100 rows.

---

### PERF-004 — `ImmutableArray` vs. `List<T>` in AST Nodes · P2

**Problem:** `ImmutableArray<ISqlNode>` for AST storage was chosen for immutability (ADR-017)
but its allocation profile vs. `ReadOnlyCollection` or `T[]` has not been measured.

**Task:** Micro-benchmark comparing:
- `ImmutableArray<ISqlNode>.Add()` (structural sharing via builder)
- `new ISqlNode[]` array copy

**Acceptance Criterion:** Confirm `ImmutableArray.Add()` does not allocate more than 2× the
raw array approach on the critical path (simple SELECT + 2 WHERE conditions).

---

### PERF-005 — `StringBuilder` Pool in `CompilationContext` · P2

**Problem:** `CompilationContext` creates a new `StringBuilder` per compilation. For high-throughput
scenarios (e.g., 100k queries/sec), this creates Gen0 pressure.

**Proposed fix:** `ArrayPool<char>`-backed `StringBuilder` via `new StringBuilder(capacity)` 
with a pre-warmed initial capacity heuristic based on node count.

**Task:** Benchmark before/after with a 5-JOIN complex SELECT query.

**Acceptance Criterion:** ≥20% reduction in Gen0 allocations for complex queries in tight loop.

---

### PERF-006 — Deterministic SQL Output Enables Compilation Cache · P3

**Problem:** If two identical ASTs always produce the same SQL string (canonical output),
a `ConcurrentDictionary<int, string>` keyed on `AST.GetHashCode()` could cache the compiled SQL.
Currently no caching exists.

**Investigation required:**
1. Verify AST records provide structural `GetHashCode()` (via `record` default equality)
2. Measure hash collision rate
3. Benchmark cache hit vs. re-compilation

**Risk:** Hidden state (ADR-024 warns against automatic caching). If implemented, must be:
- **Opt-in** via compiler configuration
- Bounded with eviction policy
- Documented clearly as a tradeoff

**Decision:** If structural equality of AST records is confirmed, propose a separate
`SqlCompilerCache` optional utility. Do NOT embed in `SqlCompilerBase`.

---

### PERF-007 — Allocation Regression Gates in CI · P1

**Problem:** There is no CI gate preventing allocation regressions. A PR could silently
increase allocations by 5x with no warning.

**Task:** Add a BenchmarkDotNet `BaselineColumn` comparison runner to CI:

```yaml
# GitHub Actions step:
- name: Run allocation regression benchmarks
  run: |
    dotnet run -c Release --project src/Benchmarks \
      --filter *CategoryA* --exporters json \
      --memory --join
    # Compare output against stored baseline
    dotnet tool run benchmark-compare --threshold 10%
```

**Acceptance Criterion:** Any PR that increases allocations by >10% on any CategoryA benchmark
fails the CI check.

---

## Benchmark Run Commands

```bash
# All Category A benchmarks (AST construction + compilation)
dotnet run -c Release --project src/EricksonLopez.SqlBuilder.Benchmarks \
  --filter *CategoryA* \
  --memory --join

# Specific benchmark group
dotnet run -c Release --project src/EricksonLopez.SqlBuilder.Benchmarks \
  --filter *BulkInsert* \
  --memory --join

# NativeAOT publish test
dotnet publish src/EricksonLopez.SqlBuilder.Benchmarks \
  -c Release -r win-x64 --self-contained \
  -p:PublishAot=true

# Compare against baseline
dotnet run -c Release --project src/EricksonLopez.SqlBuilder.Benchmarks \
  --filter *CategoryA* \
  --exporters json --baselines previous_results.json
```

---

## Performance Targets (V1.x)

| Operation | Target (Gen0 alloc) | Target (Time) | Status |
|-----------|--------------------|----|--------|
| Simple SELECT compilation | ≤ 1 object | ≤ 500ns | Unknown — needs benchmark |
| Complex SELECT (5 JOINs, 3 WHERE) | ≤ 5 objects | ≤ 2µs | Unknown |
| Single entity INSERT (AOT path) | 0 | ≤ 200ns | Unknown |
| Bulk INSERT 1000 rows (multi-row VALUES) | ≤ 1 builder | ≤ 500µs | Unknown |
| `IDataReader` hydration via SrcGen | 0 (stackalloc) | ≤ 100ns | Unknown |
| `Expression.Compile()` warm call | 0 | ≤ 50ns | Unknown |

---

## Dependencies on Technical Debt

| PERF item | Blocked by |
|-----------|-----------|
| PERF-003 | TD-007 (BulkBuilder abstract methods) |
| PERF-002 | TD-005 (Expression.Compile documentation) |
| PERF-006 | Requires ADR on opt-in caching |

---

*Run benchmarks in `Release` mode only. Debug mode results are meaningless for allocation analysis.*
