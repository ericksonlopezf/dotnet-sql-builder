// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

/// <summary>
/// Represents the result of a bulk SQL compilation, which may be split into multiple execution batches.
/// </summary>
public sealed class BulkSqlResult
{
    /// <summary>
    /// Gets the list of generated SQL batches, where each batch represents a single SQL command.
    /// </summary>
    public IReadOnlyList<SqlResult> Batches { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkSqlResult"/> class.
    /// </summary>
    /// <param name="batches">The list of compiled SQL batches.</param>
    public BulkSqlResult(IReadOnlyList<SqlResult> batches)
    {
        Batches = batches;
    }
}
