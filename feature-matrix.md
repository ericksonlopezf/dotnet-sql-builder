# FEATURE MATRIX — EricksonLopez.SqlBuilder
## Complete Strategic Audit

> **Audit Authority:** Source-of-truth generated from direct code inspection of every source file,
> compiler, test, benchmark, analyzer, and source generator in the repository.
> **Audit Date:** 2026-08-14 | **Auditor:** Principal Architect (Strategic Audit)
> **Code-test authority supersedes README/docs when they conflict.**

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Fully implemented and verified in source + tests |
| ⚠️ | Partially implemented — specific limitation noted inline |
| ❌ | Not implemented in code |
| 🚫 | Intentionally rejected — permanent (ADR exists) |
| 📋 | Planned — ADR exists, implementation deferred |
| 🔴 | Broken / produces wrong output silently |
| 📝 | Documented only — appears in README/docs but not in code |
| 🗑️ | Deprecated — exists but will be removed |

**Dialect columns:** SS = SQL Server · PG = PostgreSQL · MY = MySQL/MariaDB · LT = SQLite · OR = Oracle

---

## PART I — QUERY CONSTRUCTION

---

## 1. Core Query Entry Points

| Feature | Source File | Status | SS | PG | MY | LT | OR | Notes |
|---------|-------------|--------|----|----|----|----|----|----|
| `Sql.From<T>()` → `SelectQuery<T>` | `Sql.cs:16` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Uses `SqlEntityCache<T>` |
| `Sql.Insert<T>(entity)` | `Sql.cs:24` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| `Sql.BulkInsert<T>(entities)` | `Sql.cs:32` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Multi-row VALUES batch |
| `Sql.Bulk<T>(entities)` → `BulkBuilder<T>` | `Sql.cs:39` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Requires `IStaticEntityMetadata<T>` |
| `Sql.Update<T>()` | `Sql.cs:51` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Returns `IUpdateSetBuilder<T>` |
| `Sql.Update<T>(entity)` | `Sql.cs:62` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Full entity update |
| `Sql.Delete<T>()` | `Sql.cs:69` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ESQL001 guard |
| `Sql.Merge<T>()` | `Sql.cs:76` | 🗑️ | ⚠️ | 🚫 | 🚫 | 🚫 | ⚠️ | `[Obsolete]` — ADR-025 |
| `Sql.InsertFrom<T>(selectQuery, cols)` | `Sql.cs:91` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | INSERT INTO … SELECT |
| `Sql.Raw(FormattableString)` | `Sql.cs:103` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Auto-parameterized |
| `Sql.Raw(string, params?)` | `Sql.cs:111` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ESQL011 warns |
| `Sql.Between<T>(val, lo, hi)` | `Sql.cs:171` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Expression-only sentinel |
| `Sql.Coalesce<T>(val, fallback)` | `Sql.cs:191` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Expression-only sentinel |
| `Sql.ILike(val, pattern)` | `Sql.cs:132` | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | PG-only semantics |
| `Sql.Any<T>(val, collection)` | `Sql.cs:141` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Expression-only |
| `Sql.All<T>(val, collection)` | `Sql.cs:150` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Expression-only |
| Query tagging `.WithTag(string)` | All query types | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | OTel propagation |
| ISqlQuery immutability | All query records | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | C# `record` ADR-017 |

---

## 2. SELECT — Projection & Filtering

