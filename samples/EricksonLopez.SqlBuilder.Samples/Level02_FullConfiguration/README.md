# Level 2: Full Configuration

## Overview

Demonstrates advanced configuration options, custom `ITypeHandler` JSON serialization, diagnostics/logging setup, and AOT compilation.

## Key APIs Covered

- `SqlBuilderDiagnostics.LoggerFactory` and `SqlBuilderDiagnostics.SlowQueryThresholdMs`.
- `SqlMapper.AddTypeHandler()` and `Sql.RegisterTypeHandler<T>()`.
- `Sql.Insert<T>().Build(compiler)` for explicit query compilation.
- `Sql.Update<T>().Set(x => x.Price, ...).Where(...)`.
- `OrderByDynamic("ColumnName", descending: true)`.
