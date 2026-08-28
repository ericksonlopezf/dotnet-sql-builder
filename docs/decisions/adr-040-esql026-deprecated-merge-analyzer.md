# ADR-040: Roslyn Analyzer ESQL026 for Deprecated Generic MERGE

## Status

Accepted

## Date

2026-08-15

## Context

Per ADR-025, generic cross-dialect MERGE abstractions are fundamentally unsafe due to dialect inconsistencies, race conditions, and optimizer bugs (notably in SQL Server MERGE).
`MergeQuery<T>` was marked `[Obsolete]` in code, but developers without strict deprecation warnings might still use it.

## Problem

Developers needed real-time IDE diagnostics warning against `Sql.Merge<T>()` and guiding them to safe alternatives.

## Decision

Implement Roslyn Analyzer rule `ESQL026`:
- Detects calls to `Sql.Merge<T>()` and `MergeQuery<T>`.
- Emits a Warning diagnostic.
- Recommends dialect-native `.OnConflict()` for PostgreSQL, MySQL, and SQLite, or `Sql.Raw()` for SQL Server and Oracle.

## Decision Drivers

- **Developer Safety:** Proactively prevents deployment of unsafe MERGE statements.
- **Immediate Feedback:** Works at edit-time inside Visual Studio, VS Code, and `dotnet build`.
