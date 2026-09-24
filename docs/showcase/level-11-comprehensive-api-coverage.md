# Level 11: Comprehensive API Coverage

**Context:** Systematic coverage of all 212 public API methods, verifying no API is left undocumented. This level is the executable proof that the Showcase is complete.

---

## Coverage Scope

| Category | Count | Verified |
|----------|-------|---------|
| `SelectQuery<T>` methods | 87 | ✅ |
| `InsertQuery<T>` methods | 18 | ✅ |
| `UpdateQuery<T>` methods | 22 | ✅ |
| `DeleteQuery<T>` methods | 12 | ✅ |
| `Sql` static entry points | 11 | ✅ |
| Extension methods (Dapper) | 24 | ✅ |
| Extension methods (Pagination) | 6 | ✅ |
| Extension methods (DiffUpdate) | 1 | ✅ |
| Extension methods (DynamicSort) | 2 | ✅ |
| Extension methods (CursorPagination) | 4 | ✅ |
| Window functions | 18 | ✅ |
| Compilers | 7 | ✅ |
| **Total** | **212** | **✅** |

---

## Coverage Methodology

Every API in this level is verified by:
1. Calling the method directly in executable C# code
2. Building a `SqlResult` and printing the generated SQL
3. Asserting that the output is non-null

The level **will not compile** if any API used doesn't exist — making it a living integration test.

---

## Key Design Patterns Verified

### All Overloads of SeekAfter/SeekBefore
```csharp
query.SeekAfter(new CursorKey("id", 50))                              // 1 key
query.SeekAfter(new CursorKey("priority", 2), new CursorKey("id", 50)) // 2 keys
query.SeekBefore(new CursorKey("id", 50))
```

### All Join Overloads
```csharp
.InnerJoin<TOther>(expr ON)     // typed ON
.LeftJoin<TOther>(expr ON)      // typed ON
.RightJoin<TOther>(expr ON)     // typed ON
.FullJoin<TOther>(expr ON)      // typed ON
.CrossJoin<TOther>()            // no ON
.LateralJoin(IAstQuery, alias)  // raw
.LateralJoin<TSub>(IAstQuery, alias, expr ON)  // typed
.JoinSubquery(IAstQuery, alias, string ON)     // raw string
.JoinSubquery<TSub>(IAstQuery, alias, expr ON) // typed
```

### All Window Functions
```csharp
Window.RowNumber() / Rank() / DenseRank() / Ntile(n)
Window.Lag(col) / Lead(col)
Window.Sum(col) / Avg(col) / Count(col) / Min(col) / Max(col)
Window.FirstValue(col) / LastValue(col) / NthValue(col, n)
Window.CumeDist() / PercentRank() / StdDev(col) / Variance(col)
```

---

## Reference

**Source:** [`Level11_ComprehensiveApiCoverage/ComprehensiveApiCoverageSample.cs`](../../samples/EricksonLopez.SqlBuilder.Samples/Level11_ComprehensiveApiCoverage/ComprehensiveApiCoverageSample.cs)  
**Verified:** `dotnet run` exits with code 0, printing `[OK] ALL 212 API METHODS VERIFIED`
