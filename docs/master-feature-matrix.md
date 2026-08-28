# MASTER FEATURE MATRIX — EricksonLopez.SqlBuilder
## Section 22 Format — Complete Classification Matrix

> **Status values:** Implemented | Partial | Broken | Documented Only | Planned | Missing | Deprecated | Rejected
> **Classification:** CORE | STRATEGIC | SUPPORTING | OPTIONAL | EXPERIMENTAL | DIALECT-SPECIFIC | ADAPTER | OUT-OF-SCOPE | DEPRECATED | REJECTED
> **Priority:** P0=Critical | P1=High | P2=Medium | P3=Low | P4=Do Not Implement
> **AOT:** ✅ Safe | ⚠️ Caveat | ❌ Not Safe | N/A
> **Dialects:** SS=SQL Server | PG=PostgreSQL | MY=MySQL | LT=SQLite | OR=Oracle | ALL=Universal | N/A

---

## Domain: Query Entry Points

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| Entry | `Sql.From<T>()` → SelectQuery | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| Entry | `Sql.Insert<T>(entity)` | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| Entry | `Sql.BulkInsert<T>(entities)` | Implemented | CORE | P1 | Core | Core | SS/PG/MY/LT | ✅ | High (intentional) | Small | High | **KEEP** |
| Entry | `Sql.Bulk<T>()` → BulkBuilder | Implemented | STRATEGIC | P1 | Core | Core | SS/PG/MY/LT | ✅ | High | Medium | High | **KEEP** |
| Entry | `Sql.Update<T>()` | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| Entry | `Sql.Delete<T>()` | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| Entry | `Sql.Merge<T>()` | Deprecated | DEPRECATED | P4 | Core | — | SS/OR only | ✅ | Low | — | None | **REMOVE in v2.0** |
| Entry | `Sql.InsertFrom<T>(selectQuery)` | Implemented | CORE | P1 | Core | Core | ALL | ✅ | Low | Small | High | **KEEP** |
| Entry | `Sql.Raw(FormattableString)` | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | High | **KEEP** |
| Entry | `Sql.Raw(string, params?)` | Implemented | SUPPORTING | P2 | Core | Core | ALL | ✅ | Low | Trivial | Low | **KEEP but WARN harder** |
| Entry | Query tagging `.WithTag(string)` | Implemented | STRATEGIC | P2 | Core | Core | ALL | ✅ | None | Trivial | High | **KEEP** |

---

## Domain: SELECT Projection

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| SELECT | `SELECT *` (default) | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Medium | **KEEP** (ESQL SQL003 warns) |
| SELECT | Typed projection `Select(x => new {…})` | Implemented | CORE | P0 | Core | Core | ALL | ⚠️ | Low | Medium | Critical | **KEEP** |
| SELECT | String column projection `Select(params string[])` | Implemented | SUPPORTING | P1 | Core | Core | ALL | ✅ | Low | Trivial | Medium | **KEEP** |
| SELECT | Raw SELECT `RawSelect(FormattableString)` | Implemented | SUPPORTING | P2 | Core | Core | ALL | ✅ | Low | Trivial | Medium | **KEEP** |
| SELECT | `SELECT DISTINCT` | Implemented | CORE | P1 | Core | Core | ALL | ✅ | Low | Trivial | High | **KEEP** |
| SELECT | `SELECT DISTINCT ON (col)` | Implemented | DIALECT-SPECIFIC | P2 | PostgreSql | PostgreSql | PG | ✅ | Low | Small | High | **KEEP in PG package** |
| SELECT | Window function in SELECT | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | Medium | Medium | Critical | **KEEP** |
| SELECT | CASE expression in SELECT | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | Low | Medium | High | **KEEP** |
| SELECT | Subquery as FROM | Implemented | CORE | P1 | Core | Core | ALL | ✅ | Low | Medium | High | **KEEP** |
| SELECT | UNNEST FROM | Implemented | DIALECT-SPECIFIC | P3 | PostgreSql | PostgreSql | PG | ✅ | Low | Small | Medium | **KEEP in PG package** |
| SELECT | GROUPING SETS | Implemented | CORE | P2 | Core | Core | SS/PG/MY/OR | ✅ | Low | Small | High | **KEEP** |
| SELECT | ROLLUP | Implemented | CORE | P2 | Core | Core | SS/PG/MY/OR | ✅ | Low | Small | High | **KEEP** |
| SELECT | CUBE | Implemented | CORE | P2 | Core | Core | SS/PG/MY/OR | ✅ | Low | Small | High | **KEEP** |

