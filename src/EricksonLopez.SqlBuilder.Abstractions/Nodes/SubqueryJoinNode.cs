// Copyright © Erickson Lopez. MIT License.
using System.Linq.Expressions;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a subquery JOIN clause.
/// </summary>
/// <param name="Type">The join type.</param>
/// <param name="Subquery">The subquery AST.</param>
/// <param name="Alias">The table alias.</param>
/// <param name="OnCondition">The raw ON condition.</param>
/// <param name="IsLateral">Whether this is a LATERAL join.</param>
/// <param name="ExpressionCondition">Optional typed ON expression condition.</param>
public sealed record SubqueryJoinNode(
    JoinType Type,
    IAstQuery Subquery,
    string Alias,
    string? OnCondition = null,
    bool IsLateral = false,
    Expression? ExpressionCondition = null
) : ISqlNode
{
    /// <inheritdoc />
    public void Accept(ISqlVisitor visitor) => visitor.Visit(this);

    /// <inheritdoc />
    public void ContributeToFingerprint(IQueryFingerprinter fingerprinter)
    {
        fingerprinter.Contribute(GetType().Name);
        fingerprinter.Contribute((int)Type);
        Subquery?.ContributeToFingerprint(fingerprinter);
        fingerprinter.Contribute(Alias);
        fingerprinter.Contribute(OnCondition);
        fingerprinter.Contribute(IsLateral);
    }
}
