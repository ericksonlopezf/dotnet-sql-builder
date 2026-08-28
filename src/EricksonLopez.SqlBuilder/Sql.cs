// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EricksonLopez.SqlBuilder;

using System.ComponentModel;

/// <summary>
/// Provides entry points and static factory methods for constructing type-safe SQL queries.
/// </summary>
public static class Sql
{
    /// <summary>
    /// Creates a new SELECT query for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The type of the entity to query.</typeparam>
    /// <returns>A new <see cref="SelectQuery{T}"/> instance.</returns>
    public static SelectQuery<T> From<T>() where T : class, new() => new SelectQuery<T>().AddNode(new FromNode(SqlEntityCache<T>.TableName));

    /// <summary>
    /// Creates a new INSERT query for the specified entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity to insert.</typeparam>
    /// <param name="entity">The entity instance containing the data to insert.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance configured with the entity values.</returns>
    public static InsertQuery<T> Insert<T>(T entity) where T : class, new() => new InsertQuery<T>().Values(entity);

    /// <summary>
    /// Creates a new bulk INSERT query for the specified entities.
    /// </summary>
    /// <typeparam name="T">The type of the entities to insert.</typeparam>
    /// <param name="entities">The collection of entities to insert.</param>
    /// <returns>A new <see cref="InsertQuery{T}"/> instance configured for bulk insertion.</returns>
    public static InsertQuery<T> BulkInsert<T>(IEnumerable<T> entities) where T : class, new() => new InsertQuery<T>().Bulk(entities);

    /// <summary>
    /// Creates a highly optimized bulk operation builder for a collection of entities.
    /// </summary>
    /// <typeparam name="T">The type of the entities, which must implement <see cref="EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="entities">The entities to process in bulk.</param>
    /// <returns>A new <see cref="EricksonLopez.SqlBuilder.Builders.Bulk.BulkBuilder{T}"/> instance.</returns>
    public static EricksonLopez.SqlBuilder.Builders.Bulk.BulkBuilder<T> Bulk<T>(IEnumerable<T> entities) 
        where T : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<T> 
        => new EricksonLopez.SqlBuilder.Builders.Bulk.BulkBuilder<T>(entities);

    /// <summary>
    /// Creates a new UPDATE query for the specified entity type.
    /// </summary>
    /// <remarks>
    /// Initiates an explicit update where columns must be set manually.
    /// </remarks>
    /// <typeparam name="T">The type of the entity to update.</typeparam>
    /// <returns>A new <see cref="EricksonLopez.SqlBuilder.Abstractions.IUpdateSetBuilder{T}"/> instance.</returns>
    public static EricksonLopez.SqlBuilder.Abstractions.IUpdateSetBuilder<T> Update<T>() where T : class, new() => new UpdateQuery<T>().Update(SqlEntityCache<T>.TableName);

    /// <summary>
    /// Creates a new UPDATE query for the specified entity.
    /// </summary>
    /// <remarks>
    /// Initiates a full update using the properties of the provided entity.
    /// </remarks>
    /// <typeparam name="T">The type of the entity to update.</typeparam>
    /// <param name="entity">The entity instance containing the updated data.</param>
    /// <returns>A new <see cref="EricksonLopez.SqlBuilder.Abstractions.IUpdateSetBuilder{T}"/> instance.</returns>
    public static EricksonLopez.SqlBuilder.Abstractions.IUpdateSetBuilder<T> Update<T>(T entity) where T : class, new() => new UpdateQuery<T>().Update(SqlEntityCache<T>.TableName).Set(entity);

    /// <summary>
    /// Creates a new DELETE query for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The type of the entity to delete.</typeparam>
    /// <returns>A new <see cref="EricksonLopez.SqlBuilder.Abstractions.IDeleteFromBuilder{T}"/> instance.</returns>
    public static EricksonLopez.SqlBuilder.Abstractions.IDeleteFromBuilder<T> Delete<T>() where T : class, new() => new DeleteQuery<T>().Delete(SqlEntityCache<T>.TableName);

