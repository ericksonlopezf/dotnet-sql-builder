# EricksonLopez.SqlBuilder.Analyzers

Roslyn Analyzers and Code Fixes for the `EricksonLopez.SqlBuilder` ecosystem.

## Purpose

Constructing dynamic SQL queries can be error-prone even with a fluent API: developers might accidentally omit a `WHERE` clause on a `DELETE`, perform unindexed wildcard LIKE queries (`%pattern`), or invoke deprecated operations.

`EricksonLopez.SqlBuilder.Analyzers` inspects C# code in real time inside Visual Studio, JetBrains Rider, or VS Code, proactively surfacing compilation warnings and code fixes for safety, performance, and API correctness.

## Diagnostic Rules

- **ESQL001 (SelectStarAnalyzer):** Warns against unqualified `SELECT *` in favor of explicit typed projections.
- **ESQL002 (DeleteWithoutWhereAnalyzer):** Emits compile-time diagnostics on `Sql.Delete<T>()` invocations lacking a `.Where()` clause.
- **ESQL003 (UnsafeStringConcatenationAnalyzer):** Flags potentially unsafe string concatenation in raw SQL helpers.
- **ESQL012 (TransactionRetryAnalyzer):** Detects execution inside ambient transactions without proper retry policies.
- **ESQL026 (MergeQueryAnalyzer):** Warns against deprecated `Sql.Merge<T>()` and suggests dialect-specific `OnConflict` APIs or `Sql.Raw()`.

## Installation

This package operates as a Roslyn analyzer analyzer dependency and requires no runtime initialization:

```xml
<PackageReference Include="EricksonLopez.SqlBuilder.Analyzers" Version="..." PrivateAssets="all" />
```