| Feature | API | Status | SS | PG | MY | LT | OR | Notes |
|---------|-----|--------|----|----|----|----|----|----|
| `SELECT *` (default) | `From<T>()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | SQL003 warns |
| Typed projection | `.Select<TResult>(x => new {…})` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ExpressionSelectNode |
| String column projection | `.Select(params string[])` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Raw SELECT | `.RawSelect(FormattableString)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| `SELECT DISTINCT` | `.Distinct()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| `SELECT DISTINCT ON (col)` | `.DistinctOn(col)` | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | PG-only `DistinctOnNode` |
| Window function in SELECT | `Window.Rank<T>()…As("alias")` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| CASE expression in SELECT | `.SelectCase(c => c.When(…).Then(…))` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Subquery as FROM | `.From(subquery, alias)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `SubqueryFromNode` |
| UNNEST FROM | `.Unnest(array[], alias)` | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | PG-only `UnnestNode` |
| Aliased subquery | `.Alias(string)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `QueryAliasNode` |
| Multi-column GROUP BY | `.GroupBy(params string[])` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| GROUPING SETS | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not in API; raw SQL only |
| ROLLUP | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not in API; raw SQL only |
| CUBE | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not in API; raw SQL only |

---

## 3. WHERE Clause

| Feature | API | Status | SS | PG | MY | LT | OR | Notes |
|---------|-----|--------|----|----|----|----|----|----|
| Typed expression WHERE | `.Where(x => x.Prop == val)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `SqlExpressionVisitor` |
| Typed AND | `.And(x => …)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Typed OR | `.Or(x => …)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Raw WHERE | `.Where(FormattableString)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| WHERE EXISTS | `.WhereExists(subquery)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| WHERE NOT EXISTS | `.WhereNotExists(subquery)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| OR EXISTS | `.OrExists(subquery)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| OR NOT EXISTS | `.OrNotExists(subquery)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| WhereAll (explicit full-table) | `.WhereAll()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Suppresses ESQL001/003 |
| BETWEEN | `.Where(x => x.Col >= lo && x.Col <= hi)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Via expression visitor |
| BETWEEN (explicit) | `.Where(x => Sql.Between(x.Age, 18, 65))` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Emits `BETWEEN` keyword |
| IN (values collection) | `.Where(x => ids.Contains(x.Id))` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Via expression visitor |
| LIKE (StartsWith) | `.Where(x => x.Name.StartsWith("A"))` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `LIKE 'A%'` |
| LIKE (EndsWith) | `.Where(x => x.Name.EndsWith("z"))` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `LIKE '%z'` |
| LIKE (Contains) | `.Where(x => x.Name.Contains("foo"))` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `LIKE '%foo%'` |
| ILIKE | `.Where(x => Sql.ILike(x.Name, "%foo%"))` | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | PG-only |
| IS NULL | `.Where(x => x.Col == null)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Emits `IS NULL` |
| IS NOT NULL | `.Where(x => x.Col != null)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| COALESCE | `.Where(x => x.Name.Coalesce("X") == "Y")` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Nested groups | `.WhereGroup(g => g.And(…).Or(…))` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| NOT prefix | `.Where(x => !x.IsActive)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| IS DISTINCT FROM | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not in API; raw SQL only |
| NULLIF | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not in API; raw SQL only |

---

## 4. JOIN Clauses

| Feature | API | Status | SS | PG | MY | LT | OR | Notes |
|---------|-----|--------|----|----|----|----|----|----|
| INNER JOIN (string) | `.InnerJoin(table, alias, on)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| LEFT JOIN | `.LeftJoin(table, alias, on)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| RIGHT JOIN | `.RightJoin(table, alias, on)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| FULL OUTER JOIN | `.FullJoin(table, alias, on)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| CROSS JOIN | `.CrossJoin(table, alias)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Typed INNER JOIN | `.InnerJoin<TOther>(alias, on)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Uses `SqlEntityCache<T>` |
| Typed expression JOIN | `.Join<TOther>(x, y => x.Id == y.FkId)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Raw JOIN | `.JoinRaw(FormattableString)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Subquery JOIN | `.JoinSubquery(subquery, alias, on)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| CROSS APPLY | `.CrossApply(subquery, alias)` | ✅ | ✅ | 🟡 | ❌ | ❌ | ❌ | PG→CROSS JOIN LATERAL |
| OUTER APPLY | `.OuterApply(subquery, alias)` | ✅ | ✅ | 🟡 | ❌ | ❌ | ❌ | PG→LEFT JOIN LATERAL |
| LATERAL JOIN | `.LateralJoin(subquery, alias)` | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | `SubqueryJoinNode(IsLateral)` |
| Outer-reference LATERAL | `.LateralJoin(…Sql.Outer<T>(…))` | 📋 | ❌ | 📋 | ❌ | ❌ | ❌ | ADR-019 deferred v1.3 |

---

## 5. ORDER BY / PAGINATION

| Feature | API | Status | SS | PG | MY | LT | OR | Notes |
|---------|-----|--------|----|----|----|----|----|----|
| ORDER BY ASC | `.OrderBy(x => x.Col)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| ORDER BY DESC | `.OrderByDescending(x => x.Col)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| ThenBy / ThenByDescending | `.ThenBy(x => x.Col)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| NULLS FIRST | `.OrderBy(…, NullsPosition.First)` | ⚠️ | ✅ | ✅ | 🔴 | 🔴 | ✅ | SS: now emits CASE WHEN; MY/LT: silently NOP |
| NULLS LAST | `.OrderBy(…, NullsPosition.Last)` | ⚠️ | ✅ | ✅ | 🔴 | 🔴 | ✅ | Same issue |
| Raw ORDER BY | `.OrderBy(FormattableString)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Dynamic sorting | `.OrderByDynamic("Name", desc)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Property name resolution |
| LIMIT | `.Limit(n)` | ✅ | 🟡 | ✅ | ✅ | ✅ | 🟡 | SS→FETCH NEXT; OR→FETCH FIRST (12c+) |
| OFFSET | `.Offset(n)` | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 | OR→OFFSET n ROWS (12c+) |
| `.Page(page, size)` convenience | `.Page(1, 20)` | ✅ | ✅ | ✅ | ✅ | ✅ | 🟡 | |
| Window page (ROW_NUMBER-based) | `.WindowPage(page, size, col)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Wraps in `WITH __wp AS (…)` CTE |
| Keyset cursor (simple) | `.Where(x => x.Id > lastId)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Manual composition |
| Composite cursor | `.SeekAfter(CursorKey[])` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `CompositeCursorNode` |
| `.SeekBefore(CursorKey[])` | `.SeekBefore(CursorKey[])` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Oracle ROWNUM pagination | — | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | TD-006: falls back to base (produces wrong SQL on OR <12c) |

