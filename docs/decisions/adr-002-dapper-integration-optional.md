# ADR-002: Dapper Integration Is Optional

## Status
Accepted

## Context
EricksonLopez.SqlBuilder's Core produces `SqlResult` (SQL string + parameters). Executing that result requires a database driver and a micro-ORM or ADO.NET directly.

## Problem
If Dapper execution were included in Core, every user would carry a Dapper dependency even if they execute SQL differently (e.g., using direct ADO.NET, a different micro-ORM, or EF Core raw SQL).

## Options Considered
### Option A: Include Dapper in Core
- Pro: Simpler getting started
- Con: Forces Dapper on all users; increases Core package size; couples SQL generation to execution

### Option B: Separate `EricksonLopez.SqlBuilder.Dapper` package
- Pro: Optional; clean separation; users who use raw ADO.NET pay no extra cost
- Con: Users must install two packages

## Decision
Dapper integration lives entirely in `EricksonLopez.SqlBuilder.Dapper`. Core never references the Dapper namespace.

## Rationale
- Separation of concerns: SQL generation (Core) vs SQL execution (Dapper)
- Extensibility: users can write their own execution layer if needed
- Future-proof: if a better micro-ORM emerges, users can adopt it without changing Core

## Consequences
### Positive
- Core is usable with any execution layer
- Package graph stays clean

### Negative
- Slightly more complex initial setup

## API Impact
Core exposes `ISqlQuery.Build(ISqlCompiler)` → `SqlResult`. Execution extensions (`QueryAsync<T>`, `ExecuteAsync`) are in `EricksonLopez.SqlBuilder.Dapper` only.

## Reconsideration Criteria
None — this is foundational.
