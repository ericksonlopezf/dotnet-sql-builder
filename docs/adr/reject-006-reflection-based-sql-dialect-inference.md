# Architectural Decision Record: REJECT-006
## Rejection of Runtime Reflection for SQL Dialect Inference

### Status
**REJECTED (Permanent Directorial Invariant)**

### Context
Consideration was given to auto-detecting SQL dialects (PostgreSQL, SQL Server, MySQL, SQLite, Oracle) at runtime by inspecting `DbConnection` type names using reflection.

### Decision
Permanently rejected. Dialects must be explicitly configured via strongly-typed builders, compile-time generics, or explicit DI registrations (`ISqlDialect`).

### Consequences
- 100% Native AOT and trimming safe (`IsAotCompatible=true`, `TreatWarningsAsErrors=true`).
- Zero performance penalties on hot paths.
- Deterministic SQL compilation.
