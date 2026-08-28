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
/// Represents an UPDATE query syntax tree builder for the specified entity type.
/// </summary>
/// <remarks>
/// This type is immutable. All modification methods return a new instance
/// preserving the previous state.
/// </remarks>
/// <typeparam name="T">The type of the entity to update.</typeparam>
public sealed record UpdateQuery<T> : IAstQuery, IUpdateSetBuilder<T>, IUpdateWhereBuilder<T> where T : class, new()
{
    /// <summary>
    /// Gets the optional tag associated with this query for diagnostics or interception.
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Creates a new <see cref="UpdateQuery{T}"/> with the specified diagnostic tag.
    /// </summary>
    /// <param name="tag">The diagnostic tag to associate with the query.</param>
    /// <returns>A new query instance containing the applied tag.</returns>
    public UpdateQuery<T> WithTag(string tag) => this with { Tag = tag };

    /// <summary>
    /// Gets the collection of nodes that compose the abstract syntax tree of this query.
    /// </summary>
    public System.Collections.Immutable.ImmutableArray<ISqlNode> Nodes { get; init; } = System.Collections.Immutable.ImmutableArray<ISqlNode>.Empty.Add(new UpdateNode(SqlEntityCache<T>.TableName));
    
    /// <inheritdoc/>
    IReadOnlyList<ISqlNode> IAstQuery.Nodes => Nodes;

    /// <summary>
    /// Appends the specified node to the query AST.
    /// </summary>
    /// <param name="node">The node to append to the query.</param>
    /// <returns>A new <see cref="UpdateQuery{T}"/> instance containing the appended node.</returns>
    public UpdateQuery<T> AddNode(ISqlNode node) => this with { Nodes = Nodes.Add(node) };

