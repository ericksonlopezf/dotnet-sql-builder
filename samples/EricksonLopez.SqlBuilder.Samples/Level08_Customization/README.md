# Level 8: Customization & Extensibility

## Overview

Demonstrates extending the framework: custom `ITypeHandler`, parameter managers, reusable `ISqlFilter<T>` specifications, and dialect compilers.

## Key APIs Covered

- Custom `ITypeHandler` for complex domain objects.
- Custom `IParameterManager` for query inspection and auditing.
- `ISqlFilter<T>` specification pattern for composable filters.
- Low-level compilation using `ISqlCompiler`.
- Dual registration with `DapperExtensions.RegisterTypeHandler<T>()`.
