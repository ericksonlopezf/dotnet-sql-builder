# ADR-008: No LINQ IQueryable Provider

## Status
Accepted

## Context
A full `IQueryable<T>` LINQ provider would allow standard LINQ chains (`.Where()`, `.Select()`, `.OrderBy()`, etc.) to be compiled to SQL at query execution time.

## Problem
Implementing a correct and complete `IQueryable<T>` provider requires:

1. **Handling 50+ LINQ operators** — `GroupJoin`, `SelectMany`, `Aggregate`, `Skip`, `Take`, `Any`, `All`, `Count`, `Sum`, `Average`, `Min`, `Max`, `Distinct`, `Concat`, `Intersect`, `Except`, `Reverse`, `Zip`, etc.
2. **Runtime translation errors** — operators not supported throw `NotSupportedException` at runtime, not compile time. This is the most common complaint with EF Core providers.
3. **Maintained forever** — every new LINQ operator in future .NET versions must be handled.
4. **Expression tree edge cases** — `let`, `into`, transparent identifiers, query comprehensions generate complex expression trees.
5. **Product redefinition** — implementing a LINQ provider converts SqlBuilder into "EF Core Lite" — exactly what it should NOT be.

## Options Considered

### Option A: Full IQueryable provider
- Rejected: Unsustainable maintenance, complex errors, product redefinition

### Option B: Partial IQueryable (only safe operators)
- Rejected: Partial providers confuse users with inconsistent behavior

### Option C: Expression trees only for WHERE, SELECT, ORDER BY (current approach)
- Chosen: Bounded, well-understood semantics

## Decision
No `IQueryable<T>` implementation. Expression trees are used only for:
- `SELECT` projection: `Expression<Func<T, TResult>>`
- `WHERE` predicate: `Expression<Func<T, bool>>`
- `ORDER BY` key: `Expression<Func<T, object>>`
- `JOIN` condition: `Expression<Func<T, TOther, bool>>`

These have bounded, testable semantics. The SQL they generate is predictable.

## Consequences
### Positive
- No runtime `NotSupportedException`
- Predictable SQL generation
- No LINQ provider maintenance burden

### Negative
- Cannot use LINQ comprehension syntax directly
- Users familiar with EF Core LINQ must adapt to the fluent API

### Mitigation
The fluent API is clean and discoverable:
```csharp
// Instead of LINQ:
var q = from u in db.Users where u.IsActive orderby u.Name select new { u.Id, u.Name };

// SqlBuilder:
var q = Sql.From<User>()
           .Select(u => new { u.Id, u.Name })
           .Where(u => u.IsActive)
           .OrderBy(u => u.Name);
```

## API Impact
None — current Expression Tree API is the correct design.

## Reconsideration Criteria
If a source-generator-based LINQ provider technology emerges that provides compile-time operator validation (no runtime `NotSupportedException`), revisit this decision.
