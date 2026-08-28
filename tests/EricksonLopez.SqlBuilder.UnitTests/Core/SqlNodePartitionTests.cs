// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlNodePartitionTests
{
    private static ISqlNode CreateUninitialized<T>() where T : ISqlNode
    {
        return (ISqlNode)RuntimeHelpers.GetUninitializedObject(typeof(T));
    }

    [Fact]
    public void SqlNodePartition_ShouldMaintainMultipleNodesOfSameType_ToKillCoalesceAssignmentMutations()
    {
        // Act
        // We pass multiple nodes of types that use `??= new().Add()` to ensure they append instead of replace.
        var nodes = new ISqlNode[]
        {
            CreateUninitialized<UpdateNode>(), CreateUninitialized<UpdateNode>(),
            CreateUninitialized<SetNode>(), CreateUninitialized<SetNode>(),
            CreateUninitialized<SelectNode>(), CreateUninitialized<SelectNode>(),
            CreateUninitialized<JoinNode>(), CreateUninitialized<JoinNode>(),
            CreateUninitialized<GroupByNode>(), CreateUninitialized<GroupByNode>(),
            CreateUninitialized<WindowNode>(), CreateUninitialized<WindowNode>(),
            CreateUninitialized<ExpressionWhereNode>(), CreateUninitialized<ExpressionWhereNode>(),
            CreateUninitialized<RawWhereNode>(), CreateUninitialized<RawWhereNode>(),
            CreateUninitialized<ExistsWhereNode>(), CreateUninitialized<ExistsWhereNode>(),
            CreateUninitialized<CteNode>(), CreateUninitialized<CteNode>(),
            CreateUninitialized<SetOperationNode>(), CreateUninitialized<SetOperationNode>(),
            CreateUninitialized<ThenByNode>(), CreateUninitialized<ThenByNode>(),
            CreateUninitialized<OrderByNode>(), CreateUninitialized<OrderByNode>(),
            CreateUninitialized<RawJoinNode>(), CreateUninitialized<RawJoinNode>(),
            CreateUninitialized<SubqueryJoinNode>(), CreateUninitialized<SubqueryJoinNode>(),
            CreateUninitialized<ExpressionSelectNode>(), CreateUninitialized<ExpressionSelectNode>(),
            CreateUninitialized<RawSelectNode>(), CreateUninitialized<RawSelectNode>(),
            CreateUninitialized<WindowFunctionNode>(), CreateUninitialized<WindowFunctionNode>(),
            CreateUninitialized<RawOrderByNode>(), CreateUninitialized<RawOrderByNode>(),
            CreateUninitialized<ExpressionHavingNode>(), CreateUninitialized<ExpressionHavingNode>(),
            CreateUninitialized<RawHavingNode>(), CreateUninitialized<RawHavingNode>(),
            CreateUninitialized<UnnestNode>(), CreateUninitialized<UnnestNode>(),
            CreateUninitialized<ConcurrencyTokenNode>(), CreateUninitialized<ConcurrencyTokenNode>(),
            CreateUninitialized<CaseNode>(), CreateUninitialized<CaseNode>(),
            CreateUninitialized<CompositeCursorNode>(), CreateUninitialized<CompositeCursorNode>(),
            // Extension node (any unknown implementation)
            Substitute.For<ISqlNode>(), Substitute.For<ISqlNode>()
        };

        var partition = new SqlNodePartition(nodes);

        // Assert - verify each collection has exactly 2 elements
        partition.UpdateNodes.Should().HaveCount(2);
        partition.SetNodes.Should().HaveCount(2);
        
        // Select nodes are collected from many types
        partition.SelectNodes.Should().HaveCount(10); // 2 SelectNode + 2 ExprSelect + 2 RawSelect + 2 WindowFunc + 2 CaseNode
        
        // Join nodes
        partition.JoinNodes.Should().HaveCount(6); // 2 JoinNode + 2 RawJoinNode + 2 SubqueryJoinNode
        
        partition.GroupByNodes.Should().HaveCount(2);
        partition.WindowNodes.Should().HaveCount(2);
        partition.WhereNodes.Should().HaveCount(6); // ExpressionWhereNode + RawWhereNode + ExistsWhereNode
        partition.CteNodes.Should().HaveCount(2);
        partition.SetOpNodes.Should().HaveCount(2);
        
        // Order nodes
        partition.OrderNodes.Should().HaveCount(6); // 2 ThenBy + 2 OrderBy + 2 RawOrderBy
        
        // Having nodes
        partition.HavingNodes.Should().HaveCount(4); // 2 ExprHaving + 2 RawHaving
        
        partition.UnnestNodes.Should().HaveCount(2);
        partition.ConcurrencyTokenNodes.Should().HaveCount(2);
        partition.CompositeCursorNodes.Should().HaveCount(2);
        partition.ExtensionNodes.Should().HaveCount(2);
    }

    [Fact]
    public void SqlNodePartition_LimitOffsetNode_MergesValuesCorrectly_ToKillNullCoalescingMutations()
    {
        // LimitOffsetNode constructors are simple enough
        // Case 1: First node has Limit only, second node has Offset only
        var partition1 = new SqlNodePartition(new ISqlNode[]
        {
            new LimitOffsetNode(10, null),
            new LimitOffsetNode(null, 5)
        });
        partition1.LimitNode.Should().NotBeNull();
        partition1.LimitNode!.Limit.Should().Be(10);
        partition1.LimitNode!.Offset.Should().Be(5);

        // Case 2: First node has Offset only, second node has Limit only
        var partition2 = new SqlNodePartition(new ISqlNode[]
        {
            new LimitOffsetNode(null, 20),
            new LimitOffsetNode(15, null)
        });
        partition2.LimitNode.Should().NotBeNull();
        partition2.LimitNode!.Limit.Should().Be(15);
        partition2.LimitNode!.Offset.Should().Be(20);

        // Case 3: Both nodes have values, second node overrides
        var partition3 = new SqlNodePartition(new ISqlNode[]
        {
            new LimitOffsetNode(100, 200),
            new LimitOffsetNode(300, 400)
        });
        partition3.LimitNode.Should().NotBeNull();
        partition3.LimitNode!.Limit.Should().Be(300);
        partition3.LimitNode!.Offset.Should().Be(400);
    }
}


