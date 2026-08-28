# ADR-045: Rejection of Braces Column Expansion Shorthand (GAP-05)

## Status
Rejected (Anti-Feature) — August 2026

## Context
During the functional parity audit against SqlKata (GAP-05), the braces string expansion shorthand was evaluated:
```csharp
// SqlKata style:
query.Select("{id, name, email as user_email}");
```
In SqlKata, this expands the curly-braced string into separate column projection elements.

## Decision
**Formally reject Braces Column Expansion as an intentional Anti-Feature.**

`EricksonLopez.SqlBuilder` will **NOT** implement `{col1, col2}` string expansion.

## Rationale
1. **Zero Functional Gain**: The library already provides cleaner, zero-allocation, and type-safe alternatives:
   - Strongly-typed LINQ expressions: `query.Select(x => new { x.Id, x.Name, x.Email })`
   - Explicit column parameter lists: `query.Select("id", "name", "email")`
2. **Contradicts AOT-First & Zero-Allocation Principles**: Parsing arbitrary string tokens at runtime incurs avoidable allocations and CPU overhead in high-throughput hot paths.
3. **Syntax & Parsing Ambiguities**: Parsing commas within curly braces creates severe edge-case bugs when SQL functions, expressions, or subqueries contain commas (e.g. `{id, COALESCE(col, 'default'), CONCAT(first, ' ', last)}`).
4. **Preserves Type-Safety Focus**: Unlike SqlKata (which is fundamentally string-based), this library is designed for compile-time safety and Roslyn analyzer verification.

## Consequences
- No runtime string-splitting heuristics in `SelectQuery<T>`.
- Users leverage either typed lambda projections or explicit `params string[]` / `RawSelect` for projections.