---

## 6. HAVING

| Feature | API | Status | SS | PG | MY | LT | OR |
|---------|-----|--------|----|----|----|----|---|
| Typed HAVING | `.Having(x => x.Sum > 0)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Raw HAVING | `.Having(FormattableString)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| OR HAVING | `.OrHaving(…)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## 7. CTEs & Set Operations

| Feature | API | Status | SS | PG | MY | LT | OR | Notes |
|---------|-----|--------|----|----|----|----|----|----|
| Non-recursive CTE | `.CTE(name, subquery)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Recursive CTE | `.RecursiveCTE(name, subquery)` | ✅ | ✅ | ✅ | ✅ 8+ | ✅ | ✅ | |
| Multiple CTEs | Multiple `.CTE()` calls | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Materialized CTE hint | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | PG-only; not in API |
| NOT MATERIALIZED hint | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | PG-only; not in API |
| Named WINDOW clause | `.Window(name, partitionBy, orderBy)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| UNION | `.Union(query)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| UNION ALL | `.UnionAll(query)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| INTERSECT | `.Intersect(query)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| INTERSECT ALL | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not exposed in API |
| EXCEPT | `.Except(query)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| EXCEPT ALL | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not exposed in API |

---

## 8. Window Functions

| Function | Factory Method | Status | SS | PG | MY | LT | OR | Notes |
|---------|----------------|--------|----|----|----|----|----|----|
| ROW_NUMBER() | `Window.RowNumber<T>()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| RANK() | `Window.Rank<T>()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| DENSE_RANK() | `Window.DenseRank<T>()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| NTILE(n) | `Window.Ntile<T>(n)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| LAG(col, offset) | `Window.Lag<T,TKey>(sel, offset)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| LEAD(col, offset) | `Window.Lead<T,TKey>(sel, offset)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| FIRST_VALUE(col) | `Window.FirstValue<T,TKey>(sel)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| LAST_VALUE(col) | `Window.LastValue<T,TKey>(sel)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| NTH_VALUE(col, n) | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not in API; PG/MY/LT support |
| SUM OVER | `Window.Sum<T,TVal>(sel)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| AVG / COUNT / MIN / MAX OVER | `Window.Avg/Count/Min/Max(…)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| PARTITION BY (typed) | `.PartitionBy(x => x.Col)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| ORDER BY in OVER (typed) | `.OrderBy(x => x.Col)` on WindowBuilder | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| FILTER (WHERE) in OVER | ❌ | 📋 | 📋 | 📋 | 📋 | 📋 | 📋 | ADR-018 deferred v1.2 |
| ROWS/RANGE frame | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not in API; raw SQL only |
| GROUPS frame | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | Not in API |

---

## PART II — DML OPERATIONS

---

## 9. INSERT Operations

| Feature | API | Status | SS | PG | MY | LT | OR | Notes |
|---------|-----|--------|----|----|----|----|----|----|
| Single entity insert | `Sql.Insert(entity)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Multi-row VALUES batch | `.Bulk(entities)` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | Oracle: no multi-row VALUES |
| INSERT DEFAULT VALUES | `.DefaultValues()` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | Oracle: not supported |
| INSERT INTO … SELECT | `Sql.InsertFrom<T>(query, cols)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| RETURNING clause | `.Returning(cols[])` / typed expr | ✅ | 🟡 | ✅ | ❌ | ✅ | ✅ | SS→OUTPUT INSERTED |
| OUTPUT (SQL Server) | `.Returning()` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | SS only via visit dispatch |
| RETURNING INTO (Oracle) | `.Returning("col")` | ✅ | ❌ | ❌ | ❌ | ❌ | ✅ | Oracle requires explicit cols |
| ON CONFLICT (col) | `.OnConflict(cols[])` | ✅ | ❌ | ✅ | 🟡 | ✅ | ❌ | MY→ON DUPLICATE KEY |
| DO NOTHING | `.DoNothing()` | ✅ | ❌ | ✅ | 🟡 | ✅ | ❌ | MY: `id=id` emulation |
| DO UPDATE SET | `.DoUpdate(expr)` | ✅ | ❌ | ✅ | 🟡 | ✅ | ❌ | MY: VALUES(col) form |
| Ignore nulls on bulk | `.Bulk(entities, ignoreNulls:true)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Custom target table | `.Into(tableName)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |

