// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Represents an INSERT query syntax tree builder for the specified entity type.
/// </summary>
/// <remarks>
/// This type is immutable. All modification methods return a new instance
/// preserving the previous state.
/// </remarks>
/// <typeparam name="T">The type of the entity to insert.</typeparam>
public sealed record InsertQuery<T> : IAstQuery where T : class, new()
{
    /// <summary>
    /// Gets the optional tag associated with this query for diagnostics or interception.
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Creates a new <see cref="InsertQuery{T}"/> with the specified diagnostic tag.
    /// </summary>
    /// <param name="tag">The diagnostic tag to associate with the query.</param>
    /// <returns>A new query instance containing the applied tag.</returns>
    public InsertQuery<T> WithTag(string tag) => this with { Tag = tag };

    /// <summary>
    /// Gets the collection of nodes that compose the abstract syntax tree of this query.
    /// </summary>
    public System.Collections.Immutable.ImmutableArray<ISqlNode> Nodes { get; init; } = System.Collections.Immutable.ImmutableArray<ISqlNode>.Empty;
    
    /// <inheritdoc/>
    IReadOnlyList<ISqlNode> IAstQuery.Nodes => Nodes;

    /// <summary>
    /// Appends the specified node to the query AST.
    /// </summary>
    /// <param name="node">The node to append to the query.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance containing the appended node.</returns>
    public InsertQuery<T> AddNode(ISqlNode node) => this with { Nodes = Nodes.Add(node) };

