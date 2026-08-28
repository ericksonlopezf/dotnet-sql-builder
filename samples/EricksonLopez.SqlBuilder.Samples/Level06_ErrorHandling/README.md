# Level 6: Error Handling & Resilience

## Overview

Demonstrates resilience pipelines, retry strategies with exponential backoff, optimistic concurrency checks, and diagnostics.

## Key APIs Covered

- Execution retry policies with transient error detection.
- `.WithConcurrencyToken(x => x.Version, expected)`.
- `ExecuteWithConcurrencyCheckAsync<T>()` and `DbConcurrencyException`.
- Output / Returning clauses for generated keys.
- OpenTelemetry metrics and ActivitySource diagnostics.
