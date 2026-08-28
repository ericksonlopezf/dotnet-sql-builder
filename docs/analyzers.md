# Roslyn Analyzers Reference

`EricksonLopez.SqlBuilder.Analyzers` provides a suite of Roslyn Analyzers that evaluate SQL generation logic at compile-time — in the IDE and in CI builds.

---

## Quick Reference

| Rule ID | Analyzer File | Severity | Description |
|---------|--------------|----------|-------------|
| **ESQL001** | `DeleteWithoutWhereAnalyzer` | Error | `DELETE` without `WHERE` clause |
| **ESQL002** | `UnsafeStringConcatenationAnalyzer` | Error | Raw string concatenation (SQL injection risk) |
| **ESQL003** | `DeleteWithoutWhereAnalyzer` | Error | `UPDATE` without `WHERE` clause |
| **ESQL004** | `QueryPerformanceAnalyzer` | Warning | Query performance concern |
| **ESQL005** | `DapperCompilerAnalyzer` | Warning | Dapper compiler misconfiguration |
| **ESQL006** | `JoinConditionAnalyzer` | Warning | Missing `ON` condition in `JOIN` |
| **ESQL007** | `MissingIndexAnalyzer` | Info | Potential missing index hint |
| **ESQL008** | `LargeOffsetAnalyzer` | Warning | Large `OFFSET` value (keyset pagination recommended) |
| **ESQL009** | `LikeWildcardAnalyzer` | Warning | `LIKE` leading wildcard (non-sargable) |
| **ESQL010** | `LikeWildcardAnalyzer` | Warning | `LIKE` wildcard usage concern |
| **ESQL011** | `RawStringOverloadAnalyzer` | Warning | `Sql.Raw(string)` unsafe overload usage |
| **ESQL012** | `RetryInsideTransactionAnalyzer` | Warning | Retry pipeline wraps a transaction (data corruption risk) |
| **ESQL020** | `DialectSpecificOverloadAnalyzer` | Warning | Dialect-specific API called with incompatible compiler |
| **ESQL021** | `MissingSourceGeneratorAnalyzer` | Warning | `[SqlEntity]` model without Source Generator configured |
| **ESQL022** | `TypeMapRegistrationAnalyzer` | Warning | Type mapping registration issue |
| **ESQL023** | `SyncOnUiThreadAnalyzer` | Warning | Synchronous SQL call on UI thread |
| **ESQL024** | `CartesianJoinAnalyzer` | Warning | Cartesian product (missing join condition) |
| **ESQL025** | `SqlKataMigrationAnalyzer` | Info | SqlKata API detected — migration code fix available |
| **SQL003** | `SelectStarAnalyzer` | Warning | `SELECT *` usage |
| **SQL004** | `RedundantWhereAnalyzer` | Warning | Redundant `WHERE` condition |
| **SQL009** | `MissingColumnAnalyzer` | Warning | Missing column reference |

---

## Rule Naming Convention

The codebase contains two prefixes reflecting an in-progress migration:

| Prefix | Status | Description |
|--------|--------|-------------|
| `SQL0xxx` | Legacy (Shipped) | Early rules shipped before the naming standardization |
| `ESQL` | Current | All new and future rules use this prefix |

**Legacy rules** (`SQL003`, `SQL004`, `SQL009`) remain in the shipped package for backward compatibility. They will not be renamed (would be a breaking change for consumers who have `#pragma warning disable SQL003` in their code). All new rules use `ESQL`.

---

## Shipped vs Unshipped Rules

Rules are managed via `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` in the `Analyzers` project.

> **Note:** The exact shipped/unshipped boundary is enforced by `Microsoft.CodeAnalysis.PublicApiAnalyzers`. The authoritative list is in the source files. The table above reflects all rules discovered in the current source.

---

## Detailed Rule Descriptions

### ESQL001 — DELETE without WHERE

**Severity:** Error  
**Category:** SQL Safety  
**File:** `DeleteWithoutWhereAnalyzer.cs`

Fires when `Sql.Delete<T>()` is compiled without a `.Where()` clause. This prevents accidental full-table deletions.

