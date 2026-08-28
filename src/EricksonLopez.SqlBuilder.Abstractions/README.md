# EricksonLopez.SqlBuilder.Abstractions

Fundamental contracts and interfaces of the `EricksonLopez.SqlBuilder` ecosystem.

## Purpose

To ensure `SqlBuilder` remains strictly modular and framework-agnostic, the core architecture enforces zero circular dependencies and loose coupling between query builders, dialect-specific compilers (PostgreSQL, SQL Server, MySQL, SQLite, Oracle), and execution adapters (such as Dapper or Native AOT).

`Abstractions` contains the foundational building blocks (`ISqlQuery`, `ISqlCompiler`, `ISqlNode`, `Result<T>`) enabling third-party engines and custom adapters to integrate and extend the SQL building pipeline without depending on concrete database driver implementations.

## Core Components

- **`ISqlNode`**: Root interface for all immutable AST query elements.
- **`ISqlCompiler`**: Contract for translating an AST into dialect-specific parameterized SQL statements.
- **`SqlResult`**: Immutable model containing the generated SQL string and parameter collection.
- **`Result<T>` & `Error`**: Functional types for structured error handling and flow control.

## Extending with Custom Dialects

To implement support for additional database engines (e.g., Firebird, DB2, DuckDB), reference `EricksonLopez.SqlBuilder.Abstractions` and implement `ISqlCompiler`.
