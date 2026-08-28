// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlNodePartitionCoverageTests
{
    private record DummyNode : SqlExtensionNode
    {
        public override void Accept(ISqlVisitor visitor) => visitor.VisitExtension(this);
    }

    [Fact]
    public void VisitExtension_AddsToExtensionNodes()
    {
        var node = new DummyNode();
        var partition = new SqlNodePartition(new[] { node });
        partition.ExtensionNodes.Should().Contain(node);
    }

    [Fact]
    public void EmptyProperties_ReturnEmptyArray_NotNull()
    {
        var partition = new SqlNodePartition(Array.Empty<ISqlNode>());
        
        partition.CteNodes.Should().NotBeNull();
        partition.SelectNodes.Should().NotBeNull();
        partition.JoinNodes.Should().NotBeNull();
        partition.WhereNodes.Should().NotBeNull();
        partition.GroupByNodes.Should().NotBeNull();
        partition.HavingNodes.Should().NotBeNull();
        partition.WindowNodes.Should().NotBeNull();
        partition.SetOpNodes.Should().NotBeNull();
        partition.OrderNodes.Should().NotBeNull();
        partition.UpdateNodes.Should().NotBeNull();
        partition.SetNodes.Should().NotBeNull();
        partition.ExtensionNodes.Should().NotBeNull();
        partition.UnnestNodes.Should().NotBeNull();
    }

    [Fact]
    public void VisitLimitOffsetNode_MergesValues()
    {
        var node1 = new LimitOffsetNode(10, null);
        var node2 = new LimitOffsetNode(null, 5);
        var partition = new SqlNodePartition(new[] { node1, node2 });
        
        partition.LimitNode.Should().NotBeNull();
        partition.LimitNode.Limit.Should().Be(10);
        partition.LimitNode.Offset.Should().Be(5);
    }

    [Fact]
    public void VisitUnnestNode_SetsFromNode_IfNull()
    {
        var node = new UnnestNode(new object[] { "col" }, "alias");
        var partition = new SqlNodePartition(new[] { node });
        
        partition.UnnestNodes.Should().Contain(node);
        partition.FromNode.Should().Be(node);
    }

    [Fact]
    public void VisitUnnestNode_DoesNotOverwriteFromNode()
    {
        var fromNode = new FromNode("table");
        var node = new UnnestNode(new object[] { "col" }, "alias");
        var partition = new SqlNodePartition(new ISqlNode[] { fromNode, node });
        
        partition.UnnestNodes.Should().Contain(node);
        partition.FromNode.Should().Be(fromNode);
    }
}


