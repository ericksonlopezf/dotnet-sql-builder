// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Represents a SELECT query syntax tree builder for the specified entity type.
/// </summary>
/// <remarks>
/// This type is immutable. All modification methods return a new instance
/// preserving the previous state.
/// </remarks>
/// <typeparam name="T">The type of the entity to query.</typeparam>
public sealed record SelectQuery<T> : IAstQuery where T : class, new()
{
    /// <summary>
    /// Gets the optional tag associated with this query for diagnostics or interception.
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Creates a new <see cref="SelectQuery{T}"/> with the specified diagnostic tag.
    /// </summary>
    /// <param name="tag">The diagnostic tag to associate with the query.</param>
    /// <returns>A new query instance containing the applied tag.</returns>
    public SelectQuery<T> WithTag(string tag) => this with { Tag = tag };

    /// <summary>
    /// Gets the collection of nodes that compose the abstract syntax tree of this query.
    /// </summary>
    public System.Collections.Immutable.ImmutableArray<ISqlNode> Nodes { get; init; } = System.Collections.Immutable.ImmutableArray<ISqlNode>.Empty;
    // Stryker disable once Unary : Default unassigned index value
    private int SelectNodeIndex { get; init; } = -1;
    
    /// <inheritdoc/>
    IReadOnlyList<ISqlNode> IAstQuery.Nodes => Nodes;

    /// <summary>
    /// Appends the specified node to the query AST.
    /// </summary>
    /// <param name="node">The node to append to the query.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance containing the appended node.</returns>
    public SelectQuery<T> AddNode(ISqlNode node)
    {
        var newNodes = Nodes.Add(node);
        int newIndex = SelectNodeIndex;
        if (node is SelectNode || node is ExpressionSelectNode || node is RawSelectNode || node is ScalarSubquerySelectNode)
        {
            newIndex = newNodes.Length - 1;
        }
        return this with { Nodes = newNodes, SelectNodeIndex = newIndex };
    }