    /// <summary>
    /// Creates an INSERT INTO ... SELECT query, inserting the results of a SELECT into the target table.
    /// </summary>
    /// <typeparam name="T">The type of the target entity.</typeparam>
    /// <param name="selectQuery">The SELECT query whose results are to be inserted.</param>
    /// <param name="columns">Optional explicit column names for the INSERT column list.</param>
    /// <returns>An <see cref="InsertQuery{T}"/> configured with an INSERT INTO ... SELECT node.</returns>
    /// <example>
    /// <code>
    /// var archiveQuery = Sql.From&lt;Order&gt;().Where(o => o.Status == "completed");
    /// var query = Sql.InsertFrom&lt;ArchiveOrder&gt;(archiveQuery, "id", "customer_id", "total");
    /// </code>
    /// </example>
    public static InsertQuery<T> InsertFrom<T>(ISqlQuery selectQuery, params string[] columns) where T : class, new()
    {
        var tableName = SqlEntityCache<T>.TableName;
        return new InsertQuery<T>().AddNode(new InsertSelectNode(tableName, columns.Length > 0 ? columns : null, selectQuery));
    }


    /// <summary>
    /// Creates a raw SQL query from a <see cref="System.FormattableString"/>.
    /// </summary>
    /// <param name="sql">The formatted string containing the SQL command and its parameters.</param>
    /// <returns>A new <see cref="RawQuery"/> instance.</returns>
    public static RawQuery Raw(System.FormattableString sql) => new RawQuery(sql);

    /// <summary>
    /// Creates a raw SQL query from a string and optional parameters.
    /// </summary>
    /// <param name="sql">The SQL string.</param>
    /// <param name="parameters">Optional parameters dictionary or entity.</param>
    /// <returns>A new <see cref="RawQuery"/> instance.</returns>
    public static RawQuery Raw(string sql, object? parameters = null) => new RawQuery(sql, parameters);



    
    internal static System.Collections.Concurrent.ConcurrentDictionary<System.Type, EricksonLopez.SqlBuilder.Abstractions.ITypeHandler> TypeHandlers { get; } = new();

    /// <summary>
    /// Registers a custom type handler for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to register the handler for.</typeparam>
    /// <param name="handler">The handler implementation to use for the type.</param>
    public static void RegisterTypeHandler<T>(EricksonLopez.SqlBuilder.Abstractions.ITypeHandler handler)
    {
        TypeHandlers[typeof(T)] = handler;
    }

    /// <summary>
    /// Evaluates if a string matches a pattern (case-insensitive) for SQL translation.
    /// </summary>
    /// <param name="value">The source string to evaluate.</param>
    /// <param name="pattern">The pattern string to compare against.</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool ILike(this string value, string pattern)
    {
        throw new System.InvalidOperationException("Sql.ILike is for SQL expression building only.");
    }

    /// <summary>
    /// Evaluates if a value is equal to any element in the specified collection.
    /// </summary>
    /// <typeparam name="TItem">The type of items to evaluate.</typeparam>
    /// <param name="value">The value to test.</param>
    /// <param name="collection">The candidate collection.</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool Any<TItem>(this TItem value, IEnumerable<TItem> collection)
    {
        throw new System.InvalidOperationException("Sql.Any is for SQL expression building only.");
    }

    /// <summary>
    /// Evaluates if a value is equal to all elements in the specified collection.
    /// </summary>
    /// <typeparam name="TItem">The type of items to evaluate.</typeparam>
    /// <param name="value">The value to test.</param>
    /// <param name="collection">The candidate collection.</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool All<TItem>(this TItem value, IEnumerable<TItem> collection)
    {
        throw new System.InvalidOperationException("Sql.All is for SQL expression building only.");
    }

    /// <summary>
    /// Evaluates if a value is inclusively between two bounds. 
    /// Translates to SQL: <c>column BETWEEN @from AND @to</c>.
    /// </summary>
    /// <typeparam name="TItem">The type of the value and bounds (must be comparable).</typeparam>
    /// <param name="value">The column value (the left-hand side of BETWEEN).</param>
    /// <param name="from">The lower bound (inclusive).</param>
    /// <param name="to">The upper bound (inclusive).</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    /// <example>
    /// <code>
    /// // WHERE age BETWEEN 18 AND 65
    /// Sql.From&lt;User&gt;().Where(u =&gt; u.Age.Between(18, 65))
    /// </code>
    /// </example>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool Between<TItem>(this TItem value, TItem from, TItem to)
        where TItem : System.IComparable<TItem>
    {
        throw new System.InvalidOperationException("Sql.Between is for SQL expression building only.");
    }

