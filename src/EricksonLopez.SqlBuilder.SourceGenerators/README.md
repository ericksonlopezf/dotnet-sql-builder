# EricksonLopez.SqlBuilder.SourceGenerators

Supercharge your development experience with Roslyn `Incremental Source Generators`. Zero reflection, maximum performance.

## What Problem Does It Solve?

Traditional Query Builders and ORMs rely heavily on runtime *Reflection* to map column names, extract values, and infer schemas. This degrades startup performance and prevents true `NativeAOT` deployment.

This package inspects your code at compile time and emits native, trim-safe C# code that the Builder and Dapper consume directly.

## Features

- **Column Metadata & Caching:** Detects the `[SqlEntity]` attribute and generates exact mappings between C# properties and SQL columns (`snake_case` by default).
- **Type-Safe Filters:** Automatically generates filter classes (e.g. `UserFilter`) without manual boilerplate.
- **NativeAOT Friendly:** By eliminating runtime reflection, all generated code is static, fast, and fully trimmable.

## Usage

Simply reference the package, decorate your entity with `[SqlEntity]`, and let the generator handle the rest. No additional configuration is required.