    /// <summary>
    /// Compiles the abstract syntax tree into an executable SQL string and its parameters.
    /// </summary>
    /// <param name="compiler">The SQL compiler specific to the target database provider.</param>
    /// <returns>The compiled SQL result.</returns>
    [RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
    [RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
    public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);

    /// <summary>
    /// Specifies the columns to project in the SELECT clause.
    /// </summary>
    /// <param name="columns">The names of the columns to select.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the projection applied.</returns>
    public SelectQuery<T> Select(params string[] columns) => AddNode(new SelectNode(columns, false));
    
    /// <summary>
    /// Specifies the projection for the SELECT clause using an expression.
    /// </summary>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="selector">The expression defining the columns to select.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the projection applied.</returns>
    public SelectQuery<T> Select<TResult>(Expression<Func<T, TResult>> selector) => AddNode(new ExpressionSelectNode(selector, false));

    /// <summary>
    /// Specifies a scalar subquery projection for the SELECT clause.
    /// </summary>
    /// <param name="subquery">The subquery that calculates the scalar value.</param>
    /// <param name="alias">The alias name assigned to the scalar column.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the scalar subquery applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subquery"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="alias"/> is empty or whitespace</exception>
    public SelectQuery<T> Select(ISqlQuery subquery, string alias)
    {
        ArgumentNullException.ThrowIfNull(subquery);
        if (string.IsNullOrWhiteSpace(alias)) throw new ArgumentException("Alias cannot be null or whitespace.", nameof(alias));
        return AddNode(new ScalarSubquerySelectNode(subquery, alias));
    }
    
    /// <summary>
    /// Specifies a raw SQL projection for the SELECT clause.
    /// </summary>
    /// <param name="sql">The formatted string containing the raw projection and its parameters.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the raw projection applied.</returns>
    public SelectQuery<T> RawSelect(FormattableString sql)
    {
        return AddNode(new RawSelectNode(sql.Format, sql.GetArguments(), false));
    }

    /// <summary>
    /// Projects a COUNT(*) aggregation.
    /// </summary>
    /// <param name="alias">The column alias (default: "count").</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the count projection applied.</returns>
    public SelectQuery<T> AsCount(string alias = "count") => AddNode(new RawSelectNode(string.IsNullOrWhiteSpace(alias) ? "COUNT(*)" : $"COUNT(*) AS {alias}", null, false));

    /// <summary>
    /// Projects a SUM(column) aggregation.
    /// </summary>
    /// <param name="column">The column name to sum.</param>
    /// <param name="alias">Optional column alias.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the sum projection applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is empty or whitespace</exception>
    public SelectQuery<T> AsSum(string column, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column));
        var sql = string.IsNullOrWhiteSpace(alias) ? $"SUM({column})" : $"SUM({column}) AS {alias}";
        return AddNode(new RawSelectNode(sql, null, false));
    }

    /// <summary>
    /// Projects an AVG(column) aggregation.
    /// </summary>
    /// <param name="column">The column name to average.</param>
    /// <param name="alias">Optional column alias.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the average projection applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is empty or whitespace</exception>
    public SelectQuery<T> AsAvg(string column, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column));
        var sql = string.IsNullOrWhiteSpace(alias) ? $"AVG({column})" : $"AVG({column}) AS {alias}";
        return AddNode(new RawSelectNode(sql, null, false));
    }

    /// <summary>
    /// Projects a MIN(column) aggregation.
    /// </summary>
    /// <param name="column">The column name to find the minimum for.</param>
    /// <param name="alias">Optional column alias.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the min projection applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is empty or whitespace</exception>
    public SelectQuery<T> AsMin(string column, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column));
        var sql = string.IsNullOrWhiteSpace(alias) ? $"MIN({column})" : $"MIN({column}) AS {alias}";
        return AddNode(new RawSelectNode(sql, null, false));
    }

    /// <summary>
    /// Projects a MAX(column) aggregation.
    /// </summary>
    /// <param name="column">The column name to find the maximum for.</param>
    /// <param name="alias">Optional column alias.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the max projection applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> is empty or whitespace</exception>
    public SelectQuery<T> AsMax(string column, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column));
        var sql = string.IsNullOrWhiteSpace(alias) ? $"MAX({column})" : $"MAX({column}) AS {alias}";
        return AddNode(new RawSelectNode(sql, null, false));
    }

    /// <summary>
    /// Appends one or more window function expressions to the SELECT clause.
    /// </summary>
    /// <param name="windows">
    /// One or more <see cref="WindowFunctionNode"/> instances produced by the <see cref="Window"/> factory.
    /// </param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the window functions applied.</returns>
    /// <example>
    /// <code>
    /// var query = Sql.From&lt;Employee&gt;()
    ///     .Select(
    ///         Window.Rank&lt;Employee&gt;()
    ///               .PartitionBy(e => e.Department)
    ///               .OrderByDescending(e => e.Salary)
    ///               .As("rank"),
    ///         Window.RowNumber&lt;Employee&gt;()
    ///               .OrderBy(e => e.CreatedAt)
    ///               .As("row_num"));
    /// </code>
    /// </example>
    public SelectQuery<T> Select(params EricksonLopez.SqlBuilder.Abstractions.Nodes.WindowFunctionNode[] windows)
    {
        var q = this;
        foreach (var window in windows)
        {
            q = q.AddNode(window);
        }
        return q;
    }

    /// <summary>
    /// Applies the DISTINCT modifier to the current SELECT projection to eliminate duplicate rows.
    /// </summary>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the DISTINCT modifier applied.</returns>
    public SelectQuery<T> Distinct()
    {
        // Stryker disable once Equality : Boundary check for array index
        if (SelectNodeIndex >= 0 && SelectNodeIndex < Nodes.Length)
        {
            var node = Nodes[SelectNodeIndex];
            var sn = node as SelectNode;
            if (sn != null)
            {
                return this with { Nodes = Nodes.SetItem(SelectNodeIndex, sn with { IsDistinct = true }) };
            }
            var esn = node as ExpressionSelectNode;
            if (esn != null)
            {
                return this with { Nodes = Nodes.SetItem(SelectNodeIndex, esn with { IsDistinct = true }) };
            }
            var rsn = node as RawSelectNode;
            if (rsn != null)
            {
                return this with { Nodes = Nodes.SetItem(SelectNodeIndex, rsn with { IsDistinct = true }) };
            }
        }
        
        return AddNode(new SelectNode(Array.Empty<string>(), true));
    }

    /// <summary>
    /// Specifies the primary table to query from.
    /// </summary>
    /// <param name="tableName">The name of the source table.</param>
    /// <param name="alias">An optional alias for the source table.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the FROM clause applied.</returns>
    public SelectQuery<T> From(string tableName, string? alias = null) => AddNode(new FromNode(tableName, alias));
    
    /// <summary>
    /// Specifies a subquery as the primary data source.
    /// </summary>
    /// <param name="query">The subquery to execute.</param>
    /// <param name="alias">The alias for the subquery result set.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the subquery FROM clause applied.</returns>
    public SelectQuery<T> From(ISqlQuery query, string alias) => AddNode(new SubqueryFromNode(query, alias));
    
    /// <summary>
    /// Applies an alias to the current query when used as a subquery.
    /// </summary>
    /// <param name="alias">The alias to apply.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the alias applied.</returns>
    public SelectQuery<T> Alias(string alias) => AddNode(new QueryAliasNode(alias));

    /// <summary>
    /// Appends an INNER JOIN clause to the query.
    /// </summary>
    /// <param name="tableName">The name of the table to join.</param>
    /// <param name="alias">The alias for the joined table.</param>
    /// <param name="on">The join condition.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the join clause applied.</returns>
    public SelectQuery<T> Join(string tableName, string alias, string on) => AddNode(new JoinNode(JoinType.Inner, tableName, alias, on));
    
    /// <summary>
    /// Appends a strongly-typed INNER JOIN clause to the query based on a predicate.
    /// </summary>
    /// <typeparam name="TOther">The type of the joined entity.</typeparam>
    /// <param name="on">The predicate expression defining the join condition.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the join clause applied.</returns>
    public SelectQuery<T> Join<TOther>(Expression<Func<T, TOther, bool>> on) where TOther : new() => AddNode(new JoinNode(JoinType.Inner, SqlEntityCache<TOther>.TableName, null, null, on));
    
    /// <summary>
    /// Appends an INNER JOIN clause to the query.
    /// </summary>
    /// <param name="tableName">The name of the table to join.</param>
    /// <param name="alias">The alias for the joined table.</param>
    /// <param name="on">The join condition.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the join clause applied.</returns>
    public SelectQuery<T> InnerJoin(string tableName, string alias, string on) => AddNode(new JoinNode(JoinType.Inner, tableName, alias, on));
    
    /// <summary>
    /// Appends a LEFT OUTER JOIN clause to the query.
    /// </summary>
    /// <param name="tableName">The name of the table to join.</param>
    /// <param name="alias">The alias for the joined table.</param>
    /// <param name="on">The join condition.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the join clause applied.</returns>
    public SelectQuery<T> LeftJoin(string tableName, string alias, string on) => AddNode(new JoinNode(JoinType.Left, tableName, alias, on));
    
    /// <summary>
    /// Appends a RIGHT OUTER JOIN clause to the query.
    /// </summary>
    /// <param name="tableName">The name of the table to join.</param>
    /// <param name="alias">The alias for the joined table.</param>
    /// <param name="on">The join condition.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the join clause applied.</returns>
    public SelectQuery<T> RightJoin(string tableName, string alias, string on) => AddNode(new JoinNode(JoinType.Right, tableName, alias, on));
    
    /// <summary>
    /// Appends a CROSS JOIN clause to the query.
    /// </summary>
    /// <param name="tableName">The name of the table to join.</param>
    /// <param name="alias">The alias for the joined table.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the join clause applied.</returns>
    public SelectQuery<T> CrossJoin(string tableName, string alias) => AddNode(new JoinNode(JoinType.Cross, tableName, alias));
    
    /// <summary>
    /// Appends a FULL OUTER JOIN clause to the query.
    /// </summary>
    /// <param name="tableName">The name of the table to join.</param>
    /// <param name="alias">The alias for the joined table.</param>
    /// <param name="on">The join condition.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the join clause applied.</returns>
    public SelectQuery<T> FullJoin(string tableName, string alias, string on) => AddNode(new JoinNode(JoinType.Full, tableName, alias, on));
    
    /// <summary>
    /// Appends a raw SQL JOIN clause to the query.
    /// </summary>
    /// <param name="joinSql">The formatted string containing the raw join command and its parameters.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the join clause applied.</returns>
    public SelectQuery<T> RawJoin(FormattableString joinSql)
    {
        return AddNode(new RawJoinNode(joinSql.Format, joinSql.GetArguments()));
    }

    /// <summary>
    /// Appends a LATERAL JOIN clause (INNER JOIN LATERAL) to the query.
    /// The subquery is evaluated per-row of the outer query, like a correlated subquery.
    /// </summary>
    /// <param name="subquery">The correlated subquery to laterally join.</param>
    /// <param name="alias">The alias for the lateral result set.</param>
    /// <param name="on">Optional ON condition string. Usually omitted for LATERAL (use <c>TRUE</c> or <see langword="null"/>).</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the LATERAL JOIN applied.</returns>
    /// <remarks>
    /// Supported by: PostgreSQL, MySQL 8.0+. For SQL Server, use CROSS APPLY syntax via <see cref="RawJoin"/>.
    /// Not supported by: SQLite, Oracle (use alternatives).
    /// </remarks>
    public SelectQuery<T> LateralJoin(IAstQuery subquery, string alias, string? on = null)
        => AddNode(new SubqueryJoinNode(JoinType.Inner, subquery, alias, on, IsLateral: true));

    /// <summary>
    /// Appends a LATERAL JOIN clause (INNER JOIN LATERAL) with a typed ON expression.
    /// </summary>
    public SelectQuery<T> LateralJoin<TSub>(IAstQuery subquery, string alias, Expression<Func<T, TSub, bool>> on)
        => AddNode(new SubqueryJoinNode(JoinType.Inner, subquery, alias, null, IsLateral: true, ExpressionCondition: on));

    /// <summary>
    /// Appends a LATERAL JOIN clause constructed via a fluent subquery factory.
    /// </summary>
    public SelectQuery<T> LateralJoin<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias, string? on = null) where TSub : class, new()
        => LateralJoin(subqueryFactory(Sql.From<TSub>()), alias, on);

    /// <summary>
    /// Appends a LATERAL JOIN clause constructed via a fluent subquery factory with a typed ON expression.
    /// </summary>
    public SelectQuery<T> LateralJoin<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias, Expression<Func<T, TSub, bool>> on) where TSub : class, new()
        => LateralJoin(subqueryFactory(Sql.From<TSub>()), alias, on);

    /// <summary>
    /// Appends a LEFT LATERAL JOIN clause (LEFT JOIN LATERAL) to the query.
    /// Rows from the outer query that have no match in the lateral subquery are preserved.
    /// </summary>
    /// <param name="subquery">The correlated subquery to laterally join.</param>
    /// <param name="alias">The alias for the lateral result set.</param>
    /// <param name="on">Optional ON condition string.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the LEFT LATERAL JOIN applied.</returns>
    public SelectQuery<T> LateralLeftJoin(IAstQuery subquery, string alias, string? on = null)
        => AddNode(new SubqueryJoinNode(JoinType.Left, subquery, alias, on, IsLateral: true));

    /// <summary>
    /// Appends a LEFT LATERAL JOIN clause (LEFT JOIN LATERAL) with a typed ON expression.
    /// </summary>
    public SelectQuery<T> LateralLeftJoin<TSub>(IAstQuery subquery, string alias, Expression<Func<T, TSub, bool>> on)
        => AddNode(new SubqueryJoinNode(JoinType.Left, subquery, alias, null, IsLateral: true, ExpressionCondition: on));

    /// <summary>
    /// Appends a LEFT LATERAL JOIN clause constructed via a fluent subquery factory.
    /// </summary>
    public SelectQuery<T> LateralLeftJoin<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias, string? on = null) where TSub : class, new()
        => LateralLeftJoin(subqueryFactory(Sql.From<TSub>()), alias, on);

    /// <summary>
    /// Appends a LEFT LATERAL JOIN clause constructed via a fluent subquery factory with a typed ON expression.
    /// </summary>
    public SelectQuery<T> LateralLeftJoin<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias, Expression<Func<T, TSub, bool>> on) where TSub : class, new()
        => LateralLeftJoin(subqueryFactory(Sql.From<TSub>()), alias, on);

    /// <summary>
    /// Appends a JOIN to a subquery (non-lateral, standard INNER JOIN with subquery in parentheses).
    /// </summary>
    /// <param name="subquery">The subquery to join.</param>
    /// <param name="alias">The alias for the derived table.</param>
    /// <param name="on">The ON condition for the join.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the subquery join applied.</returns>
    public SelectQuery<T> JoinSubquery(IAstQuery subquery, string alias, string? on = null)
        => AddNode(new SubqueryJoinNode(JoinType.Inner, subquery, alias, on, IsLateral: false));

    /// <summary>
    /// Appends a JOIN to a subquery with a typed ON expression.
    /// </summary>
    public SelectQuery<T> JoinSubquery<TSub>(IAstQuery subquery, string alias, Expression<Func<T, TSub, bool>> on)
        => AddNode(new SubqueryJoinNode(JoinType.Inner, subquery, alias, null, IsLateral: false, ExpressionCondition: on));

    /// <summary>
    /// Appends a LEFT JOIN to a subquery (non-lateral).
    /// </summary>
    /// <param name="subquery">The subquery to join.</param>
    /// <param name="alias">The alias for the derived table.</param>
    /// <param name="on">The ON condition for the join.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the subquery join applied.</returns>
    public SelectQuery<T> LeftJoinSubquery(IAstQuery subquery, string alias, string? on = null)
        => AddNode(new SubqueryJoinNode(JoinType.Left, subquery, alias, on, IsLateral: false));

    /// <summary>
    /// Appends a LEFT JOIN to a subquery with a typed ON expression.
    /// </summary>
    public SelectQuery<T> LeftJoinSubquery<TSub>(IAstQuery subquery, string alias, Expression<Func<T, TSub, bool>> on)
        => AddNode(new SubqueryJoinNode(JoinType.Left, subquery, alias, null, IsLateral: false, ExpressionCondition: on));

    /// <summary>
    /// Appends a WHERE clause to the query using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the filtering condition.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the filter applied.</returns>
    public SelectQuery<T> Where(Expression<Func<T, bool>> predicate) => AddNode(new ExpressionWhereNode(predicate));
    
    /// <summary>
    /// Appends a raw SQL WHERE clause to the query.
    /// </summary>
    /// <param name="condition">The formatted string containing the raw filter and its parameters.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the filter applied.</returns>
    public SelectQuery<T> Where(FormattableString condition)
    {
        return AddNode(new RawWhereNode(condition.Format, condition.GetArguments()));
    }

    /// <summary>
    /// Appends a comparison between two columns to the WHERE clause.
    /// </summary>
    /// <param name="column1">The left-hand column name.</param>
    /// <param name="operator">The comparison operator (e.g. "=", "&lt;&gt;", "&lt;", "&gt;", "&lt;=", "&gt;=").</param>
    /// <param name="column2">The right-hand column name.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column1"/>, <paramref name="operator"/>, or <paramref name="column2"/> is empty or whitespace</exception>
    public SelectQuery<T> WhereColumns(string column1, string @operator, string column2)
    {
        if (string.IsNullOrWhiteSpace(column1)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column1));
        if (string.IsNullOrWhiteSpace(@operator)) throw new ArgumentException("Operator cannot be null or whitespace.", nameof(@operator));
        if (string.IsNullOrWhiteSpace(column2)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column2));
        return AddNode(new RawWhereNode($"{column1} {@operator} {column2}", null));
    }

    /// <summary>
    /// Appends a date comparison condition to the WHERE clause.
    /// </summary>
    /// <param name="column">The column name containing the date.</param>
    /// <param name="operator">The comparison operator (e.g., "=", "&gt;", "&lt;").</param>
    /// <param name="value">The date value to compare against.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> or <paramref name="operator"/> is empty or whitespace</exception>
    public SelectQuery<T> WhereDate(string column, string @operator, object value)
    {
        if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column));
        if (string.IsNullOrWhiteSpace(@operator)) throw new ArgumentException("Operator cannot be null or whitespace.", nameof(@operator));
        return AddNode(new RawWhereNode($"{column} {@operator} {{0}}", new object?[] { value }));
    }

    /// <summary>
    /// Appends a year comparison predicate to the WHERE clause.
    /// </summary>
    /// <param name="column">The column name containing the date.</param>
    /// <param name="operator">The comparison operator (e.g., "=", "&gt;", "&lt;").</param>
    /// <param name="year">The year integer to compare against.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> or <paramref name="operator"/> is empty or whitespace</exception>
    public SelectQuery<T> WhereYear(string column, string @operator, int year)
    {
        if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column));
        if (string.IsNullOrWhiteSpace(@operator)) throw new ArgumentException("Operator cannot be null or whitespace.", nameof(@operator));
        return AddNode(new RawWhereNode($"EXTRACT(YEAR FROM {column}) {@operator} {{0}}", new object?[] { year }));
    }

    /// <summary>
    /// Appends a month comparison predicate to the WHERE clause.
    /// </summary>
    /// <param name="column">The column name containing the date.</param>
    /// <param name="operator">The comparison operator (e.g., "=", "&gt;", "&lt;").</param>
    /// <param name="month">The month integer (1-12) to compare against.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> or <paramref name="operator"/> is empty or whitespace</exception>
    public SelectQuery<T> WhereMonth(string column, string @operator, int month)
    {
        if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column));
        if (string.IsNullOrWhiteSpace(@operator)) throw new ArgumentException("Operator cannot be null or whitespace.", nameof(@operator));
        return AddNode(new RawWhereNode($"EXTRACT(MONTH FROM {column}) {@operator} {{0}}", new object?[] { month }));
    }

    /// <summary>
    /// Appends a day comparison predicate to the WHERE clause.
    /// </summary>
    /// <param name="column">The column name containing the date.</param>
    /// <param name="operator">The comparison operator (e.g., "=", "&gt;", "&lt;").</param>
    /// <param name="day">The day integer (1-31) to compare against.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance.</returns>
    /// <exception cref="ArgumentException"><paramref name="column"/> or <paramref name="operator"/> is empty or whitespace</exception>
    public SelectQuery<T> WhereDay(string column, string @operator, int day)
    {
        if (string.IsNullOrWhiteSpace(column)) throw new ArgumentException("Column name cannot be null or whitespace.", nameof(column));
        if (string.IsNullOrWhiteSpace(@operator)) throw new ArgumentException("Operator cannot be null or whitespace.", nameof(@operator));
        return AddNode(new RawWhereNode($"EXTRACT(DAY FROM {column}) {@operator} {{0}}", new object?[] { day }));
    }
    
    /// <summary>
    /// Appends an AND logical condition to the current WHERE clause using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the additional filtering condition.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the filter applied.</returns>
    public SelectQuery<T> And(Expression<Func<T, bool>> predicate) => AddNode(new ExpressionWhereNode(predicate, IsOr: false));
    
    /// <summary>
    /// Appends an OR logical condition to the current WHERE clause using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the alternative filtering condition.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the filter applied.</returns>
    public SelectQuery<T> Or(Expression<Func<T, bool>> predicate) => AddNode(new ExpressionWhereNode(predicate, IsOr: true));
    
    /// <summary>
    /// Appends a WHERE EXISTS (subquery) condition to the query.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside EXISTS.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the EXISTS filter applied.</returns>
    public SelectQuery<T> WhereExists(ISqlQuery subquery) => AddNode(new ExistsWhereNode(subquery, IsNot: false, IsOr: false));
    
    /// <summary>
    /// Appends a WHERE NOT EXISTS (subquery) condition to the query.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside NOT EXISTS.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the NOT EXISTS filter applied.</returns>
    public SelectQuery<T> WhereNotExists(ISqlQuery subquery) => AddNode(new ExistsWhereNode(subquery, IsNot: true, IsOr: false));
    
    /// <summary>
    /// Appends an OR EXISTS (subquery) condition to the query.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside EXISTS.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the OR EXISTS filter applied.</returns>
    public SelectQuery<T> OrExists(ISqlQuery subquery) => AddNode(new ExistsWhereNode(subquery, IsNot: false, IsOr: true));
    
    /// <summary>
    /// Appends an OR NOT EXISTS (subquery) condition to the query.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside NOT EXISTS.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the OR NOT EXISTS filter applied.</returns>
    public SelectQuery<T> OrNotExists(ISqlQuery subquery) => AddNode(new ExistsWhereNode(subquery, IsNot: true, IsOr: true));
    
    /// <summary>
    /// Appends a GROUP BY clause to the query to aggregate results.
    /// </summary>
    /// <param name="columns">The names of the columns to group by.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the grouping applied.</returns>
    public SelectQuery<T> GroupBy(params string[] columns) => AddNode(new GroupByNode(columns));

    /// <summary>
    /// Appends a GROUP BY ROLLUP clause to the query for hierarchical subtotal aggregations.
    /// </summary>
    /// <param name="columns">The names of the columns in the rollup hierarchy.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the ROLLUP grouping applied.</returns>
    public SelectQuery<T> GroupByRollup(params string[] columns) => AddNode(new GroupByNode(columns, GroupByType.Rollup));

    /// <summary>
    /// Appends a GROUP BY CUBE clause to the query for multidimensional aggregations.
    /// </summary>
    /// <param name="columns">The names of the columns to produce cube cross-tabulations for.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the CUBE grouping applied.</returns>
    public SelectQuery<T> GroupByCube(params string[] columns) => AddNode(new GroupByNode(columns, GroupByType.Cube));

    /// <summary>
    /// Appends a GROUP BY GROUPING SETS clause to the query for explicit multi-level aggregations.
    /// </summary>
    /// <param name="sets">The explicit column grouping combinations.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the GROUPING SETS grouping applied.</returns>
    public SelectQuery<T> GroupingSets(params IReadOnlyList<string>[] sets) => AddNode(new GroupByNode(System.Array.Empty<string>(), GroupByType.GroupingSets, sets));
    
    /// <summary>
    /// Appends a HAVING clause to filter aggregated results using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the aggregation filter.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the aggregation filter applied.</returns>
    public SelectQuery<T> Having(Expression<Func<T, bool>> predicate) => AddNode(new ExpressionHavingNode(predicate));
    
    /// <summary>
    /// Appends an OR logical condition to the current HAVING clause using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the alternative aggregation filter.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the aggregation filter applied.</returns>
    public SelectQuery<T> OrHaving(Expression<Func<T, bool>> predicate) => AddNode(new ExpressionHavingNode(predicate, true));
    
    /// <summary>
    /// Appends a raw SQL HAVING clause to filter aggregated results.
    /// </summary>
    /// <param name="condition">The formatted string containing the raw filter and its parameters.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the aggregation filter applied.</returns>
    public SelectQuery<T> Having(FormattableString condition)
    {
        return AddNode(new RawHavingNode(condition.Format, condition.GetArguments()));
    }
    
    /// <summary>
    /// Appends a raw SQL OR condition to the current HAVING clause.
    /// </summary>
    /// <param name="condition">The formatted string containing the raw alternative filter and its parameters.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the aggregation filter applied.</returns>
    public SelectQuery<T> OrHaving(FormattableString condition)
    {
        return AddNode(new RawHavingNode(condition.Format, condition.GetArguments(), true));
    }
    
    /// <summary>
    /// Appends an ORDER BY clause to sort the results in ascending order.
    /// </summary>
    /// <param name="keySelector">The expression specifying the sorting key.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the sorting rule applied.</returns>
    public SelectQuery<T> OrderBy(Expression<Func<T, object>> keySelector) => AddNode(new OrderByNode(keySelector, false));

    /// <summary>
    /// Appends an ORDER BY clause to sort the results in ascending order with explicit NULL placement.
    /// </summary>
    /// <param name="keySelector">The expression specifying the sorting key.</param>
    /// <param name="nulls">Controls whether NULLs appear first or last in the ordering.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the sorting rule applied.</returns>
    public SelectQuery<T> OrderBy(Expression<Func<T, object>> keySelector, NullsPosition nulls) => AddNode(new OrderByNode(keySelector, false, nulls));

    /// <summary>
    /// Appends an ORDER BY clause to sort the results in descending order.
    /// </summary>
    /// <param name="keySelector">The expression specifying the sorting key.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the sorting rule applied.</returns>
    public SelectQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector) => AddNode(new OrderByNode(keySelector, true));

    /// <summary>
    /// Appends an ORDER BY clause to sort the results in descending order with explicit NULL placement.
    /// </summary>
    /// <param name="keySelector">The expression specifying the sorting key.</param>
    /// <param name="nulls">Controls whether NULLs appear first or last in the ordering.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the sorting rule applied.</returns>
    public SelectQuery<T> OrderByDescending(Expression<Func<T, object>> keySelector, NullsPosition nulls) => AddNode(new OrderByNode(keySelector, true, nulls));

    /// <summary>
    /// Appends a subsequent sort condition in ascending order.
    /// </summary>
    /// <param name="keySelector">The expression specifying the additional sorting key.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the additional sorting rule applied.</returns>
    public SelectQuery<T> ThenBy(Expression<Func<T, object>> keySelector) => AddNode(new ThenByNode(keySelector, false));

    /// <summary>
    /// Appends a subsequent sort condition in ascending order with explicit NULL placement.
    /// </summary>
    /// <param name="keySelector">The expression specifying the additional sorting key.</param>
    /// <param name="nulls">Controls whether NULLs appear first or last.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the additional sorting rule applied.</returns>
    public SelectQuery<T> ThenBy(Expression<Func<T, object>> keySelector, NullsPosition nulls) => AddNode(new ThenByNode(keySelector, false, nulls));

    /// <summary>
    /// Appends a subsequent sort condition in descending order.
    /// </summary>
    /// <param name="keySelector">The expression specifying the additional sorting key.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the additional sorting rule applied.</returns>
    public SelectQuery<T> ThenByDescending(Expression<Func<T, object>> keySelector) => AddNode(new ThenByNode(keySelector, true));

    /// <summary>
    /// Appends a subsequent sort condition in descending order with explicit NULL placement.
    /// </summary>
    /// <param name="keySelector">The expression specifying the additional sorting key.</param>
    /// <param name="nulls">Controls whether NULLs appear first or last.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the additional sorting rule applied.</returns>
    public SelectQuery<T> ThenByDescending(Expression<Func<T, object>> keySelector, NullsPosition nulls) => AddNode(new ThenByNode(keySelector, true, nulls));
    
    /// <summary>
    /// Appends a raw SQL ORDER BY clause to sort the results in ascending order.
    /// </summary>
    /// <param name="sql">The formatted string containing the raw sorting column or expression.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the sorting rule applied.</returns>
    public SelectQuery<T> OrderBy(FormattableString sql)
    {
        return AddNode(new RawOrderByNode(sql.Format, false, sql.GetArguments()));
    }
    
    /// <summary>
    /// Appends a raw SQL ORDER BY clause to sort the results in descending order.
    /// </summary>
    /// <param name="sql">The formatted string containing the raw sorting column or expression.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the sorting rule applied.</returns>
    public SelectQuery<T> OrderByDescending(FormattableString sql)
    {
        return AddNode(new RawOrderByNode(sql.Format, true, sql.GetArguments()));
    }
    
    /// <summary>
    /// Restricts the maximum number of rows returned by the query.
    /// </summary>
    /// <param name="limit">The maximum number of rows.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the row limit applied.</returns>
    public SelectQuery<T> Limit(int limit) => AddNode(new LimitOffsetNode(limit, null));
    
    /// <summary>
    /// Specifies the number of rows to skip before returning results.
    /// </summary>
    /// <param name="offset">The number of rows to skip.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the offset applied.</returns>
    public SelectQuery<T> Offset(int offset) => AddNode(new LimitOffsetNode(null, offset));
    
    /// <summary>
    /// Restricts the maximum number of rows returned by the query.
    /// </summary>
    /// <remarks>
    /// Obsolete. Use <see cref="Limit(int)"/> instead, which provides the identical functionality.
    /// </remarks>
    /// <param name="rows">The maximum number of rows to fetch.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the row limit applied.</returns>
    /// <summary>Specifies row limit (synonym for Limit).</summary>
    public SelectQuery<T> Fetch(int rows) => AddNode(new LimitOffsetNode(rows, null));
    
    /// <summary>
    /// Defines a Common Table Expression (CTE) for the query.
    /// </summary>
    /// <param name="name">The name assigned to the CTE.</param>
    /// <param name="query">The query defining the CTE data set.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the CTE applied.</returns>
    public SelectQuery<T> CTE(string name, ISqlQuery query) => AddNode(new CteNode(name, query));

    /// <summary>
    /// Defines a Common Table Expression (CTE) for the query with an explicit materialization hint.
    /// </summary>
    /// <param name="name">The name assigned to the CTE.</param>
    /// <param name="query">The query defining the CTE data set.</param>
    /// <param name="hint">The materialization hint (e.g. MATERIALIZED or NOT MATERIALIZED in PostgreSQL).</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the CTE applied.</returns>
    public SelectQuery<T> CTE(string name, ISqlQuery query, MaterializationHint hint) => AddNode(new CteNode(name, query, false, hint));
    
    /// <summary>
    /// Defines a recursive Common Table Expression (CTE) for the query.
    /// </summary>
    /// <param name="name">The name assigned to the CTE.</param>
    /// <param name="query">The query defining the CTE data set.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the recursive CTE applied.</returns>
    public SelectQuery<T> RecursiveCTE(string name, ISqlQuery query) => AddNode(new CteNode(name, query, true));

    /// <summary>
    /// Defines a recursive Common Table Expression (CTE) for the query with an explicit materialization hint.
    /// </summary>
    /// <param name="name">The name assigned to the CTE.</param>
    /// <param name="query">The query defining the CTE data set.</param>
    /// <param name="hint">The materialization hint (e.g. MATERIALIZED or NOT MATERIALIZED in PostgreSQL).</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the recursive CTE applied.</returns>
    public SelectQuery<T> RecursiveCTE(string name, ISqlQuery query, MaterializationHint hint) => AddNode(new CteNode(name, query, true, hint));
    
    /// <summary>
    /// Defines a named window specification for window functions.
    /// </summary>
    /// <param name="name">The name of the window.</param>
    /// <param name="partitionBy">An optional array of column names to partition the window.</param>
    /// <param name="orderBy">An optional array of column names to order the window.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the window specification applied.</returns>
    public SelectQuery<T> Window(string name, string[]? partitionBy = null, string[]? orderBy = null) => AddNode(new WindowNode(name, partitionBy, orderBy));
    
    /// <summary>
    /// Appends a UNION set operation to combine results with another query, eliminating duplicates.
    /// </summary>
    /// <param name="query">The query to union with.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the set operation applied.</returns>
    public SelectQuery<T> Union(ISqlQuery query) => AddNode(new SetOperationNode("UNION", query));
    
    /// <summary>
    /// Appends a UNION ALL set operation to combine results with another query, retaining duplicates.
    /// </summary>
    /// <param name="query">The query to union with.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the set operation applied.</returns>
    public SelectQuery<T> UnionAll(ISqlQuery query) => AddNode(new SetOperationNode("UNION ALL", query));
    
    /// <summary>
    /// Appends an INTERSECT set operation to return only the rows present in both query results.
    /// </summary>
    /// <param name="query">The query to intersect with.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the set operation applied.</returns>
    public SelectQuery<T> Intersect(ISqlQuery query) => AddNode(new SetOperationNode("INTERSECT", query));

    /// <summary>
    /// Appends an INTERSECT ALL set operation to return matching rows from both queries, preserving duplicates.
    /// </summary>
    /// <param name="query">The query to intersect with.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the set operation applied.</returns>
    public SelectQuery<T> IntersectAll(ISqlQuery query) => AddNode(new SetOperationNode("INTERSECT ALL", query));
    
    /// <summary>
    /// Appends an EXCEPT set operation to return rows from the current query that are not present in the specified query.
    /// </summary>
    /// <param name="query">The query to compare against.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the set operation applied.</returns>
    public SelectQuery<T> Except(ISqlQuery query) => AddNode(new SetOperationNode("EXCEPT", query));

    /// <summary>
    /// Appends an EXCEPT ALL set operation to return rows from the current query not present in the specified query, preserving duplicate counts.
    /// </summary>
    /// <param name="query">The query to compare against.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the set operation applied.</returns>
    public SelectQuery<T> ExceptAll(ISqlQuery query) => AddNode(new SetOperationNode("EXCEPT ALL", query));

    /// <summary>
    /// Applies Window-based pagination (ROW_NUMBER) using the specified column for ordering.
    /// </summary>
    /// <remarks>
    /// This is an alternative to Offset pagination for better deep-pagination performance.
    /// </remarks>
    /// <param name="pageNumber">The one-based page number to retrieve.</param>
    /// <param name="pageSize">The maximum number of items per page. Must be greater than zero.</param>
    /// <param name="orderByColumn">The name of the column to sort by before assigning row numbers.</param>
    /// <param name="descending">If <see langword="true"/>, sorts the column in descending order; otherwise, ascending.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with window-based pagination applied.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="pageSize"/> is less than or equal to zero</exception>
    public SelectQuery<T> WindowPage(int pageNumber, int pageSize, string orderByColumn, bool descending = false)
    {
        if (pageSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
        }
        
        int p = System.Math.Max(1, pageNumber);
        return AddNode(new WindowPageNode(p, pageSize, orderByColumn, descending));
    }

    // ─── CROSS APPLY / OUTER APPLY ─────────────────────────────────────────────

    /// <summary>
    /// Appends a CROSS APPLY clause (SQL Server) / INNER JOIN LATERAL (PostgreSQL) to the query.
    /// CROSS APPLY returns only rows from the outer query that have matching rows in the correlated subquery.
    /// </summary>
    /// <param name="subquery">The correlated subquery to apply.</param>
    /// <param name="alias">The alias for the applied result set.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the CROSS APPLY applied.</returns>
    /// <remarks>
    /// Supported by: SQL Server, PostgreSQL (as CROSS JOIN LATERAL). Not supported by: SQLite, MySQL &lt; 8.0, Oracle.
    /// </remarks>
    [RequiresCapability(ProviderCapability.Apply | ProviderCapability.Lateral)]
    public SelectQuery<T> CrossApply(IAstQuery subquery, string alias)
        => AddNode(new SubqueryJoinNode(JoinType.CrossApply, subquery, alias, null, IsLateral: false));

    /// <summary>
    /// Appends a CROSS APPLY clause constructed via a fluent subquery factory.
    /// </summary>
    [RequiresCapability(ProviderCapability.Apply | ProviderCapability.Lateral)]
    public SelectQuery<T> CrossApply<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias) where TSub : class, new()
        => CrossApply(subqueryFactory(Sql.From<TSub>()), alias);

    /// <summary>
    /// Appends an OUTER APPLY clause (SQL Server) / LEFT JOIN LATERAL (PostgreSQL) to the query.
    /// OUTER APPLY preserves rows from the outer query even when the correlated subquery returns no rows.
    /// </summary>
    /// <param name="subquery">The correlated subquery to apply.</param>
    /// <param name="alias">The alias for the applied result set.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the OUTER APPLY applied.</returns>
    [RequiresCapability(ProviderCapability.Apply | ProviderCapability.Lateral)]
    public SelectQuery<T> OuterApply(IAstQuery subquery, string alias)
        => AddNode(new SubqueryJoinNode(JoinType.OuterApply, subquery, alias, null, IsLateral: false));

    /// <summary>
    /// Appends an OUTER APPLY clause constructed via a fluent subquery factory.
    /// </summary>
    [RequiresCapability(ProviderCapability.Apply | ProviderCapability.Lateral)]
    public SelectQuery<T> OuterApply<TSub>(Func<SelectQuery<TSub>, IAstQuery> subqueryFactory, string alias) where TSub : class, new()
        => OuterApply(subqueryFactory(Sql.From<TSub>()), alias);

    // ─── CASE Expression ───────────────────────────────────────────────────────

    /// <summary>
    /// Adds a CASE expression column to the SELECT clause using a fluent builder.
    /// </summary>
    /// <param name="configure">A delegate that configures the <see cref="Builders.CaseExpressionBuilder"/>.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the CASE column added.</returns>
    /// <example>
    /// <code>
    /// Sql.From&lt;User&gt;()
    ///    .SelectCase(c => c
    ///        .When("status = {0}", 1).Then("'Active'")
    ///        .When("status = {0}", 2).Then("'Inactive'")
    ///        .Else("'Unknown'")
    ///        .As("status_label"));
    /// </code>
    /// </example>
    public SelectQuery<T> SelectCase(System.Func<Builders.CaseExpressionBuilder, Builders.CaseExpressionBuilder> configure)
    {
        var builder = configure(new Builders.CaseExpressionBuilder());
        return AddNode(builder.Build());
    }

    /// <summary>
    /// Adds a pre-built <see cref="Abstractions.Nodes.CaseNode"/> directly to the SELECT clause.
    /// </summary>
    /// <param name="caseNode">The prepared case node containing the conditional logic.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance with the CASE column added.</returns>
    public SelectQuery<T> SelectCase(Abstractions.Nodes.CaseNode caseNode)
        => AddNode(caseNode);

    // ─── Composite Cursor Pagination ──────────────────────────────────────────

    /// <summary>
    /// Applies a forward composite keyset cursor, seeking rows that come after the specified anchor keys.
    /// </summary>
    /// <param name="keys">The anchor key columns and their values from the last row on the current page.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the cursor WHERE predicate applied.</returns>
    /// <remarks>
    /// For two ascending keys <c>(col1, col2)</c> with values <c>(v1, v2)</c>, generates:
    /// <code>WHERE (col1 &gt; @p0 OR (col1 = @p0 AND col2 &gt; @p1))</code>
    /// </remarks>
    public SelectQuery<T> SeekAfter(params CursorKey[] keys)
        => AddNode(new CompositeCursorNode(keys, IsAfter: true));

    /// <summary>
    /// Applies a backward composite keyset cursor, seeking rows that come before the specified anchor keys.
    /// </summary>
    /// <param name="keys">The anchor key columns and their values from the first row on the current page.</param>
    /// <returns>A new <see cref="SelectQuery{T}"/> with the cursor WHERE predicate applied.</returns>
    public SelectQuery<T> SeekBefore(params CursorKey[] keys)
        => AddNode(new CompositeCursorNode(keys, IsAfter: false));
}








