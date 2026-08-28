# Level 3: Real Use Cases

## Overview

Covers advanced real-world SQL patterns: Common Table Expressions (CTEs), Window Functions, Analytical Aggregations (CUBE/ROLLUP), and Keyset Pagination.

## Key APIs Covered

- `.CTE("name", subquery)`.
- `Window.RowNumber()`, `Window.Rank()`, `Window.Sum()`.
- `.GroupByCube(...)` and `.GroupByRollup(...)`.
- `WhereColumns(c1, op, c2)` for column-to-column comparisons.
- `PagedList` and `CountedPagedList`.