---

## Domain: WHERE Predicates

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| WHERE | Typed expression `.Where(x => x.Prop == val)` | Implemented | CORE | P0 | Core | Core | ALL | ⚠️ | Low | Medium | Critical | **KEEP** |
| WHERE | Typed AND / OR | Implemented | CORE | P0 | Core | Core | ALL | ⚠️ | Low | Trivial | Critical | **KEEP** |
| WHERE | Raw WHERE `FormattableString` | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | High | **KEEP** |
| WHERE | WHERE EXISTS / NOT EXISTS | Implemented | CORE | P1 | Core | Core | ALL | ✅ | Low | Small | High | **KEEP** |
| WHERE | WhereAll (explicit full-table) | Implemented | CORE | P1 | Core | Core | ALL | ✅ | None | Trivial | High | **KEEP** (suppresses ESQL001) |
| WHERE | BETWEEN (via expression) | Implemented | CORE | P1 | Core | Core | ALL | ⚠️ | Low | Trivial | Medium | **KEEP** |
| WHERE | IN (via `Contains()`) | Implemented | CORE | P0 | Core | Core | ALL | ⚠️ | Low | Trivial | Critical | **KEEP** |
| WHERE | LIKE (StartsWith/EndsWith/Contains) | Implemented | CORE | P0 | Core | Core | ALL | ⚠️ | Low | Trivial | High | **KEEP** |
| WHERE | ILIKE | Implemented | DIALECT-SPECIFIC | P2 | Core (sentinel) | Core | PG | ⚠️ | Low | Trivial | High | **KEEP in Core as sentinel** |
| WHERE | IS NULL / IS NOT NULL | Implemented | CORE | P0 | Core | Core | ALL | ⚠️ | Low | Trivial | High | **KEEP** |
| WHERE | COALESCE in WHERE | Implemented | SUPPORTING | P2 | Core | Core | ALL | ⚠️ | Low | Trivial | Medium | **KEEP** |
| WHERE | Nested predicate groups | Implemented | CORE | P1 | Core | Core | ALL | ⚠️ | Low | Medium | High | **KEEP** |
| WHERE | IS DISTINCT FROM | Implemented | SUPPORTING | P3 | Core | Core | PG/LT | ✅ | None | Small | Medium | **KEEP** |
| WHERE | NULLIF | Implemented | SUPPORTING | P3 | Core | Core | ALL | ✅ | None | Small | Low | **KEEP** |

---

## Domain: JOIN

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| JOIN | INNER / LEFT / RIGHT / FULL / CROSS JOIN | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| JOIN | Typed INNER JOIN with expression | Implemented | STRATEGIC | P1 | Core | Core | ALL | ⚠️ | Low | Medium | High | **KEEP** |
| JOIN | Subquery JOIN | Implemented | CORE | P1 | Core | Core | ALL | ✅ | Low | Medium | High | **KEEP** |
| JOIN | CROSS APPLY | Implemented | DIALECT-SPECIFIC | P2 | Core | Core | SS/PG(→LATERAL) | ✅ | Low | Medium | High | **KEEP** |
| JOIN | OUTER APPLY | Implemented | DIALECT-SPECIFIC | P2 | Core | Core | SS/PG(→LATERAL) | ✅ | Low | Medium | High | **KEEP** |
| JOIN | LATERAL JOIN (explicit, PG) | Implemented | DIALECT-SPECIFIC | P2 | PostgreSql | PostgreSql | PG | ✅ | Low | Medium | High | **KEEP in PG package** |
| JOIN | Outer-reference LATERAL (typed Sql.Outer) | Planned | STRATEGIC | P2 | — | Core + PostgreSql | PG | ⚠️ | Low | Large | High | **BUILD in v1.3.0 (ADR-019)** |

---

