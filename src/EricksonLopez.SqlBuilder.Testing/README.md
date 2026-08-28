# EricksonLopez.SqlBuilder.Testing

Official testing utilities and semantic assertions for `EricksonLopez.SqlBuilder`.

## Purpose

Validating that repository and query components generate expected SQL is often brittle when using raw string comparison: minor differences in whitespace, line breaks, casing, or parameter indexing cause false positives.

`EricksonLopez.SqlBuilder.Testing` provides semantic AST assertions (`QueryAssert`, `SnapshotAssert`) that normalize syntax, parameter formats, and whitespace, validating the logical equivalence of generated SQL queries.

## Quick Example

```csharp
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

[Fact]
public void GetActiveUsers_ShouldGenerateCorrectSql()
{
    var query = repository.GetActiveUsersQuery();
    
    // Normalizes whitespace, formatting, and parameter bindings:
    QueryAssert.SqlEquals(
        "SELECT id, name FROM users WHERE is_active = @p0",
        query,
        new PostgreSqlCompiler()
    );
}
```
