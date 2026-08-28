// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder;

/// <summary>
/// Provides extension methods for generating query contracts.
/// </summary>
public static class QueryContractExtensions
{
    /// <summary>
    /// Generates a contract representing the verifiable structural shape of the specified query.
    /// </summary>
    /// <param name="query">The query to analyze.</param>
    /// <returns>A new <see cref="QueryContract"/> containing the fingerprint and referenced database objects.</returns>
    public static QueryContract GetContract(this IAstQuery query)
    {
        var fingerprint = query.GetFingerprint();
        var tables = new List<string>();
        var columns = new List<string>();

        foreach (var node in query.Nodes)
        {
            if (node is FromNode fromNode)
            {
                tables.Add(fromNode.TableName);
            }
            else if (node is JoinNode joinNode)
            {
                tables.Add(joinNode.TableName);
            }
            else if (node is SelectNode selectNode)
            {
                foreach (var col in selectNode.Columns)
                {
                    columns.Add(col);
                }
            }
        }

        return new QueryContract(
            fingerprint,
            tables.ToArray(),
            columns.ToArray()
        );
    }
}