## Domain: ORDER BY & Pagination

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| ORDER | ORDER BY ASC/DESC typed | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| ORDER | ThenBy / ThenByDescending | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| ORDER | NULLS FIRST (native PG/OR) | Implemented | DIALECT-SPECIFIC | P1 | Core | Core | PG/OR | ✅ | Low | Trivial | High | **KEEP** |
| ORDER | NULLS LAST (native PG/OR) | Implemented | DIALECT-SPECIFIC | P1 | Core | Core | PG/OR | ✅ | Low | Trivial | High | **KEEP** |
| ORDER | NULLS FIRST (emulated SS via CASE WHEN) | Implemented | DIALECT-SPECIFIC | P1 | SqlServer | SqlServer | SS | ✅ | Negligible | Small | High | **KEEP** |
| ORDER | NULLS FIRST/LAST (emulated MY/LT) | Implemented | DIALECT-SPECIFIC | P2 | MySql/Sqlite | MySql/Sqlite | MY/LT | ✅ | Negligible | Small | High | **KEEP (emulated via CASE WHEN)** |
| ORDER | Dynamic sorting | Implemented | SUPPORTING | P2 | Core | Core | ALL | ✅ | Low | Small | High | **KEEP** |
| PAGING | `.Limit(n)` | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| PAGING | `.Offset(n)` | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| PAGING | `.Page(page, size)` | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | High | **KEEP** |
| PAGING | Window page (ROW_NUMBER-based) | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | Medium | Medium | High | **KEEP** |
| PAGING | Composite keyset cursor `.SeekAfter()` | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | Low | Large | Critical | **KEEP — unique differentiator** |
| PAGING | `.SeekBefore()` | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | Low | Medium | High | **KEEP** |
| PAGING | Oracle ROWNUM pagination (11g legacy) | Implemented | DIALECT-SPECIFIC | P2 | Oracle | Oracle | OR | ✅ | Low | Small | High | **KEEP (FETCH FIRST / ROWNUM)** |

---

## Domain: CTEs & Set Operations

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| CTE | Non-recursive CTE | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Small | High | **KEEP** |
| CTE | Recursive CTE | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | Low | Medium | High | **KEEP** |
| CTE | Multiple CTEs | Implemented | CORE | P1 | Core | Core | ALL | ✅ | Low | Trivial | High | **KEEP** |
| CTE | Materialized / NOT MATERIALIZED hint | Implemented | DIALECT-SPECIFIC | P3 | PostgreSql | PostgreSql | PG | ✅ | None | Trivial | Medium | **KEEP in PG package** |
| CTE | Named WINDOW clause | Implemented | SUPPORTING | P2 | Core | Core | ALL | ✅ | Low | Small | Medium | **KEEP** |
| SET OPS | UNION / UNION ALL | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | High | **KEEP** |
| SET OPS | INTERSECT / EXCEPT | Implemented | CORE | P1 | Core | Core | ALL | ✅ | Low | Trivial | Medium | **KEEP** |
| SET OPS | INTERSECT ALL / EXCEPT ALL | Implemented | SUPPORTING | P3 | Core | Core | SS/PG/MY/OR | ✅ | None | Trivial | Low | **KEEP** |

---

## Domain: Window Functions

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| WINDOW | ROW_NUMBER / RANK / DENSE_RANK / NTILE | Implemented | STRATEGIC | P0 | Core | Core | ALL | ✅ | Low | Medium | Critical | **KEEP** |
| WINDOW | LAG / LEAD / FIRST_VALUE / LAST_VALUE | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | Low | Medium | High | **KEEP** |
| WINDOW | SUM / AVG / COUNT / MIN / MAX OVER | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | Low | Medium | High | **KEEP** |
| WINDOW | PARTITION BY (typed) | Implemented | STRATEGIC | P0 | Core | Core | ALL | ✅ | Low | Small | Critical | **KEEP** |
| WINDOW | ORDER BY in OVER (typed) | Implemented | STRATEGIC | P0 | Core | Core | ALL | ✅ | Low | Small | Critical | **KEEP** |
| WINDOW | FILTER (WHERE) in OVER | Implemented | STRATEGIC | P2 | Core | Core | PG (native); SS/MY/LT/OR (throw) | ✅ | Low | Medium | High | **KEEP (ADR-018/ADR-037)** |
| WINDOW | NTH_VALUE(col, n) | Missing | SUPPORTING | P3 | — | Core | PG/MY/LT | ✅ | None | Small | Low | **BUILD in v1.3.0** |
| WINDOW | ROWS/RANGE/GROUPS frame | Missing | SUPPORTING | P3 | — | Core | ALL | ✅ | Low | Medium | Low | **RAW SQL only for now** |

---