---

## 10. UPDATE Operations

| Feature | API | Status | SS | PG | MY | LT | OR | Notes |
|---------|-----|--------|----|----|----|----|----|----|
| SET typed expression | `.Set<TVal>(x => x.Col, value)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| SET raw SQL | `.Set(FormattableString)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| SET from entity (all cols) | `.Set(entity)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| SET from entity (ignore nulls) | `.Set(entity, ignoreNulls:true)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| Diff UPDATE (changed props only) | `.ApplyDiff(original, current)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `DiffUpdateExtensions` |
| UPDATE with JOIN | `.InnerJoin(…)` after `.Update()` | ✅ | ✅ | 🟡 | ✅ | ❌ | ❌ | PG uses FROM; LT: no JOIN |
| RETURNING from UPDATE | `.Returning(cols[])` | ⚠️ | 🟡 | ✅ | ❌ | ✅ | ✅ | SS: OUTPUT INSERTED |
| Concurrency token | `.WithConcurrencyToken(col, exp, new)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | `ConcurrencyTokenNode` ADR-022 |

---

## 11. DELETE Operations

| Feature | API | Status | SS | PG | MY | LT | OR | Notes |
|---------|-----|--------|----|----|----|----|----|----|
| DELETE with WHERE | `.Where(x => x.Id == id)` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | |
| WhereAll (explicit full delete) | `.WhereAll()` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | Bypasses ESQL001 |
| DELETE with USING (PG) | `.Using(table, alias)` | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | PG: `DELETE FROM t1 USING t2` |
| DELETE with JOIN | `.InnerJoin(…)` | ✅ | ✅ | ❌ | ✅ | ❌ | ❌ | PG uses USING; LT: no JOIN |
| RETURNING from DELETE | `.Returning(cols[])` | ⚠️ | 🟡 | ✅ | ❌ | ✅ | ✅ | SS: OUTPUT DELETED |

---

## 12. MERGE / UPSERT

| Feature | API | Status | Notes |
|---------|-----|--------|-------|
| `MergeQuery<T>` | `Sql.Merge<T>()` | 🗑️ | `[Obsolete]` — ADR-025; SS + OR only; string-based clauses |
| ON CONFLICT (PG/SQLite) | `InsertQuery<T>.OnConflict(…)` | ✅ | |
| ON DUPLICATE KEY UPDATE (MySQL) | `InsertQuery<T>.OnConflict(…)` | ✅ | Emulated |
| SQL Server MERGE (recommended) | `Sql.Raw(FormattableString)` | ✅ | Escape hatch per ADR-025 |
| Oracle MERGE INTO | `Sql.Merge<T>()` [Obsolete] | 🗑️ | Escape hatch |
| Generic cross-dialect MERGE | ❌ | 🚫 | Permanently rejected — ADR-025 |

---

## PART III — EXECUTION & INTEGRATION

---

## 13. AOT & Source Generator

