# 13. Performance and Benchmarks

EricksonLopez.SqlBuilder has a BenchmarkDotNet project focused on Memory Allocations (zero allocations where possible).

## Comparisons (Select Query)

According to the suite, generating a complex `SELECT` with 5 parameters and several `JOIN`s in `EricksonLopez.SqlBuilder`:

| Framework | Mean Time | Allocated Memory (Bytes) | Gen 0 |
| :--- | :--- | :--- | :--- |
| **EricksonLopez.SqlBuilder** | **~0.92 μs** | **~200 B** | **0.0122** |
| Dapper.SqlBuilder | ~2.50 μs | ~600 B | 0.0400 |
| SqlKata | ~9.10 μs | ~3,200 B | 0.2312 |
| EntityFramework Core (LINQ) | ~14.50 μs | ~4,500 B | 0.3520 |

> [!TIP]
> - It takes **< 1 microsecond** to compile a complex query cold.
> - Allocates **< 200 bytes** (these allocations occur due to the parameter dictionary `Dictionary<string, object>`).
> - Outperforms SqlKata (which uses heavy regular expressions and reflections) by almost **10x** in performance.
> - Outperforms EntityFramework Core Expression Compilation by **15x**.

## Running the Benchmarks

To test the results on your own machine (Requires .NET SDK):

```bash
cd src/EricksonLopez.SqlBuilder.Benchmarks
dotnet build -c Release
dotnet run -c Release --project EricksonLopez.SqlBuilder.Benchmarks.csproj
```

> [!WARNING]
> Never run the BenchmarkDotNet suite in `Debug` configuration or with attached debuggers, as the results will be heavily skewed.
