# Level 0: Conceptual Foundation

## What is EricksonLopez.SqlBuilder?

`EricksonLopez.SqlBuilder` is an immutable, type-safe, NativeAOT-ready SQL query builder for modern .NET (8.0/9.0/10.0).

It provides a clean, fluent abstraction to construct SQL queries without relying on fragile string concatenation or heavy ORM overhead.

## Core Pillars

1. **Immutable AST:** Every method call (`Where`, `OrderBy`, `Join`, etc.) returns a new, independent immutable query instance. Queries can be shared safely across threads.
2. **Type-Safe Expression Trees:** Use strongly-typed C# lambda expressions (`x => x.Active && x.Age > 18`) to build parameterized filters.
3. **Dialect Awareness:** Compiles to SQL Server, PostgreSQL, MySQL, MariaDB, SQLite, and Oracle.
4. **NativeAOT & Zero Reflection:** Full support for Native AOT and aggressive trimming via Roslyn Source Generators.
5. **Dapper Synergy:** Seamless integration with Dapper via extension methods (`QueryAsync<T>`, `ExecuteAsync`, `BulkInsertAsync`, etc.).

## Architectural Position

| Paradigm | Example Libraries | State Mutability | Compile-time Safety | Performance Overhead |
|----------|-------------------|------------------|---------------------|----------------------|
| Full ORM | Entity Framework Core | Mutable | High | Medium/High |
| Micro ORM | Dapper, RepoDB | Stateless | Low (Raw SQL strings) | Very Low |
| Query Builder (Mutable) | SqlKata | Mutable | Medium | Low |
| **Immutable Query Builder** | **EricksonLopez.SqlBuilder** | **Immutable** | **High** | **Minimal / Zero Alloc** |
