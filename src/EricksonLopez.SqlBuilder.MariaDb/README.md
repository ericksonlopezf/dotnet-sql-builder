# EricksonLopez.SqlBuilder.MariaDb

[![Build Status](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml/badge.svg)](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/EricksonLopez.SqlBuilder.MariaDb.svg)](https://www.nuget.org/packages/EricksonLopez.SqlBuilder.MariaDb/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NativeAOT Ready](https://img.shields.io/badge/NativeAOT-Supported-brightgreen.svg)](docs/architecture.md)
[![Target Frameworks](https://img.shields.io/badge/.NET-net8.0%20%7C%20net9.0-blue.svg)](https://dotnet.microsoft.com)

MariaDB dialect compiler extending the MySQL dialect with native MariaDB 10.5+ features including `RETURNING` clause support, backtick identifier escaping, `LIMIT/OFFSET` pagination, and `ON DUPLICATE KEY UPDATE`.

## Installation

```bash
dotnet add package EricksonLopez.SqlBuilder.MariaDb
```

## Features

- 🚀 **Zero Reflection**: Optimized for maximum execution speed without dynamic runtime inspection.
- ⚡ **NativeAOT Compliant**: 100% compatible with Native AOT compilation and trimming.
- 🛡️ **Secure by Default**: Injection immunity through strict parameterization.
- 🔄 **NULLS Emulation**: Deterministic `CASE WHEN` emulation for `NULLS FIRST` and `NULLS LAST`.
- ✅ **Native RETURNING**: Supports `RETURNING col1, col2` on INSERT, UPDATE, and DELETE (MariaDB 10.5+).

## Key Differences from MySQL Dialect

| Feature | MySQL | MariaDB |
|---------|-------|---------|
| `RETURNING` clause | ❌ Throws `NotSupportedException` | ✅ Native (10.5+) |
| Identifier quoting | `` `backtick` `` | `` `backtick` `` |
| `LIMIT/OFFSET` | ✅ | ✅ |
| `ON DUPLICATE KEY UPDATE` | ✅ | ✅ |
| `NULLS FIRST/LAST` | Emulated via `CASE WHEN` | Emulated via `CASE WHEN` |

## Quick Start

```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.MariaDb;

var compiler = new MariaDbCompiler();

// INSERT with RETURNING (native MariaDB 10.5+ support)
var query = Sql.Insert<User>(new User { Name = "Alice", Age = 30 })
               .Returning(u => u.Id);

var result = compiler.Compile(query);
// SQL: INSERT INTO `users` (`name`, `age`) VALUES (@p0, @p1) RETURNING `id`
```

## With Dapper

```csharp
using EricksonLopez.SqlBuilder.Dapper;
using MySqlConnector;

// Register MariaDb compiler for MySqlConnection (wire-compatible)
DapperExtensions.RegisterCompiler<MySqlConnection>(() => new MariaDbCompiler());

using var conn = new MySqlConnection(connectionString);
var id = await conn.QuerySingleAsync<int>(
    Sql.Insert<User>(user).Returning(u => u.Id));
```

## Documentation & Resources

- [Architecture Guide](docs/architecture.md)
- [Cookbook](docs/cookbook.md)
- [API Reference](docs/api-reference.md)

## License

This project is licensed under the [MIT License](LICENSE).
