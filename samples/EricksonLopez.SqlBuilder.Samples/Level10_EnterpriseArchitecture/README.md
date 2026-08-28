# Level 10: Enterprise Architecture & Patterns

## Overview

Demonstrates integrating `EricksonLopez.SqlBuilder` into clean, enterprise-grade architectures: Generic Repository pattern, Differential Updates, Specification pattern, and CQRS.

## Key APIs Covered

- Generic Repository pattern implementation using `Sql.From<T>()`.
- Differential UPDATEs via `DiffUpdateExtensions.ApplyDiff()`.
- Specification pattern combining `ISqlFilter<T>` with repository queries.
- CQRS separation of command handlers and query handlers.
- Multi-compiler registration for polyglot persistence.
