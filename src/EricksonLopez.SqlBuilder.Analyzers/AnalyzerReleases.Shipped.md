## Release 1.1.1

; NOTE: IDs SQL0001-SQL0011 use the legacy naming scheme established before the ESQL/ELSB prefix convention.
; These IDs cannot be renamed in a patch release without breaking existing rule suppressions.
; Future major version will migrate to the ESQL prefix (ESQL001-ESQL011).
; Cross-reference table is documented in FEATURE_MATRIX.md §14 and AnalyzerReleases.Unshipped.md.

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SQL0001 | EricksonLopez.SqlBuilder.Analyzers | Warning | DeleteWithoutWhere — DELETE statement without a WHERE clause detected
SQL0002 | EricksonLopez.SqlBuilder.Analyzers | Error | UnsafeStringConcatenation — SQL injection risk via unsafe string concatenation
SQL0003 | EricksonLopez.SqlBuilder.Analyzers | Warning | SelectStar — SELECT * usage without explicit column list
SQL0004 | EricksonLopez.SqlBuilder.Analyzers | Warning | JoinCondition — JOIN clause without a matching condition
SQL0005 | EricksonLopez.SqlBuilder.Analyzers | Warning | LargeOffset — Large OFFSET without cursor-based pagination
SQL0006 | EricksonLopez.SqlBuilder.Analyzers | Warning | LikeWildcard — LIKE pattern with leading wildcard (performance degradation)
SQL0007 | EricksonLopez.SqlBuilder.Analyzers | Info | MissingColumn — Column referenced in query does not exist in entity
SQL0008 | EricksonLopez.SqlBuilder.Analyzers | Info | MissingIndex — Field used in WHERE clause has no index configured
SQL0009 | EricksonLopez.SqlBuilder.Analyzers | Warning | QueryPerformance — Query performance anti-pattern detected
SQL0010 | EricksonLopez.SqlBuilder.Analyzers | Info | RedundantWhere — Redundant or always-true WHERE clause
SQL0011 | EricksonLopez.SqlBuilder.Analyzers | Warning | DapperCompiler — Using Dapper compiler directly instead of SqlBuilder API
