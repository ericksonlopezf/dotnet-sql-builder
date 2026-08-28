# ESQL011 — Unsafe `Sql.Raw(string)` Overload

## Summary

Using `Sql.Raw(string, ...)` passes raw SQL strings to the query builder without parameterizing interpolated values. This is a potential SQL injection vector.

## Rule Details

**Diagnostic ID:** ESQL011  
**Category:** Security  
**Severity:** Warning  
**Analyzer:** `RawStringOverloadAnalyzer`

### Violation

```csharp
// ❌ ESQL011 warning — string overload does not parameterize arguments
var name = GetNameFromInput();
var query = Sql.Raw("SELECT * FROM users WHERE name = '" + name + "'");
```

### Fix

```csharp
// ✅ Safe — use FormattableString overload
var name = GetNameFromInput();
var query = Sql.Raw($"SELECT * FROM users WHERE name = {name}");
// Generates: SELECT * FROM users WHERE name = @p0 with @p0 = name
```

## Rationale

The `Sql.Raw(FormattableString)` overload automatically converts all interpolated holes into named parameters. The `Sql.Raw(string, object?)` overload is marked `[Obsolete]` and should never be used in new code.

## See Also

- [ADR-002: Security Model](../../SECURITY.md)
- [ESQL002 — Unsafe string concatenation in lambda SQL](./ESQL002.md)
