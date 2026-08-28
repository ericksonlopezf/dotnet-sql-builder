// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class SqlVisitorBaseTests
{
    public class TestSqlVisitor : SqlVisitorBase
    {
    }

    [Fact]
    public void All_Virtual_Methods_Should_Not_Throw()
    {
        var visitor = new TestSqlVisitor();

        // Calling all virtual methods with null to ensure they just do nothing as per base implementation.
        // We can pass null because the base methods don't use the argument.
        
        Action act = () =>
        {
            visitor.Visit((CteNode)null!);
            visitor.Visit((DeleteNode)null!);
            visitor.Visit((FromNode)null!);
            visitor.Visit((SubqueryFromNode)null!);
            visitor.Visit((UnnestNode)null!);
            visitor.Visit((GroupByNode)null!);
            visitor.Visit((ExpressionHavingNode)null!);
            visitor.Visit((RawHavingNode)null!);
            visitor.Visit((InsertNode)null!);
            visitor.Visit((ValuesNode)null!);
            visitor.Visit((ReturningNode)null!);
            visitor.Visit((OnConflictNode)null!);
            visitor.Visit((DefaultValuesNode)null!);
            visitor.Visit((JoinNode)null!);
            visitor.Visit((RawJoinNode)null!);
            visitor.Visit((SubqueryJoinNode)null!);
            visitor.Visit((LimitOffsetNode)null!);
            visitor.Visit((ScalarSubquerySelectNode)null!);
            visitor.Visit((OrderByNode)null!);
            visitor.Visit((ThenByNode)null!);
            visitor.Visit((RawOrderByNode)null!);
            visitor.Visit((SelectNode)null!);
            visitor.Visit((ExpressionSelectNode)null!);
            visitor.Visit((QueryAliasNode)null!);
            visitor.Visit((DistinctOnNode)null!);
            visitor.Visit((RawSelectNode)null!);
            visitor.Visit((SetOperationNode)null!);
            visitor.Visit((UpdateNode)null!);
            visitor.Visit((SetNode)null!);
            visitor.Visit((ExpressionWhereNode)null!);
            visitor.Visit((RawWhereNode)null!);
            visitor.Visit((WindowNode)null!);
            visitor.Visit((WindowPageNode)null!);
            visitor.Visit((ExistsWhereNode)null!);
            visitor.Visit((ConcurrencyTokenNode)null!);
            visitor.Visit((WindowFunctionNode)null!);
            visitor.Visit((CaseNode)null!);
            visitor.Visit((InsertSelectNode)null!);
            visitor.Visit((CompositeCursorNode)null!);
            
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void VisitExtension_Should_Throw_NotSupportedException()
    {
        var visitor = new TestSqlVisitor();
        
        Action act = () => visitor.VisitExtension(null!);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void VisitUnknown_Should_Throw_NotSupportedException()
    {
        var visitor = new TestSqlVisitor();
        var mockNode = Substitute.For<ISqlNode>();

        Action act = () => visitor.VisitUnknown(mockNode);

        act.Should().Throw<NotSupportedException>().WithMessage($"Node type {mockNode.GetType().Name} is not supported by {typeof(TestSqlVisitor).Name}.");
    }

    [Fact]
    public void VisitUnknown_WithNull_Should_Throw_NotSupportedException()
    {
        var visitor = new TestSqlVisitor();

        Action act = () => visitor.VisitUnknown(null!);

        act.Should().Throw<NotSupportedException>().WithMessage($"Node type null is not supported by {typeof(TestSqlVisitor).Name}.");
    }
}



