# EricksonLopez.SqlBuilder.Sqlite

[![Build Status](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml/badge.svg)](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/EricksonLopez.SqlBuilder.Sqlite.svg)](https://www.nuget.org/packages/EricksonLopez.SqlBuilder.Sqlite/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NativeAOT Ready](https://img.shields.io/badge/NativeAOT-Supported-brightgreen.svg)](docs/architecture.md)
[![Target Frameworks](https://img.shields.io/badge/.NET-net8.0%20%7C%20net9.0-blue.svg)](https://dotnet.microsoft.com)

Lightweight, high-performance SQLite dialect compiler optimized for embedded databases, edge computing, and local integration testing.

## Installation

```bash
dotnet add package EricksonLopez.SqlBuilder.Sqlite
```

## Features

- 🚀 **Zero Reflection**: Designed for maximum throughput without runtime reflection overhead.
- ⚡ **NativeAOT Compliant**: 100% compatible with Native AOT compilation and trimming.
- 🛡️ **Secure by Default**: Injection immunity through strict parameterization.
- 🔄 **NULLS Emulation**: Deterministic `CASE WHEN` emulation for `NULLS FIRST` and `NULLS LAST`.

## Documentation & Resources

- [Architecture Guide](docs/architecture.md)
- [Cookbook](docs/cookbook.md)
- [API Reference](docs/api-reference.md)

## License

This project is licensed under the [MIT License](LICENSE).
