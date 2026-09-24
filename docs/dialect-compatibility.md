# Dialect Compatibility Matrix — EricksonLopez.SqlBuilder

`EricksonLopez.SqlBuilder` compiles portable, strongly-typed Abstract Syntax Tree (AST) query representations into high-performance, dialect-specific SQL across six database engines:
- **SS**: Microsoft SQL Server / Azure SQL (`EricksonLopez.SqlBuilder.SqlServer`)
- **PG**: PostgreSQL (`EricksonLopez.SqlBuilder.PostgreSql`)
- **MY**: MySQL (`EricksonLopez.SqlBuilder.MySql`)
- **MA**: MariaDB (`EricksonLopez.SqlBuilder.MariaDb`)
- **LT**: SQLite (`EricksonLopez.SqlBuilder.Sqlite`)
- **OR**: Oracle Database (`EricksonLopez.SqlBuilder.Oracle`)

---

## Dialect Identity & Compiler Defaults

| Dialect | Package Name | Primary Compiler Class | Identifier Quoting | Parameter Style | Parameter Ceiling |
|---|---|---|---|---|---|
| **SQL Server** | `EricksonLopez.SqlBuilder.SqlServer` | `SqlServerCompiler` | `[identifier]` | `@p0`, `@p1` | 2,100 parameters |
| **PostgreSQL** | `EricksonLopez.SqlBuilder.PostgreSql` | `PostgreSqlCompiler` | `"identifier"` | `@p0`, `@p1` (or `$1`, `$2` native) | 65,535 parameters |
| **MySQL** | `EricksonLopez.SqlBuilder.MySql` | `MySqlCompiler` | `` `identifier` `` | `@p0`, `@p1` | 65,535 parameters |
| **MariaDB** | `EricksonLopez.SqlBuilder.MariaDb` | `MariaDbCompiler` | `` `identifier` `` | `@p0`, `@p1` | 65,535 parameters |
| **SQLite** | `EricksonLopez.SqlBuilder.Sqlite` | `SqliteCompiler` | `"identifier"` | `@p0`, `@p1` | 999 parameters |
| **Oracle** | `EricksonLopez.SqlBuilder.Oracle` | `OracleCompiler` | `"IDENTIFIER"` (UPPERCASE) | `:p0`, `:p1` | 65,535 parameters |

---

## Classification Key

| Symbol | Class | Meaning |
|---|---|---|
| 🌐 | **Universal** | Identical API surface, syntax, and semantics across all database engines. |
| 🔵 | **DialectSpecific** | Syntax differs per engine, but exposed natively by the respective dialect package. |
| 🟡 | **DialectEmulated** | Syntax translated or emulated by the compiler to achieve the desired semantic. |
| ❌ | **Unsupported** | Capability not available in this database engine. |
| ⛔ | **UnsafeToAbstract** | Feature intentionally rejected from a generic abstraction to prevent false safety or data anomalies. |

---

## 1. Identifier Quoting & Case Sensitivity

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---|---|---|---|---|---|---|---|
| Identifier Quoting | `[x]` | `"x"` | `` `x` `` | `` `x` `` | `"x"` | `"X"` (UPPER) | 🔵 DialectSpecific |
| Case Sensitivity Default | Insensitive | Case-preserved in quotes | Insensitive | Insensitive | Insensitive | UPPERCASE unless quoted | 🔵 DialectSpecific |
| Schema Qualified (`schema.table`) | `[dbo].[Table]` | `"public"."Table"` | `` `db`.`Table` `` | `` `db`.`Table` `` | `"main"."Table"` | `"SCHEMA"."TABLE"` | 🌐 Universal |

---

## 2. Pagination & Offset Traversal

| Pagination Strategy | SS | PG | MY | MA | LT | OR | Class |
|---|---|---|---|---|---|---|---|
| `.Limit(n)` | 🟡 `FETCH NEXT n ROWS ONLY` | ✅ `LIMIT n` | ✅ `LIMIT n` | ✅ `LIMIT n` | ✅ `LIMIT n` | 🟡 `FETCH NEXT n ROWS ONLY` (12c+) | 🟡 DialectEmulated |
| `.Offset(n)` | ✅ `OFFSET n ROWS` | ✅ `OFFSET n` | ✅ `OFFSET n` | ✅ `OFFSET n` | ✅ `OFFSET n` | 🟡 `OFFSET n ROWS` (12c+) | 🟡 DialectEmulated |
| `.Page(page, size)` | ✅ Native Offset-Fetch | ✅ `LIMIT/OFFSET` | ✅ `LIMIT/OFFSET` | ✅ `LIMIT/OFFSET` | ✅ `LIMIT/OFFSET` | ✅ `OFFSET...FETCH` (12c+) | 🟡 DialectEmulated |
| Oracle 11g `ROWNUM` Paging | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ `SELECT * FROM (SELECT a_.*, ROWNUM...)` | 🔵 DialectSpecific |
| Keyset Cursor (`.SeekAfter()`, `.SeekBefore()`) | ✅ $O(1)$ | ✅ $O(1)$ | ✅ $O(1)$ | ✅ $O(1)$ | ✅ $O(1)$ | ✅ $O(1)$ | 🌐 Universal |
| Window Function Paging (`.WindowPage()`) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |

