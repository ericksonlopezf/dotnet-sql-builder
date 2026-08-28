# EricksonLopez.SqlBuilder (Core)

The immutable, allocation-conscious, strongly typed SQL compiler core.

## Purpose

Building dynamic SQL statements via raw string concatenation is error-prone, insecure, and difficult to compose. Conversely, full-featured ORMs introduce heavyweight dependencies, unpredictable query plan generation, and substantial memory overhead.

`EricksonLopez.SqlBuilder` provides a fluent, strongly typed API for constructing an immutable Abstract Syntax Tree (AST) of SQL queries. All builders are built on immutable C# `record` semantics, guaranteeing thread safety and zero side effects when sharing query templates. The architecture compiles ASTs into dialect-optimized SQL and parameter dictionaries across all major database engines.

## Core Components

- **`SelectQuery<T>`, `InsertQuery<T>`, `UpdateQuery<T>`, `DeleteQuery<T>`:** Strongly typed query builders.
- **AST Nodes (`ISqlNode`):** Immutable record hierarchy (`SelectNode`, `WhereNode`, `JoinNode`, `CteNode`, etc.).
- **`ParameterManager`:** Injection-safe parameter coordinator supporting custom type handlers and domain primitive unwrapping.
- **Fluent API:** `.Where()`, `.OrderBy()`, `.Paginate()`, `.Join()`, `.WithCte()`, `.Window()`.

## Quick Example

```csharp
var query = Sql.From<Order>()
    .Select(o => new { o.Id, o.Total, o.CreatedAt })
    .Where(o => o.Total > 500)
    .OrderByDescending(o => o.CreatedAt);

// The 'query' instance is immutable and can be safely branched without side effects:
var pagedQuery = query.Paginate(1, 10);
var countQuery = query.Select("COUNT(*)");
```
