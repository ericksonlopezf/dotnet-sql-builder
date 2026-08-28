// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Testing;

public static class QueryComparer
{
    /// <summary>
    /// Compares two queries by compiling them and comparing their AST nodes, SQL strings, and parameters.
    /// </summary>
    /// <param name="expected">The reference query to compare against.</param>
    /// <param name="actual">The query being tested.</param>
    /// <param name="compiler">The SQL compiler used to compile both queries.</param>
    /// <param name="normalizeWhitespace">If <see langword="true"/>, collapses all whitespace before comparing SQL strings.</param>
    /// <returns>A <see cref="QueryComparerResult"/> describing any differences found.</returns>
    public static QueryComparerResult Compare(ISqlQuery expected, ISqlQuery actual, ISqlCompiler compiler, bool normalizeWhitespace = true)
    {
        var differences = new List<string>();

        var resultExpected = expected.Build(compiler);
        var resultActual = actual.Build(compiler);

        string sqlExp = normalizeWhitespace ? Normalize(resultExpected.Sql) : resultExpected.Sql;
        string sqlAct = normalizeWhitespace ? Normalize(resultActual.Sql) : resultActual.Sql;

        if (!string.Equals(sqlExp, sqlAct, StringComparison.Ordinal))
        {
            differences.Add($"SQL mismatch:\nExpected: {sqlExp}\nActual:   {sqlAct}");
        }

        if (resultExpected.Parameters.Count != resultActual.Parameters.Count)
        {
            differences.Add($"Parameter count mismatch: Expected {resultExpected.Parameters.Count}, Actual {resultActual.Parameters.Count}");
        }
        else
        {
            foreach (var kv in resultExpected.Parameters)
            {
                if (!resultActual.Parameters.TryGetValue(kv.Key, out var actVal))
                {
                    differences.Add($"Missing parameter '{kv.Key}' in actual query.");
                }
                else if (!Equals(kv.Value, actVal))
                {
                    differences.Add($"Parameter value mismatch for '{kv.Key}': Expected '{kv.Value}', Actual '{actVal}'");
                }
            }
        }

        if (expected is IAstQuery astExp && actual is IAstQuery astAct)
        {
            var nodesExp = astExp.Nodes.ToList();
            var nodesAct = astAct.Nodes.ToList();

            if (nodesExp.Count != nodesAct.Count)
            {
                differences.Add($"AST Node count mismatch: Expected {nodesExp.Count}, Actual {nodesAct.Count}");
            }
            else
            {
                for (int i = 0; i < nodesExp.Count; i++)
                {
                    if (nodesExp[i].GetType() != nodesAct[i].GetType())
                    {
                        differences.Add($"AST Node type mismatch at index {i}: Expected {nodesExp[i].GetType().Name}, Actual {nodesAct[i].GetType().Name}");
                    }
                }
            }
        }

        return new QueryComparerResult(differences.Count == 0, differences);
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
