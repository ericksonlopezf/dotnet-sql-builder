// Copyright © Erickson Lopez. MIT License.
using System.Linq.Expressions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a JOIN clause in a SQL query.
/// </summary>
/// <param name="Type">The type of join.</param>
/// <param name="TableName">The table name.</param>
/// <param name="Alias">The table alias.</param>
/// <param name="RawCondition">The raw ON condition.</param>
/// <param name="ExpressionCondition">The typed ON condition.</param>
public sealed record JoinNode(JoinType Type, string TableName, string? Alias, string? RawCondition = null, Expression? ExpressionCondition = null) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute((int)Type);
        fingerprinter.Contribute(TableName);
        fingerprinter.Contribute(Alias);
        fingerprinter.Contribute(RawCondition);
        if (ExpressionCondition != null)
        {
            var visitor = new SkeletonExpressionVisitor();
            fingerprinter.Contribute(visitor.GetSkeleton(ExpressionCondition));
        }
    }
}