| Feature | Package | Status | Notes |
|---------|---------|--------|-------|
| `[SqlEntity]` attribute | `SourceGenerators` | ✅ | Incremental `IIncrementalGenerator` |
| `ISqlEntity` implementation generation | `SourceGenerators` | ✅ | `GetTableName()`, `GetColumnNames()`, `GetValues()` |
| `Parser` class (ordinal-cached `IDataReader` mapping) | `SourceGenerators` | ✅ | Zero-reflection hydration |
| `AotMetadata` / `IEntityMetadataProvider<T>` | `SourceGenerators` | ✅ | `ColumnFlags`, `IsNull`, `IsDefault` |
| `SqlAlias` class generation | `SourceGenerators` | ✅ | `new User.SqlAlias("u").Id → "u.id"` |
| `SelectAllTemplate` constant | `SourceGenerators` | ✅ | Compile-time SELECT template |
| `PropertyMap` dictionary | `SourceGenerators` | ✅ | `nameof(Prop) → "col_name"` |
| `GetIndexedColumns()` | `SourceGenerators` | ✅ | Via `[SqlIndex]` attribute |
| `[SqlKey]` → `PrimaryKey` ColumnFlag | `SourceGenerators` | ✅ | |
| `[SqlIgnore]` / `[SqlGenerated]` | `SourceGenerators` | ✅ | `ColumnFlags.Generated` |
| `IBulkSerializer<T>` implementation | `SourceGenerators` | ✅ | For `BulkBuilder<T>` AOT path |
| Auto-generated `GetReaderParser()` (v2.0) | `SourceGenerators` | 📋 | AOT-004; planned v2.0 |
| `SqlEntityCache<T>` with `[SqlEntity]` | Core | ✅ | AOT-safe via static generic init |
| `SqlEntityCache<T>` without `[SqlEntity]` | Core | 🔴 | Reflection fallback; silently produces empty columns; TD-003 |
| `[RequiresUnreferencedCode]` on fallback | Core | ❌ | Missing — TD-003, AOT-002 |
| `IsAotCompatible = true` in `.csproj` | Packaging | ❌ | Missing — TD-002, AOT-001 |
| `Expression.Compile()` on first WHERE call | Core | ⚠️ | Cached after; not strictly AOT-safe; TD-005 |
| `[RequiresDynamicCode]` on expression visitor | Core | ❌ | Missing — AOT-003 |
| NativeAOT core AST | Core | ✅ | No reflection in AST construction |
| NativeAOT Dapper `QueryAsync<T>` | Dapper | ❌ | Dapper permanent limitation |
| `QueryAotAsync<T>(userMapper)` | Dapper | ✅ | Reflection-free |
| `AotQueryExecutor` (full ADO.NET path) | Aot | ✅ | Complete; no Dapper dependency |
| CI NativeAOT gate | CI | ❌ | Missing — AOT-006 |
| Filter source generator (`FilterGenerator`) | SourceGenerators | ✅ | Dynamic filter class generation |
| Multi-map descriptor generator | SourceGenerators | ✅ | `MultiMapDescriptorGenerator` |

---

## 14. Execution Layer (Dapper)

| Feature | Status | Notes |
|---------|--------|-------|
| `QueryAsync<T>` | ✅ | Reflection mapper — NOT NativeAOT |
| `QueryAotAsync<T>(mapper)` | ✅ | User-supplied `Func<IDataReader,T>` |
| `QueryFirstOrDefaultAsync<T>` | ✅ | |
| `QueryFirstAsync<T>` | ✅ | |
| `ExecuteAsync` | ✅ | |
| `ExecuteScalarAsync<T>` | ✅ | |
| `QueryMultipleAsync` | ✅ | |
| `IAsyncEnumerable<T>` streaming | ✅ | Unbuffered execution |
| Dynamic compiler resolution by connection type | ✅ | `RegisterCompiler<TConn>()` |
| Multi-mapping 2–7 types | ✅ | Dapper native |
| Multi-mapping 8+ types | ✅ | `MultiMapBuilder<T>` fluent API |
| `RegisterTypeHandler<T>` (dual Dapper+ESQL) | ✅ | Bridges both type systems |
| OpenTelemetry auto-instrumentation | ✅ | Per-query `ActivitySource` + Meter |
| Slow query logging | ✅ | Via OTel threshold |
| Bulk insert via `IBulkStrategy` | ✅ | Plugin model per-provider |
| `DapperPaginationExtensions` | ✅ | `.Page(params)` convenience |

---

## 15. Resilience (Polly v8)

| Feature | Status | Notes |
|---------|--------|-------|
| `ExecuteWithResilienceAsync` | ✅ | Wraps `ExecuteAsync` |
| `QueryWithResilienceAsync<T>` | ✅ | |
| `QueryFirstWithResilienceAsync<T>` | ✅ | |
| `SqlServerTransientErrorDetector` | ✅ | Error codes 1205, 40197, 40501, etc. |
| `PostgreSqlTransientErrorDetector` | ✅ | SQLSTATE 40001, 08006 |
| `MySqlTransientErrorDetector` | ✅ | Error codes 1213, 2006, 2013 |
| `SqliteTransientErrorDetector` | ✅ | SQLITE_BUSY/LOCKED |
| `OracleTransientErrorDetector` | ✅ | ORA errors |
| `SqlResilienceDefaults.Standard(detector)` | ✅ | 3 retries, exp. backoff, 30s timeout |
| `SqlResilienceDefaults.Aggressive(detector)` | ✅ | 5 retries, 60s timeout |
| `SqlResilienceDefaults.Conservative(detector)` | ✅ | 1 retry, 120s timeout |
| Provider shortcuts (`.ForSqlServer()`, etc.) | ✅ | |
| ESQL012 analyzer: retry inside transaction | ✅ | Prevents mutation retry risk |
| Circuit breaker strategy | ❌ | Not in defaults; user can compose via Polly |
| Idempotency guard for mutations | ❌ | Developer responsibility per ADR-016 |

