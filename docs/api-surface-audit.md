# API Surface Audit — EricksonLopez.SqlBuilder

> **Purpose:** Enumerates every public API entry point, its stability classification,
> and marks any surface areas that require deprecation or breaking-change management.
> Last audit: 2026-08-14

---

## Stability Classification

| Class | Meaning |
|-------|---------|
| `STABLE` | API is locked; no breaking changes without a major version bump |
| `PREVIEW` | API is functional but shape may change in minor versions |
| `DEPRECATED` | API is `[Obsolete]`; will be removed in a future major version |
| `INTERNAL` | Not public API; implementation detail |

---

## 1. Entry Points (`Sql` static class)

| Method | Signature | Stability |
|--------|-----------|----------|
| `Sql.From<T>()` | `→ SelectQuery<T>` | STABLE |
| `Sql.Insert<T>(entity)` | `→ InsertQuery<T>` | STABLE |
| `Sql.Insert<T>(null!)` | `→ InsertQuery<T>` (builder mode) | STABLE |
| `Sql.Update<T>()` | `→ UpdateQuery<T>` | STABLE |
| `Sql.Delete<T>()` | `→ DeleteQuery<T>` | STABLE |
| `Sql.Merge<T>()` | `→ MergeQuery<T>` | DEPRECATED |
| `Sql.Raw(FormattableString)` | `→ RawQuery` | STABLE |
| `Sql.Raw(string)` *(unsafe)* | `→ RawQuery` | STABLE (ESQL011 warns) |
| `Sql.RegisterTypeHandler<T>(handler)` | `void` | STABLE |

---

## 2. `SelectQuery<T>` Methods

| Method | Notes | Stability |
|--------|-------|----------|
| `.Select(params string[])` | String columns | STABLE |
| `.Select(expr)` | Typed `x => new { x.Col }` | STABLE |
| `.SelectRaw(FormattableString)` | Raw SELECT | STABLE |
| `.SelectCase(configure)` | CASE expression builder | STABLE |
| `.SelectCase(CaseNode)` | Pre-built node | STABLE |
| `.Select(WindowFunctionNode[])` | Window functions | STABLE |
| `.Distinct()` | `SELECT DISTINCT` | STABLE |
| `.DistinctOn(col)` | PG-only | STABLE |
| `.From(tableName, alias)` | Override table | STABLE |
| `.From(subquery, alias)` | Subquery FROM | STABLE |
| `.Unnest(arrays, alias)` | PG-only | STABLE |
| `.InnerJoin(table, alias, on)` | String condition | STABLE |
| `.LeftJoin(...)` / `.RightJoin(...)` / `.FullJoin(...)` / `.CrossJoin(...)` | All join types | STABLE |
| `.InnerJoin<TOther>(alias, on)` | Typed join | STABLE |
| `.JoinRaw(FormattableString)` | Raw JOIN | STABLE |
| `.JoinSubquery(subquery, alias, on)` | Subquery join | STABLE |
| `.CrossApply(subquery, alias)` | `[RequiresCapability]` | STABLE |
| `.OuterApply(subquery, alias)` | `[RequiresCapability]` | STABLE |
| `.LateralJoin(subquery, alias, on)` | PG-only | STABLE |
| `.Where(Expression<Func<T, bool>>)` | Typed | STABLE |
| `.Where(FormattableString)` | Raw | STABLE |
| `.And(expr)` / `.Or(expr)` | Boolean composition | STABLE |
| `.WhereExists(subquery)` / `.WhereNotExists(...)` | EXISTS | STABLE |
| `.OrExists(...)` / `.OrNotExists(...)` | OR EXISTS | STABLE |
| `.WhereAll()` | Explicit full-table | STABLE |
| `.WhereGroup(g => ...)` | Nested group | STABLE |
| `.GroupBy(params string[])` | | STABLE |
| `.Having(expr)` / `.Having(FormattableString)` | | STABLE |
| `.OrHaving(...)` | | STABLE |
| `.OrderBy(keySelector)` | ASC | STABLE |
| `.OrderBy(keySelector, NullsPosition)` | With null control | STABLE |
| `.OrderByDescending(keySelector)` | DESC | STABLE |
| `.OrderByDescending(keySelector, NullsPosition)` | | STABLE |
| `.ThenBy(...)` / `.ThenByDescending(...)` | Secondary sort | STABLE |
| `.OrderBy(FormattableString)` / `.OrderByDescending(FormattableString)` | Raw ORDER BY | STABLE |
| `.Limit(int)` | | STABLE |
| `.Offset(int)` | | STABLE |
| `.Page(pageNumber, pageSize)` | Convenience wrapper | STABLE |
| `.Fetch(int)` | | DEPRECATED → use `.Limit()` |
| `.WindowPage(page, size, col, desc)` | ROW_NUMBER pagination | STABLE |
| `.SeekAfter(CursorKey[])` | Forward cursor | STABLE |
| `.SeekBefore(CursorKey[])` | Backward cursor | STABLE |
| `.CTE(name, query)` | Non-recursive | STABLE |
| `.RecursiveCTE(name, query)` | Recursive | STABLE |
| `.Window(name, partitionBy, orderBy)` | Named window | STABLE |
| `.Union(query)` / `.UnionAll(query)` | Set ops | STABLE |
| `.Intersect(query)` / `.Except(query)` | Set ops | STABLE |
| `.WithTag(string)` | Diagnostic tag | STABLE |
| `.Build(ISqlCompiler)` | Compiles AST | STABLE |
| `.AddNode(ISqlNode)` | Low-level extension | PREVIEW |

