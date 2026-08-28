# Dialect Matrix — EricksonLopez.SqlBuilder

> **Purpose:** Provider capability matrix with Universal/DialectSpecific/DialectEmulated/
> Unsupported/UnsafeToAbstract classification for every feature dimension.
> Last audit: 2026-08-19 | Based on direct compiler source inspection.
> Dialects: **SS**=SQL Server, **PG**=PostgreSQL, **MY**=MySQL, **MA**=MariaDB, **LT**=SQLite, **OR**=Oracle

---

## Classification Key

| Symbol | Class | Meaning |
|--------|-------|---------|
| 🌐 | Universal | Same API and semantics across all dialects |
| 🔵 | DialectSpecific | Different syntax per dialect; same semantic; dialect package exposes it |
| 🟡 | DialectEmulated | Syntax translated by compiler to approximate the semantic |
| ❌ | Unsupported | Not available in this dialect |
| ⛔ | UnsafeToAbstract | Abstraction would create false safety or incorrect behavior |

---

## 1. Identifier Quoting

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| Identifier quoting | `[x]` | `"x"` | `` `x` `` | `` `x` `` | `"x"` | `"X"` UPPER | 🔵 DialectSpecific |
| Case sensitivity | Insensitive | Case-preserved in `""` | Insensitive | Insensitive | Insensitive | UPPERCASE unless quoted | 🔵 |

---

## 2. Parameter Binding

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| Parameter prefix | `@` | `@` | `@` | `@` | `@` | `:` | 🔵 |
| Max parameters | 2100 | None | None | None | None | None | 🔵 |
| Named parameters | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ (required) | 🌐 |
| Positional parameters | ❌ | ✅ (`$1`) | ❌ | ❌ | ❌ | ❌ | 🔵 |

---

## 3. SELECT Modifiers

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| `SELECT DISTINCT` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `SELECT DISTINCT ON (col)` | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | 🔵 DialectSpecific |
| `SELECT TOP n` | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | 🔵 (not exposed in API) |
| `LIMIT n` | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | 🔵 |
| `FETCH NEXT n ROWS ONLY` | ✅ | ✅ | ❌ | ❌ | ❌ | ✅ (12c+) | 🔵 |

---

## 4. Pagination

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| `.Limit(n)` API | 🟡→FETCH | ✅ | ✅ | ✅ | ✅ | 🟡→FETCH(12c) | 🟡 DialectEmulated |
| `.Offset(n)` API | ✅ OFFSET n ROWS | ✅ | ✅ | ✅ | ✅ | 🟡→OFFSET(12c) | 🟡 |
| `.Page(page, size)` | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 | 🟡 |
| Window page (ROW_NUMBER) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| Keyset cursor | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| Composite cursor | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| ROWNUM pagination | ❌ | ❌ | ❌ | ❌ | ❌ | 📋 | 🔵 (TD-006) |

---

## 5. ORDER BY / NULL Ordering

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| `ORDER BY col ASC/DESC` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `NULLS FIRST` native | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ | 🔵 |
| `NULLS LAST` native | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ | 🔵 |
| `NULLS FIRST` emulation (IIF) | 📋 TD-004 | — | 📋 | 📋 | 📋 | — | 🟡 (planned) |

---

## 6. JOIN Types

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| INNER / LEFT / RIGHT / FULL / CROSS JOIN | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| JOIN in UPDATE | ✅ | 🟡→FROM | ✅ | ✅ | ❌ | ❌ | 🟡 |
| JOIN in DELETE | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | 🔵 |
| DELETE USING | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | 🔵 |
| CROSS APPLY | ✅ | 🟡→LATERAL | ❌ | ❌ | ❌ | ❌ | 🟡 |
| OUTER APPLY | ✅ | 🟡→LATERAL | ❌ | ❌ | ❌ | ❌ | 🟡 |
| LATERAL JOIN (explicit) | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | 🔵 |
| UNNEST FROM | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | 🔵 |

