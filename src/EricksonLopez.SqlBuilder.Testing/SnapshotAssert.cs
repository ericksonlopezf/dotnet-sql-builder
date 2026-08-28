// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using VerifyXunit;

namespace EricksonLopez.SqlBuilder.Testing;

/// <summary>
/// Provides assertion helpers for verifying SQL queries against snapshot files.
/// </summary>
public static class SnapshotAssert
{
    /// <summary>
    /// Verifies the compiled SQL and parameters of an ISqlQuery against a snapshot.
    /// </summary>
    /// <param name="query">The SQL query to compile and snapshot.</param>
    /// <param name="compiler">The SQL compiler used to build the query.</param>
    /// <returns>A task representing the asynchronous snapshot verification.</returns>
    public static async Task Verify(ISqlQuery query, ISqlCompiler compiler)
    {
        var result = query.Build(compiler);
        await Verify(result);
    }

    /// <summary>
    /// Verifies a SqlResult against a snapshot.
    /// </summary>
    /// <param name="result">The compiled SQL result to snapshot.</param>
    /// <returns>A task representing the asynchronous snapshot verification.</returns>
    public static async Task Verify(SqlResult result)
    {
        var snapshot = new
        {
            Sql = result.Sql,
            Parameters = result.Parameters
        };
        await Verifier.Verify(snapshot);
    }

    /// <summary>
    /// Compares the compiled SQL against an inline or file snapshot string.
    /// </summary>
    /// <param name="query">The SQL query to compile and compare.</param>
    /// <param name="compiler">The SQL compiler used to build the query.</param>
    /// <param name="snapshotSql">The expected SQL snapshot string.</param>
    /// <param name="normalizeWhitespace">If <see langword="true"/>, collapses all whitespace before comparing SQL strings.</param>
    public static void MatchesSnapshot(ISqlQuery query, ISqlCompiler compiler, string snapshotSql, bool normalizeWhitespace = true)
    {
        var result = query.Build(compiler);
        var actual = normalizeWhitespace ? Normalize(result.Sql) : result.Sql;
        var expected = normalizeWhitespace ? Normalize(snapshotSql) : snapshotSql;

        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new System.InvalidOperationException(
                $"Snapshot SQL Mismatch.\nExpected:\n{expected}\nActual:\n{actual}");
        }
    }

    private static string Normalize(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        var words = sql.Split(new[] { ' ', '\r', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words);
    }

    /// <summary>
    /// Verifies the structural contract of an ISqlQuery against a snapshot using Verify.
    /// </summary>
    /// <param name="query">The AST query whose structural contract is to be verified.</param>
    /// <returns>A task representing the asynchronous snapshot verification.</returns>
    public static async Task VerifyContract(IAstQuery query)
    {
        var contract = query.GetContract();
        await Verifier.Verify(contract);
    }

    /// <summary>
    /// Compares the structural contract of an ISqlQuery against an expected fingerprint.
    /// </summary>
    /// <param name="query">The AST query whose fingerprint is to be compared.</param>
    /// <param name="expectedFingerprint">The expected fingerprint string.</param>
    public static void MatchesContract(IAstQuery query, string expectedFingerprint)
    {
        var contract = query.GetContract();
        if (contract.Fingerprint != expectedFingerprint)
        {
            throw new System.InvalidOperationException(
                $"Query Contract Mismatch.\nExpected Fingerprint:\n{expectedFingerprint}\nActual Fingerprint:\n{contract.Fingerprint}");
        }
    }
}