## Domain: DML — INSERT

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| INSERT | Single entity insert | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| INSERT | Multi-row VALUES | Implemented | CORE | P0 | Core | Core | SS/PG/MY/LT | ✅ | High | Small | High | **KEEP** |
| INSERT | INSERT INTO … SELECT | Implemented | CORE | P1 | Core | Core | ALL | ✅ | Low | Small | High | **KEEP** |
| INSERT | INSERT DEFAULT VALUES | Implemented | SUPPORTING | P2 | Core | Core | SS/PG/MY/LT | ✅ | None | Trivial | Low | **KEEP** |
| INSERT | RETURNING clause | Implemented | DIALECT-SPECIFIC | P1 | Core | Core | PG/LT/OR | ✅ | Low | Medium | High | **KEEP** |
| INSERT | OUTPUT clause (SS) | Implemented | DIALECT-SPECIFIC | P1 | Core | Core | SS | ✅ | Low | Medium | High | **KEEP** |
| INSERT | ON CONFLICT … DO UPDATE (PG/LT) | Implemented | DIALECT-SPECIFIC | P1 | Core | Core | PG/LT | ✅ | Low | Medium | Critical | **KEEP** |
| INSERT | ON CONFLICT … DO NOTHING | Implemented | DIALECT-SPECIFIC | P1 | Core | Core | PG/LT | ✅ | Low | Small | High | **KEEP** |
| INSERT | ON DUPLICATE KEY UPDATE (MY emulation) | Implemented | DIALECT-SPECIFIC | P1 | MySql | MySql | MY | ✅ | Low | Medium | High | **KEEP in MY package** |

---

## Domain: DML — UPDATE

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| UPDATE | Typed SET expression | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Small | Critical | **KEEP** |
| UPDATE | Raw SET `FormattableString` | Implemented | SUPPORTING | P1 | Core | Core | ALL | ✅ | Low | Trivial | Medium | **KEEP** |
| UPDATE | SET from entity (all/ignore nulls) | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Small | High | **KEEP** |
| UPDATE | Diff UPDATE (changed props only) | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | Low | Medium | High | **KEEP — rare feature in this class** |
| UPDATE | Optimistic concurrency token | Implemented | STRATEGIC | P1 | Core | Core | ALL | ✅ | None | Medium | High | **KEEP** |
| UPDATE | UPDATE with JOIN | Implemented | CORE | P1 | Core | Core | SS/MY (native); PG(FROM) | ✅ | Low | Medium | High | **KEEP** |
| UPDATE | RETURNING from UPDATE | Implemented | DIALECT-SPECIFIC | P2 | Core | Core | PG/LT/SS(OUTPUT)/OR | ✅ | Low | Medium | High | **KEEP** |

---

## Domain: DML — DELETE

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| DELETE | Typed DELETE with WHERE | Implemented | CORE | P0 | Core | Core | ALL | ✅ | Low | Trivial | Critical | **KEEP** |
| DELETE | WhereAll (explicit full delete) | Implemented | CORE | P1 | Core | Core | ALL | ✅ | Low | Trivial | High | **KEEP** |
| DELETE | DELETE USING (PG) | Implemented | DIALECT-SPECIFIC | P2 | PostgreSql | PostgreSql | PG | ✅ | Low | Small | High | **KEEP in PG package** |
| DELETE | DELETE with JOIN (SS/MY) | Implemented | DIALECT-SPECIFIC | P2 | Core | Core | SS/MY | ✅ | Low | Medium | Medium | **KEEP** |
| DELETE | RETURNING from DELETE | Implemented | DIALECT-SPECIFIC | P2 | Core | Core | PG/LT/SS(OUTPUT)/OR | ✅ | Low | Medium | High | **KEEP** |

---

## Domain: UPSERT / MERGE

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| UPSERT | ON CONFLICT PG/LT | Implemented | DIALECT-SPECIFIC | P0 | Core | Core | PG/LT | ✅ | Low | Medium | Critical | **KEEP** |
| UPSERT | ON DUPLICATE KEY MY (emulated) | Implemented | DIALECT-SPECIFIC | P0 | MySql | MySql | MY | ✅ | Low | Medium | High | **KEEP** |
| UPSERT | SQL Server MERGE via `Sql.Raw()` | Implemented | SUPPORTING | P1 | Core | Core | SS | ✅ | Low | Trivial | Medium | **KEEP as escape hatch** |
| UPSERT | Oracle MERGE via `Sql.Raw()` | Implemented | SUPPORTING | P1 | Core | Core | OR | ✅ | Low | Trivial | Medium | **KEEP as escape hatch** |
| UPSERT | `MergeQuery<T>` generic builder | Deprecated | DEPRECATED | P4 | Core | — | SS/OR | ✅ | Low | — | None | **REMOVE in v2.0** |
| UPSERT | Generic cross-dialect MERGE abstraction | Rejected | REJECTED | P4 | — | — | — | — | — | — | None | **NEVER — ADR-025** |

