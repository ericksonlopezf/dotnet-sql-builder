# Public API Inventory

This document contains the comprehensive public API inventory for the `EricksonLopez.SqlBuilder` ecosystem.
Total catalogued public types across all 16 assemblies: **329** (206 production types, 123 testing/mocking types).

### CountedPagedList`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.Pagination.CountedPagedList`1` |
| **Namespace** | `EricksonLopez.Pagination` |
| **Responsibility** | EricksonLopez.SqlBuilder.Dapper public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### PagedList`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.Pagination.PagedList`1` |
| **Namespace** | `EricksonLopez.Pagination` |
| **Responsibility** | EricksonLopez.SqlBuilder.Dapper public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DbConcurrencyException

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.DbConcurrencyException` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlBuilderException

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Exceptions.SqlBuilderException` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Exceptions` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlSafetyException

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Exceptions.SqlSafetyException` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Exceptions` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlSyntaxException

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Exceptions.SqlSyntaxException` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Exceptions` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlValidationException

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Exceptions.SqlValidationException` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Exceptions` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### IAstQuery

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IAstQuery` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Represents an immutable SQL query |
| **Dependencies** | Core |
| **Use Cases** | Query definition and compilation |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 01: QuickStart / Level 03: RealUseCases) |

### IBulkSerializer`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IBulkSerializer`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### IDeleteFromBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IDeleteFromBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 05: Processing — Sql.Delete<T>) |

### IDeleteWhereBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IDeleteWhereBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 05: Processing — Delete.Where) |

### IParameterManager

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IParameterManager` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization — CustomParameterManager) |

### IQueryFingerprinter

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IQueryFingerprinter` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ISqlCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 01: QuickStart / Level 08: Customization — ISqlCompiler) |

### ISqlNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ISqlNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 11: ComprehensiveApiCoverage) |

### ISqlQuery

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ISqlQuery` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Represents an immutable SQL query |
| **Dependencies** | Core |
| **Use Cases** | Query definition and compilation |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 01: QuickStart / Level 09: Extensions — SqlResult) |

### ISqlVisitor

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ISqlVisitor` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 11: ComprehensiveApiCoverage) |

### ITypeHandler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ITypeHandler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 02: FullConfiguration / Level 08: Customization — Custom TypeHandler) |

### IUpdateSetBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IUpdateSetBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 06: ErrorHandling — Sql.Update<T>) |

### IUpdateWhereBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IUpdateWhereBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 06: ErrorHandling — Update.Where) |

### ColumnFlags

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ColumnMetadata

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 09: Extensions / Level 11: ComprehensiveApiCoverage) |

### ColumnToken

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### IStaticEntityMetadata`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### SqlOperation

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.SqlOperation` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### CaseNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.CaseNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### CaseWhenBranch

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.CaseWhenBranch` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### CompositeCursorNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.CompositeCursorNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ConcurrencyTokenNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ConcurrencyTokenNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling — WithConcurrencyToken) |

### CteNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.CteNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases / Level 10: EnterpriseArchitecture — With CTE) |

### CursorKey

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.CursorKey` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DefaultValuesNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.DefaultValuesNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 11: ComprehensiveApiCoverage) |

### DeleteNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.DeleteNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 05: Processing — Sql.Delete) |

### DistinctOnNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.DistinctOnNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Distinct) |

### ExistsWhereNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExistsWhereNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 04: AdvancedIntegration — WhereExists) |

### ExpressionHavingNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionHavingNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Having) |

### ExpressionMergeOnNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionMergeOnNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Merge) |

### ExpressionSelectNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionSelectNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 03: RealUseCases) |

### ExpressionWhereNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionWhereNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 03: RealUseCases) |

### FromNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.FromNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart — Sql.From<T>) |

### GroupByNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.GroupByNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — GroupBy) |

### GroupByType

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.GroupByType` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### InsertNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.InsertNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 05: Processing — Sql.Insert) |

### InsertSelectNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.InsertSelectNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### JoinNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 04: AdvancedIntegration — Join) |

### JoinType

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Options enumeration |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 04: AdvancedIntegration — InnerJoin/LeftJoin) |

### LimitOffsetNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.LimitOffsetNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 07: Scalability — Limit/Offset) |

### MaterializationHint

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.MaterializationHint` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MergeNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.MergeNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Merge) |

### MergeUsingNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.MergeUsingNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Merge Using) |

### MergeWhenMatchedNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.MergeWhenMatchedNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — WhenMatched) |

### MergeWhenNotMatchedNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.MergeWhenNotMatchedNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — WhenNotMatched) |

### NullsPosition

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.NullsPosition` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### OnConflictNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.OnConflictNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling / Level 11: ComprehensiveApiCoverage) |

### OrderByNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.OrderByNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 07: Scalability — OrderBy) |

### QueryAliasNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.QueryAliasNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 04: AdvancedIntegration / Level 10: EnterpriseArchitecture) |

### RawHavingNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawHavingNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Having Raw) |

### RawJoinNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawJoinNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 04: AdvancedIntegration — Join Raw) |

### RawMergeOnNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawMergeOnNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Merge On Raw) |

### RawOrderByNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawOrderByNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 07: Scalability — OrderBy Raw) |

### RawSelectNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawSelectNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 09: Extensions — Sql.Raw) |

### RawWhereNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawWhereNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 09: Extensions — Where Raw) |

### ReturningNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ReturningNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling — Returning) |

### ScalarSubquerySelectNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ScalarSubquerySelectNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SelectNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SelectNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart — Select) |

### SetNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SetNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 06: ErrorHandling — Set) |

### SetOperationNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SetOperationNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Union/Intersect) |

### SqlExtensionNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SqlExtensionNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 09: Extensions / Level 11: ComprehensiveApiCoverage) |

### SubqueryFromNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SubqueryFromNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 04: AdvancedIntegration — From Subquery) |

### SubqueryJoinNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SubqueryJoinNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 04: AdvancedIntegration — SubqueryJoin) |

### ThenByNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ThenByNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 07: Scalability — ThenBy) |

### UnnestNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.UnnestNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 04: AdvancedIntegration / Level 11: ComprehensiveApiCoverage) |

### UpdateNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.UpdateNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 06: ErrorHandling — Sql.Update) |

### ValuesNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ValuesNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 05: Processing — Values) |

### WindowFunctionNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.WindowFunctionNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Window Functions) |

### WindowNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.WindowNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Window) |

### WindowPageNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.WindowPageNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 07: Scalability — WindowPage) |

### ProviderCapability

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ProviderCapability` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### RequiresCapabilityAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.RequiresCapabilityAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlResult

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.SqlResult` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 09: Extensions — Compiled Query) |

