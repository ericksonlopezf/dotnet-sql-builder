# ADR-044: Contractual Result Pattern Dependency in SqlBuilder.Abstractions

## Status
Accepted — August 2026

## Date
2026-09-04

## Context
`EricksonLopez.SqlBuilder.Abstractions` defines compilation, validation, and dialect interfaces where operations can fail functionally. The package references `EricksonLopez.Result` to provide functional error handling across parsing, AST validation, and AST transformation pipelines.

The ecosystem audit reviewed whether a Foundation package should pull in `EricksonLopez.Result`.

## Decision
Formally approve and maintain the direct technical dependency of `EricksonLopez.SqlBuilder.Abstractions` on `EricksonLopez.Result`:
- `Result<T>` is the universal, zero-allocation functional error-handling standard across the entire `EricksonLopez.*` ecosystem.
- Avoiding exception-based control flow in SQL compilation is a core non-functional requirement.
- Creating an artificial intermediate shim interface would introduce abstraction overhead without architectural benefit.

## Consequences
- All consumers of `SqlBuilder.Abstractions` transitively consume `EricksonLopez.Result`. Given the struct-based, zero-dependency nature of `Result`, this footprint is negligible and functionally coherent.
- **Current State (v1.0):** To preserve natural and idiomatic C# developer ergonomics, `ISqlCompiler.Compile(IAstQuery)` currently returns `SqlResult` directly and throws standard exceptions on syntax/dialect violations.
- **Roadmap (v2.0):** Functional compilation overloads returning `Result<SqlResult>` will be introduced in v2.0 alongside strict AST validation pipelines, eliminating exception-based control flow in high-throughput compilation.
