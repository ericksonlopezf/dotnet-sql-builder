# Level 04: Advanced Integration

**Context:** Demonstrates all advanced query-building capabilities: subquery joins, lateral joins, APPLY operations, CASE expressions, window functions, set operations, CTE with materialization hints, ApplyDiff, and typed predicates.

---

## Topics Covered

| # | Feature | API |
|---|---------|-----|
| 1 | Scalar subquery in SELECT | `Select(ISqlQuery, alias)` |
| 2 | CTE — Common Table Expression | `CTE(name, query)` |
| 3 | CASE expression builder | `CaseExpressionBuilder.When/Then/Else/As` |
| 4 | Window functions | `Window.RowNumber / Sum / Rank ...` |
| 5 | Subquery JOIN | `JoinSubquery(IAstQuery, alias, on)` |
| 6 | INSERT INTO ... SELECT | `Sql.InsertFrom<T>(selectQuery, columns)` |
| 7 | WHERE EXISTS / NOT EXISTS | `WhereExists / WhereNotExists` |
| 8 | Set operations | `Union / UnionAll / Intersect / IntersectAll / Except / ExceptAll` |
| 9 | Sql.Raw | `Sql.Raw(FormattableString)` |
| 10 | DISTINCT | `Distinct()` |
| 11 | Join variants | `InnerJoin / LeftJoin / RightJoin / FullJoin / CrossJoin` |
| 12 | Typed Join | `Join<TOther>(Expression<Func<T, TOther, bool>>)` |
| 14 | OrExists / OrNotExists | `OrExists / OrNotExists` |
| 15 | From variants | `From(tableName, alias) / From(ISqlQuery, alias) / Alias()` |
| 16 | RawSelect | `RawSelect(FormattableString)` |
| 17 | RawJoin | `RawJoin(FormattableString)` |
| 18 | Scalar subquery | `Select(ISqlQuery subquery, alias)` |
| 19 | Window function catalog | All 18 Window factory methods |
| 20 | LateralJoin / LateralLeftJoin | 4 overloads each |
| 21 | JoinSubquery / LeftJoinSubquery | Typed + raw overloads |
| 22 | CrossApply / OuterApply | Factory + direct overloads |
| 23 | CTE MaterializationHint | `Materialized / NotMaterialized` |
| 24 | ThenBy / ThenByDescending | Multi-key ORDER BY |
| 25 | OrHaving | `OrHaving(Expression) / OrHaving(FormattableString)` |
| 26 | WhereDay | `WhereDay(col, op, day)` |
| 27 | OrderBy(FormattableString) | Raw ORDER BY |
| 28 | Offset(int) | Skip-only |
| 29 | Limit(int) | Restrict row count |
| 30 | And / Or | Logical predicates |
| 31 | ApplyDiff | `DiffUpdateExtensions.ApplyDiff<T>(original, modified)` |
| 32 | LateralJoin typed ON | `LateralJoin<TSub>(IAstQuery, alias, Expression ON)` |
| 33 | LateralLeftJoin typed ON | `LateralLeftJoin<TSub>(IAstQuery, alias, Expression ON)` |
| 34 | JoinSubquery typed ON | `JoinSubquery<TSub>(IAstQuery, alias, Expression ON)` |
| 35 | RecursiveCTE + hint | `RecursiveCTE(name, query, MaterializationHint)` |

---

## Key API Design Patterns

### ApplyDiff Chain
```csharp
// CORRECT: ApplyDiff returns IUpdateWhereBuilder<T> — use .And() not .Where()
int id = entity.Id;
Sql.Update<T>().ApplyDiff(original, modified).And(e => e.Id == id)
```

### CTE Materialization
```csharp
.CTE("name", subquery, MaterializationHint.Materialized)      // force temp table
.CTE("name", subquery, MaterializationHint.NotMaterialized)   // force inline
```

---

## Reference

**Source:** [`Level04_AdvancedIntegration/AdvancedIntegrationSample.cs`](../../samples/EricksonLopez.SqlBuilder.Samples/Level04_AdvancedIntegration/AdvancedIntegrationSample.cs)