```csharp
// ❌ ESQL001: Unbounded DELETE — will not compile
Sql.Delete<User>();

// ✅ Correct: explicit WHERE required
Sql.Delete<User>().Where(u => u.Id == userId);
```

A **Code Fix** is available that adds a `.Where()` stub.

---

### ESQL002 — Unsafe String Concatenation

**Severity:** Error  
**Category:** SQL Security  
**File:** `UnsafeStringConcatenationAnalyzer.cs`

Detects raw C# string concatenation when building SQL fragments, which is a SQL injection vector.

```csharp
// ❌ ESQL002: Potential SQL injection
var sql = "SELECT * FROM users WHERE name = '" + userName + "'";

// ✅ Use parameterized expressions
var query = Sql.From<User>().Where(u => u.Name == userName);
```

A **Code Fix** is available to suggest the typed equivalent.

---

### ESQL003 — UPDATE without WHERE

**Severity:** Error  
**Category:** SQL Safety  
**File:** `DeleteWithoutWhereAnalyzer.cs`

Fires when `Sql.Update<T>()` is compiled without a `.Where()` clause. Prevents accidental full-table updates.

```csharp
// ❌ ESQL003: Unbounded UPDATE
Sql.Update<User>().Set(u => u.IsActive, false);

// ✅ Correct
Sql.Update<User>().Set(u => u.IsActive, false).Where(u => u.Id == userId);
```

---

### ESQL011 — Sql.Raw() Unsafe Overload

**Severity:** Warning  
**Category:** SQL Security  
**File:** `RawStringOverloadAnalyzer.cs`

Warns when `Sql.Raw()` is called with a non-constant, non-FormattableString argument. Raw SQL fragments bypass parameterization.

```csharp
// ⚠️ ESQL011: Unsafe — string variable is not parameterized
Sql.Raw(someVariable);

// ✅ Safer: use FormattableString (values are parameterized)
Sql.Raw($"custom_function({param})");
```

---

### ESQL012 — Retry Inside Transaction

**Severity:** Warning  
**Category:** Architecture  
**File:** `RetryInsideTransactionAnalyzer.cs`

Detects Polly retry pipelines that wrap transactional code. If a transaction fails mid-way and is retried, it may cause data corruption or constraint violations. See [ADR-016](decisions/adr-016-transaction-retry-semantics.md).

```csharp
// ⚠️ ESQL012: Retry wraps a transaction — dangerous
pipeline.ExecuteAsync(async ct =>
{
    await using var uow = await conn.BeginUnitOfWorkAsync(ct);
    await conn.ExecuteAsync(cmd, uow.Transaction, ct);
    await uow.CommitAsync(ct);  // ← ESQL012 fires here
});

// ✅ Retry should wrap the entire unit-of-work creation, not wrap commit
```

---

### ESQL021 — Missing Source Generator

**Severity:** Warning  
**Category:** Configuration  
**File:** `MissingSourceGeneratorAnalyzer.cs`

Fires when a class is marked with `[SqlEntity]` but the `EricksonLopez.SqlBuilder.SourceGenerators` package is not configured as an Analyzer in the project file. Without Source Generators, the library falls back to reflection (breaking NativeAOT).

---

### ESQL025 — SqlKata Migration

**Severity:** Info  
**Category:** Migration  
**File:** `SqlKataMigrationAnalyzer.cs`

Detects SqlKata API patterns in code and offers a **Code Fix** to migrate them to the `EricksonLopez.SqlBuilder` equivalent. See [`docs/migration-sqlkata.md`](migration-sqlkata.md) for the full migration guide.

---

## Installation

```xml
<ItemGroup>
  <PackageReference Include="EricksonLopez.SqlBuilder.Analyzers"
                    Version="*"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

> The package targets `netstandard2.0` and works across .NET 8, 9, and 10 projects.

## Suppressing Rules

To suppress a rule for a specific line:

```csharp
#pragma warning disable ESQL011 // justified: table name from trusted enum, not user input
Sql.Raw($"[{tableName}]");
#pragma warning restore ESQL011
```

To suppress globally in your project:

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);ESQL011</NoWarn>
</PropertyGroup>
```
