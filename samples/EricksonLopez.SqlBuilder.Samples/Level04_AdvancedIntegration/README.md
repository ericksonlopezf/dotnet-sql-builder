# Level 4: Advanced Integration

## Overview

Level 4 covers deep database integration scenarios: managing strict transaction boundaries, implementing the Specification Pattern via `ISqlFilter<T>`, complex `CASE` expression building, typed joins, subquery joins, lateral joins, and set operations.

---

## Architectural Patterns

1. **Transaction Boundaries**:
   Combine multiple queries into an atomic transaction using ADO.NET `DbTransaction` without losing query builder parameterization.
2. **Specification Pattern (`ISqlFilter<T>`)**:
   Encapsulate business filtering logic into reusable, composable filter classes that can be dynamically applied to queries.
3. **Conditional Logic in SQL (`SelectCase` Builder)**:
   Generate SQL `CASE WHEN ... THEN ... ELSE ... END` structures entirely with strongly-typed parameters.

---

## Key Code Patterns

### Transaction Scoping
```csharp
using var transaction = connection.BeginTransaction();
try
{
    var withdraw = Sql.Update<Account>().Set(a => a.Balance, 900m).Where(a => a.Id == 1);
    var deposit  = Sql.Update<Account>().Set(a => a.Balance, 150m).Where(a => a.Id == 2);

    await connection.ExecuteAsync(withdraw, transaction);
    await connection.ExecuteAsync(deposit, transaction);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### Specification Filters
```csharp
public class ActiveAccountsFilter : ISqlFilter<Account>
{
    public SelectQuery<Account> Apply(SelectQuery<Account> query)
        => query.Where(a => a.IsActive);
}

var query = Sql.From<Account>().ApplyFilters(new ActiveAccountsFilter(), new MinimumBalanceFilter(100m));
```

### CASE Expression Builder
```csharp
var caseQuery = Sql.From<Order>()
    .SelectCase(c => c
        .When("status = {0}", "completed").Then("'DONE'")
        .When("status = {0}", "pending").Then("'WAITING'")
        .Else("'UNKNOWN'")
        .As("status_label"));
```

---

## Execution Output

```text
=== LEVEL 4: ADVANCED INTEGRATION ===
[+] 1. Transactions (Funds transfer)
    Transfer committed successfully.
[+] 2. Specification Pattern with ISqlFilter<T>
    Valid account: Id=1, Balance=900
[+] 3. CASE Expression with SelectCase builder
[+] 4. Window Functions (ROW_NUMBER, RANK, SUM)
[+] 5. Subquery Join
```