### SqlVisitorBase

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.SqlVisitorBase` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### BatchSizeExceedsMaxAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.BatchSizeExceedsMaxAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (BatchSizeExceedsMaxAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### CartesianJoinAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.CartesianJoinAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (CartesianJoinAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DapperCompilerAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.DapperCompilerAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (DapperCompilerAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DeleteWithoutWhereAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.DeleteWithoutWhereAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (DeleteWithoutWhereAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DeleteWithoutWhereCodeFix

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.DeleteWithoutWhereCodeFix` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (DeleteWithoutWhereCodeFix) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DialectSpecificOverloadAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.DialectSpecificOverloadAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (DialectSpecificOverloadAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DynamicIdentifierAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.DynamicIdentifierAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (DynamicIdentifierAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### JoinConditionAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.JoinConditionAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (JoinConditionAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### LargeOffsetAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.LargeOffsetAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (LargeOffsetAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### LikeWildcardAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.LikeWildcardAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (LikeWildcardAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MergeQueryAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.MergeQueryAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (MergeQueryAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MissingColumnAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.MissingColumnAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (MissingColumnAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MissingIndexAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.MissingIndexAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (MissingIndexAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MissingSourceGeneratorAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.MissingSourceGeneratorAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (MissingSourceGeneratorAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### QueryPerformanceAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.QueryPerformanceAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (QueryPerformanceAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### RawStringOverloadAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.RawStringOverloadAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (RawStringOverloadAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### RedundantWhereAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.RedundantWhereAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (RedundantWhereAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### RetryInsideTransactionAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.RetryInsideTransactionAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (RetryInsideTransactionAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SchemaValidator

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.SchemaValidator` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (SchemaValidator) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SelectStarAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.SelectStarAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (SelectStarAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SelectStarCodeFix

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.SelectStarCodeFix` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (SelectStarCodeFix) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlKataMigrationAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.SqlKataMigrationAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (SqlKataMigrationAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlKataMigrationCodeFixProvider

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.SqlKataMigrationCodeFixProvider` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (SqlKataMigrationCodeFixProvider) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SyncOnUiThreadAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.SyncOnUiThreadAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (SyncOnUiThreadAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### TypeMapRegistrationAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.TypeMapRegistrationAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (TypeMapRegistrationAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### UnsafeStringConcatenationAnalyzer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.UnsafeStringConcatenationAnalyzer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (UnsafeStringConcatenationAnalyzer) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### UnsafeStringConcatenationCodeFix

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Analyzers.UnsafeStringConcatenationCodeFix` |
| **Namespace** | `EricksonLopez.SqlBuilder.Analyzers` |
| **Responsibility** | Diagnostic analyzer detecting anti-patterns or optimization opportunities (UnsafeStringConcatenationCodeFix) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code analysis and IDE diagnostics |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DatabaseGeneratedAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.DatabaseGeneratedAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 02: FullConfiguration / Level 03: RealUseCases — [DatabaseGenerated]) |

### GeneratedColumnAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.GeneratedColumnAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 02: FullConfiguration — [GeneratedColumn]) |

### IndexedAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.IndexedAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 02: FullConfiguration — [Indexed]) |

### ISqlEntity

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.ISqlEntity` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 02: FullConfiguration / Level 11: ComprehensiveApiCoverage) |

### PostgreSqlCompositeTypeAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.PostgreSqlCompositeTypeAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 09: Extensions / Level 11: ComprehensiveApiCoverage) |

### PostgreSqlEnumAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.PostgreSqlEnumAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 09: Extensions / Level 11: ComprehensiveApiCoverage) |

### SqlEntityAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.SqlEntityAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 01: QuickStart / Level 02: FullConfiguration — [SqlEntity]) |

### AotConnectionExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Aot.AotConnectionExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Aot` |
| **Responsibility** | Connection extension methods for zero-reflection Native AOT query execution |
| **Dependencies** | Core, ADO.NET |
| **Use Cases** | Ahead-Of-Time compiled database operations without Reflection.Emit |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 05: Processing — QueryAotAsync / Level 11: ComprehensiveApiCoverage) |

### AotQueryExecutor

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Aot.AotQueryExecutor` |
| **Namespace** | `EricksonLopez.SqlBuilder.Aot` |
| **Responsibility** | Zero-reflection query executor orchestrating AOT reader delegates |
| **Dependencies** | Core, ADO.NET |
| **Use Cases** | Materializing entities under Native AOT publish profile |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 05: Processing — QueryAotAsync / Level 11: ComprehensiveApiCoverage) |

### AotSqlRendererBase

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.AotSqlRendererBase` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### BulkBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Bulk.BulkBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Bulk` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage — Sql.BulkInsert) |

### BulkInsertResult`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Bulk.BulkInsertResult`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Bulk` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### BulkOptions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Bulk.BulkOptions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Bulk` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### BulkSqlResult

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### IBulkOperation`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations.IBulkOperation`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### IEntitySource`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations.IEntitySource`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations` |
| **Responsibility** | EricksonLopez.SqlBuilder.Abstractions public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### CaseExpressionBuilder

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.CaseExpressionBuilder` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders` |
| **Responsibility** | EricksonLopez.SqlBuilder public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### InsertBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Insert.InsertBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Insert` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ISqlRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.ISqlRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### UpdateBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Update.UpdateBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Update` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ColumnSelectionContext`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.ColumnSelectionContext`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ColumnSelectionEngine`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.ColumnSelectionEngine`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### IColumnSelectionRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### RulePhase

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.RulePhase` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection` |
| **Responsibility** | Options enumeration |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 08: Customization / Level 11: ComprehensiveApiCoverage) |

### ExceptColumnsRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.ExceptColumnsRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 11: ComprehensiveApiCoverage) |

### ExcludeGeneratedRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.ExcludeGeneratedRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ExcludePrimaryKeysRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.ExcludePrimaryKeysRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 11: ComprehensiveApiCoverage) |

### IgnoreNullsRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.IgnoreNullsRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 11: ComprehensiveApiCoverage) |

### OnlyColumnsRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.OnlyColumnsRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 11: ComprehensiveApiCoverage) |

### CursorPaginationExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.CursorPaginationExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### AotDapperExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Aot.AotDapperExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Aot` |
| **Responsibility** | Dapper extension methods optimized for Native AOT execution |
| **Dependencies** | Core, Dapper, Aot |
| **Use Cases** | Dapper query materialization without reflection |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### BoundSelectQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.BoundSelectQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 04: AdvancedIntegration / Level 11: ComprehensiveApiCoverage) |

### ConnectionSqlExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.ConnectionSqlExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 01: QuickStart / Level 05: Processing — QueryAsync/ExecuteAsync) |

### DapperConcurrencyExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.DapperConcurrencyExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | EricksonLopez.SqlBuilder.Dapper public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DapperExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.DapperExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 01: QuickStart / Level 08: Customization — RegisterTypeHandler) |

### DapperMultiMappingExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.DapperMultiMappingExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 04: AdvancedIntegration / Level 11: ComprehensiveApiCoverage) |

### DapperPaginationExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.DapperPaginationExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### IBulkStrategy

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.IBulkStrategy` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### JsonbTypeHandler`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.JsonbTypeHandler`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization — ITypeHandler/Json) |

### PostgreSqlTypeHandlerRegistrar

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.PostgreSqlTypeHandlerRegistrar` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ISqlTransientErrorDetector

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.ISqlTransientErrorDetector` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling — Transient Error Detection) |

### MySqlTransientErrorDetector

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.MySqlTransientErrorDetector` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling / Level 11: ComprehensiveApiCoverage) |

### PostgreSqlTransientErrorDetector

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.PostgreSqlTransientErrorDetector` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling / Level 11: ComprehensiveApiCoverage) |

### SqlResilienceDefaults

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.SqlResilienceDefaults` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling — Retry Defaults) |

### SqlResilienceExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.SqlResilienceExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 06: ErrorHandling — ExecuteWithRetryAsync) |

### SqlServerTransientErrorDetector

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.SqlServerTransientErrorDetector` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling / Level 11: ComprehensiveApiCoverage) |

### SqlBuilderConnectionContext

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.SqlBuilderConnectionContext` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling / Level 10: EnterpriseArchitecture) |

### SqlBuilderDapperBulkExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.SqlBuilderDapperBulkExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ISavepoint

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork.ISavepoint` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling / Level 10: EnterpriseArchitecture) |

### IUnitOfWork

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork.IUnitOfWork` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 06: ErrorHandling / Level 10: EnterpriseArchitecture — Transactions) |

### UnitOfWorkExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork.UnitOfWorkExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 06: ErrorHandling / Level 10: EnterpriseArchitecture) |

### DeleteQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.DeleteQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 05: Processing — Sql.Delete<T>) |

### DiffUpdateExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.DiffUpdateExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DynamicSortingExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.DynamicSortingExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 07: Scalability — OrderByDynamic) |

### FilterExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Filters.FilterExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Filters` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 08: Customization — WhereFilter / ISqlFilter<T>) |

### ISqlFilter`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Filters.ISqlFilter`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Filters` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 10: EnterpriseArchitecture — Specifications) |

### InsertQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.InsertQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 02: FullConfiguration — Sql.Insert<T>) |

### MariaDbCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MariaDb.MariaDbCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.MariaDb` |
| **Responsibility** | Transforms AST into MariaDB-specific SQL with backtick quoting |
| **Dependencies** | Core, MariaDb |
| **Use Cases** | MariaDB query compilation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### MariaDbExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MariaDb.MariaDbExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.MariaDb` |
| **Responsibility** | Extension methods for MariaDB dialect features such as ON DUPLICATE KEY UPDATE |
| **Dependencies** | Core, MariaDb |
| **Use Cases** | MariaDB upsert and bulk query execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage — BuildOnDuplicateKeyUpdate) |

### MariaDbRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MariaDb.MariaDbRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.MariaDb` |
| **Responsibility** | Low-level SQL rendering engine for MariaDB syntax |
| **Dependencies** | Core, MariaDb |
| **Use Cases** | SQL text rendering for MariaDB |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MergeQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MergeQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Merge) |

### ColumnFlags

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.ColumnFlags` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ColumnMetadata

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.ColumnMetadata` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 09: Extensions / Level 11: ComprehensiveApiCoverage) |

### ColumnToken

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.ColumnToken` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### EntityMetadataResolver

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.EntityMetadataResolver` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### IEntityMetadata`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.IEntityMetadata`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### IEntityMetadataProvider`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.IEntityMetadataProvider`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### MySqlBatchStrategy

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MySql.MySqlBatchStrategy` |
| **Namespace** | `EricksonLopez.SqlBuilder.MySql` |
| **Responsibility** | EricksonLopez.SqlBuilder.MySql public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MySqlBulkMergeStrategy

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MySql.MySqlBulkMergeStrategy` |
| **Namespace** | `EricksonLopez.SqlBuilder.MySql` |
| **Responsibility** | EricksonLopez.SqlBuilder.MySql public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MySqlCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MySql.MySqlCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.MySql` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 10: EnterpriseArchitecture / Level 11: ComprehensiveApiCoverage) |

### MySqlExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MySql.MySqlExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.MySql` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MySqlRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MySql.MySqlRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.MySql` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlBuilderInstrumentation

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.OpenTelemetry.SqlBuilderInstrumentation` |
| **Namespace** | `EricksonLopez.SqlBuilder.OpenTelemetry` |
| **Responsibility** | OpenTelemetry instrumentation helper for query tracing and metrics |
| **Dependencies** | Core, OpenTelemetry |
| **Use Cases** | Distributed tracing and query performance telemetry |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 10: EnterpriseArchitecture / Level 11: ComprehensiveApiCoverage) |

### OracleBulkCopyStrategy

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Oracle.OracleBulkCopyStrategy` |
| **Namespace** | `EricksonLopez.SqlBuilder.Oracle` |
| **Responsibility** | EricksonLopez.SqlBuilder.Oracle public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### OracleCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Oracle.OracleCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Oracle` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 10: EnterpriseArchitecture / Level 11: ComprehensiveApiCoverage) |

### OracleDialectVersion

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Oracle.OracleDialectVersion` |
| **Namespace** | `EricksonLopez.SqlBuilder.Oracle` |
| **Responsibility** | EricksonLopez.SqlBuilder.Oracle public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### OracleParameterManager

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Oracle.OracleParameterManager` |
| **Namespace** | `EricksonLopez.SqlBuilder.Oracle` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage — OracleParameterManager) |

### OracleRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Oracle.OracleRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Oracle` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlBuilderPaginationExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Pagination.SqlBuilderPaginationExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Pagination` |
| **Responsibility** | Pagination extension methods bridging SqlBuilder with the Pagination library |
| **Dependencies** | Core, EricksonLopez.Pagination |
| **Use Cases** | Offset and cursor pagination calculation |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 07: Scalability / Level 11: ComprehensiveApiCoverage) |

### PaginationExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PaginationExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 03: RealUseCases / Level 07: Scalability — Paginate/ToPagedListAsync) |

### Param

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Param` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### BulkParameters

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.BulkParameters` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### BulkParameters`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.BulkParameters`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### CopyNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.CopyNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### CopyQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.CopyQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### NpgsqlBulkMergeStrategy

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.NpgsqlBulkMergeStrategy` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | EricksonLopez.SqlBuilder.PostgreSql public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### NpgsqlCopyStrategy

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.NpgsqlCopyStrategy` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | EricksonLopez.SqlBuilder.PostgreSql public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### PgSql

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PgSql` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 04: AdvancedIntegration / Level 11: ComprehensiveApiCoverage) |

### PostgreSqlCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 03: RealUseCases / Level 10: EnterpriseArchitecture) |

### PostgreSqlDapperExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlDapperExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### PostgreSqlExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 04: AdvancedIntegration / Level 11: ComprehensiveApiCoverage) |

### PostgreSqlRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### TransactionExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.TransactionExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### QueryContract

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.QueryContract` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | EricksonLopez.SqlBuilder public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### QueryContractExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.QueryContractExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | EricksonLopez.SqlBuilder public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### RawQuery

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.RawQuery` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Represents an immutable SQL query |
| **Dependencies** | Core |
| **Use Cases** | Query definition and compilation |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SelectQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SelectQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 03: RealUseCases — Sql.From<T>) |

### FilterGenerator

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SourceGenerators.FilterGenerator` |
| **Namespace** | `EricksonLopez.SqlBuilder.SourceGenerators` |
| **Responsibility** | Source generator producing zero-reflection metadata and mappings (FilterGenerator) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code generation for Native AOT |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MultiMapDescriptorGenerator

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SourceGenerators.MultiMapDescriptorGenerator` |
| **Namespace** | `EricksonLopez.SqlBuilder.SourceGenerators` |
| **Responsibility** | Source generator producing zero-reflection metadata and mappings (MultiMapDescriptorGenerator) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code generation for Native AOT |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlEntityGenerator

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SourceGenerators.SqlEntityGenerator` |
| **Namespace** | `EricksonLopez.SqlBuilder.SourceGenerators` |
| **Responsibility** | Source generator producing zero-reflection metadata and mappings (SqlEntityGenerator) |
| **Dependencies** | Roslyn CodeAnalysis |
| **Use Cases** | Compile-time code generation for Native AOT |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### Sql

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Sql` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01 to Level 11 — Static Entry Point) |

### SqlBuilderDiagnostics

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlBuilderDiagnostics` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlCompilerBase

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlCompilerBase` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 11: ComprehensiveApiCoverage) |

### SqlExpressionVisitor

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlExpressionVisitor` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 11: ComprehensiveApiCoverage) |

### SqliteCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Sqlite` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 01 to Level 10 — Primary SQLite Engine) |

### SqliteRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Sqlite.SqliteRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Sqlite` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlParameter

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlParameter` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 08: Customization / Level 09: Extensions) |

### SqlQueryFingerprintExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlQueryFingerprintExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | EricksonLopez.SqlBuilder public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlBulkCopyStrategy

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlServer.SqlBulkCopyStrategy` |
| **Namespace** | `EricksonLopez.SqlBuilder.SqlServer` |
| **Responsibility** | EricksonLopez.SqlBuilder.SqlServer public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlBulkMergeStrategy

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlServer.SqlBulkMergeStrategy` |
| **Namespace** | `EricksonLopez.SqlBuilder.SqlServer` |
| **Responsibility** | EricksonLopez.SqlBuilder.SqlServer public component |
| **Dependencies** | Core |
| **Use Cases** | Query composition and execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlServerCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.SqlServer` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 03: RealUseCases / Level 10: EnterpriseArchitecture) |

### SqlServerRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlServer.SqlServerRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.SqlServer` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity Level** | Advanced |
| **Existing Example** | Yes (Level 05: Processing / Level 11: ComprehensiveApiCoverage) |

### StrykerWorkarounds

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.StrykerWorkarounds` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### CrudTestsBase`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Abstractions.CrudTestsBase`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Abstractions` |
| **Responsibility** | Reusable base test harness verifying CRUD operations across database fixtures |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Integration and compliance testing for database fixtures |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### CustomerBuilder

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.DataBuilders.CustomerBuilder` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.DataBuilders` |
| **Responsibility** | Test data builder and object mother for domain entities |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Creating standardized test fixtures and domain models |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ObjectMother

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.DataBuilders.ObjectMother` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.DataBuilders` |
| **Responsibility** | Test data builder and object mother for domain entities |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Creating standardized test fixtures and domain models |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### OrderBuilder

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.DataBuilders.OrderBuilder` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.DataBuilders` |
| **Responsibility** | Test data builder and object mother for domain entities |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Creating standardized test fixtures and domain models |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### ProductBuilder

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.DataBuilders.ProductBuilder` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.DataBuilders` |
| **Responsibility** | Test data builder and object mother for domain entities |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Creating standardized test fixtures and domain models |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### TestEntity

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.DataBuilders.TestEntity` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.DataBuilders` |
| **Responsibility** | Testing domain entity or utility for TestEntity |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### UserBuilder

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.DataBuilders.UserBuilder` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.DataBuilders` |
| **Responsibility** | Test data builder and object mother for domain entities |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Creating standardized test fixtures and domain models |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DiagnosticActivityScope

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.DiagnosticActivityScope` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | Testing domain entity or utility for DiagnosticActivityScope |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### Address

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.Address` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for Address |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### AuditLog

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.AuditLog` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for AuditLog |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### Category

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.Category` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for Category |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### CrudUser

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.CrudUser` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for CrudUser |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### Customer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.Customer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for Customer |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### Invoice

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.Invoice` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for Invoice |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### OracleCrudUser

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.OracleCrudUser` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for OracleCrudUser |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### Order

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.Order` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for Order |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### OrderItem

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.OrderItem` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for OrderItem |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### Payment

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.Payment` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for Payment |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### Product

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.Product` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for Product |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### Role

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.Role` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for Role |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### TestUser

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.TestUser` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for TestUser |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### User

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.User` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for User |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### UserRole

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Domain.UserRole` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Domain` |
| **Responsibility** | Testing domain entity or utility for UserRole |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DummyEntity

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.DummyEntity` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | Testing domain entity or utility for DummyEntity |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### GoldenFileAssert

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.GoldenFileAssert` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | Assertion utilities for SQL equality, parameter matching, and golden file snapshots |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Unit test assertions on generated SQL queries |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage — QueryAssert / SnapshotAssert) |