---

## 3. ORDER BY & NULL Ordering

| Ordering Syntax | SS | PG | MY | MA | LT | OR | Class |
|---|---|---|---|---|---|---|---|
| Standard `ORDER BY col ASC/DESC` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |
| `NULLS FIRST` (Native) | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ | 🔵 DialectSpecific |
| `NULLS LAST` (Native) | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ | 🔵 DialectSpecific |
| `NULLS FIRST / LAST` Emulation | 🟡 `CASE WHEN col IS NULL...` | — (Native) | 🟡 `CASE WHEN col IS NULL...` | 🟡 `CASE WHEN col IS NULL...` | 🟡 `CASE WHEN col IS NULL...` | — (Native) | 🟡 DialectEmulated |

---

## 4. JOIN Types & Lateral Navigation

| JOIN Feature | SS | PG | MY | MA | LT | OR | Class |
|---|---|---|---|---|---|---|---|
| `INNER`, `LEFT`, `RIGHT`, `FULL` JOIN | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |
| `CROSS JOIN` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |
| `CROSS APPLY` | ✅ | 🟡 `LATERAL` | ❌ | ❌ | ❌ | ❌ | 🟡 DialectEmulated |
| `OUTER APPLY` | ✅ | 🟡 `LEFT JOIN LATERAL ... ON true` | ❌ | ❌ | ❌ | ❌ | 🟡 DialectEmulated |
| Explicit `LATERAL JOIN` | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | 🔵 DialectSpecific |
| Multi-Table Joins (2 to 7 Entities) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |
| Extended Multi-Mapping (8+ Entities) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |

---

## 5. DML Mutations, UPSERT & Conflict Resolution

| Mutation Feature | SS | PG | MY | MA | LT | OR | Class |
|---|---|---|---|---|---|---|---|
| `INSERT INTO ... VALUES` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |
| Multi-Row Batch `VALUES (…),(…)` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ (requires `INSERT ALL`) | 🔵 DialectSpecific |
| `INSERT INTO ... SELECT` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |
| `ON CONFLICT (cols) DO NOTHING` | ❌ | ✅ | 🟡 `ON DUPLICATE KEY UPDATE` | 🟡 `ON DUPLICATE KEY UPDATE` | ✅ | ❌ | 🟡 DialectEmulated |
| `ON CONFLICT (cols) DO UPDATE SET` | ❌ | ✅ | 🟡 `ON DUPLICATE KEY UPDATE` | 🟡 `ON DUPLICATE KEY UPDATE` | ✅ | ❌ | 🟡 DialectEmulated |
| `ON DUPLICATE KEY UPDATE` | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 🔵 DialectSpecific |
| Cross-Dialect Generic `MERGE` | ⛔ UnsafeToAbstract | ⛔ UnsafeToAbstract | ⛔ UnsafeToAbstract | ⛔ UnsafeToAbstract | ⛔ UnsafeToAbstract | ⛔ UnsafeToAbstract | ⛔ UnsafeToAbstract (ADR-025) |
| Optimistic Concurrency Token Update | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |

---

## 6. Returning & Output Clauses

| Clause Feature | SS | PG | MY | MA | LT | OR | Class |
|---|---|---|---|---|---|---|---|
| Unified `.Returning()` AST Node | 🟡 `OUTPUT INSERTED.*` | ✅ `RETURNING ...` | ❌ (Throws) | ✅ `RETURNING ...` (10.5+) | ✅ `RETURNING ...` | 🟡 `RETURNING ... INTO` | 🟡 DialectEmulated |
| Return Generated Identity on INSERT | ✅ `OUTPUT INSERTED.Id` | ✅ `RETURNING "Id"` | ❌ (Scope Identity) | ✅ `RETURNING Id` | ✅ `RETURNING rowid` | ✅ `RETURNING ID INTO :out` | 🔵 DialectSpecific |
| Return Modified Columns on UPDATE | ✅ `OUTPUT INSERTED.*` | ✅ `RETURNING *` | ❌ | ✅ `RETURNING *` | ✅ `RETURNING *` | ✅ `RETURNING ... INTO` | 🔵 DialectSpecific |
| Return Deleted Columns on DELETE | ✅ `OUTPUT DELETED.*` | ✅ `RETURNING *` | ❌ | ✅ `RETURNING *` | ✅ `RETURNING *` | ✅ `RETURNING ... INTO` | 🔵 DialectSpecific |

---