---

## 3. `InsertQuery<T>` Methods

| Method | Notes | Stability |
|--------|-------|----------|
| `.Into(tableName)` | Override target table | STABLE |
| `.Values(col, val)` / `.Values(col, val, col, val...)` | Multi-column | STABLE |
| `.Bulk(IEnumerable<T>, ignoreNulls)` | Multi-row VALUES | STABLE |
| `.DefaultValues()` | `INSERT DEFAULT VALUES` | STABLE |
| `.FromSelect(ISqlQuery, params string[])` | INSERT INTO … SELECT | STABLE |
| `.Returning(params string[])` | Column names | STABLE |
| `.Returning<TResult>(expr)` | Typed expression | STABLE |
| `.OnConflict(params string[])` | Conflict target cols | STABLE |
| `.OnConflict(Expression<Func<T, object>>)` | Typed conflict | STABLE |
| `.DoNothing()` | Conflict → ignore | STABLE |
| `.DoUpdate(Expression<Func<T, object>>)` | Conflict → update | STABLE |
| `.DoUpdate(FormattableString)` | Raw conflict update | STABLE |
| `.WithTag(string)` | | STABLE |
| `.Build(ISqlCompiler)` | | STABLE |

---

## 4. `UpdateQuery<T>` Methods

| Method | Notes | Stability |
|--------|-------|----------|
| `.Update(tableName?)` | Override table | STABLE |
| `.Set<TVal>(expr, value)` | Typed SET | STABLE |
| `.Set(FormattableString)` | Raw SET | STABLE |
| `.Set(entity, ignoreNulls)` | Entity-based SET | STABLE |
| `.ApplyDiff(original, current)` | Extension method | STABLE |
| `.Where(expr)` / `.Where(FormattableString)` | | STABLE |
| `.WhereAll()` | | STABLE |
| `.InnerJoin(...)` etc. | JOIN in UPDATE | STABLE |
| `.WithConcurrencyToken(col, expected, newVal, autoInc)` | Optimistic locking | STABLE |
| `.Returning(cols[])` / `.Returning<TResult>(expr)` | RETURNING/OUTPUT | STABLE |
| `.WithTag(string)` | | STABLE |
| `.Build(ISqlCompiler)` | | STABLE |

---

## 5. `DeleteQuery<T>` Methods

| Method | Notes | Stability |
|--------|-------|----------|
| `.Delete(tableName?)` | Override table | STABLE |
| `.Using(tableName, alias)` | PG USING clause | STABLE |
| `.Using<TOther>(alias)` | Typed USING | STABLE |
| `.InnerJoin(...)` etc. | JOIN in DELETE | STABLE |
| `.Where(expr)` / `.Where(FormattableString)` | | STABLE |
| `.WhereAll()` | | STABLE |
| `.Returning(cols[])` / `.Returning<TResult>(expr)` | | STABLE |
| `.WithTag(string)` | | STABLE |
| `.Build(ISqlCompiler)` | | STABLE |

---

## 6. `MergeQuery<T>` (DEPRECATED)