### CrudTestsBase`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Infrastructure.CrudTestsBase`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Infrastructure` |
| **Responsibility** | Reusable base test harness verifying CRUD operations across database fixtures |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Integration and compliance testing for database fixtures |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### DatabaseFixture

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Infrastructure.DatabaseFixture` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Infrastructure` |
| **Responsibility** | Database fixture lifecycle manager providing isolated test schemas and connections |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test lifecycle management across SQLite, SQL Server, PostgreSQL, MySQL, and Oracle |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### IntegrationTestBase

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Infrastructure.IntegrationTestBase` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Infrastructure` |
| **Responsibility** | Database fixture lifecycle manager providing isolated test schemas and connections |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test lifecycle management across SQLite, SQL Server, PostgreSQL, MySQL, and Oracle |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MySqlFixture

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Infrastructure.MySqlFixture` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Infrastructure` |
| **Responsibility** | Database fixture lifecycle manager providing isolated test schemas and connections |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test lifecycle management across SQLite, SQL Server, PostgreSQL, MySQL, and Oracle |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### OracleBooleanHandler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Infrastructure.OracleBooleanHandler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Infrastructure` |
| **Responsibility** | Testing domain entity or utility for OracleBooleanHandler |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### OracleFixture

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Infrastructure.OracleFixture` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Infrastructure` |
| **Responsibility** | Database fixture lifecycle manager providing isolated test schemas and connections |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test lifecycle management across SQLite, SQL Server, PostgreSQL, MySQL, and Oracle |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### PostgreSqlFixture

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Infrastructure.PostgreSqlFixture` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Infrastructure` |
| **Responsibility** | Database fixture lifecycle manager providing isolated test schemas and connections |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test lifecycle management across SQLite, SQL Server, PostgreSQL, MySQL, and Oracle |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqliteFixture

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Infrastructure.SqliteFixture` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Infrastructure` |
| **Responsibility** | Database fixture lifecycle manager providing isolated test schemas and connections |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test lifecycle management across SQLite, SQL Server, PostgreSQL, MySQL, and Oracle |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SqlServerFixture

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Infrastructure.SqlServerFixture` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Infrastructure` |
| **Responsibility** | Database fixture lifecycle manager providing isolated test schemas and connections |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test lifecycle management across SQLite, SQL Server, PostgreSQL, MySQL, and Oracle |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### MockSqlCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.MockSqlCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | Testing domain entity or utility for MockSqlCompiler |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### QueryAssert

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.QueryAssert` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | Assertion utilities for SQL equality, parameter matching, and golden file snapshots |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Unit test assertions on generated SQL queries |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage — QueryAssert / SnapshotAssert) |

### QueryComparer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.QueryComparer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | AST and SQL structural comparison utility |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Verifying AST equivalence during test execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### QueryComparerResult

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.QueryComparerResult` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | AST and SQL structural comparison utility |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Verifying AST equivalence during test execution |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### QueryTestingExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.QueryTestingExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | Testing domain entity or utility for QueryTestingExtensions |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### StandardDataset

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Seeders.StandardDataset` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Seeders` |
| **Responsibility** | Seed dataset generator creating realistic test records with deterministic RNG |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Seeding benchmark and integration test databases |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### TestDataSeeder

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.Seeders.TestDataSeeder` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing.Seeders` |
| **Responsibility** | Seed dataset generator creating realistic test records with deterministic RNG |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Seeding benchmark and integration test databases |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### SnapshotAssert

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.SnapshotAssert` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | Assertion utilities for SQL equality, parameter matching, and golden file snapshots |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Unit test assertions on generated SQL queries |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage — QueryAssert / SnapshotAssert) |

### ThreeColumnEntity

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Testing.ThreeColumnEntity` |
| **Namespace** | `EricksonLopez.SqlBuilder.Testing` |
| **Responsibility** | Testing domain entity or utility for ThreeColumnEntity |
| **Dependencies** | Testing, ADO.NET, Core |
| **Use Cases** | Test fixture domain entity |
| **Complexity Level** | Basic |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### UpdateQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.UpdateQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 01: QuickStart / Level 06: ErrorHandling — Sql.Update<T>) |

### Window

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Window` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 11: ComprehensiveApiCoverage) |

### WindowBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.WindowBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity Level** | Intermediate |
| **Existing Example** | Yes (Level 03: RealUseCases — Window Functions) |

