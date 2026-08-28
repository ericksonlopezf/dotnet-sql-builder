// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Represents a DELETE query syntax tree builder for the specified entity type.
/// </summary>
/// <remarks>
/// This type is immutable. All modification methods return a new instance
/// preserving the previous state.
/// </remarks>
/// <typeparam name="T">The type of the entity to delete.</typeparam>
public sealed record DeleteQuery<T> : IDeleteFromBuilder<T>, IDeleteWhereBuilder<T>, ISqlQuery, IAstQuery where T : class, new()
{
    /// <summary>
    /// Gets the optional tag associated with this query for diagnostics or interception.
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Creates a new <see cref="DeleteQuery{T}"/> with the specified diagnostic tag.
    /// </summary>
    /// <param name="tag">The diagnostic tag to associate with the query.</param>
    /// <returns>A new query instance containing the applied tag.</returns>
    public DeleteQuery<T> WithTag(string tag) => this with { Tag = tag };

    /// <summary>
    /// Gets the collection of nodes that compose the abstract syntax tree of this query.
    /// </summary>
    public System.Collections.Immutable.ImmutableArray<ISqlNode> Nodes { get; init; } = System.Collections.Immutable.ImmutableArray<ISqlNode>.Empty.Add(new DeleteNode(SqlEntityCache<T>.TableName));
    
    /// <inheritdoc/>
    IReadOnlyList<ISqlNode> IAstQuery.Nodes => Nodes;

    /// <summary>
    /// Appends the specified node to the query AST.
    /// </summary>
    /// <param name="node">The node to append to the query.</param>
    /// <returns>A new <see cref="DeleteQuery{T}"/> instance containing the appended node.</returns>
    public DeleteQuery<T> AddNode(ISqlNode node) => this with { Nodes = Nodes.Add(node) };

