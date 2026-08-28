// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Represents a raw SQL query that is always fully parameterized.
/// Unlike Dapper's raw string approach, <see cref="RawQuery"/> never accepts
/// string concatenation and enforces NativeAOT-safe parameter passing.
/// </summary>
public sealed record RawQuery : ISqlQuery
{
    /// <summary>
    /// Gets the optional tag associated with this query for diagnostics or interception.
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Creates a new <see cref="RawQuery"/> with the specified diagnostic tag.
    /// </summary>
    /// <param name="tag">The diagnostic tag to associate with the query.</param>
    /// <returns>A new query instance containing the applied tag.</returns>
    public RawQuery WithTag(string tag) => this with { Tag = tag };

    /// <summary>
    /// Gets the parameterized SQL command text.
    /// </summary>
    public string RawSql { get; }

    /// <summary>
    /// Gets the parameters associated with this query.
    /// </summary>
    public object? Parameters { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RawQuery"/> class from an interpolated SQL string.
    /// </summary>
    /// <param name="sql">The formattable SQL string containing parameter placeholders and values.</param>
    /// <example>
    /// <code>
    /// int minAge = 18;
    /// string status = "Active";
    /// var q = new RawQuery($"SELECT * FROM users WHERE age &gt;= {minAge} AND status = {status}");
    /// // SQL: SELECT * FROM users WHERE age >= @p0 AND status = @p1
    /// </code>
    /// </example>
    public RawQuery(FormattableString sql)
    {
        RawSql = System.Text.RegularExpressions.Regex.Replace(sql.Format, @"\{(\d+)\}", "@p$1", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(2));
        var dict = new Dictionary<string, object?>();
        var args = sql.GetArguments();
        for (int i = 0; i < args.Length; i++)
        {
            dict[$"@p{i}"] = args[i];
        }
        Parameters = dict;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RawQuery"/> class from a SQL string and parameter collection.
    /// </summary>
    /// <param name="sql">The parameterized SQL string.</param>
    /// <param name="parameters">The parameter values container.</param>
    /// <exception cref="ArgumentException"><paramref name="sql"/> is empty or whitespace</exception>
    /// <exception cref="InvalidOperationException">Entity column count does not match value count</exception>
    /// <exception cref="NotSupportedException"><paramref name="parameters"/> uses reflection or an unsupported format</exception>
    public RawQuery(string sql, object? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL cannot be empty", nameof(sql));
        }

        RawSql = sql;
        
        if (parameters == null)
        {
            Parameters = new Dictionary<string, object?>();
        }
        else if (parameters is IReadOnlyDictionary<string, object?> readOnlyDict)
        {
            Parameters = readOnlyDict;
        }
        else if (parameters is IDictionary<string, object?> dict)
        {
            Parameters = new Dictionary<string, object?>(dict);
        }
        else if (parameters is EricksonLopez.SqlBuilder.Annotations.ISqlEntity sqlEntity)
        {
            var cols = sqlEntity.GetColumnNames();
            var vals = sqlEntity.GetValues();
            
            if (cols.Length != vals.Length)
            {
                throw new InvalidOperationException($"Entity metadata mismatch: GetColumnNames() returned {cols.Length} items, but GetValues() returned {vals.Length}. They must match.");
            }
            
            var map = new Dictionary<string, object?>(cols.Length);
            for (int i = 0; i < cols.Length; i++)
            {
                map[cols[i]] = vals[i];
            }
            Parameters = map;
        }
        else if (parameters is IEnumerable<KeyValuePair<string, object?>> kvpEnum)
        {
            var map = new Dictionary<string, object?>();
            foreach (var kvp in kvpEnum)
            {
                map[kvp.Key] = kvp.Value;
            }
            Parameters = map;
        }
        else
        {
            throw new NotSupportedException($"Passing untyped object '{parameters.GetType().Name}' via Reflection is not NativeAOT compliant. Use FormattableString ($'...'), Dictionary<string, object?>, or [SqlEntity] instead.");
        }
    }

    /// <summary>
    /// Compiles the raw query into an executable SQL string and its parameters.
    /// </summary>
    /// <param name="compiler">The SQL compiler specific to the target database provider.</param>
    /// <returns>The compiled SQL result.</returns>
    [RequiresDynamicCode("SQL expression compilation uses dynamic code generation when evaluating typed LINQ expressions. Use Sql.Raw() for NativeAOT strict paths.")]
    [RequiresUnreferencedCode("SQL expression compilation accesses member metadata that may be trimmed. Use Sql.Raw() for NativeAOT strict paths.")]
    public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);
}




