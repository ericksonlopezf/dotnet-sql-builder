```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.9168)
Unknown processor
.NET SDK 10.0.400
  [Host]     : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-MBCKNW : .NET 10.0.11 (10.0.1126.37416), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=3  LaunchCount=1  WarmupCount=1  

```
| Method                   | Mean       | Error        | StdDev    | Gen0   | Allocated |
|------------------------- |-----------:|-------------:|----------:|-------:|----------:|
| SqlBuilder_SimpleSelect  |   544.0 ns |     28.80 ns |   1.58 ns | 0.0381 |   1.92 KB |
| SqlBuilder_ComplexSelect | 1,305.3 ns |  4,479.81 ns | 245.55 ns | 0.0687 |   3.65 KB |
| SqlBuilder_GroupBy       | 1,567.2 ns | 14,110.14 ns | 773.42 ns | 0.0458 |   2.42 KB |