    /// <summary>
    /// Compiles the abstract syntax tree into an executable SQL string and its parameters.
    /// </summary>
    /// <param name="compiler">The SQL compiler specific to the target database provider.</param>
    /// <returns>The compiled SQL result.</returns>
    [RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
    [RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
    public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);

    /// <summary>
    /// Specifies the target table to update.
    /// </summary>
    /// <param name="tableName">An optional table name to override the default entity table.</param>
    /// <returns>A new <see cref="UpdateQuery{T}"/> instance with the target table applied.</returns>
    public UpdateQuery<T> Update(string? tableName = null) 
    {
        if (tableName != null)
        {
            return AddNode(new UpdateNode(tableName));
        }

        // Remove Reflection
        return AddNode(new UpdateNode(SqlEntityCache<T>.TableName));
    }
    
    /// <summary>
    /// Appends SET clauses based on the properties of the provided entity.
    /// </summary>
    /// <param name="entity">The entity instance containing the updated values.</param>
    /// <param name="ignoreNulls">If <see langword="true"/>, properties with null values will not generate SET assignments.</param>
    /// <returns>A new <see cref="IUpdateSetBuilder{T}"/> instance with the SET assignments applied.</returns>
    /// <exception cref="InvalidOperationException"><typeparamref name="T"/> does not implement <see cref="EricksonLopez.SqlBuilder.Annotations.ISqlEntity"/> or metadata column count does not match value count</exception>
    public IUpdateSetBuilder<T> Set(T entity, bool ignoreNulls = false)
    {
        var sqlEntity = entity as EricksonLopez.SqlBuilder.Annotations.ISqlEntity;
        if (sqlEntity != null)
        {
            var columns = sqlEntity.GetColumnNames();
            var values = sqlEntity.GetValues();
            
            if (columns.Length != values.Length)
            {
                throw new InvalidOperationException($"Entity {typeof(T).Name} metadata mismatch: GetColumnNames() returned {columns.Length} items, but GetValues() returned {values.Length}. They must match.");
            }
            
            var q = this;
            for (int i = 0; i < columns.Length; i++)
            {
                if (!ignoreNulls || values[i] != null)
                {
                    q = q.AddNode(new SetNode(columns[i], values[i]));
                }
            }
            return q;
        }
        
        throw new System.InvalidOperationException($"Entity {typeof(T).Name} does not implement ISqlEntity. Please ensure EricksonLopez.SqlBuilder.SourceGenerators is added and the entity is marked with [SqlEntity].");
    }

    /// <summary>
    /// Appends a SET clause for a specific property using an expression.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property being updated.</typeparam>
    /// <param name="property">The expression selecting the property to update.</param>
    /// <param name="value">The new value to assign to the property.</param>
    /// <returns>A new <see cref="IUpdateSetBuilder{T}"/> instance with the SET assignment applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="property"/> is not a valid member expression</exception>
    public IUpdateSetBuilder<T> Set<TProperty>(System.Linq.Expressions.Expression<System.Func<T, TProperty>> property, TProperty value)
    {
        var member = property.Body as MemberExpression;
        if (member != null) 
        {
            var columnName = SqlNamingHelper.ToSnakeCase(member.Member.Name);
            return AddNode(new SetNode(columnName, value));
        }
        
        throw new ArgumentException("Expression must be a member expression (e.g. x => x.Property)", nameof(property));
    }

    /// <summary>
    /// Appends a raw SQL SET assignment.
    /// </summary>
    /// <param name="sql">The formatted string containing the raw SET assignment and its parameters.</param>
    /// <returns>A new <see cref="IUpdateSetBuilder{T}"/> instance with the SET assignment applied.</returns>
    public IUpdateSetBuilder<T> Set(FormattableString sql)
    {
        return AddNode(new SetNode(null, null, sql.Format, sql.GetArguments()));
    }
    
    /// <summary>
    /// Specifies an additional source table for the UPDATE operation (FROM clause).
    /// </summary>
    /// <param name="tableName">The name of the additional table.</param>
    /// <param name="alias">An optional alias for the additional table.</param>
    /// <returns>A new <see cref="IUpdateSetBuilder{T}"/> instance with the FROM clause applied.</returns>
    public IUpdateSetBuilder<T> From(string tableName, string? alias = null) => AddNode(new FromNode(tableName, alias));

    /// <summary>
    /// Appends an INNER JOIN clause to the query.
    /// </summary>
    /// <param name="tableName">The name of the table to join.</param>
    /// <param name="alias">The alias for the joined table.</param>
    /// <param name="on">The join condition.</param>
    /// <returns>A new <see cref="IUpdateSetBuilder{T}"/> instance with the join clause applied.</returns>
    public IUpdateSetBuilder<T> Join(string tableName, string alias, string on) => AddNode(new JoinNode(JoinType.Inner, tableName, alias, on));

    /// <summary>
    /// Appends a strongly-typed INNER JOIN clause to the query based on a predicate.
    /// </summary>
    /// <typeparam name="TOther">The type of the joined entity.</typeparam>
    /// <param name="onExpression">The predicate expression defining the join condition.</param>
    /// <returns>A new <see cref="IUpdateSetBuilder{T}"/> instance with the join clause applied.</returns>
    public IUpdateSetBuilder<T> Join<TOther>(System.Linq.Expressions.Expression<System.Func<T, TOther, bool>> onExpression) where TOther : new() => AddNode(new JoinNode(JoinType.Inner, SqlEntityCache<TOther>.TableName, null, null, onExpression));
    
    /// <summary>
    /// Appends a WHERE clause to filter the rows to update using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the filtering condition.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the filter applied.</returns>
    public IUpdateWhereBuilder<T> Where(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate) => AddNode(new ExpressionWhereNode(predicate));

    /// <summary>
    /// Appends a raw SQL WHERE clause to filter the rows to update.
    /// </summary>
    /// <param name="condition">The formatted string containing the raw filter and its parameters.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the filter applied.</returns>
    public IUpdateWhereBuilder<T> Where(FormattableString condition)
    {
        return AddNode(new RawWhereNode(condition.Format, condition.GetArguments()));
    }

    /// <summary>
    /// Explicitly indicates that the update should apply to all rows without filtering.
    /// </summary>
    /// <returns>The current <see cref="IUpdateWhereBuilder{T}"/> instance.</returns>
    public IUpdateWhereBuilder<T> WhereAll() => this;

    /// <summary>
    /// Appends a WHERE EXISTS (subquery) condition to the UPDATE statement.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside EXISTS.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the EXISTS filter applied.</returns>
    public IUpdateWhereBuilder<T> WhereExists(ISqlQuery subquery) => AddNode(new ExistsWhereNode(subquery, IsNot: false, IsOr: false));

    /// <summary>
    /// Appends a WHERE NOT EXISTS (subquery) condition to the UPDATE statement.
    /// </summary>
    /// <param name="subquery">The subquery to evaluate inside NOT EXISTS.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the NOT EXISTS filter applied.</returns>
    public IUpdateWhereBuilder<T> WhereNotExists(ISqlQuery subquery) => AddNode(new ExistsWhereNode(subquery, IsNot: true, IsOr: false));

    /// <summary>
    /// Appends an AND logical condition to the current WHERE clause using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the additional filtering condition.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the filter applied.</returns>
    public IUpdateWhereBuilder<T> And(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate) => AddNode(new ExpressionWhereNode(predicate, false));

    /// <summary>
    /// Appends an OR logical condition to the current WHERE clause using a predicate expression.
    /// </summary>
    /// <param name="predicate">The expression defining the alternative filtering condition.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the filter applied.</returns>
    public IUpdateWhereBuilder<T> Or(System.Linq.Expressions.Expression<System.Func<T, bool>> predicate) => AddNode(new ExpressionWhereNode(predicate, true));
    
    /// <summary>
    /// Specifies the columns to return from the updated rows.
    /// </summary>
    /// <param name="columns">The names of the columns to return.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the returning clause applied.</returns>
    public IUpdateWhereBuilder<T> Returning(params string[] columns) => AddNode(new ReturningNode(columns));

    /// <summary>
    /// Specifies the columns to return from the updated rows using a predicate expression.
    /// </summary>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="selector">The expression defining the columns to return.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the returning clause applied.</returns>
    public IUpdateWhereBuilder<T> Returning<TResult>(System.Linq.Expressions.Expression<System.Func<T, TResult>> selector)
    {
        var cols = new List<string>();
        var newExpr = selector.Body as System.Linq.Expressions.NewExpression;
        var memExpr = selector.Body as System.Linq.Expressions.MemberExpression;
        if (newExpr != null && newExpr.Members != null)
        {
            foreach (var member in newExpr.Members)
            {
                cols.Add(SqlNamingHelper.ToSnakeCase(member.Name));
            }
        }
        else if (memExpr != null)
        {
            cols.Add(SqlNamingHelper.ToSnakeCase(memExpr.Member.Name));
        }
        return AddNode(new ReturningNode(cols.ToArray()));
    }
    /// <summary>
    /// Appends an optimistic concurrency check to the UPDATE statement.
    /// Generates <c>AND {column} = @expectedValue</c> in the WHERE clause
    /// and <c>SET {column} = {column} + 1</c> for integer tokens.
    /// </summary>
    /// <typeparam name="TToken">The type of the concurrency token column (e.g., <see langword="int"/>, <see cref="Guid"/>).</typeparam>
    /// <param name="tokenSelector">Expression identifying the concurrency token column.</param>
    /// <param name="expectedValue">The expected current value to match in the WHERE clause.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the concurrency check applied.</returns>
    /// <remarks>
    /// For <see langword="int"/> and <see langword="long"/> tokens, the value auto-increments (<c>column = column + 1</c>).
    /// For other types (e.g., <see cref="Guid"/>), supply the new value via the three-argument overload.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="tokenSelector"/> is not a member expression</exception>
    public IUpdateWhereBuilder<T> WithConcurrencyToken<TToken>(
        System.Linq.Expressions.Expression<System.Func<T, TToken>> tokenSelector,
        TToken expectedValue)
    {
        var member = tokenSelector.Body as System.Linq.Expressions.MemberExpression;
        if (member == null)
            throw new ArgumentException("Expression must be a member expression (e.g. x => x.Version)", nameof(tokenSelector));

        var columnName = SqlNamingHelper.ToSnakeCase(member.Member.Name);
        bool autoIncrement = typeof(TToken) == typeof(int) || typeof(TToken) == typeof(long);
        return AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.ConcurrencyTokenNode(columnName, expectedValue, null, autoIncrement));
    }