    /// <summary>
    /// Compiles the abstract syntax tree into an executable SQL string and its parameters.
    /// </summary>
    /// <param name="compiler">The SQL compiler specific to the target database provider.</param>
    /// <returns>The compiled SQL result.</returns>
    [RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
    [RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
    public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);

    /// <summary>
    /// Specifies the target table for the DELETE operation.
    /// </summary>
    /// <param name="tableName">An optional table name to override the default entity table.</param>
    /// <returns>A new <see cref="IDeleteFromBuilder{T}"/> instance with the target table applied.</returns>
    public IDeleteFromBuilder<T> Delete(string? tableName = null) => AddNode(new DeleteNode(tableName ?? SqlEntityCache<T>.TableName));
    
    /// <summary>
    /// Specifies an additional source table for the DELETE operation (USING clause).
    /// </summary>
    /// <param name="tableName">The name of the additional table.</param>
    /// <param name="alias">An optional alias for the additional table.</param>
    /// <returns>A new <see cref="IDeleteFromBuilder{T}"/> instance with the USING clause applied.</returns>
    public IDeleteFromBuilder<T> Using(string tableName, string? alias = null) => AddNode(new FromNode(tableName, alias));

    /// <summary>
    /// Specifies an additional strongly-typed source table for the DELETE operation (USING clause).
    /// </summary>
    /// <typeparam name="TOther">The type of the additional entity.</typeparam>
    /// <param name="alias">An optional alias for the additional table.</param>
    /// <returns>A new <see cref="IDeleteFromBuilder{T}"/> instance with the USING clause applied.</returns>
    public IDeleteFromBuilder<T> Using<TOther>(string? alias = null) where TOther : new() => AddNode(new FromNode(SqlEntityCache<TOther>.TableName, alias));

    /// <summary>
    /// Appends an INNER JOIN clause to the query.
    /// </summary>
    /// <param name="tableName">The name of the table to join.</param>
    /// <param name="alias">The alias for the joined table.</param>
    /// <param name="on">The join condition.</param>
    /// <returns>A new <see cref="IDeleteFromBuilder{T}"/> instance with the join clause applied.</returns>
    public IDeleteFromBuilder<T> Join(string tableName, string alias, string on) => AddNode(new JoinNode(JoinType.Inner, tableName, alias, on));

    /// <summary>
    /// Appends a strongly-typed INNER JOIN clause to the query based on a predicate.
    /// </summary>
    /// <typeparam name="TOther">The type of the joined entity.</typeparam>
    /// <param name="onExpression">The predicate expression defining the join condition.</param>
    /// <returns>A new <see cref="IDeleteFromBuilder{T}"/> instance with the join clause applied.</returns>
    public IDeleteFromBuilder<T> Join<TOther>(System.Linq.Expressions.Expression<System.Func<T, TOther, bool>> onExpression) where TOther : new() => AddNode(new JoinNode(JoinType.Inner, SqlEntityCache<TOther>.TableName, null, null, onExpression));
    
    /// <summary>
    /// Appends a WHERE clause to filter the rows to delete using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the filtering condition.</param>
    /// <returns>A new <see cref="IDeleteWhereBuilder{T}"/> instance with the filter applied.</returns>
    public IDeleteWhereBuilder<T> Where(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate) => AddNode(new ExpressionWhereNode(predicate));

    /// <summary>
    /// Appends a raw SQL WHERE clause to filter the rows to delete.
    /// </summary>
    /// <param name="condition">The formatted string containing the raw filter and its parameters.</param>
    /// <returns>A new <see cref="IDeleteWhereBuilder{T}"/> instance with the filter applied.</returns>
    public IDeleteWhereBuilder<T> Where(FormattableString condition)
    {
        return AddNode(new RawWhereNode(condition.Format, condition.GetArguments()));
    }

    /// <summary>
    /// Explicitly indicates that the delete should apply to all rows without filtering.
    /// </summary>
    /// <returns>The current <see cref="IDeleteWhereBuilder{T}"/> instance.</returns>
    public IDeleteWhereBuilder<T> WhereAll() => this;

    /// <summary>
    /// Appends a WHERE EXISTS (subquery) condition to the DELETE statement.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside EXISTS.</param>
    /// <returns>A new <see cref="IDeleteWhereBuilder{T}"/> instance with the EXISTS filter applied.</returns>
    public IDeleteWhereBuilder<T> WhereExists(ISqlQuery subquery) => AddNode(new ExistsWhereNode(subquery, IsNot: false, IsOr: false));

    /// <summary>
    /// Appends a WHERE NOT EXISTS (subquery) condition to the DELETE statement.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside NOT EXISTS.</param>
    /// <returns>A new <see cref="IDeleteWhereBuilder{T}"/> instance with the NOT EXISTS filter applied.</returns>
    public IDeleteWhereBuilder<T> WhereNotExists(ISqlQuery subquery) => AddNode(new ExistsWhereNode(subquery, IsNot: true, IsOr: false));

    /// <summary>
    /// Appends an AND logical condition to the current WHERE clause using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the additional filtering condition.</param>
    /// <returns>A new <see cref="IDeleteWhereBuilder{T}"/> instance with the filter applied.</returns>
    public IDeleteWhereBuilder<T> And(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate) => AddNode(new ExpressionWhereNode(predicate, false));

    /// <summary>
    /// Appends an OR logical condition to the current WHERE clause using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the alternative filtering condition.</param>
    /// <returns>A new <see cref="IDeleteWhereBuilder{T}"/> instance with the filter applied.</returns>
    public IDeleteWhereBuilder<T> Or(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate) => AddNode(new ExpressionWhereNode(predicate, true));
    
    /// <summary>
    /// Specifies the columns to return from the deleted rows.
    /// </summary>
    /// <param name="columns">The names of the columns to return.</param>
    /// <returns>A new <see cref="IDeleteWhereBuilder{T}"/> instance with the returning clause applied.</returns>
    public IDeleteWhereBuilder<T> Returning(params string[] columns) => AddNode(new ReturningNode(columns));

    /// <summary>
    /// Specifies the columns to return from the deleted rows using a predicate expression.
    /// </summary>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="selector">The expression defining the columns to return.</param>
    /// <returns>A new <see cref="IDeleteWhereBuilder{T}"/> instance with the returning clause applied.</returns>
    public IDeleteWhereBuilder<T> Returning<TResult>(System.Linq.Expressions.Expression<System.Func<T, TResult>> selector)
    {
        var cols = new List<string>();
        var newExpr = selector.Body as System.Linq.Expressions.NewExpression;
        if (newExpr != null && newExpr.Members != null)
        {
            foreach (var member in newExpr.Members)
            {
                cols.Add(SqlNamingHelper.ToSnakeCase(member.Name));
            }
        }
        else
        {
            var memExpr = selector.Body as System.Linq.Expressions.MemberExpression;
            if (memExpr != null)
            {
                cols.Add(SqlNamingHelper.ToSnakeCase(memExpr.Member.Name));
            }
        }
        return AddNode(new ReturningNode(cols.ToArray()));
    }
}