| Method | Stability |
|--------|----------|
| `.Into(targetTable)` | DEPRECATED |
| `.Using(sourceTable, alias)` | DEPRECATED |
| `.On(Expression<Func<T, bool>>)` / `.On(FormattableString)` | DEPRECATED |
| `.WhenMatchedThenUpdate(FormattableString)` | DEPRECATED |
| `.WhenNotMatchedThenInsert(FormattableString)` | DEPRECATED |

> **Replacement:** Use `InsertQuery<T>.OnConflict(...).DoUpdate(...)` for PG/MY/SQLite.
> Use `Sql.Raw()` for SQL Server MERGE syntax.

---

## 7. Window Function API (`Window` / `WindowBuilder<T>`)

| Method | Stability |
|--------|----------|
| `Window.RowNumber<T>()` through `Window.Max<T, TKey>(sel)` | STABLE |
| `.PartitionBy(expr)` / `.PartitionBy(string)` | STABLE |
| `.OrderBy(expr)` / `.OrderByDescending(expr)` | STABLE |
| `.As(alias)` | STABLE |

---

## 8. Dapper Integration API

| Method | Stability |
|--------|----------|
| `DapperExtensions.RegisterCompiler<TConnection>(factory)` | STABLE |
| `DapperExtensions.RegisterTypeHandler<T>(handler)` | STABLE |
| `DapperExtensions.GetCompiler(IDbConnection)` | STABLE |
| `connection.QueryAsync<T>(query, compiler, ct)` | STABLE (not AOT) |
| `connection.QueryAotAsync<T>(query, mapper, compiler, ct)` | STABLE (AOT) |
| `connection.QueryFirstOrDefaultAsync<T>(...)` | STABLE |
| `connection.ExecuteAsync(query, compiler, ct)` | STABLE |
| `connection.ExecuteScalarAsync<T>(...)` | STABLE |
| `connection.QueryMultipleAsync(...)` | STABLE |
| `connection.QueryAsAsyncEnumerable<T>(...)` | STABLE |
| `MultiMapBuilder<...>.MapAsync(splitOn, mapper)` | STABLE |

---

## 9. Resilience API

| Method | Stability |
|--------|----------|
| `connection.ExecuteWithResilienceAsync(query, pipeline, ...)` | STABLE |
| `connection.QueryWithResilienceAsync<T>(...)` | STABLE |
| `connection.QueryFirstWithResilienceAsync<T>(...)` | STABLE |
| `SqlResilienceDefaults.Standard(detector)` | STABLE |
| `SqlServerTransientErrorDetector.Default` | STABLE |
| `PostgreSqlTransientErrorDetector.Default` | STABLE |
| `MySqlTransientErrorDetector.Default` | STABLE |

---

## 10. Unit of Work API

| Method | Stability |
|--------|----------|
| `connection.BeginUnitOfWorkAsync(level, ct)` | STABLE |
| `IUnitOfWork.CommitAsync(ct)` | STABLE |
| `IUnitOfWork.RollbackAsync(ct)` | STABLE |
| `IUnitOfWork.CreateSavepointAsync(name, ct)` | STABLE |
| `IUnitOfWork.Transaction` | STABLE |
| `IUnitOfWork.IsolationLevel` | STABLE |
| `ISavepoint.RollbackAsync(ct)` | STABLE |
| `ISavepoint.ReleaseAsync(ct)` | STABLE |

---

## 11. Source Generator Public Output

All generated members are `public` and form part of the contract for AOT users:

| Generated Member | Stability |
|-----------------|----------|
| `T.TableName` (const string) | STABLE |
| `T.Columns` (static class with string consts) | STABLE |
| `T.SelectAllTemplate` (readonly string) | STABLE |
| `T.PropertyMap` (IReadOnlyDictionary) | STABLE |
| `T.Metadata` (IEntityMetadata<T>) | STABLE |
| `T.GetReaderParser()` | STABLE |
| `T.FromReader(IDataReader)` | STABLE |
| `T.SqlAlias` (inner class) | STABLE |
| `T.Parser` (inner class) | PREVIEW |
| `T.GetColumnNames()` / `T.GetValues()` / etc. | STABLE |

---

## Deprecation Timeline

| API | Deprecated Since | Remove In |
|-----|-----------------|-----------|
| `SelectQuery<T>.Fetch(int)` | v0.8 | v2.0 |
| `MergeQuery<T>` + all methods | v1.0 | v2.0 |
| `Sql.Merge<T>()` | v1.0 | v2.0 |

---

*This document must be updated for every public API addition, deprecation, or removal.*
