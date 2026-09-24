// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a LIMIT and/or OFFSET clause for query pagination.
/// </summary>
public sealed record LimitOffsetNode : ISqlNode
{
    /// <summary>
    /// Gets the maximum number of rows to return. <see langword="null"/> means no limit is applied.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets the number of rows to skip before beginning to return rows. <see langword="null"/> means no offset is applied.
    /// </summary>
    public int? Offset { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LimitOffsetNode"/> record.
    /// </summary>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <param name="offset">The number of rows to skip.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="limit"/> or <paramref name="offset"/> is negative</exception>
    public LimitOffsetNode(int? limit, int? offset)
    {
        if (limit.HasValue && limit.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit cannot be negative.");
        if (offset.HasValue && offset.Value < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");

        Limit = limit;
        Offset = offset;
    }

    /// <summary>
    /// Deconstructs the node into its limit and offset components.
    /// </summary>
    /// <param name="limit">When returning, contains the maximum number of rows to return, or <see langword="null"/> if none.</param>
    /// <param name="offset">When returning, contains the number of rows to skip, or <see langword="null"/> if none.</param>
    public void Deconstruct(out int? limit, out int? offset)
    {
        limit = Limit;
        offset = Offset;
    }

    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute(Limit.HasValue);
        fingerprinter.Contribute(Offset.HasValue);
    }
}
