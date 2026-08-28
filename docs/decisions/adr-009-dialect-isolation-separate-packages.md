# ADR-009: Dialect Isolation in Separate Packages

## Status
Accepted

## Date
2026-08-12

## Context
SQL dialects differ significantly in syntax, identifier quoting, LIMIT/OFFSET semantics, upsert mechanisms, bulk copy APIs, and type mappings. Any query builder that supports multiple databases must decide how to package this dialect knowledge.

## Problem
- Bundling all dialects in Core creates unnecessary dependencies (e.g., Npgsql in a SQL Server-only app)
- Shared dialect code creates coupling; a PostgreSQL fix shouldn't require rebuilding the SQL Server package
- Dialect-specific features (e.g., PostgreSQL `UNNEST`, SQL Server `MERGE OUTPUT`) don't belong in a shared API

## Options Considered

### Option A: One Core package with all dialects embedded
- Rejected: unnecessary transitive dependencies, coupled release cycles

### Option B: One dialect package containing all compilers
- Rejected: still bloated; forces all compilers on users who only need one

### Option C: One package per dialect (chosen)
- **Chosen**: `EricksonLopez.SqlBuilder.SqlServer`, `...PostgreSql`, `...MySql`, `...Sqlite`, `...Oracle`

### Option D: Dialect resolution via DI / plugin model at runtime
- Rejected: AOT-incompatible (requires `Assembly.Load`), adds startup complexity

## Decision
Each database dialect ships as its own NuGet package with a single compiler class:

```
EricksonLopez.SqlBuilder.SqlServer   → SqlServerCompiler
EricksonLopez.SqlBuilder.PostgreSql  → PostgreSqlCompiler
EricksonLopez.SqlBuilder.MySql       → MySqlCompiler
EricksonLopez.SqlBuilder.Sqlite      → SqliteCompiler
EricksonLopez.SqlBuilder.Oracle      → OracleCompiler
```

**Invariant:** All dialect packages depend only on `EricksonLopez.SqlBuilder` (Core) — never on each other.

**Dialect-specific features** (e.g., `UNNEST`, `RETURNING`, `OUTPUT INSERTED`) are implemented as overrides in the respective compiler, not as shared abstractions.

**Identifier quoting** per dialect:
- SQL Server: `[identifier]`
- PostgreSQL, SQLite, Oracle: `"identifier"`
- MySQL: `` `identifier` ``

## Consequences

### Positive
- ✅ Minimal footprint — users only add the package for their database
- ✅ Independent release cycles per dialect
- ✅ Adding a new dialect (e.g., DuckDB) doesn't affect existing packages
- ✅ Dialect features can be dialect-specific without API leakage

### Negative
- ❌ Multi-dialect testing requires all packages (integration test matrix)
- ❌ Cross-dialect features (e.g., a unified upsert API) require more careful abstraction

## Reconsideration Criteria
If a user commonly deploys to 3+ dialects in the same application, evaluate a meta-package `EricksonLopez.SqlBuilder.All` that depends on all dialect packages.

## References
- [FEATURE_MATRIX.md §23 — Final Package Architecture](../../FEATURE_MATRIX.md)
- `src/EricksonLopez.SqlBuilder.SqlServer/`
- `src/EricksonLopez.SqlBuilder.PostgreSql/`