---

## 7. WHERE Predicates

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| `=`, `<>`, `<`, `>`, `<=`, `>=` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `IS NULL` / `IS NOT NULL` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `IN (values)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `BETWEEN a AND b` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `LIKE '%x%'` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `ILIKE` (case-insensitive LIKE) | ❌ | ✅ | 🟡→LIKE | 🟡→LIKE | ❌ | ❌ | 🔵 (not exposed in API) |
| `IS DISTINCT FROM` | ❌ | ✅ | ❌ | ❌ | ✅ | ❌ | 🔵 (not exposed in API) |
| EXISTS / NOT EXISTS | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| Nested group (AND/OR parentheses) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |

---

## 8. INSERT / UPSERT

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| `INSERT INTO … VALUES` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| Multi-row `VALUES (…),(…)` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 🔵 |
| `INSERT DEFAULT VALUES` | ✅ | ✅ | ✅ | ✅ | ✅ | ⛔ | 🔵 |
| `INSERT INTO … SELECT` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `OUTPUT INSERTED.*` | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | 🔵 |
| `RETURNING col` | ❌ | ✅ | ❌ | ✅ 10.5+ | ✅ | ✅ | 🔵 |
| `RETURNING … INTO :out` | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ | 🔵 |
| `ON CONFLICT (col) DO NOTHING` | ❌ | ✅ | 🟡 | 🟡 | ✅ | ❌ | 🟡 |
| `ON CONFLICT (col) DO UPDATE` | ❌ | ✅ | 🟡 | 🟡 | ✅ | ❌ | 🟡 |
| `ON DUPLICATE KEY UPDATE` | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 🔵 |
| `MERGE INTO … WHEN MATCHED` | ✅ | ⛔ | ❌ | ❌ | ❌ | ✅ | ⛔ UnsafeToAbstract |

---

## 9. RETURNING / OUTPUT

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| Unified `.Returning()` API | 🟡→OUTPUT | ✅ | ❌ throws | ✅ 10.5+ | ✅ | 🟡→INTO | 🟡 |
| From INSERT | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | 🔵 |
| From UPDATE | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | 🔵 |
| From DELETE | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ | 🔵 |
| Requires explicit columns | ❌ | ❌ | N/A | ❌ | ❌ | ✅ required | 🔵 |

---

## 10. MERGE / Upsert Strategy Safety

| Strategy | SS | PG | MY | MA | LT | OR | Class | Safety |
|---------|----|----|----|----|----|----|-------|--------|
| `MergeQuery<T>` (string-based) | ⚠️ Obs | ❌ | ❌ | ❌ | ❌ | ⚠️ Obs | ⛔ UnsafeToAbstract | Low — concurrency bugs in SS MERGE |
| `OnConflict().DoUpdate()` | ❌ | ✅ | 🟡 | 🟡 | ✅ | ❌ | 🟡 | High |
| `ON DUPLICATE KEY UPDATE` | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 🔵 | Medium |
| Raw `MERGE INTO` via `Sql.Raw()` | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | N/A | Developer responsibility |

> **Note:** Generic MERGE abstraction is `UnsafeToAbstract` — SQL Server MERGE has documented
> concurrency and correctness bugs that a generic abstraction would silently expose.
> See ADR-021 and `docs/architecture-boundaries.md`.

---

## 11. Aggregate & Group By

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| `COUNT / SUM / AVG / MIN / MAX` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `GROUP BY` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `HAVING` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| `GROUPING SETS` | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 🔵 (not in API yet) |
| `ROLLUP` | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 🔵 (not in API yet) |
| `CUBE` | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 🔵 (not in API yet) |

---

## 12. Window Functions

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| ROW_NUMBER / RANK / DENSE_RANK | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| LAG / LEAD / FIRST_VALUE / LAST_VALUE | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| SUM / AVG / COUNT / MIN / MAX OVER | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| NTILE(n) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| NTH_VALUE(col, n) | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ | 🔵 (not in API yet) |
| Named WINDOW clause | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| FRAME (ROWS / RANGE) | ❌ API | ❌ API | ❌ API | ❌ API | ❌ API | ❌ API | 🔵 (planned — raw only) |
| FILTER (WHERE) in window | 📋 v1.3 | 📋 v1.3 | 📋 v1.3 | 📋 v1.3 | 📋 v1.3 | 📋 v1.3 | 🔵 ADR-018 |

---

## 13. CTEs

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| Non-recursive CTE | ✅ | ✅ | ✅ 8+ | ✅ | ✅ | ✅ | 🌐 |
| Recursive CTE | ✅ | ✅ | ✅ 8+ | ✅ | ✅ | ✅ | 🌐 |
| Materialized CTE hint | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | 🔵 (not in API) |
| NOT MATERIALIZED hint | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | 🔵 (not in API) |
| Multiple CTEs | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |

---

## 14. Set Operations

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| UNION / UNION ALL | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| INTERSECT / INTERSECT ALL | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| EXCEPT / EXCEPT ALL | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |
| ORDER BY after set op | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 🌐 |

---

## 15. Bulk Operations

| Feature | SS | PG | MY | MA | LT | OR | Class |
|---------|----|----|----|----|----|----|-------|
| Multi-row `VALUES` insert | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 🔵 |
| `MySqlBulkCopy` native | ❌ | ❌ | ✅ | ✅ (compat) | ❌ | ❌ | 🔵 |
| `COPY FROM STDIN` | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ | 🔵 |
| Bulk upsert (upsert batch) | 🟡 | ✅ | ✅ | ✅ | ✅ | ❌ | 🟡 |
| Transaction support in bulk | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 🔵 |

---

## 16. Type System Nuances

| Type | SS | PG | MY | MA | LT | OR | Notes |
|------|----|----|----|----|----|----|-------|
| `bool` | `BIT` | `BOOLEAN` | `TINYINT(1)` | `TINYINT(1)` | `INTEGER` | `NUMBER(1)` | Driver handles |
| `Guid` / `UUID` | `UNIQUEIDENTIFIER` | `UUID` | `CHAR(36)` | `CHAR(36)` | `TEXT` | `RAW(16)` | `RegisterTypeHandler<T>` needed |
| `DateTimeOffset` | ✅ | ✅ | ⚠️ | ⚠️ | ❌ | ✅ | MA/MY store as UTC only |
| JSON column | `NVARCHAR`/`JSON` | `jsonb`/`json` | `JSON` | `JSON` | `TEXT` | `CLOB` | No typed API in ESQL |
| Arrays | ❌ | ✅ native | ❌ | ❌ | ❌ | ❌ | PG: `UnnestNode`; others: raw |
| `decimal` precision | ✅ | ✅ | ✅ | ✅ | ❌ exact | ✅ | LT uses REAL (imprecise) |

---

## Feature Classification Summary

| Class | Count | Examples |
|-------|-------|---------|
| 🌐 Universal | ~35 | SELECT, WHERE, JOINs, CTEs, UNION, window funcs, keyset pagination |
| 🔵 DialectSpecific | ~25 | RETURNING vs OUTPUT, LATERAL, UNNEST, identifier quoting, ON CONFLICT vs MERGE |
| 🟡 DialectEmulated | ~10 | CROSS APPLY→LATERAL, LIMIT→FETCH, NULLS FIRST→IIF (planned) |
| ❌ Unsupported | ~8 | MySQL RETURNING, Oracle multi-row VALUES, SQLite JOINs in DML |
| ⛔ UnsafeToAbstract | 2 | Generic MERGE, MySQL ON CONFLICT target columns |

---

*This document supersedes the compatibility section of the competitive analysis.
Re-audit after each dialect compiler change.*
