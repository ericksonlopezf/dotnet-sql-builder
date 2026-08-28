// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Nodes;

public class AstNodesTests
{
    [Fact]
    public void DeleteNode_Properties_WorkCorrectly()
    {
        var node = new DeleteNode("Users");
        node.TableName.Should().Be("Users");
    }

    [Fact]
    public void InsertNode_Properties_WorkCorrectly()
    {
        var node = new InsertNode("Users", new[] { "Name", "Age" });
        node.TableName.Should().Be("Users");
        node.Columns.Should().BeEquivalentTo("Name", "Age");
    }

    [Fact]
    public void UpdateNode_Properties_WorkCorrectly()
    {
        var node = new UpdateNode("Users");
        node.TableName.Should().Be("Users");
    }

    [Fact]
    public void ReturningNode_Properties_WorkCorrectly()
    {
        var node = new ReturningNode(new[] { "Id", "CreatedAt" });
        node.Columns.Should().BeEquivalentTo("Id", "CreatedAt");
    }

    [Fact]
    public void LimitOffsetNode_Properties_WorkCorrectly()
    {
        var node = new LimitOffsetNode(10, 20);
        node.Limit.Should().Be(10);
        node.Offset.Should().Be(20);
    }

    [Fact]
    public void ScalarSubquerySelectNode_Properties_WorkCorrectly()
    {
        var raw = new RawQuery("SELECT COUNT(*) FROM table");
        var node = new ScalarSubquerySelectNode(raw, "count_alias");
        node.Subquery.Should().BeSameAs(raw);
        node.Alias.Should().Be("count_alias");
    }

    [Fact]
    public void OnConflictNode_Properties_WorkCorrectly()
    {
        var conflict = new OnConflictNode(new[] { "Id" }, "DO NOTHING");
        conflict.TargetColumns.Should().Contain("Id");
        conflict.UpdateAction.Should().Be("DO NOTHING");
    }

    [Fact]
    public void SubqueryJoinNode_Properties_WorkCorrectly()
    {
        var subquery = NSubstitute.Substitute.For<EricksonLopez.SqlBuilder.Abstractions.IAstQuery>();
        var node = new SubqueryJoinNode(JoinType.Inner, subquery, "AliasA", "AliasA.Id = B.Id");
        node.Subquery.Should().BeSameAs(subquery);
        node.Alias.Should().Be("AliasA");
        node.OnCondition.Should().Be("AliasA.Id = B.Id");
        node.Type.Should().Be(JoinType.Inner);
    }

    [Fact]
    public void ExpressionSelectNode_Properties_WorkCorrectly()
    {
        System.Linq.Expressions.Expression<Func<int>> expr = () => 1;
        var node = new ExpressionSelectNode(expr, true);
        node.Selector.Should().NotBeNull();
        node.IsDistinct.Should().BeTrue();
    }

    [Fact]
    public void QueryAliasNode_Properties_WorkCorrectly()
    {
        var node = new QueryAliasNode("U");
        node.Alias.Should().Be("U");
    }

    [Fact]
    public void DistinctOnNode_Properties_WorkCorrectly()
    {
        var node = new DistinctOnNode(new[] { "Id", "CreatedAt" });
        node.Columns.Should().BeEquivalentTo("Id", "CreatedAt");
    }
}



