// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Testing;

/// <summary>
/// Provides extension methods for fluent testing of SQL queries.
/// </summary>
public static class QueryTestingExtensions
{
    /// <summary>
    /// Asserts that compiling the query generates the expected SQL and parameters.
    /// </summary>
    /// <param name="query">The SQL query to compile.</param>
    /// <param name="compiler">The SQL compiler used to build the query.</param>
    /// <param name="expectedSql">The expected SQL string (whitespace is normalized before comparison).</param>
    /// <param name="expectedParameters">The expected parameter values in the order they were bound.</param>
    public static void ShouldGenerate(this IAstQuery query, ISqlCompiler compiler, string expectedSql, params object?[] expectedParameters)
    {
        var result = query.Build(compiler);
        
        var normalizedActual = NormalizeSql(result.Sql);
        var normalizedExpected = NormalizeSql(expectedSql);
        
        if (normalizedActual != normalizedExpected)
        {
            throw new Exception($"SQL mismatch.\nExpected:\n{normalizedExpected}\nActual:\n{normalizedActual}");
        }

        if (expectedParameters != null && expectedParameters.Length > 0)
        {
            if (result.Parameters.Count != expectedParameters.Length)
            {
                throw new Exception($"Parameter count mismatch. Expected {expectedParameters.Length}, but got {result.Parameters.Count}.");
            }

            var actualValues = result.Parameters.Values.ToList();
            for (int i = 0; i < expectedParameters.Length; i++)
            {
                if (!object.Equals(actualValues[i], expectedParameters[i]))
                {
                    throw new Exception($"Parameter at index {i} mismatch. Expected '{expectedParameters[i]}', but got '{actualValues[i]}'.");
                }
            }
        }
    }

    private static string NormalizeSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        // Remove excess whitespace
        var words = sql.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words);
    }
    
    /// <summary>
    /// Compiles the query and verifies it against a Snapshot.
    /// </summary>
    /// <param name="query">The SQL query to compile and snapshot.</param>
    /// <param name="compiler">The SQL compiler used to build the query.</param>
    /// <param name="sourceFile">The source file path, automatically supplied by the compiler via <see cref="System.Runtime.CompilerServices.CallerFilePathAttribute"/>.</param>
    /// <returns>A task representing the asynchronous snapshot verification.</returns>
    public static Task VerifyQueryAsync(
        this IAstQuery query, 
        ISqlCompiler compiler,
        [System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
    {
        var result = query.Build(compiler);
        return VerifyXunit.Verifier.Verify(new { Sql = NormalizeSql(result.Sql), Parameters = result.Parameters }, sourceFile: sourceFile);
    }
}





