# Safety & Correctness Guarantees

> **These are not aspirations. They are architectural invariants.**
>
> Each guarantee listed here is either enforced by the C# type system, Roslyn analyzers (build failure), source generation (build-time verification), or architectural design (structurally impossible to violate without bypassing the framework).

---

## Level 1: Compile-Time Guarantees (Build Fails)

These errors **cannot reach production** because the build will fail before deployment.

### G-001: DELETE without WHERE → Build Error

```csharp
// ESQL001 — Roslyn Error — build fails
Sql.Delete<User>();

// Correct: Explicit full-table delete requires WhereAll()
Sql.Delete<User>().WhereAll();
```

**How it''s enforced:** Roslyn Analyzer `ESQL001` reports `DiagnosticSeverity.Error`. The build pipeline stops.

---

### G-002: UPDATE without WHERE → Build Error

```csharp
// ESQL003 — Roslyn Error — build fails
Sql.Update<User>().Set(u => u.IsActive, true);

// Correct:
Sql.Update<User>().Set(u => u.IsActive, true).WhereAll();
```

**How it''s enforced:** Roslyn Analyzer `ESQL003` reports `DiagnosticSeverity.Error`.

---

### G-003: Generic MERGE API → Build Error

The generic `Sql.Merge<T>()` / `MergeQuery<T>` API has been removed. Roslyn analyzer `ESQL026` reports `DiagnosticSeverity.Error` if any usage is detected.

Use per-dialect upsert instead (see [ADR-025](decisions/adr-025-no-generic-merge-abstraction.md)):

```csharp
// PostgreSQL / SQLite
Sql.Insert(entity).OnConflict(u => u.Email).DoUpdate(u => u.Name);

// SQL Server / Oracle - use Sql.Raw() with MERGE syntax
```

---

### G-004: Renaming a mapped property → Build Error

Expression trees are strongly typed. Renaming `User.IsActive` to `User.IsEnabled` immediately causes a C# compile error on all lambdas that reference it. This is standard C# type safety, not a framework feature.

---

## Level 2: Build-Time Guarantees (Source Generators)

### G-005: AOT-safe entity mapper generated at build time

For any type decorated with `[SqlEntity]`, a `GetReaderParser()` static method is generated at build time. The mapper uses explicit column ordinals — zero reflection. If the entity cannot be mapped, the source generator fails the build.

### G-006: `ESQL021` fires when Source Generator is missing

Roslyn Analyzer `ESQL021` warns when `[SqlEntity]` is applied but the Source Generator is not configured correctly in the project.

---

## Level 3: Runtime-Early Guarantees (AST Validation)

### G-007: Invalid AST combinations fail on first compilation

The `AstValidatorVisitor` (invoked during `ISqlCompiler.Compile()`) detects structurally invalid queries immediately:
- SELECT with no table source
- Pagination applied to non-SELECT query
- Window functions outside SELECT context

An `InvalidOperationException` is thrown at compile time, before any query executes against the database.

---

## Level 4: Security Guarantees

### G-008: All user-provided values are automatically parameterized

Every value passed via an expression predicate is parameterized. It is architecturally impossible to inject SQL via typed expressions.

```csharp
// userId = "1; DROP TABLE Users"
var query = Sql.From<User>().Where(u => u.Id == userId);
// SQL: WHERE [Id] = @p0   (@p0 = "1; DROP TABLE Users")
// NOT: WHERE Id = 1; DROP TABLE Users
```

### G-009: `Sql.Raw(FormattableString)` is injection-safe

```csharp
// Automatically parameterized
var raw = Sql.Raw($"SELECT * FROM Users WHERE Id = {userId}");
// Produces: SELECT * FROM Users WHERE Id = @p0

// ESQL011 Warning: unsafe string overload
var unsafe = Sql.Raw("SELECT * FROM Users WHERE Id = " + userId);
```

### G-010: Dynamic column identifiers require an allowlist

Roslyn Analyzer `ESQL004` detects dynamic identifiers used without an allowlist and warns at compile time.

---

## Level 5: Correctness Guarantees

### G-011: Immutable AST — base queries never mutated

Every builder method returns a **new record**. Two derived queries from the same base can never affect each other. Enforced by C# `record` types with `ImmutableArray<ISqlNode>`.

```csharp
var baseQuery = Sql.From<User>().Where(u => u.IsActive);
var admins = baseQuery.Where(u => u.Role == "Admin");  // new object
var guests = baseQuery.Where(u => u.Role == "Guest");  // new object
// baseQuery is still unchanged
```

### G-012: Retry pipelines must not wrap `CommitAsync`

Roslyn Analyzer `ESQL012` detects `CommitAsync` calls inside `ResiliencePipeline.ExecuteAsync` lambdas, preventing the classic double-commit bug on transient error retry.

---

## Level 6: Portability Guarantees

### G-013: 92–95% of application code unchanged when switching SQL providers

The portable SQL core (SELECT, WHERE, JOIN, ORDER BY, GROUP BY, CTE, INSERT, UPDATE, DELETE, pagination) produces correct SQL for all 6 supported dialects from the same AST.

Only these change when switching providers:
- The compiler instance: `new SqlServerCompiler()` → `new PostgreSqlCompiler()` (one line)
- Dialect-specific method calls (`.PostgreSql(...)`, `.SqlServer(...)`, etc.)
- The bulk strategy: `SqlBulkCopyStrategy` → `NpgsqlCopyStrategy`

### G-014: Dialect-specific methods require the correct provider package

If only `EricksonLopez.SqlBuilder.PostgreSql` is installed, SQL Server-specific extension methods don't exist and produce compile errors if referenced. Package separation is the enforcement mechanism.

---

## Scope Boundaries — What Is NOT Guaranteed

| Error Class | Why Not Preventable |
|-------------|---------------------|
| Column doesn't exist in the actual table | Requires schema awareness (out of scope) |
| Query timeout | Depends on data volume, locks, and server load |
| Deadlock | Not statically predictable |
| Business-logic result correctness | Semantic contract, not a SQL safety concern |
| SQL injection via `Sql.Raw(string)` (ESQL011 suppressed) | Developer explicitly opted into the unsafe path |
| Runtime errors in Oracle + NativeAOT | Oracle driver limitation, not a framework defect |

---

## Summary Matrix

| Guarantee | Level | Enforcement | Verifiable |
|-----------|-------|-------------|------------|
| DELETE without WHERE → error | Compile-time | ESQL001 | Unit test |
| UPDATE without WHERE → error | Compile-time | ESQL003 | Unit test |
| Generic MERGE → error | Compile-time | ESQL026 | Unit test |
| Renamed property → error | Compile-time | C# type system | By language |
| AOT mapper generated | Build-time | Source generator | Compilation |
| All values parameterized | Structural | Expression visitor | Integration test |
| `Sql.Raw(FormattableString)` safe | Structural | FormattableString decomp. | Unit test |
| Immutable AST | Structural | C# record semantics | By language |
| Retry + CommitAsync → warning | Compile-time | ESQL012 | Unit test |
| 92-95% portability | Architectural | Package separation | Cross-dialect tests |
