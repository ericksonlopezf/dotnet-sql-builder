// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace EricksonLopez.SqlBuilder.Abstractions;

internal sealed class SkeletonExpressionVisitor : ExpressionVisitor
{
    private readonly StringBuilder _builder = new();

    /// <summary>
    /// Gets a structural skeleton string representation of the given expression tree.
    /// </summary>
    /// <param name="node">The expression node to analyze.</param>
    /// <returns>A string representing the structure of the expression.</returns>
    public string GetSkeleton(Expression node)
    {
        _builder.Clear();
        Visit(node);
        return _builder.ToString();
    }

    /// <inheritdoc />
    protected override Expression VisitConstant(ConstantExpression node)
    {
        _builder.Append("?");
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitMember(MemberExpression node)
    {
        _builder.Append(node.Member.Name);
        return base.VisitMember(node);
    }

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        _builder.Append('(');
        Visit(node.Left);
        _builder.Append(' ').Append(node.NodeType).Append(' ');
        Visit(node.Right);
        _builder.Append(')');
        return node;
    }

    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        _builder.Append(node.Method.Name).Append('(');
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            if (i > 0) _builder.Append(',');
            Visit(node.Arguments[i]);
        }
        _builder.Append(')');
        return node;
    }
}




