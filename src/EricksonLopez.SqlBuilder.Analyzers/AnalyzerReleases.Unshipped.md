; Release notes for analyzer rules
; Each rule should have a single line containing rule ID, category, title, description, help link, and severity.

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
ESQL001 | Usage | Error | DELETE without WHERE clause
ESQL002 | Security | Error | Unsafe string concatenation in SQL
ESQL003 | Usage | Error | UPDATE without WHERE clause
ESQL004 | Performance | Warning | Use of ToString() or similar in SQL Expressions
ESQL005 | Usage | Info | Call to Dapper extensions without compiler
ESQL006 | Correctness | Warning | Incompatible types in Join
ESQL007 | Performance | Warning | OrderBy on unindexed column
ESQL008 | Performance | Warning | Large Offset detected
ESQL009 | Performance | Warning | Use of LIKE without wildcards
ESQL010 | Performance | Warning | Use of LIKE with leading wildcard
ESQL011 | Security | Warning | Unsafe Sql.Raw(string) overload
ESQL012 | Usage | Warning | Retry pipeline wraps transaction commit
ESQL020 | Correctness | Warning | Capability requirement might not be met
ESQL021 | Usage | Warning | Source Generator package not referenced
ESQL022 | Usage | Info | Verify Dapper Type Maps registration on startup
ESQL023 | Performance | Warning | Synchronous execution on UI thread
ESQL024 | Correctness | Warning | Potential Cartesian Join (Missing ON Condition)
ESQL025 | Migration | Info | Migrate SqlKata Query to SqlBuilder
ESQL026 | Design | Error | Generic Sql.Merge<T>() is removed in v2.0
ELSB004 | Security | Warning | Dynamic SQL identifier without allowlist
ELSB006 | Performance | Warning | Batch size exceeds provider parameter limit

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SQL0001 | EricksonLopez.SqlBuilder.Analyzers | Warning | Migrated to ESQL001
SQL0002 | EricksonLopez.SqlBuilder.Analyzers | Error | Migrated to ESQL002
SQL0005 | EricksonLopez.SqlBuilder.Analyzers | Warning | Migrated to ESQL008
SQL0006 | EricksonLopez.SqlBuilder.Analyzers | Warning | Migrated to ESQL009/ESQL010
SQL0007 | EricksonLopez.SqlBuilder.Analyzers | Info | Migrated to SQL0009
SQL0008 | EricksonLopez.SqlBuilder.Analyzers | Info | Migrated to ESQL007
SQL0010 | EricksonLopez.SqlBuilder.Analyzers | Info | Migrated to SQL0004
SQL0011 | EricksonLopez.SqlBuilder.Analyzers | Warning | Migrated to ESQL005
