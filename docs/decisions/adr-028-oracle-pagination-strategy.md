# ADR-028: Oracle Pagination Strategy (FETCH FIRST / ROWNUM Emulation)

## Status

Accepted

## Date

2026-08-15

## Context

Oracle Database does not support standard ANSI `LIMIT / OFFSET` syntax. Prior to Oracle 12c (12.1), pagination required complex `ROWNUM` subquery wrapping:
```sql
SELECT * FROM (
    SELECT a.*, ROWNUM rnum FROM (
        SELECT * FROM users ORDER BY id ASC
    ) a WHERE ROWNUM <= :limit_max
) WHERE rnum > :offset;
```
Starting with Oracle 12c Release 1, Oracle introduced native ANSI standard row limiting:
```sql
SELECT * FROM users ORDER BY id ASC OFFSET :offset ROWS FETCH NEXT :limit ROWS ONLY;
```

In the codebase, `OracleCompiler` previously inherited from `SqlCompilerBase` without overriding `CompileLimitOffset()`, resulting in silent generation of unsupported `LIMIT / OFFSET` SQL on Oracle instances.

## Problem

How should `EricksonLopez.SqlBuilder.Oracle` handle pagination across different Oracle Database server versions without producing silent syntax failures or forcing runtime version discovery?

## Options Considered

### Option A: Emit LIMIT / OFFSET and assume a proxy/gateway translates it (Rejected)
- Produces `ORA-00933: SQL command not properly ended` on real Oracle Database instances.
- Violates the dialect-aware guarantee.

### Option B: Always emit Oracle 11g ROWNUM wrapper subqueries (Rejected)
- Obsolete for all modern Oracle versions (12c, 18c, 19c, 21c, 23ai).
- Destroys query readability and impedes optimizer plans on modern versions.
- Requires complex AST rewriting at compilation time.

### Option C: Default to Oracle 12c+ `OFFSET ... FETCH NEXT ...` with explicit `OracleDialectVersion.Oracle11g` flag for legacy ROWNUM (Chosen)
- `OracleCompiler` defaults to `OracleDialectVersion.Oracle12cPlus`.
- Emits clean `OFFSET n ROWS FETCH NEXT m ROWS ONLY` (or `FETCH FIRST m ROWS ONLY` when offset is 0).
- When configured with `OracleDialectVersion.Oracle11g`, compiles using deterministic `ROWNUM` subquery partitioning.

## Decision

**Option C.** `OracleCompiler` defaults to `OFFSET ... ROWS FETCH NEXT ... ROWS ONLY` for Oracle 12c+. For legacy Oracle 11g instances, developers explicitly configure `new OracleCompiler(OracleDialectVersion.Oracle11g)` to enable `ROWNUM` query transformation.

## Decision Drivers

- **Dialect Correctness:** Eliminates silent `ORA-00933` syntax errors.
- **Zero Runtime Discovery:** Configuration is static and compile/initialization-time, preserving Native AOT safety.
- **Modern Default:** Oracle 12c+ has been the standard for over a decade; 19c/23ai are long-term support targets.

## Consequences

### Positive

- Oracle pagination works out of the box on all modern Oracle instances (12c, 19c, 21c, 23ai).
- Legacy Oracle 11g instances are supported via explicit dialect configuration.
- Completely reflection-free and Native AOT compatible.

### Negative

- Users targeting legacy Oracle 11g must explicitly pass `OracleDialectVersion.Oracle11g` when creating the compiler.

## Reconsideration Criteria

This decision will be updated if Oracle introduces newer row-limiting keywords in future ANSI SQL revisions.