---

## 16. Unit of Work / Transactions

| Feature | Status | Notes |
|---------|--------|-------|
| `IUnitOfWork` abstraction | ✅ | |
| `BeginUnitOfWorkAsync(IsolationLevel)` | ✅ | |
| `CommitAsync()` | ✅ | |
| `RollbackAsync()` | ✅ | |
| `CreateSavepointAsync(name)` | ✅ | No-op fallback for non-`DbTransaction` |
| `ISavepoint.RollbackAsync()` | ✅ | |
| `ISavepoint.ReleaseAsync()` | ✅ | |
| Auto-rollback on `DisposeAsync()` | ✅ | If `CommitAsync` not called |
| `IsolationLevel` passthrough | ✅ | |
| Nested transactions | ❌ | Not supported; use savepoints |

---

## 17. Bulk Operations

| Feature | Status | Notes |
|---------|--------|-------|
| `BulkBuilder<T>` fluent API | ✅ | |
| `IBulkStrategy` plugin model | ✅ | Per-provider, per-operation |
| `SqlBulkCopy` strategy (SQL Server) | ✅ | Native high-throughput |
| `SqlBulkMergeStrategy` (SQL Server) | ✅ | Temp table + MERGE |
| `COPY FROM STDIN` strategy (PostgreSQL) | ✅ | `CopyNode` + `NpgsqlBinaryImporter` |
| `NpgsqlBulkMergeStrategy` | ✅ | |
| `MySqlBatchStrategy` (bulk values) | ✅ | |
| `MySqlBulkMergeStrategy` | ✅ | |
| `OracleBulkCopyStrategy` | ⚠️ | Minimal implementation; needs expansion |
| `RenderBulkInsert/Update/Upsert` base | ⚠️ | Base throws `NotSupportedException`; should be `abstract` — TD-007 |
| AOT bulk path (Source Generator metadata) | ✅ | `IStaticEntityMetadata<T>` |
| Column selection rules | ✅ | `IColumnSelectionRule<T>` |
| Batch size control | ✅ | `BulkBuilder<T>.BatchSize(n)` |
| Identity retrieval after bulk | ❌ | Not universally supported |
| Partial failure handling | ❌ | Developer responsibility |

---

## 18. Roslyn Analyzers

| Rule ID | Description | Severity | Status | False+ Risk | Fix Available |
|---------|-------------|----------|--------|-------------|---------------|
| ESQL001 | DELETE without WHERE | Error | ✅ | Low | ✅ (WhereAll code fix) |
| ESQL002 | Raw SQL string concatenation | Error | ✅ | Low | ✅ (FormattableString fix) |
| ESQL003 | UPDATE without WHERE | Error | ✅ | Low | ✅ |
| ESQL004 | Query performance concern | Warning | ✅ | Medium | ❌ |
| ESQL005 | Dapper compiler misconfiguration | Warning | ✅ | Low | ❌ |
| ESQL006 | Missing ON condition in JOIN | Warning | ✅ | Low | ❌ |
| ESQL007 | Potential missing index hint | Info | ✅ | Medium | ❌ |
| ESQL008 | Large OFFSET value | Warning | ✅ | Low | ❌ |
| ESQL009 | LIKE leading wildcard | Warning | ✅ | Low | ❌ |
| ESQL010 | LIKE wildcard usage concern | Warning | ✅ | Medium | ❌ |
| ESQL011 | `Sql.Raw(string)` unsafe overload | Warning | ✅ | Low | ✅ |
| ESQL012 | Retry pipeline inside transaction | Warning | ✅ | Low | ❌ |
| ESQL020 | Dialect-specific API + incompatible compiler | Warning | ✅ | Low | ❌ |
| ESQL021 | `[SqlEntity]` without Source Generator | Warning | ✅ | Low | ❌ |
| ESQL022 | Type mapping registration issue | Warning | ✅ | Medium | ❌ |
| ESQL023 | Synchronous SQL call on UI thread | Warning | ✅ | Low | ❌ |
| ESQL024 | Cartesian product (missing join condition) | Warning | ✅ | Low | ❌ |
| ESQL025 | SqlKata API detected (migration code fix) | Info | ✅ | Low | ✅ |
| SQL003 | `SELECT *` usage | Warning | ✅ | Medium | ✅ (SELECT col code fix) |
| SQL004 | Redundant WHERE condition | Warning | ✅ | High | ❌ |
| SQL009 | Missing column reference | Warning | ✅ | High | ❌ |
| ESQL026 | `Sql.Merge<T>()` used — prefer OnConflict | Warning | ❌ | — | — | Planned per TD-008 |

