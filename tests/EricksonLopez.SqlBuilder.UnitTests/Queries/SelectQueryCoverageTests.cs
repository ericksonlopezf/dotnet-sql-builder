// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SelectQueryCoverageTests
{

    private class DummyNode : ISqlNode
    {
        public void Accept(EricksonLopez.SqlBuilder.Abstractions.ISqlVisitor visitor) { }
    }

    [Fact]
    public void Distinct_WithCustomSelectNode_ReturnsNewSelectNode()
    {
        var query = new SelectQuery<User>();
        
        // This normally throws if we don't have extension methods to replace node,
        // but wait, we can just hack it by accessing the Nodes list.
        var nodes = new List<ISqlNode> { new DummyNode() };
        
        // Use reflection to set the Nodes property since it's immutable
        var prop = typeof(SelectQuery<User>).GetProperty("Nodes");
        var newQuery = query with { Nodes = System.Collections.Immutable.ImmutableArray.CreateRange(nodes) };
        
        // Act
        var distinctQuery = newQuery.Distinct();
        
        // Assert
        distinctQuery.Nodes.Last().Should().BeOfType<SelectNode>();
        ((SelectNode)distinctQuery.Nodes.Last()).IsDistinct.Should().BeTrue();
    }
    [Fact]
    public void Distinct_WithRawSelectNode_ReturnsNewSelectNode()
    {
        var query = new SelectQuery<User>();
        
        var rawQuery = query.RawSelect($"a, b");
        
        var distinctQuery = rawQuery.Distinct();
        
        distinctQuery.Nodes.Last().Should().BeOfType<RawSelectNode>();
        ((RawSelectNode)distinctQuery.Nodes.Last()).IsDistinct.Should().BeTrue();
    }

    [Fact]
    public void Distinct_WithNonSelectNodeAtSelectNodeIndex_ReturnsNewSelectNode()
    {
        var query = new SelectQuery<User>().Select("a");
        var nodes = query.Nodes.SetItem(0, new DummyNode());
        var badQuery = query with { Nodes = nodes };
        
        var distinctQuery = badQuery.Distinct();
        
        distinctQuery.Nodes.Last().Should().BeOfType<SelectNode>();
        ((SelectNode)distinctQuery.Nodes.Last()).IsDistinct.Should().BeTrue();
    }
}



