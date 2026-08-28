# Dialect Compatibility Matrix — EricksonLopez.SqlBuilder

> **Scope:** This document details per-feature, per-dialect behavior for SQL builders, compilers,
> and execution integrations. Verified against compiler source code.
> Last audit: 2026-08-14

---

## Dialect Identity

| Dialect | Package | Compiler Class | Identifier Quoting | Parameter Style |
|---------|---------|---------------|--------------------|----------------|
| SQL Server | `EricksonLopez.SqlBuilder.SqlServer` | `SqlServerCompiler` | `[identifier]` | `@p0`, `@p1` (max 2100) |
| PostgreSQL | `EricksonLopez.SqlBuilder.PostgreSql` | `PostgreSqlCompiler` | `"identifier"` | `@p0`, `@p1` |
| MySQL/MariaDB | `EricksonLopez.SqlBuilder.MySql` | `MySqlCompiler` | `` `identifier` `` | `@p0`, `@p1` |
| SQLite | `EricksonLopez.SqlBuilder.Sqlite` | `SqliteCompiler` | `"identifier"` | `@p0`, `@p1` |
| Oracle | `EricksonLopez.SqlBuilder.Oracle` | `OracleCompiler` | `"IDENTIFIER"` (UPPERCASE) | `:p0`, `:p1` |

---

## Pagination

| Syntax | SS | PG | MY | LT | OR | Notes |
|--------|----|----|----|----|-----|-------|
| `LIMIT n` | ❌ | ✅ | ✅ | ✅ | ❌ | SS: not supported |
| `FETCH NEXT n ROWS ONLY` | ✅ | ❌ | ❌ | ❌ | ✅ (12c+) | SS emits this for `.Limit()` |
| `OFFSET n ROWS` | ✅ | ❌ | ❌ | ❌ | ✅ (12c+) | SS form |
| `OFFSET n` | ❌ | ✅ | ✅ | ✅ | ❌ | PG/MY/LT form |
| `ROW_NUMBER()` window page | ✅ | ✅ | ✅ | ✅ | ✅ | `.WindowPage()` — `WITH __wp AS (...)` |
| Composite cursor | ✅ | ✅ | ✅ | ✅ | ✅ | `.SeekAfter()` / `.SeekBefore()` |
| `ROWNUM` / `FETCH FIRST` (Oracle native) | ❌ | ❌ | ❌ | ❌ | 📋 | TD-006; target v1.4 |

---

## ORDER BY NULL Position

| Behavior | SS | PG | MY | LT | OR | Notes |
|----------|----|----|----|----|-----|-------|
| Default NULL position (ASC) | Last | Last | Last | Last | Last | Standard SQL |
| `NULLS FIRST` native | ❌ | ✅ | ❌ | ❌ | ✅ | |
| `NULLS LAST` native | ❌ | ✅ | ❌ | ❌ | ✅ | |
| `NULLS FIRST` emulation via `IIF()` | ⚠️ NOP | — | ⚠️ NOP | ⚠️ NOP | — | TD-004; SS/MY/LT currently ignore |

---

## UPSERT / Conflict Resolution

| Feature | SS | PG | MY | LT | OR | Notes |
|---------|----|----|----|----|-----|-------|
| `ON CONFLICT (cols) DO NOTHING` | ❌ | ✅ | ⚠️ | ✅ | ❌ | MY: `ON DUPLICATE KEY UPDATE id=id` |
| `ON CONFLICT (cols) DO UPDATE SET` | ❌ | ✅ | ⚠️ | ✅ | ❌ | MY: `ON DUPLICATE KEY UPDATE col=VALUES(col)` |
| `MERGE INTO … WHEN MATCHED` | ✅ `[Obs.]` | ❌ | ❌ | ❌ | ✅ `[Obs.]` | `MergeQuery<T>` is `[Obsolete]` |
| `INSERT … ON DUPLICATE KEY UPDATE` | ❌ | ❌ | ✅ | ❌ | ❌ | MySQL native form |

---

## RETURNING / OUTPUT

| Feature | SS | PG | MY | LT | OR | Notes |
|---------|----|----|----|----|-----|-------|
| `RETURNING col, col` (INSERT) | ❌ | ✅ | ❌ | ✅ | ✅ | MY: throws `NotSupportedException` |
| `RETURNING *` (INSERT) | ❌ | ✅ | ❌ | ✅ | ❌ | OR: requires explicit columns |
| `OUTPUT INSERTED.col` (INSERT) | ✅ | ❌ | ❌ | ❌ | ❌ | SS-specific via `SqlServerVisitor` |
| `RETURNING` from UPDATE | ❌ | ✅ | ❌ | ✅ | ✅ | SS emits `OUTPUT INSERTED.*` |
| `OUTPUT DELETED.*` from DELETE | ✅ | ❌ | ❌ | ❌ | ❌ | SS only |
| `RETURNING` from DELETE | ❌ | ✅ | ❌ | ✅ | ✅ | |
| Oracle `RETURNING … INTO :out_col` | ❌ | ❌ | ❌ | ❌ | ✅ | Named OUT params; requires explicit cols |

---

## JOIN Variants