---

## 19. Observability (OpenTelemetry)

| Feature | Status | Notes |
|---------|--------|-------|
| `ActivitySource` per query | ✅ | `"EricksonLopez.SqlBuilder"` |
| `db.statement` activity tag | ✅ | Full SQL captured |
| `db.parameter.*` tags (masked by default) | ✅ | `LogParameters = true` to expose |
| `sqlbuilder.query_type` tag | ✅ | e.g. `INSERT_AOT` |
| Query execution counter (Meter) | ✅ | |
| Slow query detection | ✅ | |
| `SqlBuilderInstrumentation.StartQueryActivity()` | ✅ | Manual instrumentation |
| `AddSqlBuilderInstrumentation()` OTLP builder | ✅ | |
| `.WithTag()` → OTel activity | ✅ | |
| `db.system` semantic attribute | ⚠️ | Sets `"sql"` generically; should be `mssql`/`postgresql`/etc. |
| W3C trace context propagation | ❌ | Not explicitly implemented in SqlBuilder layer |
| PII/credential scrubbing in SQL log | ⚠️ | Parameters masked by default; SQL statement itself not scrubbed |
| OTel target info (db.server.address) | ❌ | Not set |

---

## 20. Security Assessment

| Concern | Status | Notes |
|---------|--------|-------|
| SQL injection via parameterization | ✅ | All typed paths parameterize correctly |
| Parameterized `FormattableString` | ✅ | `Sql.Raw(FormattableString)` auto-params |
| Raw string SQL injection | ⚠️ | `Sql.Raw(string)` accepts raw — ESQL011 warns but doesn't block |
| Dynamic identifier injection | ⚠️ | `OrderByDynamic` resolves via `PropertyMap`; validates against known columns |
| Dynamic sorting SQL injection | ✅ | Property resolution guards against arbitrary column names |
| WHERE injection via expr | ✅ | Expression visitor always parameterizes values |
| SQL logging / PII exposure | ⚠️ | Parameters masked by default; SQL still in OTel activity |
| 2100 SQL Server param limit | ✅ | `ParameterManager` enforces limit; throws |
| Oracle injection via identifier case | ✅ | Always `UPPER` in `EscapeIdentifier` |

---

## 21. Package Architecture — Actual State

| Package | TFM | AOT Declared | Key Dependencies | Status |
|---------|-----|:------------:|-----------------|--------|
| `EricksonLopez.SqlBuilder.Abstractions` | net8.0, net10.0 | ❌ | None | ✅ Stable |
| `EricksonLopez.SqlBuilder` | net8.0, net9.0, net10.0 | ❌ | Abstractions, System.Collections.Immutable, Microsoft.Extensions.ObjectPool, **EricksonLopez.Pagination** | ⚠️ External local dep |
| `EricksonLopez.SqlBuilder.SqlServer` | net8.0, net10.0 | ❌ | Core | ✅ |
| `EricksonLopez.SqlBuilder.PostgreSql` | net8.0, net10.0 | ❌ | Core, Npgsql | ✅ |
| `EricksonLopez.SqlBuilder.MySql` | net8.0, net10.0 | ❌ | Core | ✅ |
| `EricksonLopez.SqlBuilder.Sqlite` | net8.0, net10.0 | ❌ | Core | ✅ |
| `EricksonLopez.SqlBuilder.Oracle` | net8.0, net10.0 | ❌ | Core, Oracle.ManagedDataAccess.Core | ✅ |
| `EricksonLopez.SqlBuilder.Aot` | net8.0, net10.0 | ❌ | Abstractions | ✅ |
| `EricksonLopez.SqlBuilder.Dapper` | net8.0, net10.0 | ❌ | Core, Dapper | ✅ |
| `EricksonLopez.SqlBuilder.Dapper.UnitOfWork` | net8.0, net10.0 | ❌ | Dapper pkg | ✅ |
| `EricksonLopez.SqlBuilder.Dapper.Resilience` | net8.0, net10.0 | ❌ | Dapper pkg, Polly | ✅ |
| `EricksonLopez.SqlBuilder.Dapper.MultiMap` | net8.0, net10.0 | ❌ | Dapper pkg | ✅ |
| `EricksonLopez.SqlBuilder.OpenTelemetry` | net8.0 | ❌ | Abstractions, OTel | ⚠️ Only net8.0 |
| `EricksonLopez.SqlBuilder.SourceGenerators` | netstandard2.0 | ❌ | Roslyn | ✅ |
| `EricksonLopez.SqlBuilder.Analyzers` | netstandard2.0 | ❌ | Roslyn | ✅ |
| `EricksonLopez.SqlBuilder.Testing` | Internal only | N/A | Core | ✅ |
| `EricksonLopez.SqlBuilder.Benchmarks` | net10.0 | N/A | BenchmarkDotNet | ✅ |

