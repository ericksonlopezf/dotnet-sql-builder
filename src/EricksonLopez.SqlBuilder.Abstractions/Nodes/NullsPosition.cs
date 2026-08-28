// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Controls the placement of NULL values within an ORDER BY clause.
/// </summary>
public enum NullsPosition
{
    /// <summary>Use the database default (no explicit NULLS clause).</summary>
    None,
    /// <summary>Place NULL values before non-NULL values (NULLS FIRST).</summary>
    First,
    /// <summary>Place NULL values after non-NULL values (NULLS LAST).</summary>
    Last
}