| Feature | SS | PG | MY | LT | OR | Notes |
|---------|----|----|----|----|-----|-------|
| INNER / LEFT / RIGHT / FULL / CROSS JOIN | ✅ | ✅ | ✅ | ✅ | ✅ | |
| JOIN in UPDATE | ✅ | ⚠️ | ✅ | ❌ | ❌ | PG uses `FROM`; LT: no JOIN in UPDATE |
| JOIN in DELETE | ✅ | ❌ | ✅ | ❌ | ❌ | PG uses `USING`; LT: no JOIN in DELETE |
| CROSS APPLY | ✅ | 🔄 | ❌ | ❌ | ❌ | PG: translates to CROSS JOIN LATERAL |
| OUTER APPLY | ✅ | 🔄 | ❌ | ❌ | ❌ | PG: translates to LEFT JOIN LATERAL |
| LATERAL JOIN | ❌ | ✅ | ❌ | ❌ | ❌ | PG only; `SubqueryJoinNode(IsLateral:true)` |
| UNNEST FROM | ❌ | ✅ | ❌ | ❌ | ❌ | PG: `FROM UNNEST(@arr) AS alias` |

---

## SELECT Modifiers

| Feature | SS | PG | MY | LT | OR | Notes |
|---------|----|----|----|----|-----|-------|
| `DISTINCT` | ✅ | ✅ | ✅ | ✅ | ✅ | |
| `DISTINCT ON (col)` | ❌ | ✅ | ❌ | ❌ | ❌ | PG-only; `DistinctOnNode` |
| `COPY FROM` | ❌ | ✅ | ❌ | ❌ | ❌ | PG-only bulk load via `CopyNode` |

---

## DELETE Semantics

| Feature | SS | PG | MY | LT | OR | Notes |
|---------|----|----|----|----|-----|-------|
| `DELETE FROM table WHERE ...` | ✅ | ✅ | ✅ | ✅ | ✅ | |
| `DELETE FROM t1 USING t2` | ❌ | ✅ | ❌ | ❌ | ❌ | PG: `PostgreSqlCompiler.CompileDelete` |
| `DELETE t1 FROM t1 JOIN t2` | ✅ | ❌ | ✅ | ❌ | ❌ | MySQL JOIN delete pattern |

---

## Bulk Operations

| Feature | SS | PG | MY | LT | OR | Notes |
|---------|----|----|----|----|-----|-------|
| Multi-row VALUES insert | ✅ | ✅ | ✅ | ✅ | ✅ | `BulkInsert` with VALUES batching |
| `SqlBulkCopy` native | ✅ | ❌ | ❌ | ❌ | ❌ | SS-specific `IBulkStrategy` |
| `COPY FROM STDIN` (NpgsqlBinaryImporter) | ❌ | ✅ | ❌ | ❌ | ❌ | PG-specific |
| `INSERT … ON DUPLICATE KEY` bulk | ❌ | ❌ | ✅ | ❌ | ❌ | MySQL bulk upsert |
| `BulkBuilder<T>` AOT path | ✅ | ✅ | ✅ | ✅ | ✅ | Requires `[SqlEntity]` + SourceGenerators |

---

## CTE Support

| Feature | SS | PG | MY | LT | OR | Notes |
|---------|----|----|----|----|-----|-------|
| Non-recursive CTE | ✅ | ✅ | ✅ | ✅ | ✅ | MySQL 8.0+ required |
| Recursive CTE | ✅ | ✅ | ✅ | ✅ | ✅ | MySQL 8.0+ required |
| CTE in INSERT | ✅ | ✅ | ✅ | ✅ | ✅ | |
| CTE in UPDATE | ✅ | ✅ | ✅ | ✅ | ✅ | |
| CTE in DELETE | ✅ | ✅ | ✅ | ✅ | ✅ | |

---

## ProviderCapability Flags

| Capability | SS | PG | MY | LT | OR |
|-----------|----|----|----|----|-----|
| `ProviderCapability.Apply` | ✅ | ✅* | ❌ | ❌ | ❌ |
| `ProviderCapability.Cte` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `ProviderCapability.WindowFunctions` | ✅ | ✅ | ✅ | ✅ | ✅ |
| `ProviderCapability.Merge` | ✅ | ❌ | ❌ | ❌ | ✅ |
| `ProviderCapability.Lateral` | ❌ | ✅ | ❌ | ❌ | ❌ |

> \* PostgreSQL translates `Apply` into `LATERAL` automatically.

---

## Known Dialect-Specific Limitations

| Dialect | Limitation | Workaround |
|---------|-----------|-----------|
| SQL Server | `NULLS FIRST/LAST` silently ignored (TD-004) | Use `.OrderBy($"IIF({col} IS NULL, 0, 1), {col}")` |
| SQL Server | Max 2100 parameters enforced | Batch queries; use `SqlBulkCopy` for large inserts |
| MySQL | `RETURNING` throws `NotSupportedException` | Use `LAST_INSERT_ID()` or query after insert |
| MySQL | `ON CONFLICT` target columns ignored (maps to `ON DUPLICATE KEY`) | Use raw SQL if specific conflict target needed |
| Oracle | `ON CONFLICT` throws `NotSupportedException` | Use `MergeQuery<T>` (obsolete) or `Sql.Raw()` |
| Oracle | `RETURNING` requires explicit column list | Always pass column names to `.Returning()` |
| Oracle | `LIMIT`/`OFFSET` not translated (TD-006) | Use `Sql.Raw()` with `FETCH FIRST n ROWS ONLY` |
| SQLite | No JOIN in UPDATE or DELETE | Use subqueries or restructure query |
| All | `MergeQuery<T>` is `[Obsolete]` | Use `OnConflict().DoUpdate()` for PG/MY/LT; raw SQL for SS/OR |

---

*This document is generated from compiler source code audit. Re-verify after each dialect compiler change.*
