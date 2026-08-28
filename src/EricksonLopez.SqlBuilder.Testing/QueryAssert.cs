// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;
using VerifyXunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing;

/// <summary>
/// Provides assertion helpers for validating SQL queries and parameters in unit tests.
/// </summary>
public static class QueryAssert
{
    /// <summary>
    /// Asserts that the parameters match the expected keys and values.
    /// </summary>
    /// <param name="result">The compiled SQL result whose parameters to inspect.</param>
    /// <param name="expectedParams">The expected key-value pairs that the result parameters must contain.</param>
    public static void ParametersMatch(SqlResult result, params (string Key, object Value)[] expectedParams)
    {
        Assert.Equal(expectedParams.Length, result.Parameters.Count);
        foreach (var (key, value) in expectedParams)
        {
            Assert.True(result.Parameters.ContainsKey(key), $"Missing parameter: {key}");
            Assert.Equal(value, result.Parameters[key]);
        }
    }

    /// <summary>
    /// Verifies the compiled SQL and parameters using Snapshot verification (Verify).
    /// </summary>
    /// <param name="result">The compiled SQL result to verify.</param>
    /// <param name="target">Reserved for future use; pass <see langword="null"/>.</param>
    /// <returns>A task representing the asynchronous snapshot verification.</returns>
    public static Task VerifySql(SqlResult result, object? target = null)
    {
        return SnapshotAssert.Verify(result);
    }
    
    /// <summary>
    /// Compiles and verifies the SQL query using Snapshot verification.
    /// </summary>
    /// <param name="query">The SQL query to compile and verify.</param>
    /// <param name="compiler">The SQL compiler to use when building the query.</param>
    /// <returns>A task representing the asynchronous snapshot verification.</returns>
    public static Task VerifySql(ISqlQuery query, ISqlCompiler compiler)
    {
        return SnapshotAssert.Verify(query, compiler);
    }

    /// <summary>
    /// Asserts that two SQL queries generate equivalent SQL and parameters.
    /// </summary>
    /// <param name="expected">The reference query to compare against.</param>
    /// <param name="actual">The query being tested.</param>
    /// <param name="compiler">The SQL compiler used to compile both queries.</param>
    /// <param name="normalizeWhitespace">If <see langword="true"/>, collapses all whitespace before comparing SQL strings.</param>
    public static void QueriesMatch(ISqlQuery expected, ISqlQuery actual, ISqlCompiler compiler, bool normalizeWhitespace = true)
    {
        var comparison = QueryComparer.Compare(expected, actual, compiler, normalizeWhitespace);
        if (!comparison.AreEqual)
        {
            throw new Xunit.Sdk.XunitException("Queries do not match:\n" + string.Join("\n", comparison.Differences));
        }
    }

    /// <summary>
    /// Asserts that the compiled query SQL matches the expected SQL.
    /// </summary>
    /// <param name="query">The SQL query to compile and verify.</param>
    /// <param name="compiler">The SQL compiler used to build the query.</param>
    /// <param name="expectedSql">The expected SQL string (whitespace is normalized before comparison).</param>
    public static void SqlMatches(ISqlQuery query, ISqlCompiler compiler, string expectedSql)
    {
        var result = query.Build(compiler);
        var normalizedActual = Normalize(result.Sql);
        var normalizedExpected = Normalize(expectedSql);
        Assert.Equal(normalizedExpected, normalizedActual);
    }

    /// <summary>
    /// Compiles the query using the PostgreSQL compiler and asserts the SQL matches.
    /// </summary>
    /// <param name="query">The SQL query to compile and verify.</param>
    /// <param name="expectedSql">The expected SQL string.</param>
    public static void SqlMatchesPostgreSql(ISqlQuery query, string expectedSql)
        => SqlMatches(query, new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler(), expectedSql);

    /// <summary>
    /// Compiles the query using the SQL Server compiler and asserts the SQL matches.
    /// </summary>
    /// <param name="query">The SQL query to compile and verify.</param>
    /// <param name="expectedSql">The expected SQL string.</param>
    public static void SqlMatchesSqlServer(ISqlQuery query, string expectedSql)
        => SqlMatches(query, new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler(), expectedSql);

    /// <summary>
    /// Compiles the query using the SQLite compiler and asserts the SQL matches.
    /// </summary>
    /// <param name="query">The SQL query to compile and verify.</param>
    /// <param name="expectedSql">The expected SQL string.</param>
    public static void SqlMatchesSqlite(ISqlQuery query, string expectedSql)
        => SqlMatches(query, new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler(), expectedSql);

    /// <summary>
    /// Compiles the query using the MySQL compiler and asserts the SQL matches.
    /// </summary>
    /// <param name="query">The SQL query to compile and verify.</param>
    /// <param name="expectedSql">The expected SQL string.</param>
    public static void SqlMatchesMySql(ISqlQuery query, string expectedSql)
        => SqlMatches(query, new EricksonLopez.SqlBuilder.MySql.MySqlCompiler(), expectedSql);

    /// <summary>
    /// Compiles the query using the Oracle compiler and asserts the SQL matches.
    /// </summary>
    /// <param name="query">The SQL query to compile and verify.</param>
    /// <param name="expectedSql">The expected SQL string.</param>
    public static void SqlMatchesOracle(ISqlQuery query, string expectedSql)
        => SqlMatches(query, new EricksonLopez.SqlBuilder.Oracle.OracleCompiler(), expectedSql);

    private static string Normalize(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        var words = sql.Split(new[] { ' ', '\r', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words);
    }
}





