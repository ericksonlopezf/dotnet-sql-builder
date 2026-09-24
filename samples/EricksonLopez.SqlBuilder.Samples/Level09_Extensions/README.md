# Level 9: Extensions & Diagnostics

## Overview

Level 9 demonstrates ecosystem utilities: query structural fingerprinting (`GetFingerprint()`), contract schema validation (`GetContract()`), query projection (`ProjectTo`), functional Result wrapping (`ToResultAsync`, `ToPagedListAsync`), query tagging (`WithTag`), and safe raw SQL interpolation (`Sql.Raw`).

---

## Utility Features

1. **Query Structural Fingerprint (`GetFingerprint()`)**:
   Produces a deterministic SHA-256 hash of the query's AST shape, invariant to parameter values. Perfect for building prepared statement caches or tracking query frequency in observability systems.
2. **Contract Schema Validation (`GetContract()`)**:
   Extracts all referenced tables and columns from the query AST, enabling automated deployment verification against schema migration scripts.
3. **Functional Result Pattern**:
   Wraps query execution in a functional `Result<T>` to handle errors without throwing exceptions across architectural boundaries.

---

## Code Examples

### Query Fingerprinting & Caching
```csharp
var q1 = Sql.From<Report>().Where(r => r.Year == 2024).Limit(10);
var q2 = Sql.From<Report>().Where(r => r.Year == 2025).Limit(10);

// Both share the exact same structural fingerprint
bool sameShape = q1.GetFingerprint() == q2.GetFingerprint(); // True
```

### Contract Inspection
```csharp
QueryContract contract = q1.GetContract();
Console.WriteLine($"Referenced Tables: {string.Join(", ", contract.Tables)}");
Console.WriteLine($"Referenced Columns: {string.Join(", ", contract.Columns)}");
```

---

## Execution Output

```text
=== LEVEL 9: EXTENSIONS AND ADVANCED UTILITIES ===
[+] 1. SqlResult — Inspect compiled SQL and its parameters
[+] 2. GetFingerprint() — Hash identifier for query structure
[+] 3. GetContract() — Contract of query tables and columns
[+] 4. ProjectTo<T, TResult> — Reuse query with different result type
[+] 5. ToResultAsync — Result<IReadOnlyList<T>> for functional error handling
[+] 6. ToPagedListAsync — Result<IPagedList<T>> unified
[+] 7. WithTag — Query tagging for OpenTelemetry traceability
[+] 8. Sql.Raw — Raw SQL with safe interpolation (never concatenation)
```
