// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Specifies the type of aggregation grouping in a GROUP BY clause.
/// </summary>
public enum GroupByType
{
    /// <summary>Standard GROUP BY column list.</summary>
    Standard = 0,

    /// <summary>Hierarchical ROLLUP aggregation (GROUP BY ROLLUP(c1, c2)).</summary>
    Rollup = 1,

    /// <summary>Multidimensional CUBE aggregation (GROUP BY CUBE(c1, c2)).</summary>
    Cube = 2,

    /// <summary>Explicit GROUPING SETS aggregation (GROUP BY GROUPING SETS ((c1), (c2))).</summary>
    GroupingSets = 3
}
