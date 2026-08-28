# ADR-044: Contractual Result Pattern Dependency in SqlBuilder.Abstractions

## Status
Accepted — August 2026

## Context
`EricksonLopez.SqlBuilder.Abstractions` defines compilation and dialect interfaces where query building can fail functionally. The package directly references `EricksonLopez.Result` to return `Result<QueryResult>`.

The ecosystem audit reviewed whether a Foundation package should pull in `EricksonLopez.Result`.

## Decision
Formally approve and maintain the direct technical dependency of `EricksonLopez.SqlBuilder.Abstractions` on `EricksonLopez.Result`:
- `Result<T>` is the universal, zero-allocation functional error-handling standard across the entire `EricksonLopez.*` ecosystem.
- Avoiding exception-based control flow in SQL compilation is a core non-functional requirement.
- Creating an artificial intermediate shim interface would introduce abstraction overhead without architectural benefit.

## Consequences
- All consumers of `SqlBuilder.Abstractions` transitively consume `EricksonLopez.Result`. Given the struct-based, zero-dependency nature of `Result`, this footprint is negligible and functionally coherent.
