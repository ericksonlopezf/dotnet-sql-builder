// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Defines a visitor for SQL AST nodes, enabling double-dispatch without coupling to a concrete implementation.
/// </summary>
public interface ISqlVisitor
{
    /// <summary>Visits a <see cref="CteNode"/> representing a Common Table Expression.</summary>
    /// <param name="node">The CTE node to process.</param>
    void Visit(CteNode node);
    /// <summary>Visits a <see cref="DeleteNode"/> representing the target table of a DELETE statement.</summary>
    /// <param name="node">The delete node to process.</param>
    void Visit(DeleteNode node);
    /// <summary>Visits a <see cref="FromNode"/> representing the primary FROM clause.</summary>
    /// <param name="node">The from node to process.</param>
    void Visit(FromNode node);
    /// <summary>Visits a <see cref="SubqueryFromNode"/> representing a subquery used as the FROM data source.</summary>
    /// <param name="node">The subquery from node to process.</param>
    void Visit(SubqueryFromNode node);
    /// <summary>Visits an <see cref="UnnestNode"/> representing an UNNEST array expansion clause.</summary>
    /// <param name="node">The unnest node to process.</param>
    void Visit(UnnestNode node);
    /// <summary>Visits a <see cref="GroupByNode"/> representing the GROUP BY clause.</summary>
    /// <param name="node">The group-by node to process.</param>
    void Visit(GroupByNode node);
    /// <summary>Visits an <see cref="ExpressionHavingNode"/> representing a HAVING clause derived from a LINQ expression.</summary>
    /// <param name="node">The expression having node to process.</param>
    void Visit(ExpressionHavingNode node);
    /// <summary>Visits a <see cref="RawHavingNode"/> representing a raw SQL HAVING clause.</summary>
    /// <param name="node">The raw having node to process.</param>
    void Visit(RawHavingNode node);
    /// <summary>Visits an <see cref="InsertNode"/> representing the INSERT INTO header clause.</summary>
    /// <param name="node">The insert node to process.</param>
    void Visit(InsertNode node);
    /// <summary>Visits a <see cref="ValuesNode"/> representing the VALUES clause of an INSERT statement.</summary>
    /// <param name="node">The values node to process.</param>
    void Visit(ValuesNode node);
    /// <summary>Visits a <see cref="ReturningNode"/> representing the RETURNING or OUTPUT clause.</summary>
    /// <param name="node">The returning node to process.</param>
    void Visit(ReturningNode node);
    /// <summary>Visits an <see cref="OnConflictNode"/> representing the ON CONFLICT clause of an INSERT statement.</summary>
    /// <param name="node">The on-conflict node to process.</param>
    void Visit(OnConflictNode node);
    /// <summary>Visits a <see cref="DefaultValuesNode"/> representing an INSERT DEFAULT VALUES clause.</summary>
    /// <param name="node">The default values node to process.</param>
    void Visit(DefaultValuesNode node);
    /// <summary>Visits a <see cref="JoinNode"/> representing a typed JOIN clause.</summary>
    /// <param name="node">The join node to process.</param>
    void Visit(JoinNode node);
    /// <summary>Visits a <see cref="RawJoinNode"/> representing a raw SQL JOIN clause.</summary>
    /// <param name="node">The raw join node to process.</param>
    void Visit(RawJoinNode node);
    /// <summary>Visits a <see cref="SubqueryJoinNode"/> representing a JOIN to a subquery or lateral subquery.</summary>
    /// <param name="node">The subquery join node to process.</param>
    void Visit(SubqueryJoinNode node);
    /// <summary>Visits a <see cref="LimitOffsetNode"/> representing the LIMIT and OFFSET pagination clauses.</summary>
    /// <param name="node">The limit/offset node to process.</param>
    void Visit(LimitOffsetNode node);
    /// <summary>Visits a <see cref="ScalarSubquerySelectNode"/> representing a scalar subquery in the SELECT projection.</summary>
    /// <param name="node">The scalar subquery node to process.</param>
    void Visit(ScalarSubquerySelectNode node);
    /// <summary>Visits an <see cref="OrderByNode"/> representing the primary ORDER BY clause.</summary>
    /// <param name="node">The order-by node to process.</param>
    void Visit(OrderByNode node);
    /// <summary>Visits a <see cref="ThenByNode"/> representing a secondary sort clause.</summary>
    /// <param name="node">The then-by node to process.</param>
    void Visit(ThenByNode node);
    /// <summary>Visits a <see cref="RawOrderByNode"/> representing a raw SQL ORDER BY clause.</summary>
    /// <param name="node">The raw order-by node to process.</param>
    void Visit(RawOrderByNode node);
    /// <summary>Visits a <see cref="SelectNode"/> representing a standard SELECT column projection.</summary>
    /// <param name="node">The select node to process.</param>
    void Visit(SelectNode node);
    /// <summary>Visits an <see cref="ExpressionSelectNode"/> representing a LINQ expression-based SELECT projection.</summary>
    /// <param name="node">The expression select node to process.</param>
    void Visit(ExpressionSelectNode node);
    /// <summary>Visits a <see cref="QueryAliasNode"/> representing an alias assigned to the outer query.</summary>
    /// <param name="node">The query alias node to process.</param>
    void Visit(QueryAliasNode node);
    /// <summary>Visits a <see cref="DistinctOnNode"/> representing a PostgreSQL DISTINCT ON clause.</summary>
    /// <param name="node">The distinct-on node to process.</param>
    void Visit(DistinctOnNode node);
    /// <summary>Visits a <see cref="RawSelectNode"/> representing a raw SQL SELECT clause.</summary>
    /// <param name="node">The raw select node to process.</param>
    void Visit(RawSelectNode node);
    /// <summary>Visits a <see cref="SetOperationNode"/> representing a set operation such as UNION, INTERSECT, or EXCEPT.</summary>
    /// <param name="node">The set operation node to process.</param>
    void Visit(SetOperationNode node);
    /// <summary>Visits an <see cref="UpdateNode"/> representing the UPDATE table target clause.</summary>
    /// <param name="node">The update node to process.</param>
    void Visit(UpdateNode node);
    /// <summary>Visits a <see cref="SetNode"/> representing a single SET assignment in an UPDATE statement.</summary>
    /// <param name="node">The set node to process.</param>
    void Visit(SetNode node);
    /// <summary>Visits an <see cref="ExpressionWhereNode"/> representing a WHERE clause derived from a LINQ expression.</summary>
    /// <param name="node">The expression where node to process.</param>
    void Visit(ExpressionWhereNode node);
    /// <summary>Visits a <see cref="RawWhereNode"/> representing a raw SQL WHERE clause.</summary>
    /// <param name="node">The raw where node to process.</param>
    void Visit(RawWhereNode node);
    /// <summary>Visits an <see cref="ExistsWhereNode"/> representing a WHERE EXISTS or WHERE NOT EXISTS subquery condition.</summary>
    /// <param name="node">The exists where node to process.</param>
    void Visit(ExistsWhereNode node);
    /// <summary>Visits a <see cref="ConcurrencyTokenNode"/> representing the optimistic concurrency token check.</summary>
    /// <param name="node">The concurrency token node to process.</param>
    void Visit(ConcurrencyTokenNode node);
    /// <summary>Visits a <see cref="WindowNode"/> representing a named window specification.</summary>
    /// <param name="node">The window node to process.</param>
    void Visit(WindowNode node);
    /// <summary>Visits a <see cref="WindowPageNode"/> representing a ROW_NUMBER-based window pagination clause.</summary>
    /// <param name="node">The window page node to process.</param>
    void Visit(WindowPageNode node);
    /// <summary>Visits a <see cref="WindowFunctionNode"/> representing a window function expression in the SELECT clause.</summary>
    /// <param name="node">The window function node to process.</param>
    void Visit(WindowFunctionNode node);
    /// <summary>Visits a <see cref="CaseNode"/> representing a CASE expression in the SELECT clause.</summary>
    /// <param name="node">The case node to process.</param>
    void Visit(CaseNode node);
    /// <summary>Visits an <see cref="InsertSelectNode"/> representing an INSERT INTO ... SELECT statement.</summary>
    /// <param name="node">The insert-select node to process.</param>
    void Visit(InsertSelectNode node);
    /// <summary>Visits a <see cref="CompositeCursorNode"/> representing a composite keyset cursor predicate.</summary>
    /// <param name="node">The composite cursor node to process.</param>
    void Visit(CompositeCursorNode node);
    
    /// <summary>
    /// Extension point for processing unknown or custom nodes.
    /// </summary>
    /// <param name="node">The custom extension node to process.</param>
    void VisitExtension(SqlExtensionNode node);
    
    /// <summary>
    /// Handles a node type that is not recognized by this visitor.
    /// </summary>
    /// <param name="node">The unrecognized node.</param>
    void VisitUnknown(ISqlNode node);
}




