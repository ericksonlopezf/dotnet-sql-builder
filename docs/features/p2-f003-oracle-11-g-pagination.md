# Feature Implementation: P2-F003 - Oracle Legacy 11g ROWNUM Pagination Mode

## Metadata
* **ID:** `P2-F003`
* **Title:** Oracle Legacy 11g `ROWNUM` Pagination Dialect Mode
* **Layer / Component:** `EricksonLopez.SqlBuilder.Oracle` (`OracleCompiler.cs`, `OracleDialectVersion.cs`)
* **Priority:** P2 (Dialect Compatibility)
* **Status:** `COMPLETED`
* **Target Version:** `v1.4.0`
* **Test Coverage:** Automated unit tests in `tests/EricksonLopez.SqlBuilder.Oracle.UnitTests/OracleCompilerTests.cs`

---

## 1. Context & Motivation
Oracle Database 12c Release 1 (12.1)+ introduced standard ANSI SQL `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY` pagination.
However, Oracle Database 11g (and older enterprise deployments) does not support `OFFSET...FETCH` syntax and throws compilation/parse errors `ORA-00933: SQL command not properly ended`.

---

## 2. Technical Implementation
1. Added `OracleDialectVersion` enum:
   * `Oracle12cPlus` (default): Uses ANSI `OFFSET {n} ROWS FETCH NEXT {m} ROWS ONLY`.
   * `Oracle11g`: Uses 2-tier / 3-tier `ROWNUM` subquery pagination.

2. Updated `OracleCompiler`:
   * Constructor `OracleCompiler(OracleDialectVersion dialectVersion = OracleDialectVersion.Oracle12cPlus)`.
   * Overrides `CompileSelect` to generate:
     * When `Offset` and `Limit` are specified:
       `SELECT * FROM (SELECT a_.*, ROWNUM rnum_ FROM (<innerSql>) a_ WHERE ROWNUM <= (offset + limit)) WHERE rnum_ > offset`
     * When only `Limit` is specified:
       `SELECT * FROM (<innerSql>) WHERE ROWNUM <= limit`
     * When only `Offset` is specified:
       `SELECT * FROM (SELECT a_.*, ROWNUM rnum_ FROM (<innerSql>) a_) WHERE rnum_ > offset`

---

## 3. Verification & Test Evidence
Unit test cases added in `OracleCompilerTests.cs`:
* `Compile_WhenSelectWithLimitOffset_Oracle11g_ShouldGenerateRownumSubquery`
* `Compile_WhenSelectWithOnlyLimit_Oracle11g_ShouldGenerateRownumSubquery`
* `Compile_WhenSelectWithOnlyOffset_Oracle11g_ShouldGenerateRownumSubquery`

All 47 unit tests in `Oracle.UnitTests` pass cleanly.