---

## Domain: AOT & Source Generator

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| AOT | `[SqlEntity]` incremental source generator | Implemented | STRATEGIC | P0 | SourceGenerators | SourceGenerators | ALL | ✅ | Build-time | Large | Critical | **KEEP — expand** |
| AOT | `ISqlEntity` impl generation (table/cols/map) | Implemented | STRATEGIC | P0 | SourceGenerators | SourceGenerators | ALL | ✅ | Build-time | — | Critical | **KEEP** |
| AOT | `Parser` class (zero-reflection IDataReader mapper) | Implemented | STRATEGIC | P0 | SourceGenerators | SourceGenerators | ALL | ✅ | None | — | Critical | **KEEP** |
| AOT | `GetReaderParser()` static abstract member | Planned | STRATEGIC | P0 | — | SourceGenerators | ALL | ✅ | None | Medium | Critical | **BUILD in v2.0 (AOT-004)** |
| AOT | `AotQueryExecutor` (full ADO.NET path) | Implemented | STRATEGIC | P0 | Aot | Aot | ALL | ✅ | Low | — | Critical | **KEEP** |
| AOT | `IsAotCompatible = true` declared in packages | Partial | STRATEGIC | P0 | — | All packages | ALL | ✅ | None | Trivial | Critical | **FIX immediately — TD-002** |
| AOT | `[RequiresDynamicCode]` on expression visitor | Missing | STRATEGIC | P1 | — | Core | ALL | N/A | None | Trivial | High | **ADD — TD-005, STAB-005** |
| AOT | `SqlEntityCache<T>` fallback guard | Partial | STRATEGIC | P1 | Core | Core | ALL | — | None | Trivial | High | **FIX — TD-003, throw** |
| AOT | CI NativeAOT gate | Missing | STRATEGIC | P0 | — | CI | ALL | — | CI | Small | Critical | **ADD — AOT-006, STAB-006** |
| AOT | `IBulkSerializer<T>` source generated | Implemented | STRATEGIC | P1 | SourceGenerators | SourceGenerators | ALL | ✅ | None | — | High | **KEEP** |
| AOT | Filter expression source generated | Implemented | SUPPORTING | P2 | SourceGenerators | SourceGenerators | ALL | ✅ | Build-time | — | Medium | **KEEP** |

---

## Domain: Roslyn Analyzers

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| ANALYZER | ESQL001 DELETE without WHERE → Error | Implemented | STRATEGIC | P0 | Analyzers | Analyzers | ALL | ✅ | Build-time | — | Critical | **KEEP** |
| ANALYZER | ESQL002 SQL injection via string concat | Implemented | STRATEGIC | P0 | Analyzers | Analyzers | ALL | ✅ | Build-time | — | Critical | **KEEP** |
| ANALYZER | ESQL003 UPDATE without WHERE → Error | Implemented | STRATEGIC | P0 | Analyzers | Analyzers | ALL | ✅ | Build-time | — | Critical | **KEEP** |
| ANALYZER | ESQL011 Unsafe `Sql.Raw(string)` overload | Implemented | STRATEGIC | P1 | Analyzers | Analyzers | ALL | ✅ | Build-time | — | High | **KEEP; consider promoting to Error** |
| ANALYZER | ESQL012 Retry inside transaction | Implemented | STRATEGIC | P0 | Analyzers | Analyzers | ALL | ✅ | Build-time | — | Critical | **KEEP** |
| ANALYZER | ESQL020 Dialect-incompatible API | Implemented | STRATEGIC | P1 | Analyzers | Analyzers | ALL | ✅ | Build-time | — | High | **KEEP** |
| ANALYZER | ESQL025 SqlKata migration code fix | Implemented | OPTIONAL | P3 | Analyzers | Analyzers | ALL | ✅ | Build-time | — | Medium | **KEEP** |
| ANALYZER | ESQL026 `Sql.Merge<T>()` — prefer OnConflict | Missing | SUPPORTING | P2 | — | Analyzers | ALL | ✅ | Build-time | Small | Medium | **BUILD — STAB-008** |

