// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Testing;

/// <summary>
/// Represents the result of comparing two SQL queries.
/// </summary>
public class QueryComparerResult
{
    /// <summary>Gets a value indicating whether the queries are equal.</summary>
    public bool AreEqual { get; }
    /// <summary>Gets the list of differences between the queries.</summary>
    public IReadOnlyList<string> Differences { get; }

    /// <summary>Initializes a new instance of <see cref="QueryComparerResult"/>.</summary>
    /// <param name="areEqual">Whether the two queries were found to be equivalent.</param>
    /// <param name="differences">The list of differences found between the queries.</param>
    public QueryComparerResult(bool areEqual, IReadOnlyList<string> differences)
    {
        AreEqual = areEqual;
        Differences = differences;
    }
}
