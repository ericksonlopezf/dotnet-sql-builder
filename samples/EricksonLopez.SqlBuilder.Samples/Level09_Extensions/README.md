# Level 9: Extensions & Diagnostics

## Overview

Demonstrates advanced extensions: SQL result inspection, structural fingerprinting, contract verification, projection, and functional result patterns.

## Key APIs Covered

- `SqlResult` compiled SQL and parameter inspection.
- `GetFingerprint()` for query cache keys.
- `GetContract()` for table/column schema contract validation.
- `ProjectTo<TSource, TResult>()` for lightweight DTO projection.
- `ToResultAsync<T>()` and `ToPagedListAsync<T>()` (Result Pattern).
- `WithTag("tag")` for OpenTelemetry trace tagging.
- `Sql.Raw(FormattableString)` for safe parameterized SQL.
