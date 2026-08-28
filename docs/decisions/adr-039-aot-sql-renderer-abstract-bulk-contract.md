# ADR-039: AotSqlRendererBase Abstract Bulk Contract Enforcement

## Status

Accepted

## Date

2026-08-15

## Context

`AotSqlRendererBase` previously declared bulk operation methods (`RenderBulkInsert`, `RenderBulkUpdate`, `RenderBulkMerge`, `RenderBulkUpsert`, `RenderBulkInsertIgnore`) as `virtual` throwing generic `NotSupportedException`.
Dialect implementors could omit implementations without a compile-time check.

## Problem

Silent missing implementations were only caught at runtime rather than enforced by the compiler.

## Decision

Change the 5 bulk renderer methods in `AotSqlRendererBase` from `virtual` to `abstract`.
Explicitly implement each method across all 5 dialect renderer packages (`SqlServerRenderer`, `PostgreSqlRenderer`, `MySqlRenderer`, `SqliteRenderer`, `OracleRenderer`) with descriptive messages directing users to dialect-native bulk strategies where appropriate.

## Decision Drivers

- **API Safety:** Compile-time guarantee that every dialect explicitly defines its bulk capabilities.
- **Clear Guidance:** Error messages clearly specify the recommended alternative (e.g. `SqlBulkCopyStrategy` on SQL Server).
