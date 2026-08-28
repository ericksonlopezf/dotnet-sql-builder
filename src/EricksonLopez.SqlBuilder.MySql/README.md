# EricksonLopez.SqlBuilder.MySql

[![Build Status](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml/badge.svg)](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/EricksonLopez.SqlBuilder.MySql.svg)](https://www.nuget.org/packages/EricksonLopez.SqlBuilder.MySql/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NativeAOT Ready](https://img.shields.io/badge/NativeAOT-Supported-brightgreen.svg)](docs/architecture.md)
[![Target Frameworks](https://img.shields.io/badge/.NET-net8.0%20%7C%20net9.0-blue.svg)](https://dotnet.microsoft.com)

MySQL and MariaDB dialect compiler with backtick identifier escaping, `LIMIT/OFFSET` pagination, null-safe equality (`<=>`), and `ON DUPLICATE KEY UPDATE` support.

## Installation

```bash
dotnet add package EricksonLopez.SqlBuilder.MySql
```

## Features

- 🚀 **Zero Reflection**: Optimized for maximum execution speed without dynamic runtime inspection.
- ⚡ **NativeAOT Compliant**: 100% compatible with Native AOT compilation and trimming.
- 🛡️ **Secure by Default**: Injection immunity through strict parameterization.
- 🔄 **NULLS Emulation**: Deterministic `CASE WHEN` emulation for `NULLS FIRST` and `NULLS LAST`.

## Documentation & Resources

- [Architecture Guide](docs/architecture.md)
- [Cookbook](docs/cookbook.md)
- [API Reference](docs/api-reference.md)

## License

This project is licensed under the [MIT License](LICENSE).
