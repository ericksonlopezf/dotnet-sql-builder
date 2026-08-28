# EricksonLopez.SqlBuilder.PostgreSql

PostgreSQL dialect compiler and native extensions for the `EricksonLopez.SqlBuilder` ecosystem.

## Purpose

PostgreSQL provides unique capabilities (such as `JSONB`, `COPY`, `ON CONFLICT DO UPDATE`, `LATERAL JOIN`, `DISTINCT ON`, and `UNNEST`) that generic SQL builders often struggle to express. This package provides a dedicated AST visitor and compiler designed specifically to harness PostgreSQL features while preserving strong type safety and high performance.

## Core Features

- **Native Compiler:** `PostgreSqlCompiler` translates immutable ASTs into standard PostgreSQL SQL.
- **High-Throughput Bulk Operations:** Native `COPY FROM STDIN` streaming binary support via `NpgsqlCopyStrategy`.
- **Advanced PostgreSQL Functions:** First-class support for `DISTINCT ON`, `FILTER (WHERE ...)`, and array operators.

## Quick Example

```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.PostgreSql;

var query = Sql.Insert(user)
    .OnConflict("email")
    .DoUpdate(u => new { u.LastLogin });

var result = query.Build(new PostgreSqlCompiler());
```