---

## Domain: Execution Layer (Dapper)

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| DAPPER | `QueryAsync<T>` (Dapper mapper) | Implemented | ADAPTER | P0 | Dapper | Dapper | ALL | ❌ | Low | Trivial | High | **KEEP — document NOT AOT** |
| DAPPER | `QueryAotAsync<T>(mapper)` (reflection-free) | Implemented | STRATEGIC | P0 | Dapper | Dapper | ALL | ✅ | Low | Small | Critical | **KEEP — primary AOT path** |
| DAPPER | `ExecuteAsync` | Implemented | ADAPTER | P0 | Dapper | Dapper | ALL | ✅ | Low | Trivial | High | **KEEP** |
| DAPPER | `ExecuteScalarAsync<T>` | Implemented | ADAPTER | P1 | Dapper | Dapper | ALL | ✅ | Low | Trivial | Medium | **KEEP** |
| DAPPER | `IAsyncEnumerable<T>` streaming | Implemented | STRATEGIC | P1 | Dapper | Dapper | ALL | ✅ | Low | Medium | High | **KEEP** |
| DAPPER | Multi-mapping 8+ types | Implemented | STRATEGIC | P2 | Dapper.MultiMap | Dapper.MultiMap | ALL | ❌ | Low | Large | High | **KEEP — unique vs Dapper** |
| DAPPER | Dynamic compiler by connection type | Implemented | STRATEGIC | P1 | Dapper | Dapper | ALL | ⚠️ | Low | Small | High | **KEEP** |
| DAPPER | `RegisterTypeHandler<T>` (dual ESQL+Dapper) | Implemented | ADAPTER | P1 | Dapper | Dapper | ALL | ✅ | None | Trivial | High | **KEEP** |
| DAPPER | `IUnitOfWork` + `ISavepoint` | Implemented | STRATEGIC | P0 | Dapper.UnitOfWork | Dapper.UnitOfWork | ALL | ✅ | Low | Medium | High | **KEEP — correct semantics** |
| DAPPER | Polly v8 resilience defaults | Implemented | OPTIONAL | P2 | Dapper.Resilience | Dapper.Resilience | ALL | ✅ | Low | Medium | High | **KEEP — with ESQL012 guard** |
| DAPPER | OTel auto-instrumentation | Implemented | OPTIONAL | P1 | OpenTelemetry | OpenTelemetry | ALL | ✅ | Negligible | Small | High | **KEEP — fix `db.system` tag** |

---

## Domain: Bulk Operations

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| BULK | `IBulkStrategy` plugin model | Implemented | STRATEGIC | P0 | Core | Core | ALL | ✅ | — | Medium | Critical | **KEEP** |
| BULK | `SqlBulkCopy` strategy (SS) | Implemented | DIALECT-SPECIFIC | P0 | SqlServer | SqlServer | SS | ✅ | Extreme | Medium | Critical | **KEEP** |
| BULK | `COPY FROM STDIN` strategy (PG) | Implemented | DIALECT-SPECIFIC | P0 | PostgreSql | PostgreSql | PG | ✅ | Extreme | Large | Critical | **KEEP — unique differentiator** |
| BULK | Multi-row VALUES bulk (MY/LT) | Implemented | DIALECT-SPECIFIC | P1 | MySql/Sqlite | MySql/Sqlite | MY/LT | ✅ | High | Small | High | **KEEP** |
| BULK | Bulk upsert strategies | Implemented | DIALECT-SPECIFIC | P1 | SS/PG/MY | SS/PG/MY | SS/PG/MY | ✅ | High | Large | High | **KEEP** |
| BULK | `AotSqlRendererBase` bulk methods as `abstract` | Partial | CORE | P3 | Core | Core | ALL | ✅ | None | Small | Medium | **FIX — TD-007, INT-003** |
| BULK | Identity retrieval after bulk insert | Missing | SUPPORTING | P3 | — | — | — | — | — | Large | Medium | **DESIGN in v2.0 — TD-016** |

---

