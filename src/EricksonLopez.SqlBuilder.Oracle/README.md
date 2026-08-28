# EricksonLopez.SqlBuilder.Oracle

[![Build Status](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml/badge.svg)](https://github.com/ericksonlopezf/dotnet-sql-builder/actions/workflows/ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/EricksonLopez.SqlBuilder.Oracle.svg)](https://www.nuget.org/packages/EricksonLopez.SqlBuilder.Oracle/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![NativeAOT Ready](https://img.shields.io/badge/NativeAOT-Supported-brightgreen.svg)](docs/architecture.md)
[![Target Frameworks](https://img.shields.io/badge/.NET-net8.0%20%7C%20net9.0-blue.svg)](https://dotnet.microsoft.com)

Oracle Database dialect compiler supporting double-quote identifier escaping (`"COL"`), `FETCH FIRST / OFFSET` (Oracle 12c+), legacy `ROWNUM` subquery wrapping (Oracle 11g), and PL/SQL parameter binding syntax (`:p0`).

## Installation

```bash
dotnet add package EricksonLopez.SqlBuilder.Oracle
```

## Features

- 🚀 **Zero Reflection**: Engineered for peak performance without runtime reflection.
- ⚡ **NativeAOT Compliant**: 100% compatible with Native AOT compilation and trimming.
- 🛡️ **Secure by Default**: Injection immunity via parameterized binding.
- 📄 **Dual Pagination Modes**: Automatic `FETCH FIRST` (12c+) and `ROWNUM` partition fallback (11g).

## Documentation & Resources

- [Architecture Guide](docs/architecture.md)
- [Cookbook](docs/cookbook.md)
- [API Reference](docs/api-reference.md)

## License

This project is licensed under the [MIT License](LICENSE).
