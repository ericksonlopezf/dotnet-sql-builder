# ADR-046: Bulk Identity Retrieval Boundary and Client-Generated Keys Strategy (GAP-10 / TD-016)

## Status
Accepted — August 2026

## Context
During the functional parity audit (GAP-10 / TD-016), returning generated primary keys (`IReadOnlyList<TKey>`) from `BulkInsertAsync` / `IBulkStrategy<T>` was evaluated.

In traditional database designs relying on server-side `IDENTITY` or `SERIAL` columns, developers frequently request the database-generated primary keys after inserting a batch of parent entities to populate foreign keys in dependent child entities.

## Analysis of Driver Protocols & Native Bulk Capabilities

1. **PostgreSQL (`COPY FROM STDIN` via `NpgsqlBinaryImporter`)**:
   - The PostgreSQL binary `COPY` protocol streams rows directly to disk/table storage with maximum throughput (~800K rows/sec).
   - At the protocol level, `COPY` does **not** support `RETURNING id`.
   - Returning IDs would require degrading from binary `COPY` to multi-row `INSERT ... RETURNING id`, causing a 5x-10x throughput penalty.

2. **SQL Server (`SqlBulkCopy`)**:
   - Microsoft ADO.NET `SqlBulkCopy` streams TDS packets directly to the engine without executing a statement pipeline that returns result sets.
   - Returning generated identity ranges safely requires creating staging tables and running `INSERT INTO RealTable ... OUTPUT inserted.Id SELECT ... FROM #StagingTable`, which doubles disk and transaction log I/O.

3. **MySQL & SQLite**:
   - MySQL `LAST_INSERT_ID()` returns only the first ID in a multi-row insert. SQLite `last_insert_rowid()` returns only the final ID. Both are prone to race conditions under concurrent workloads.

## Decision

1. **Do NOT compromise `IBulkStrategy` throughput with artificial staging tables or protocol downgrades:**
   - `IBulkStrategy<T>` remains strictly focused on maximum raw throughput (`COPY`, `SqlBulkCopy`) and returns `Task<int>` (rows affected).

2. **Standard & Moderate Batch Identity Retrieval is Already Solved by `InsertQuery<T>.Returning(...)`:**
   - For standard single-entity or small/medium batch inserts where the database generates IDs, consumers use the existing first-class DML API:
     - SQL Server: `.Returning(x => x.Id)` emits `OUTPUT inserted.Id`
     - PostgreSQL / SQLite / Oracle: `.Returning(x => x.Id)` emits `RETURNING id`

3. **High-Performance Multi-Table Bulk Strategy: Client-Generated Deterministic Keys:**
   - For high-volume multi-table bulk operations (e.g., 10,000 orders with 50,000 order lines), the official architectural recommendation is **Client-Generated Keys**:
     - **UUIDv7 / Sequential GUIDs**: Sequential, index-friendly, zero-collision.
     - **HiLo / Snowflake IDs**: Pre-allocated sequential integer/long IDs.
   - **Benefit**: The application knows all parent and child IDs *prior* to network transmission, enabling concurrent `BulkInsertAsync` across parent and child tables with zero roundtrips and zero server-side sequence locking.

## Consequences
- `IBulkStrategy` remains lightweight, zero-allocation, and true to native driver streaming capabilities.
- Resolves and closes TD-016 without introducing breaking API changes or artificial complexity.
- Documentation explicitly guides users on choosing between `InsertQuery.Returning()` (DB-generated IDs) vs. `BulkInsertAsync` + UUIDv7 (High-throughput client-generated IDs).