**Critical packaging deficiency:** `IsAotCompatible = true` is not set in any package. `EricksonLopez.SqlBuilder.csproj` has a `<ProjectReference>` to an external sibling repository (`dotnet-pagination`) — this is a serious packaging issue that will break NuGet builds unless resolved.

---

## 22. Intentionally Rejected Features

| Feature | Reason | ADR |
|---------|--------|-----|
| Change tracking | Mutable state buffer; violates immutability | ADR-007 |
| Navigation properties / lazy loading | Predictability trap; N+1 | ADR-007 |
| Identity map / first-level cache | Hidden mutable state; thread-unsafe | ADR-007 |
| LINQ `IQueryable<T>` provider | 50+ operators; impossible AOT + predictable translation | ADR-008 |
| Automatic query caching | Cache invalidation complexity; memory leak risks | ADR-024 |
| DI / `IServiceCollection` auto-registration | Unnecessary coupling; blocks AOT | ADR-023 |
| `ILogger` in Core | Forces logging framework choice | ADR-023 |
| Polly as Core dependency | Optional concern; isolated to resilience package | ADR-003 |
| Generic cross-dialect MERGE abstraction | SS MERGE has known concurrency bugs; dialect semantics differ fundamentally | ADR-025 |
| Migration engine | Different tool category; Flyway/EF Migrations | — |
| Soft delete global filter | Business logic; explicit `.Where()` required | — |
| Multi-tenancy global filter | Hidden dependency anti-pattern | — |
| Dynamic proxy / IL emit | Kills NativeAOT | — |
| Automatic convention scanning | Magic reflection; breaks AOT | — |
| Audit field automation | Business logic | — |
| Specification pattern (built-in) | App-layer pattern; builds on SqlBuilder | ADR-026 |
| Repository pattern (built-in) | App-layer pattern | ADR-027 |
| Automatic retry of mutations | Non-idempotent + retry = duplicates; ESQL012 guards | ADR-016 |

---

## 23. Technical Debt Register (Summary)

| ID | Gap | Priority | Target |
|----|-----|----------|--------|
| TD-001 | roadmap.md stale entries | P1 | ✅ Fixed |
| TD-002 | `IsAotCompatible = true` missing from all `.csproj` | P1 | v1.2.0 |
| TD-003 | `SqlEntityCache<T>` reflection fallback not guarded | P1 | v1.2.0 |
| TD-004 | NULLS FIRST/LAST silently wrong on MySQL/SQLite | P2 | v1.2.0 |
| TD-005 | `Expression.Compile()` not attributed `[RequiresDynamicCode]` | P2 | v1.2.0 |
| TD-006 | Oracle ROWNUM pagination not implemented | P2 | v1.4.0 |
| TD-007 | `AotSqlRendererBase` bulk methods should be `abstract` | P3 | v1.2.0 |
| TD-008 | `MergeQuery<T>` still in README feature highlights | P3 | v1.2.0 |
| TD-009 | External `EricksonLopez.Pagination` project reference | P1 | Stabilize before v2.0 |
| TD-010 | `InternalsVisibleTo` duplicated 4x in core `.csproj` | P3 | Cleanup |
| TD-011 | `Description` in core `.csproj` contains Spanish text (encoding issue) | P3 | Fix |
| TD-012 | `OpenTelemetry` package only targets `net8.0` (not net9/net10) | P2 | v1.2.0 |
| TD-013 | OTel `db.system` tag uses generic `"sql"` not per-dialect value | P2 | v1.3.0 |
| TD-014 | No CI NativeAOT publish gate | P1 | v1.2.0 |
| TD-015 | NULLS FIRST/LAST silently NOP for MySQL and SQLite (no emulation, no throw) | P2 | v1.3.0 |
| TD-016 | `BulkBuilder<T>` identity retrieval after insert not universally implemented | P3 | v2.0 |

---

*This document is the authoritative feature truth. Generated from code-level audit.
Re-audit after any significant AST, compiler, dialect, or package change.*