    /// <summary>
    /// Returns the first non-null value from the provided column and the fallback.
    /// Translates to SQL: <c>COALESCE(column, @fallback)</c>.
    /// </summary>
    /// <typeparam name="TItem">The type of the value and fallback.</typeparam>
    /// <param name="value">The column value (the left-hand side of COALESCE).</param>
    /// <param name="fallback">The fallback value returned when <paramref name="value"/> is null.</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    /// <example>
    /// <code>
    /// // WHERE COALESCE(name, 'Unknown') = 'Alice'
    /// Sql.From&lt;User&gt;().Where(u => u.Name.Coalesce("Unknown") == "Alice")
    /// </code>
    /// </example>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TItem Coalesce<TItem>(this TItem? value, TItem fallback)
    {
        throw new System.InvalidOperationException("Sql.Coalesce is for SQL expression building only.");
    }

    /// <summary>
    /// Returns the first non-null value among the provided values.
    /// Translates to SQL: <c>COALESCE(val1, val2, fallback)</c>.
    /// </summary>
    /// <typeparam name="TItem">The type of the values.</typeparam>
    /// <param name="val1">The first candidate value.</param>
    /// <param name="val2">The second candidate value.</param>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TItem Coalesce<TItem>(TItem? val1, TItem? val2, TItem fallback)
    {
        throw new System.InvalidOperationException("Sql.Coalesce is for SQL expression building only.");
    }

    /// <summary>
    /// Evaluates if two values are distinct, treating NULL as a comparable value (null-safe inequality).
    /// Translates to SQL: <c>left IS DISTINCT FROM right</c>.
    /// </summary>
    /// <typeparam name="TItem">The type of the values to compare.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool IsDistinctFrom<TItem>(TItem left, TItem right)
    {
        throw new System.InvalidOperationException("Sql.IsDistinctFrom is for SQL expression building only.");
    }

    /// <summary>
    /// Evaluates if two values are not distinct, treating NULL as a comparable value (null-safe equality).
    /// Translates to SQL: <c>left IS NOT DISTINCT FROM right</c>.
    /// </summary>
    /// <typeparam name="TItem">The type of the values to compare.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static bool IsNotDistinctFrom<TItem>(TItem left, TItem right)
    {
        throw new System.InvalidOperationException("Sql.IsNotDistinctFrom is for SQL expression building only.");
    }

    /// <summary>
    /// Returns NULL if both arguments are equal, otherwise returns the first argument.
    /// Translates to SQL: <c>NULLIF(value, target)</c>.
    /// </summary>
    /// <typeparam name="TItem">The type of the values to compare.</typeparam>
    /// <param name="value">The primary value.</param>
    /// <param name="target">The value to compare against.</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TItem? NullIf<TItem>(TItem? value, TItem? target)
    {
        throw new System.InvalidOperationException("Sql.NullIf is for SQL expression building only.");
    }

    /// <summary>
    /// References an outer query column inside a correlated subquery or LATERAL join.
    /// Translates to SQL referencing the outer table's column identifier.
    /// </summary>
    /// <typeparam name="TEntity">The outer entity type.</typeparam>
    /// <typeparam name="TProp">The property type.</typeparam>
    /// <param name="column">The expression identifying the outer column.</param>
    /// <returns>This exists only for SQL expression building and always throws at runtime.</returns>
    /// <exception cref="InvalidOperationException">The method is invoked directly at runtime rather than inside a query expression</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static TProp Outer<TEntity, TProp>(System.Linq.Expressions.Expression<System.Func<TEntity, TProp>> column)
    {
        throw new System.InvalidOperationException("Sql.Outer is for SQL expression building only.");
    }
}






