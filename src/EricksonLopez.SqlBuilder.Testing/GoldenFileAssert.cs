// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Testing;

/// <summary>
/// Provides assertion helpers for comparing compiled SQL against Golden Files stored on disk.
/// </summary>
public static class GoldenFileAssert
{
    /// <summary>
    /// Verifies that the compiled SQL matches the content of a golden file.
    /// If the golden file does not exist and updateGoldenFiles is true, it creates or updates the golden file.
    /// </summary>
    /// <param name="query">The SQL query to compile and verify.</param>
    /// <param name="compiler">The SQL compiler used to build the query.</param>
    /// <param name="goldenFilePath">The path to the golden file containing the expected SQL.</param>
    /// <param name="updateGoldenFiles">If <see langword="true"/>, overwrites the golden file with the actual output instead of asserting.</param>
    /// <param name="normalizeWhitespace">If <see langword="true"/>, collapses all whitespace before comparing SQL strings.</param>
    public static void MatchesGoldenFile(
        ISqlQuery query,
        ISqlCompiler compiler,
        string goldenFilePath,
        bool updateGoldenFiles = false,
        bool normalizeWhitespace = true)
    {
        var result = query.Build(compiler);
        var actualSql = normalizeWhitespace ? Normalize(result.Sql) : result.Sql;

        if (updateGoldenFiles || !File.Exists(goldenFilePath))
        {
            var dir = Path.GetDirectoryName(goldenFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(goldenFilePath, actualSql);
            return;
        }

        var expectedSql = File.ReadAllText(goldenFilePath);
        if (normalizeWhitespace)
        {
            expectedSql = Normalize(expectedSql);
        }

        if (!string.Equals(expectedSql, actualSql, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Golden File Mismatch for file '{goldenFilePath}'.\n" +
                $"Expected:\n{expectedSql}\n\n" +
                $"Actual:\n{actualSql}");
        }
    }

    private static string Normalize(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        var words = sql.Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words);
    }
}