    /// <summary>
    /// Compiles the abstract syntax tree into an executable SQL string and its parameters.
    /// </summary>
    /// <param name="compiler">The SQL compiler specific to the target database provider.</param>
    /// <returns>The compiled SQL result.</returns>
    [RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
    [RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
    public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);

    /// <summary>
    /// Specifies the target table for the insertion.
    /// </summary>
    /// <param name="tableName">The name of the target table.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the target table applied.</returns>
    public InsertQuery<T> Into(string tableName) => AddNode(new InsertNode(tableName, Array.Empty<string>()));
    
    /// <summary>
    /// Appends the values to insert based on the properties of the provided entity.
    /// </summary>
    /// <param name="entity">The entity instance containing the data to insert.</param>
    /// <param name="ignoreNulls">If <see langword="true"/>, properties with null values will be excluded from the insertion.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the values applied.</returns>
    /// <exception cref="InvalidOperationException"><typeparamref name="T"/> does not implement <see cref="EricksonLopez.SqlBuilder.Annotations.ISqlEntity"/> or metadata column count does not match value count</exception>
    public InsertQuery<T> Values(T entity, bool ignoreNulls = true) 
    {
        var sqlEntity = (EricksonLopez.SqlBuilder.Annotations.ISqlEntity)entity!;
        var columns = sqlEntity.GetColumnNames();
        var values = sqlEntity.GetValues();
        
        if (columns.Length != values.Length)
        {
            throw new InvalidOperationException($"Entity {typeof(T).Name} metadata mismatch: GetColumnNames() returned {columns.Length} items, but GetValues() returned {values.Length}. They must match.");
        }
        
        var setCols = new List<string>();
        var setVals = new List<object?>();
        
        for (int i = 0; i < values.Length; i++)
        {
            if (!ignoreNulls || values[i] != null)
            {
                setCols.Add(columns[i]);
                setVals.Add(values[i]);
            }
        }
        
        var q = this;
        if (!Enumerable.Any(Nodes.OfType<InsertNode>()))
        {
            q = q.AddNode(new InsertNode(sqlEntity.GetTableName(), setCols.ToArray()));
        }
        return q.AddNode(new ValuesNode(new List<IReadOnlyList<object?>> { setVals }));
    }
    
    /// <summary>
    /// Appends the specified raw values to the insertion.
    /// </summary>
    /// <param name="values">The array of values to insert.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the values applied.</returns>
    public InsertQuery<T> Values(params object?[] values)
    {
        var q = this;
        if (!Enumerable.Any(Nodes.OfType<InsertNode>()))
        {
            q = q.AddNode(new InsertNode(SqlEntityCache<T>.TableName, Array.Empty<string>()));
        }
        return q.AddNode(new ValuesNode(new List<IReadOnlyList<object?>> { values }));
    }
    
    /// <summary>
    /// Appends multiple sets of values for bulk insertion based on the provided collection of entities.
    /// </summary>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <param name="ignoreNulls">If <see langword="true"/>, properties with null values in the first entity will determine the columns excluded for all entities.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance configured for bulk insertion.</returns>
    /// <exception cref="InvalidOperationException">An entity in the collection does not implement <see cref="EricksonLopez.SqlBuilder.Annotations.ISqlEntity"/> or metadata column count does not match value count</exception>
    public InsertQuery<T> Bulk(IEnumerable<T> entities, bool ignoreNulls = false) 
    {
        var q = this;
        var valuesSets = new List<IReadOnlyList<object?>>();
        bool insertNodeAdded = Enumerable.Any(q.Nodes.OfType<InsertNode>());
        
        bool[]? activeIndices = null;
        
        foreach (var entity in entities)
        {
            var sqlEntity = (EricksonLopez.SqlBuilder.Annotations.ISqlEntity)entity!;
            var columns = sqlEntity.GetColumnNames();
            var values = sqlEntity.GetValues();
            
            if (columns.Length != values.Length)
            {
                throw new InvalidOperationException($"Entity {typeof(T).Name} metadata mismatch: GetColumnNames() returned {columns.Length} items, but GetValues() returned {values.Length}. They must match.");
            }
            
            if (activeIndices == null)
            {
                var setCols = new List<string>();
                activeIndices = new bool[columns.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    if (!ignoreNulls || values[i] != null)
                    {
                        setCols.Add(columns[i]);
                        activeIndices[i] = true;
                    }
                }
                
                if (!insertNodeAdded)
                {
                    q = q.AddNode(new InsertNode(sqlEntity.GetTableName(), setCols.ToArray()));
                }
            }
            
            var setVals = new List<object?>();
            for (int i = 0; i < columns.Length; i++)
            {
                if (activeIndices[i])
                {
                    setVals.Add(values[i]);
                }
            }
            valuesSets.Add(setVals);
        }
        
        if (valuesSets.Count > 0)
        {
            q = q.AddNode(new ValuesNode(valuesSets));
        }
        return q;
    }
    
    /// <summary>
    /// Configures the insertion to use default values for all columns.
    /// </summary>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance configured to use default values.</returns>
    public InsertQuery<T> DefaultValues() => AddNode(new DefaultValuesNode());
    
    /// <summary>
    /// Specifies the columns to return from the inserted row.
    /// </summary>
    /// <param name="columns">The names of the columns to return.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the returning clause applied.</returns>
    public InsertQuery<T> Returning(params string[] columns) => AddNode(new ReturningNode(columns));
    
    /// <summary>
    /// Specifies the columns to return from the inserted row using a predicate expression.
    /// </summary>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="selector">The expression defining the columns to return.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the returning clause applied.</returns>
    public InsertQuery<T> Returning<TResult>(Expression<Func<T, TResult>> selector)
    {
        var cols = new List<string>();
        var newExpr = selector.Body as NewExpression;
        var memExpr = selector.Body as MemberExpression;
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
    /// Specifies the columns that define a conflict target for UPSERT operations.
    /// </summary>
    /// <param name="columns">The names of the columns defining the conflict target.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the conflict target applied.</returns>
    public InsertQuery<T> OnConflict(params string[] columns) => AddNode(new OnConflictNode(columns));
    
    /// <summary>
    /// Specifies the columns that define a conflict target for UPSERT operations using a predicate expression.
    /// </summary>
    /// <param name="keySelector">The expression specifying the conflict target columns.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the conflict target applied.</returns>
    public InsertQuery<T> OnConflict(Expression<Func<T, object>> keySelector)
    {
        var cols = new List<string>();
        var newExpr = keySelector.Body as NewExpression;
        var memExpr = keySelector.Body as MemberExpression;
        var unaryExpr = keySelector.Body as UnaryExpression;
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
        else if (unaryExpr != null)
        {
            var mem = unaryExpr.Operand as MemberExpression;
            if (mem != null)
            {
                cols.Add(SqlNamingHelper.ToSnakeCase(mem.Member.Name));
            }
        }
        return AddNode(new OnConflictNode(cols.ToArray()));
    }
    
    /// <summary>
    /// Configures the conflict resolution to ignore the insertion if a conflict occurs.
    /// </summary>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the ignore action applied.</returns>
    public InsertQuery<T> DoNothing()
    {
        var lastConflict = Nodes.OfType<OnConflictNode>().LastOrDefault();
        if (lastConflict != null)
        {
            return this with { Nodes = Nodes.Remove(lastConflict).Add(lastConflict with { UpdateAction = "DO NOTHING" }) };
        }
        return this;
    }
    
    /// <summary>
    /// Configures the conflict resolution to update specific columns if a conflict occurs.
    /// </summary>
    /// <param name="updateSelector">The expression specifying the columns to update.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the update action applied.</returns>
    public InsertQuery<T> DoUpdate(Expression<Func<T, object>> updateSelector)
    {
        var lastConflict = Nodes.OfType<OnConflictNode>().LastOrDefault();
        if (lastConflict != null)
        {
            return this with { Nodes = Nodes.Remove(lastConflict).Add(lastConflict with { UpdateAction = "DO UPDATE SET", UpdateExpression = updateSelector }) };
        }
        return this;
    }
    
    /// <summary>
    /// Configures the conflict resolution to perform a raw SQL update if a conflict occurs.
    /// </summary>
    /// <param name="sql">The formatted string containing the raw update command and its parameters.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance with the raw update action applied.</returns>
    public InsertQuery<T> DoUpdate(FormattableString sql)
    {
        var lastConflict = Nodes.OfType<OnConflictNode>().LastOrDefault();
        if (lastConflict != null)
        {
            return this with { Nodes = Nodes.Remove(lastConflict).Add(lastConflict with { UpdateAction = "DO UPDATE SET " + sql.Format, Parameters = sql.GetArguments() }) };
        }
        return this;
    }

    /// <summary>
    /// Configures this INSERT query to use the results of a SELECT query as the source data.
    /// Generates: <c>INSERT INTO table [(columns)] SELECT ...</c>
    /// </summary>
    /// <param name="selectQuery">The SELECT query whose results will be inserted.</param>
    /// <param name="columns">
    /// Optional explicit column list. When provided, generates <c>INSERT INTO table (col1, col2) SELECT ...</c>.
    /// When omitted, the column list is inferred from the SELECT result set.
    /// </param>
    /// <returns>A new <see cref="InsertQuery{T}"/> with the INSERT INTO ... SELECT node applied.</returns>
    /// <example>
    /// <code>
    /// var archiveQuery = Sql.From&lt;Order&gt;()
    ///     .Where(o => o.Status == "completed");
    ///
    /// var insertFromSelect = Sql.Insert&lt;ArchiveOrder&gt;(null!)
    ///     .Into("archive_orders")
    ///     .FromSelect(archiveQuery, "id", "customer_id", "total");
    /// </code>
    /// </example>
    public InsertQuery<T> FromSelect(ISqlQuery selectQuery, params string[] columns)
    {
        var tableName = Nodes.OfType<InsertNode>().LastOrDefault()?.TableName
            ?? SqlEntityCache<T>.TableName;
        return AddNode(new InsertSelectNode(tableName, columns.Length > 0 ? columns : null, selectQuery));
    }
}






