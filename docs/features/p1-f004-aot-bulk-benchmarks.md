# Feature Implementation: P1-F004 - Benchmark Suite for AOT & Bulk Pipelines

## Metadata
* **ID:** `P1-F004`
* **Title:** BenchmarkDotNet Suite for NativeAOT & Bulk Pipeline Baselines
* **Layer / Component:** `src/EricksonLopez.SqlBuilder.Benchmarks`
* **Priority:** P1 (Performance Baseline & Verification)
* **Status:** `COMPLETED`
* **Test Coverage:** Compiles and runs with BenchmarkDotNet runner (`net10.0`)

---

## 1. Context & Motivation
Following the performance roadmap guidelines (ADR-013 & performance-roadmap.md), all performance comparisons must be strictly empirical, internal, and baseline-oriented (measuring against Raw SQL strings and NativeAOT zero-reflection execution paths, never against competitor names).

---

## 2. Technical Implementation
Added `AotExecutionBenchmarks.cs` covering:
* **`Baseline_RawString`**: Pure string construction baseline (0 allocations reference).
* **`SqlBuilder_AotSelect_Compile`**: Typed AST generation and compilation for SQL Server without reflection.
* **`SourceGen_GetColumnNames`**: Source-generated `[SqlEntity]` metadata retrieval.
* **`SourceGen_GetValues`**: Source-generated zero-reflection entity property array retrieval.
* **`RawQuery_FormattableString_Compile`**: NativeAOT parameterized interpolated query compilation.

Bulk benchmarks in `BulkOperationBenchmarks.cs` benchmark parameterized batch operations across batch sizes (10, 100, 1000).

---

## 3. Verification & Execution
Project builds cleanly with 0 errors:
```bash
dotnet build src/EricksonLopez.SqlBuilder.Benchmarks/EricksonLopez.SqlBuilder.Benchmarks.csproj
```
Can be executed via:
```bash
dotnet run -c Release --project src/EricksonLopez.SqlBuilder.Benchmarks -- --filter *AotExecution*
```
