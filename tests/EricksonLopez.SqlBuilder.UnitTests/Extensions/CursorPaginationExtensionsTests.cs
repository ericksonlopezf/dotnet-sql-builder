// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class CursorPaginationExtensionsTests
{

    [Fact]
    public void Seek_MemberExpressionDirectly() { var query = new SelectQuery<DummyEntity>(); query.Seek(x => x.Name, "test", ascending: true, limit: 20); } [Fact] public void Seek_AddsWhereOrderAndLimit()
    {
        // Arrange
        var query = new SelectQuery<DummyEntity>();

        // Act
        var result = query.Seek(x => x.Id, 100, ascending: true, limit: 20);

        // Assert
        var where = result.Nodes.OfType<RawWhereNode>().Single();
        where.Condition.Should().Be("id > {0}");
        where.Parameters.Should().BeEquivalentTo(new object?[] { 100 });
        result.Nodes.OfType<OrderByNode>().Single().IsDescending.Should().BeFalse();
    }

    [Fact]
    public void Seek_UnaryExpression_AddsRawWhereNode()
    {
        var query = new SelectQuery<DummyEntity>();
        var result = query.Seek(x => (object)x.Id, 100, true);

        var node = result.Nodes.OfType<RawWhereNode>().Single();
        node.Condition.Should().Be("id > {0}");
        node.Parameters.Should().BeEquivalentTo(new object?[] { 100 });
    }

    [Fact]
    public void Seek_InvalidExpression_ThrowsArgumentException()
    {
        var query = new SelectQuery<DummyEntity>();
        System.Action act = () => query.Seek(x => new object(), 100, true);
        act.Should().Throw<System.ArgumentException>().WithMessage("Must be a property expression");
    }

    [Fact]
    public void Seek_Descending_UsesLessThanAndOrderByDescending()
    {
        var query = new SelectQuery<DummyEntity>();
        var result = query.Seek(x => x.Id, 100, ascending: false);

        var node = result.Nodes.OfType<RawWhereNode>().Single();
        node.Condition.Should().Be("id < {0}");
        node.Parameters.Should().BeEquivalentTo(new object?[] { 100 });

        result.Nodes.OfType<OrderByNode>().Single().IsDescending.Should().BeTrue();
    }
}