## 7. Advanced SQL DSL (CTEs, Window Functions & Set Operations)

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---|---|---|---|---|---|---|---|
| Non-Recursive Common Table Expressions (CTEs) | ✅ | ✅ | ✅ (8.0+) | ✅ (10.2+) | ✅ (3.8+) | ✅ | 🌐 Universal |
| Recursive Common Table Expressions | ✅ | ✅ | ✅ (8.0+) | ✅ (10.2+) | ✅ (3.8+) | ✅ | 🌐 Universal |
| Materialization Hints (`MATERIALIZED` / `NOT MATERIALIZED`) | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | 🔵 DialectSpecific (ADR-037) |
| Window Functions (`ROW_NUMBER`, `RANK`, `DENSE_RANK`) | ✅ | ✅ | ✅ (8.0+) | ✅ (10.2+) | ✅ | ✅ | 🌐 Universal |
| Offset Window Functions (`LAG`, `LEAD`, `FIRST_VALUE`, `LAST_VALUE`) | ✅ | ✅ | ✅ (8.0+) | ✅ (10.2+) | ✅ | ✅ | 🌐 Universal |
| Analytical Window Functions (`NTILE`, `PERCENT_RANK`, `CUME_DIST`) | ✅ | ✅ | ✅ (8.0+) | ✅ (10.2+) | ✅ | ✅ | 🌐 Universal |
| Window Function `FILTER (WHERE ...)` Clause | ❌ | ✅ | ❌ | ❌ | ✅ | ❌ | 🔵 DialectSpecific (ADR-035) |
| Grouping Sets (`GROUPING SETS`, `ROLLUP`, `CUBE`) | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 🔵 DialectSpecific (ADR-034) |
| Set Operations (`UNION`, `UNION ALL`) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 Universal |
| Set Operations (`INTERSECT`, `EXCEPT`) | ✅ | ✅ | ✅ (8.0+) | ✅ (10.3+) | ✅ | ✅ (`MINUS`) | 🌐 Universal |

---

## 8. High-Performance Bulk Data Ingestion

| Bulk Strategy | SS | PG | MY | MA | LT | OR | Description |
|---|---|---|---|---|---|---|---|
| **SQL Server TDS Streaming** | ✅ `SqlBulkCopy` | ❌ | ❌ | ❌ | ❌ | ❌ | Direct Tabular Data Stream protocol via `Microsoft.Data.SqlClient.SqlBulkCopy`. |
| **PostgreSQL Binary COPY** | ❌ | ✅ Binary `COPY` | ❌ | ❌ | ❌ | ❌ | Maximum-speed binary stream ingestion via `NpgsqlBinaryImporter`. |
| **MySQL / MariaDB Batching** | ❌ | ❌ | ✅ `MySqlBatch` | ✅ `MySqlBatch` | ❌ | ❌ | Pipelined batch execution minimizing roundtrips via `MySqlConnector`. |
| **Oracle Native Bulk Copy** | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ `OracleBulkCopy` | Direct path bulk data loading via `Oracle.ManagedDataAccess.Client.OracleBulkCopy`. |
| **SQLite Transactional Batch** | ❌ | ❌ | ❌ | ❌ | ✅ WAL Batch | ❌ | Fast single-transaction batched statements in Write-Ahead Logging mode. |

---

## 9. Semi-Structured Data, Arrays & Specialized Types

| Capability | SS | PG | MY | MA | LT | OR | Description |
|---|---|---|---|---|---|---|---|
| **JSON / JSONB Storage & Querying** | 🟡 `JSON_VALUE` / `JSON_QUERY` | ✅ Native `jsonb` & `->` / `->>` operators | ✅ Native `JSON` & `->` / `->>` | ✅ Native `JSON` functions | 🟡 `json_extract()` | ✅ Native `JSON` (21c+) | PostgreSQL uses binary indexed JSON (`jsonb`); integrated with `JsonbTypeHandler<T>` in `EricksonLopez.SqlBuilder.Dapper`. |
| **Array Types & Unnesting** | ❌ (requires `STRING_SPLIT`) | ✅ Native arrays (`text[]`, `int[]`) & `UNNEST()` | ❌ | ❌ | ❌ | 🟡 `VARRAY` / Nested Tables | PostgreSQL supports native first-class multidimensional arrays and relational decomposition via `UNNEST()`. |
| **Vector Similarity Search (pgvector)** | ❌ | ✅ `<->` L2, `<=>` Cosine, `<#>` IP | ❌ | ❌ | ❌ | 🟡 AI Vector Search (23ai) | PostgreSQL vector distance calculations and cosine similarity indexes (`hnsw`, `ivfflat`) via raw expression helpers and type mappings. |
| **Full-Text Search DSL** | 🟡 `CONTAINS` / `FREETEXT` | ✅ `to_tsvector` / `to_tsquery` / `@@` | 🟡 `MATCH(...) AGAINST(...)` | 🟡 `MATCH(...) AGAINST(...)` | 🟡 `FTS5` virtual tables | 🟡 `CONTAINS` (Oracle Text) | Dialect-specific full-text indexing and query syntax abstractions. |

---

## 10. Architectural Boundaries & Safe Abstractions

1. **Why generic `Sql.Merge<T>()` is deprecated (ADR-025)**: SQL Server's `MERGE` statement has known concurrency bugs under high concurrency (e.g. deadlocks, duplicate key violations even with unique indexes). An ORM-like abstraction hiding these differences creates false safety. The ecosystem requires developers to use dialect-native primitives.
2. **Why raw SQL strings are validated**: Roslyn analyzers (`ESQL002`, `ESQL011`, `ELSB004`) enforce that identifier names and parameter values are never built via unvalidated string concatenation.

