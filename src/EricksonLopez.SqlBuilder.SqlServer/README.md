# EricksonLopez.SqlBuilder.SqlServer

Microsoft SQL Server T-SQL dialect compiler for the `EricksonLopez.SqlBuilder` ecosystem.

## Purpose

Microsoft SQL Server relies on T-SQL syntax specifics, such as bracket escaping (`[column]`), `OFFSET ... FETCH NEXT` pagination, `TOP` clauses, table hints (`WITH (NOLOCK)`), and `CROSS APPLY / OUTER APPLY`. This package provides a dedicated compiler translating agnostic AST queries into optimized T-SQL for SQL Server 2016+.

## Core Features

- **Native Compiler:** `SqlServerCompiler` translates immutable ASTs into dialect-accurate T-SQL.
- **Standards-Compliant Pagination:** Compiles pagination using `ORDER BY ... OFFSET ... FETCH NEXT`.
- **Bulk Integration:** Supports `SqlBulkCopyStrategy` for high-speed streaming bulk ingestion.
- **NULLS Emulation:** Automatically emulates `NULLS FIRST` and `NULLS LAST` via deterministic `CASE WHEN` expressions.

## Quick Example

```csharp
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.SqlServer;

var query = Sql.From<Invoice>()
    .Select(i => new { i.InvoiceId, i.TotalAmount })
    .Where(i => i.Status == "Pending")
    .OrderBy(i => i.CreatedDate)
    .Paginate(pageNumber: 2, pageSize: 20);

// Emits T-SQL with OFFSET 20 ROWS FETCH NEXT 20 ROWS ONLY
var result = query.Build(new SqlServerCompiler());
```