## Domain: Package Architecture

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| PACKAGE | Per-dialect isolation | Implemented | STRATEGIC | P0 | All dialect pkgs | Unchanged | ALL | ✅ | Build-time | — | Critical | **KEEP — correct pattern** |
| PACKAGE | `IsAotCompatible = true` declared | Missing | STRATEGIC | P0 | All AOT-safe pkgs | All AOT-safe pkgs | ALL | — | None | Trivial | Critical | **FIX — TD-002** |
| PACKAGE | External Pagination project reference | Broken | STRATEGIC | P0 | Core | Core | ALL | — | Build | Medium | Critical | **FIX — TD-009, STAB-007** |
| PACKAGE | `InternalsVisibleTo` duplicate cleanup | Partial | SUPPORTING | P3 | Core | Core | ALL | — | None | Trivial | Low | **FIX — TD-010** |
| PACKAGE | `Description` field English/encoding fix | Partial | SUPPORTING | P3 | Core | Core | ALL | — | None | Trivial | Low | **FIX — TD-011** |
| PACKAGE | OpenTelemetry net9/net10 targets | Missing | SUPPORTING | P2 | OpenTelemetry | OpenTelemetry | ALL | ✅ | None | Trivial | Medium | **FIX — TD-012, INT-001** |

---

## Domain: Permanently Rejected Features

| Domain | Feature | Status | Classification | Priority | Current Package | Target Package | Dialects | AOT | Performance Impact | Complexity | Strategic Value | Decision |
|--------|---------|--------|----------------|----------|----------------|----------------|----------|-----|--------------------|------------|-----------------|----------|
| ORM | Change tracking | Rejected | REJECTED | P4 | — | — | — | — | — | Extreme | None | **NEVER — ADR-007** |
| ORM | Navigation properties / lazy loading | Rejected | REJECTED | P4 | — | — | — | — | — | Extreme | None | **NEVER — ADR-007** |
| ORM | Identity map / first-level cache | Rejected | REJECTED | P4 | — | — | — | — | — | Extreme | None | **NEVER — ADR-007** |
| ORM | LINQ IQueryable provider | Rejected | REJECTED | P4 | — | — | — | — | — | Extreme | None | **NEVER — ADR-008** |
| ORM | Database migrations | Rejected | OUT-OF-SCOPE | P4 | — | — | — | — | — | Extreme | None | **NEVER — different tool** |
| INFRA | Automatic query caching | Rejected | REJECTED | P4 | — | — | — | — | — | Large | None | **NEVER — ADR-024** |
| INFRA | DI / `IServiceCollection` in Core | Rejected | REJECTED | P4 | — | — | — | — | — | Medium | None | **NEVER — ADR-023** |
| INFRA | Distributed transactions (MSDTC/XA) | Rejected | OUT-OF-SCOPE | P4 | — | — | — | — | — | Extreme | None | **NEVER** |
| SAFETY | Automatic retry of mutations | Rejected | REJECTED | P4 | — | — | — | — | — | Medium | None | **NEVER — ADR-016, ESQL012** |
| SQL | Generic cross-dialect MERGE abstraction | Rejected | REJECTED | P4 | — | — | — | — | — | Large | None | **NEVER — ADR-025** |
| BIZ | Soft-delete global filter | Rejected | OUT-OF-SCOPE | P4 | — | — | — | — | — | Medium | None | **NEVER — business logic** |
| BIZ | Multi-tenancy global filter | Rejected | OUT-OF-SCOPE | P4 | — | — | — | — | — | Medium | None | **NEVER — business logic** |
| BIZ | Audit field automation | Rejected | OUT-OF-SCOPE | P4 | — | — | — | — | — | Small | None | **NEVER — business logic** |
| ARCH | IL Emit / dynamic proxies | Rejected | REJECTED | P4 | — | — | — | — | — | — | None | **NEVER — kills AOT** |
| ARCH | Repository pattern implementation | Rejected | OUT-OF-SCOPE | P4 | — | — | — | — | — | — | None | **NEVER — ADR-027** |
| ARCH | Specification pattern in Core | Rejected | OUT-OF-SCOPE | P4 | — | — | — | — | — | — | None | **Optional adapter only — ADR-026** |

---

*This matrix is the result of full source-code inspection. Every "Implemented" claim is verified against actual source.*
*Re-run this audit after every non-trivial change to compiler, dialect, AOT, or package architecture.*
