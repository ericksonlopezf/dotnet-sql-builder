# Public API Inventory

This document contains the comprehensive public API inventory for the `EricksonLopez.SqlBuilder` ecosystem.

### IAstQuery

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IAstQuery` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Represents an immutable SQL query |
| **Dependencies** | Core |
| **Use Cases** | Query definition and compilation |
| **Complexity** | Basic |
| **Existing Example** | No |

### IDeleteFromBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IDeleteFromBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### IDeleteWhereBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IDeleteWhereBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### IParameterManager

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IParameterManager` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ISqlCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### ISqlNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ISqlNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ISqlQuery

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ISqlQuery` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Represents an immutable SQL query |
| **Dependencies** | Core |
| **Use Cases** | Query definition and compilation |
| **Complexity** | Basic |
| **Existing Example** | No |

### ISqlVisitor

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ISqlVisitor` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ITypeHandler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.ITypeHandler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### IUpdateSetBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IUpdateSetBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### IUpdateWhereBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.IUpdateWhereBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ColumnFlags

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### ColumnMetadata

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### ColumnToken

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### IStaticEntityMetadata`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### SqlOperation

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Metadata.SqlOperation` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### ConcurrencyTokenNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ConcurrencyTokenNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### CteNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.CteNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### DefaultValuesNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.DefaultValuesNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### DeleteNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.DeleteNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### DistinctOnNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.DistinctOnNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ExistsWhereNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExistsWhereNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ExpressionHavingNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionHavingNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ExpressionMergeOnNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionMergeOnNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ExpressionSelectNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionSelectNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ExpressionWhereNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ExpressionWhereNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### FromNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.FromNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### GroupByNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.GroupByNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### InsertNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.InsertNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### JoinNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### JoinType

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.JoinType` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Options enumeration |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Basic |
| **Existing Example** | No |

### LimitOffsetNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.LimitOffsetNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### MergeNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.MergeNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### MergeUsingNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.MergeUsingNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### MergeWhenMatchedNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.MergeWhenMatchedNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### MergeWhenNotMatchedNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.MergeWhenNotMatchedNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### OnConflictNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.OnConflictNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### OrderByNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.OrderByNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### QueryAliasNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.QueryAliasNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### RawHavingNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawHavingNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### RawJoinNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawJoinNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### RawMergeOnNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawMergeOnNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### RawOrderByNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawOrderByNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### RawSelectNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawSelectNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### RawWhereNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.RawWhereNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ReturningNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ReturningNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SelectNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SelectNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SetNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SetNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SetOperationNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SetOperationNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlExtensionNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SqlExtensionNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SubqueryFromNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SubqueryFromNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SubqueryJoinNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.SubqueryJoinNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ThenByNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ThenByNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### UnnestNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.UnnestNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### UpdateNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.UpdateNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ValuesNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.ValuesNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### WindowFunctionNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.WindowFunctionNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### WindowNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.WindowNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### WindowPageNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.Nodes.WindowPageNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions.Nodes` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlResult

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.SqlResult` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlVisitorBase

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Abstractions.SqlVisitorBase` |
| **Namespace** | `EricksonLopez.SqlBuilder.Abstractions` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### DatabaseGeneratedAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.DatabaseGeneratedAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity** | Advanced |
| **Existing Example** | No |

### GeneratedColumnAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.GeneratedColumnAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity** | Advanced |
| **Existing Example** | No |

### IndexedAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.IndexedAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity** | Advanced |
| **Existing Example** | No |

### ISqlEntity

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.ISqlEntity` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### PostgreSqlCompositeTypeAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.PostgreSqlCompositeTypeAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity** | Advanced |
| **Existing Example** | No |

### PostgreSqlEnumAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.PostgreSqlEnumAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity** | Advanced |
| **Existing Example** | No |

### SqlEntityAttribute

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Annotations.SqlEntityAttribute` |
| **Namespace** | `EricksonLopez.SqlBuilder.Annotations` |
| **Responsibility** | Atributo de metadatos |
| **Dependencies** | Core |
| **Use Cases** | Entity and column annotations |
| **Complexity** | Advanced |
| **Existing Example** | No |

### AotSqlRendererBase

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.AotSqlRendererBase` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### BulkBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Bulk.BulkBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Bulk` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### BulkSqlResult

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations.BulkSqlResult` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### IBulkOperation`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations.IBulkOperation`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Bulk.Operations` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### InsertBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Insert.InsertBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Insert` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ISqlRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.ISqlRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### UpdateBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Builders.Update.UpdateBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Builders.Update` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ColumnSelectionContext`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.ColumnSelectionContext`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ColumnSelectionEngine`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.ColumnSelectionEngine`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### IColumnSelectionRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### RulePhase

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.RulePhase` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection` |
| **Responsibility** | Options enumeration |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Basic |
| **Existing Example** | No |

### ExceptColumnsRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.ExceptColumnsRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ExcludeGeneratedRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.ExcludeGeneratedRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ExcludePrimaryKeysRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.ExcludePrimaryKeysRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### IgnoreNullsRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.IgnoreNullsRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### OnlyColumnsRule`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules.OnlyColumnsRule`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.ColumnSelection.Rules` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### CursorPaginationExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.CursorPaginationExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### BoundSelectQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.BoundSelectQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ConnectionSqlExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.ConnectionSqlExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### DapperExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.DapperExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### DapperMultiMappingExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.DapperMultiMappingExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### DapperPaginationExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.DapperPaginationExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### IBulkStrategy

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.IBulkStrategy` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### JsonbTypeHandler`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.JsonbTypeHandler`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### PostgreSqlTypeHandlerRegistrar

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.PostgreSqlTypeHandlerRegistrar` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ISqlTransientErrorDetector

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.ISqlTransientErrorDetector` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### MySqlTransientErrorDetector

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.MySqlTransientErrorDetector` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### PostgreSqlTransientErrorDetector

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.PostgreSqlTransientErrorDetector` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlResilienceDefaults

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.SqlResilienceDefaults` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlResilienceExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.SqlResilienceExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### SqlServerTransientErrorDetector

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.Resilience.SqlServerTransientErrorDetector` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.Resilience` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlBuilderConnectionContext

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.SqlBuilderConnectionContext` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlBuilderDapperBulkExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.SqlBuilderDapperBulkExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### ISavepoint

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork.ISavepoint` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### IUnitOfWork

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork.IUnitOfWork` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### UnitOfWorkExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork.UnitOfWorkExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Dapper.UnitOfWork` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Dapper |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### DeleteQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.DeleteQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### DiffUpdateExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.DiffUpdateExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### DynamicSortingExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.DynamicSortingExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### FilterExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Filters.FilterExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.Filters` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### ISqlFilter`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Filters.ISqlFilter`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Filters` |
| **Responsibility** | Abstraction contract |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### InsertQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.InsertQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### MergeQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MergeQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### ColumnFlags

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.ColumnFlags` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### ColumnMetadata

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.ColumnMetadata` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### ColumnToken

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.ColumnToken` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### EntityMetadataResolver

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.EntityMetadataResolver` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### IEntityMetadata`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.IEntityMetadata`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### IEntityMetadataProvider`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Metadata.IEntityMetadataProvider`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.Metadata` |
| **Responsibility** | Entity metadata management |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Advanced |
| **Existing Example** | No |

### MySqlCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MySql.MySqlCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.MySql` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### MySqlExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MySql.MySqlExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.MySql` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### MySqlRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.MySql.MySqlRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.MySql` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### OracleCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Oracle.OracleCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Oracle` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### OracleParameterManager

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Oracle.OracleParameterManager` |
| **Namespace** | `EricksonLopez.SqlBuilder.Oracle` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### OracleRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Oracle.OracleRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Oracle` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### PaginationExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PaginationExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### Param

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Param` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### BulkParameters

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.BulkParameters` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### BulkParameters`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.BulkParameters`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### CopyNode

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.CopyNode` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### CopyQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.CopyQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### PgSql

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PgSql` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### PostgreSqlCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### PostgreSqlDapperExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlDapperExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### PostgreSqlExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### PostgreSqlRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### TransactionExtensions

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.PostgreSql.TransactionExtensions` |
| **Namespace** | `EricksonLopez.SqlBuilder.PostgreSql` |
| **Responsibility** | Utility extension methods |
| **Dependencies** | Core |
| **Use Cases** | Syntactic sugar and ease-of-use utilities |
| **Complexity** | Basic |
| **Existing Example** | No |

### RawQuery

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.RawQuery` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Represents an immutable SQL query |
| **Dependencies** | Core |
| **Use Cases** | Query definition and compilation |
| **Complexity** | Basic |
| **Existing Example** | No |

### SelectQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SelectQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### Sql

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Sql` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlBuilderDiagnostics

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlBuilderDiagnostics` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlCompilerBase

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlCompilerBase` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlExpressionVisitor

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlExpressionVisitor` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqliteCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.Sqlite` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### SqliteRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Sqlite.SqliteRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.Sqlite` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### SqlParameter

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlParameter` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### SqlServerCompiler

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler` |
| **Namespace** | `EricksonLopez.SqlBuilder.SqlServer` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### SqlServerRenderer

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.SqlServer.SqlServerRenderer` |
| **Namespace** | `EricksonLopez.SqlBuilder.SqlServer` |
| **Responsibility** | Transforms the AST into dialect-specific SQL |
| **Dependencies** | Core |
| **Use Cases** | Query compilation and translation |
| **Complexity** | Advanced |
| **Existing Example** | No |

### StrykerWorkarounds

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.StrykerWorkarounds` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### UpdateQuery`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.UpdateQuery`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### Window

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.Window` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |

### WindowBuilder`1

| Field | Description |
|---|---|
| **Name** | `EricksonLopez.SqlBuilder.WindowBuilder`1` |
| **Namespace** | `EricksonLopez.SqlBuilder` |
| **Responsibility** | Core library component |
| **Dependencies** | Core |
| **Use Cases** | General usage |
| **Complexity** | Intermediate |
| **Existing Example** | No |


