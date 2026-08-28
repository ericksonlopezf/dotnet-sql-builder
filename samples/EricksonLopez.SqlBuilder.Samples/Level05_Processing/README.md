# Level 5: Batch Processing & Streaming

## Overview

Demonstrates high-throughput data processing: bulk inserts, zero-reflection NativeAOT queries, sequential LOB streaming, and cooperative task cancellation.

## Key APIs Covered

- `BulkInsertAsync<T>()` for multi-row batch execution.
- `QueryAotAsync<T>(mapper)` for zero-reflection data mapping.
- `QuerySequentialAsync<T>()` with `CommandBehavior.SequentialAccess` for large blobs/text.
- `CancellationToken` integration on all asynchronous operations.
- `BulkDeleteAsync<T>()`.