    /// <summary>
    /// Appends an optimistic concurrency check to the UPDATE statement with an explicit new token value.
    /// Generates <c>AND {column} = @expectedValue</c> in WHERE and <c>SET {column} = @newValue</c>.
    /// </summary>
    /// <typeparam name="TToken">The type of the concurrency token column (e.g., <see cref="Guid"/>, <see cref="DateTime"/>).</typeparam>
    /// <param name="tokenSelector">Expression identifying the concurrency token column.</param>
    /// <param name="expectedValue">The expected current value to match in the WHERE clause.</param>
    /// <param name="newValue">The new value to assign to the token column in the SET clause.</param>
    /// <returns>A new <see cref="IUpdateWhereBuilder{T}"/> instance with the concurrency check applied.</returns>
    /// <exception cref="ArgumentException"><paramref name="tokenSelector"/> is not a member expression</exception>
    public IUpdateWhereBuilder<T> WithConcurrencyToken<TToken>(
        System.Linq.Expressions.Expression<System.Func<T, TToken>> tokenSelector,
        TToken expectedValue,
        TToken newValue)
    {
        var member = tokenSelector.Body as System.Linq.Expressions.MemberExpression;
        if (member == null)
            throw new ArgumentException("Expression must be a member expression (e.g. x => x.Version)", nameof(tokenSelector));

        var columnName = SqlNamingHelper.ToSnakeCase(member.Member.Name);
        return AddNode(new EricksonLopez.SqlBuilder.Abstractions.Nodes.ConcurrencyTokenNode(columnName, expectedValue, newValue, AutoIncrement: false));
    }
}






