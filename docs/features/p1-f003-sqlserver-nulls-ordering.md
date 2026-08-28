# Feature Implementation: P1-F003 - SQL Server NULLS FIRST/LAST Emulation

## Metadata
* **ID:** `P1-F003`
* **Title:** SQL Server `NULLS FIRST` / `NULLS LAST` Emulation via CASE WHEN
* **Layer / Component:** `EricksonLopez.SqlBuilder.SqlServer` (`SqlServerVisitor.cs` / `SqlServerCompiler.cs`)
* **Priority:** P1 (Core Bugfix & Dialect Alignment)
* **Status:** `COMPLETED`
* **Test Coverage:** Automated unit tests in `tests/EricksonLopez.SqlBuilder.SqlServer.UnitTests/SqlServerCompilerTests.cs`

---

## 1. Context & Motivation
SQL Server (T-SQL) lacks native support for ANSI SQL standard `ORDER BY col NULLS FIRST / NULLS LAST` clauses (supported natively by PostgreSQL, Oracle, SQLite 3.30+).
Previously, `SqlServerCompiler.AppendNullsPosition` was a no-op that silently ignored `NULLS FIRST/LAST`, causing queries compiled for SQL Server to ignore explicit null ordering requests.

---

## 2. Technical Implementation
In `SqlServerVisitor` (`SqlServerCompiler.cs`), `Visit(OrderByNode node)` overrides the default behavior to prepend an inline ANSI CASE-WHEN sort expression before the target column:

* **`NullsPosition.First`:**
  Emits: `CASE WHEN [col] IS NULL THEN 0 ELSE 1 END, [col] [ASC|DESC]`
* **`NullsPosition.Last`:**
  Emits: `CASE WHEN [col] IS NULL THEN 1 ELSE 0 END, [col] [ASC|DESC]`
* **`NullsPosition.Default`:**
  Emits: `[col] [ASC|DESC]`

### Example
```csharp
var query = Sql.From<User>().OrderBy(u => u.CreatedAt, NullsPosition.First);
var result = compiler.Compile(query);
// Result SQL:
// SELECT * FROM [users] ORDER BY CASE WHEN [created_at] IS NULL THEN 0 ELSE 1 END, [created_at]
```

---

## 3. Verification & Test Evidence
Unit test cases added to `SqlServerCompilerTests.cs`:
* `Compile_OrderBy_NullsFirst_EmulatesCaseWhen`: Asserts `CASE WHEN [created_at] IS NULL THEN 0 ELSE 1 END, [created_at]`
* `Compile_OrderByDescending_NullsLast_EmulatesCaseWhen`: Asserts `CASE WHEN [created_at] IS NULL THEN 1 ELSE 0 END, [created_at] DESC`

All 35 unit tests in `SqlServer.UnitTests` pass cleanly.
