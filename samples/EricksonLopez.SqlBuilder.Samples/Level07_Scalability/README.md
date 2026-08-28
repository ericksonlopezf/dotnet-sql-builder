# Level 7: Scalability & Performance

## Overview

Focuses on scalable query patterns: thread-safe query reuse, keyset/cursor pagination, `ROW_NUMBER()` window paging, and AOT compilation caching.

## Key APIs Covered

- Thread safety: sharing immutable `SelectQuery<T>` instances across concurrent tasks.
- Keyset cursor pagination: `.Seek()`, `.SeekAfter()`, `.SeekBefore()`.
- `WindowPage()` for deep pagination.
- `OrderByDynamic()` with parameter safety.
- `QueryPagedRawAsync()`.
