# Roslyn Analyzers Reference — EricksonLopez.SqlBuilder.Analyzers

`EricksonLopez.SqlBuilder.Analyzers` provides a comprehensive suite of Roslyn Diagnostic Analyzers and Code Fix Providers that enforce SQL safety, architectural correctness, Native AOT compliance, and query performance at compile time — both inside the IDE and during CI/CD build gates.

---

## Rule Summary Catalog

The package ships with 24 compile-time analyzers categorized across safety, security, performance, usage, and migration:

| Rule ID | Analyzer Class | Severity | Category | Description |
|---|---|---|---|---|
| [**ESQL001**](#esql001--delete-without-where) | `DeleteWithoutWhereAnalyzer` | Error | Safety | `DELETE` statement compiled without a `WHERE` clause (prevents accidental full-table deletion) |
| [**ESQL002**](#esql002--raw-sql-string-concatenation) | `UnsafeStringConcatenationAnalyzer` | Error | Security | Raw string concatenation or interpolation in SQL construction (SQL injection vector) |
| [**ESQL003**](#esql003--update-without-where) | `DeleteWithoutWhereAnalyzer` | Error | Safety | `UPDATE` statement compiled without a `WHERE` clause (prevents accidental full-table update) |
| [**ESQL004**](#esql004--query-performance-concern) | `QueryPerformanceAnalyzer` | Warning | Performance | Query construction pattern flags potential execution performance bottlenecks |
| [**ESQL005**](#esql005--dapper-compiler-misconfiguration) | `DapperCompilerAnalyzer` | Warning | Usage | Dapper integration configured with mismatched dialect compiler |
| [**ESQL006**](#esql006--missing-on-condition-in-join) | `JoinConditionAnalyzer` | Warning | Safety | Missing `ON` condition or incompatible join key types in `JOIN` expression |
| [**ESQL007**](#esql007--missing-index-hint) | `MissingIndexAnalyzer` | Info | Performance | Query filter on unindexed property may lead to table scans |
| [**ESQL008**](#esql008--large-offset-pagination) | `LargeOffsetAnalyzer` | Warning | Performance | Large `.Offset(n)` value detected; keyset/seek pagination recommended |
| [**ESQL009**](#esql009--like-without-wildcards) | `LikeWildcardAnalyzer` | Info | Performance | `LIKE` predicate without `%` or `_` wildcards; exact equality (`=`) is preferred |
| [**ESQL010**](#esql010--like-leading-wildcard) | `LikeWildcardAnalyzer` | Warning | Performance | `LIKE '%...'` leading wildcard prevents B-tree index seek (non-sargable) |
| [**ESQL011**](#esql011--sqlraw-unsafe-string-overload) | `RawStringOverloadAnalyzer` | Warning | Security | `Sql.Raw(string)` called with dynamic non-constant variable; use `FormattableString` |
| [**ESQL012**](#esql012--retry-pipeline-inside-transaction) | `RetryInsideTransactionAnalyzer` | Warning | Correctness | Polly resilience retry pipeline executes inside active transaction (corruption risk) |
| [**ESQL020**](#esql020--dialect-specific-api-incompatible-compiler) | `DialectSpecificOverloadAnalyzer` | Warning | Correctness | Dialect-specific API called with incompatible compiler (e.g. `CrossApply` on MySQL) |
| [**ESQL021**](#esql021--sqlentity-without-source-generator) | `MissingSourceGeneratorAnalyzer` | Warning | Usage | `[SqlEntity]` model defined without `SourceGenerators` analyzer configured in project |
| [**ESQL022**](#esql022--type-mapping-registration) | `TypeMapRegistrationAnalyzer` | Warning | Usage | Type mapping registration issue in custom type handlers |
| [**ESQL023**](#esql023--synchronous-sql-call-on-ui-thread) | `SyncOnUiThreadAnalyzer` | Warning | Performance | Synchronous SQL execution detected on a UI synchronization context |
| [**ESQL024**](#esql024--cartesian-join-detection) | `CartesianJoinAnalyzer` | Warning | Safety | Cartesian product detected (join operation without join predicate) |
| [**ESQL025**](#esql025--sqlkata-migration-hint) | `SqlKataMigrationAnalyzer` | Info | Migration | SqlKata API detected — automated code fix available to migrate to SqlBuilder |
| [**ESQL026**](#esql026--deprecated-generic-sqlmerget) | `MergeQueryAnalyzer` | Error | Correctness | Deprecated generic `Sql.Merge<T>()` detected; use dialect-native upserts (ADR-025) |
| [**ELSB004**](#elsb004--dynamic-sql-identifier-without-allowlist) | `DynamicIdentifierAnalyzer` | Warning | Security | Dynamically constructed SQL table or column identifier passed without allowlist check |
| [**ELSB006**](#elsb006--batch-size-exceeds-parameter-ceiling) | `BatchSizeExceedsMaxAnalyzer` | Warning | Performance | Batch size exceeds database provider parameter limits (e.g. SQL Server 2,100 limit) |
| [**SQL0003**](#sql0003--select--usage) | `SelectStarAnalyzer` | Warning | Performance | Unprojected `SELECT *` query; explicit column projection recommended |
| [**SQL0004**](#sql0004--redundant-where-condition) | `RedundantWhereAnalyzer` | Warning | Correctness | Redundant or contradictory `WHERE` predicate condition detected |
| [**SQL0009**](#sql0009--missing-column-reference) | `MissingColumnAnalyzer` | Warning | Correctness | Referenced property has no mapped database column |

> **Note on Rule Numbering:** The diagnostic range `ESQL013` through `ESQL019` is reserved for future dialect capability and transaction analyzer specifications.

---

## Rule Naming & Prefix Standardization

The analyzer suite uses three diagnostic prefixes:

| Prefix | Status | Scope & Purpose |
|---|---|---|
| `ESQL` | **Standard Core** | Primary SQL safety, performance, and correctness rules across query building. |
| `ELSB` | **Ecosystem Guard** | Advanced security boundary and bulk ingestion parameter limit rules. |
| `SQL` | **Legacy Shipped** | `SQL0003`, `SQL0004`, `SQL0009` are preserved for backward compatibility so existing consumer `#pragma` suppressions remain valid. All new rules use `ESQL` or `ELSB`. |

---

## Detailed Rule Specifications & Remediation

### ESQL001 — DELETE without WHERE

- **Severity:** Error
- **Category:** Safety
- **Diagnostic ID:** `ESQL001`
- **Trigger:** A `DeleteQuery<T>` reaches `.Build()` without calling `.Where()`, `.WhereExists()`, or `.WhereAll()`.

#### Non-Compliant
```csharp
// ❌ Error: Unbounded DELETE would delete all rows in table
var query = Sql.Delete<Customer>().Build(compiler);
```

#### Compliant
```csharp
// ✅ Compliant: Filtered delete
var query = Sql.Delete<Customer>()
               .Where(c => c.Id == customerId)
               .Build(compiler);

// ✅ Compliant: Explicit full-table deletion
var truncateLike = Sql.Delete<Customer>()
                      .WhereAll()
                      .Build(compiler);
```

---

### ESQL002 — Raw SQL String Concatenation

- **Severity:** Error
- **Category:** Security
- **Diagnostic ID:** `ESQL002`
- **Trigger:** String concatenation (`+`) or raw interpolation inside SQL composition methods.

#### Non-Compliant
```csharp
// ❌ Error: Raw concatenation introduces SQL injection vector
var query = Sql.Raw("SELECT * FROM users WHERE name = '" + userInput + "'");
```

#### Compliant
```csharp
// ✅ Compliant: Strongly-typed parameterization
var query = Sql.From<User>().Where(u => u.Name == userInput);

// ✅ Compliant: FormattableString parameterization
var query = Sql.Raw($"SELECT * FROM users WHERE name = {userInput}");
```

---

### ESQL003 — UPDATE without WHERE

- **Severity:** Error
- **Category:** Safety
- **Diagnostic ID:** `ESQL003`
- **Trigger:** `UpdateQuery<T>` compiled without a `.Where()` or `.WhereAll()` clause.

#### Non-Compliant
```csharp
// ❌ Error: Unbounded UPDATE modifies every row in table
Sql.Update<Customer>().Set(c => c.IsActive, false).Build(compiler);
```

#### Compliant
```csharp
// ✅ Compliant
Sql.Update<Customer>()
   .Set(c => c.IsActive, false)
   .Where(c => c.Id == customerId)
   .Build(compiler);
```

---

### ESQL008 — Large OFFSET Pagination

- **Severity:** Warning
- **Category:** Performance
- **Diagnostic ID:** `ESQL008`
- **Trigger:** `.Offset(n)` where `n` exceeds threshold (default: 10,000).

#### Non-Compliant
```csharp
// ⚠️ Warning: Offset 50,000 forces database to scan and discard 50,000 rows
var query = Sql.From<Order>().OrderBy(o => o.Id).Limit(20).Offset(50000);
```

#### Compliant
```csharp
// ✅ Compliant: Keyset cursor pagination (constant O(1) time regardless of depth)
var query = Sql.From<Order>()
               .SeekAfter(lastSeenId)
               .OrderBy(o => o.Id)
               .Limit(20);
```

---

### ESQL009 — LIKE without Wildcards

- **Severity:** Info
- **Category:** Performance
- **Diagnostic ID:** `ESQL009`
- **Trigger:** `LIKE` condition without `%` or `_` wildcards.

#### Compliant
```csharp
// Prefer exact equality comparison over LIKE for literals without wildcards:
var query = Sql.From<User>().Where(u => u.Email == "admin@example.com");
```

---

### ESQL010 — LIKE Leading Wildcard

- **Severity:** Warning
- **Category:** Performance
- **Diagnostic ID:** `ESQL010`
- **Trigger:** Predicates producing `LIKE '%search%'` (e.g. `.Contains("search")`).

#### Compliant
```csharp
// When index seek is required and prefix match is sufficient:
var query = Sql.From<User>().Where(u => u.LastName.StartsWith("Smith"));
```

---

### ESQL011 — Sql.Raw() Unsafe String Overload

- **Severity:** Warning
- **Category:** Security
- **Diagnostic ID:** `ESQL011`
- **Trigger:** Calling `Sql.Raw(string)` with a non-constant expression.

#### Non-Compliant
```csharp
// ⚠️ Warning: Variable string bypasses compile-time parameterization
string dynamicFilter = GetFilter();
var query = Sql.Raw(dynamicFilter);
```

#### Compliant
```csharp
// ✅ Compliant: Use FormattableString overload
var query = Sql.Raw($"SELECT * FROM audit WHERE event = {eventCode}");
```

---

### ESQL012 — Retry Pipeline Inside Transaction

- **Severity:** Warning
- **Category:** Correctness
- **Diagnostic ID:** `ESQL012`
- **Trigger:** Executing a Polly resilience retry pipeline inside the scope of an active transaction or `IUnitOfWork`.

#### Non-Compliant
```csharp
// ⚠️ Warning: Retrying inside transaction can cause dirty reads or aborted states
await using var uow = await connection.BeginUnitOfWorkAsync(ct);
await pipeline.ExecuteAsync(async token =>
{
    await connection.ExecuteAsync(cmd, uow.Transaction, token);
}, ct);
await uow.CommitAsync(ct);
```

#### Compliant
```csharp
// ✅ Compliant: Resilience pipeline wraps the entire Unit of Work creation and retry
await pipeline.ExecuteAsync(async token =>
{
    await using var uow = await connection.BeginUnitOfWorkAsync(token);
    await connection.ExecuteAsync(cmd, uow.Transaction, token);
    await uow.CommitAsync(token);
}, ct);
```

---

### ESQL020 — Dialect-Specific API + Incompatible Compiler

- **Severity:** Warning
- **Category:** Correctness
- **Diagnostic ID:** `ESQL020`
- **Trigger:** Invoking dialect-specific features (e.g., `CROSS APPLY`, `RETURNING`, `DISTINCT ON`) against a compiler that does not support the capability.

---

### ESQL021 — `[SqlEntity]` without Source Generator

- **Severity:** Warning
- **Category:** Usage
- **Diagnostic ID:** `ESQL021`
- **Trigger:** An entity class is annotated with `[SqlEntity]` but `EricksonLopez.SqlBuilder.SourceGenerators` is not configured as an analyzer in the `.csproj`.

---

### ESQL025 — SqlKata Migration Hint

- **Severity:** Info
- **Category:** Migration
- **Diagnostic ID:** `ESQL025`
- **Trigger:** Use of legacy SqlKata API methods (`new Query(...)`, `.WhereLike(...)`).
- **Remediation:** Provides an automated IDE Code Fix that converts SqlKata syntax to strongly-typed `EricksonLopez.SqlBuilder` AST queries. See [`docs/migration-sqlkata.md`](migration-sqlkata.md).

---

### ESQL026 — Deprecated Generic `Sql.Merge<T>()`

- **Severity:** Error
- **Category:** Correctness
- **Diagnostic ID:** `ESQL026`
- **Trigger:** Invocation of `Sql.Merge<T>()` or `MergeQuery<T>`.
- **Rationale:** Per [ADR-025](decisions/adr-025-no-generic-merge-abstraction.md), generic cross-dialect `MERGE` abstractions are unsafe. Use dialect-native upserts (`.OnConflict().DoUpdate()` for PostgreSQL/SQLite, or `SqlBulkCopy` / `OracleBulkCopyStrategy`).

---

### ELSB004 — Dynamic SQL Identifier without Allowlist

- **Severity:** Warning
- **Category:** Security
- **Diagnostic ID:** `ELSB004`
- **Trigger:** Passing dynamically evaluated string identifiers into table or column overrides without validating against a known allowlist.

---

### ELSB006 — Batch Size Exceeds Parameter Ceiling

- **Severity:** Warning
- **Category:** Performance
- **Diagnostic ID:** `ELSB006`
- **Trigger:** Setting batch sizes in `BulkBuilder` or `BulkInsertAsync` where `BatchSize * ColumnsCount` exceeds the engine parameter limit (SQL Server: 2,100 parameters; SQLite: 999 parameters).

---

### SQL0003 — SELECT * Usage

- **Severity:** Warning
- **Category:** Performance
- **Diagnostic ID:** `SQL0003`
- **Trigger:** Query omitting `.Select(...)` clause, resulting in `SELECT *`.
- **Compliant:** Project explicit columns via `.Select(u => new { u.Id, u.Name })`.

---

## Configuration & Severity Customization

Analyzer severities can be customized per project using `.editorconfig`:

```ini
[*.cs]
# Elevate critical rules to build-breaking errors
dotnet_diagnostic.ESQL001.severity = error
dotnet_diagnostic.ESQL002.severity = error
dotnet_diagnostic.ESQL003.severity = error
dotnet_diagnostic.ESQL026.severity = error

# Adjust performance warnings
dotnet_diagnostic.ESQL008.severity = warning
dotnet_diagnostic.SQL0003.severity = suggestion
```

### Line-Level Suppression Policy

Suppressions must include an architectural rationale:

```csharp
#pragma warning disable ESQL011 // Justified: Table name verified against compile-time Enum allowlist
var query = Sql.Raw($"SELECT * FROM [{validatedTableName}]");
#pragma warning restore ESQL011
```

> Suppressing `ESQL002` (string concatenation) is strictly forbidden in production code because parameterization is always available.
