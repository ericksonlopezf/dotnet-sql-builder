// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SelectQueryTests
{

    [Fact]
    public void Distinct_WhenNoPriorSelect_AppendsDistinctSelectNode()
    {
        var query = new SelectQuery<User>().Distinct();
        query.Nodes.Should().ContainSingle(n => n is SelectNode && ((SelectNode)n).IsDistinct);
    }

    [Fact]
    public void Distinct_WhenPriorSelectExists_MarksSelectNodeAsDistinct()
    {
        var query = new SelectQuery<User>().Select("Id").Distinct();
        query.Nodes.Should().ContainSingle(n => n is SelectNode && ((SelectNode)n).IsDistinct);
        var selectNode = (SelectNode)query.Nodes.Single(n => n is SelectNode);
        selectNode.Columns.Should().Equal("Id");
    }

    [Fact]
    public void Distinct_WhenExpressionSelectExists_MarksExpressionSelectNodeAsDistinct()
    {
        var query = new SelectQuery<User>().Select(x => x.Id).Distinct();
        query.Nodes.Should().ContainSingle(n => n is ExpressionSelectNode && ((ExpressionSelectNode)n).IsDistinct);
    }

    [Fact]
    public void Distinct_WhenRawSelectExists_MarksRawSelectNodeAsDistinct()
    {
        var query = new SelectQuery<User>().RawSelect($"Id, Name").Distinct();
        query.Nodes.Should().ContainSingle(n => n is RawSelectNode && ((RawSelectNode)n).IsDistinct);
    }

    [Fact]
    public void LateralLeftJoin_SetsIsLateralToTrue()
    {
        var subquery = Sql.From<User>();
        var query = new SelectQuery<User>().LateralLeftJoin(subquery, "alias");
        query.Nodes.OfType<SubqueryJoinNode>().Single().IsLateral.Should().BeTrue();
    }

    [Fact]
    public void LeftJoinSubquery_SetsIsLateralToFalse()
    {
        var subquery = Sql.From<User>();
        var query = new SelectQuery<User>().LeftJoinSubquery(subquery, "alias");
        query.Nodes.OfType<SubqueryJoinNode>().Single().IsLateral.Should().BeFalse();
    }

    [Fact]
    public void CrossApply_SetsIsLateralToFalse()
    {
        var subquery = Sql.From<User>();
        var query = new SelectQuery<User>().CrossApply(subquery, "alias");
        query.Nodes.OfType<SubqueryJoinNode>().Single().IsLateral.Should().BeFalse();
    }

    [Fact]
    public void OuterApply_SetsIsLateralToFalse()
    {
        var subquery = Sql.From<User>();
        var query = new SelectQuery<User>().OuterApply(subquery, "alias");
        query.Nodes.OfType<SubqueryJoinNode>().Single().IsLateral.Should().BeFalse();
    }

    [Fact]
    public void OrderBy_WithNulls_SetsDescendingFalse()
    {
        var query = new SelectQuery<User>().OrderBy(x => x.Id, NullsPosition.First);
        var node = query.Nodes.OfType<OrderByNode>().Single();
        node.IsDescending.Should().BeFalse();
        node.Nulls.Should().Be(NullsPosition.First);
    }

    [Fact]
    public void OrderByDescending_WithNulls_SetsDescendingTrue()
    {
        var query = new SelectQuery<User>().OrderByDescending(x => x.Id, NullsPosition.Last);
        var node = query.Nodes.OfType<OrderByNode>().Single();
        node.IsDescending.Should().BeTrue();
        node.Nulls.Should().Be(NullsPosition.Last);
    }

    [Fact]
    public void ThenBy_WithNulls_SetsDescendingFalse()
    {
        var query = new SelectQuery<User>().ThenBy(x => x.Id, NullsPosition.First);
        var node = query.Nodes.OfType<ThenByNode>().Single();
        node.IsDescending.Should().BeFalse();
        node.Nulls.Should().Be(NullsPosition.First);
    }

    [Fact]
    public void ThenByDescending_WithNulls_SetsDescendingTrue()
    {
        var query = new SelectQuery<User>().ThenByDescending(x => x.Id, NullsPosition.Last);
        var node = query.Nodes.OfType<ThenByNode>().Single();
        node.IsDescending.Should().BeTrue();
        node.Nulls.Should().Be(NullsPosition.Last);
    }

    [Fact]
    public void WindowPage_InvalidPageSize_ThrowsWithCorrectMessage()
    {
        var query = new SelectQuery<User>();
        var act = () => query.WindowPage(1, 0, "Id");
        
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Page size must be greater than zero.*")
            .And.ParamName.Should().Be("pageSize");
    }

    [Fact]
    public void SelectCase_ConfiguresAndAddsNode()
    {
        var query = new SelectQuery<User>().SelectCase(c => c.When("id = 1", 1).Then("1"));
        var node = query.Nodes.OfType<CaseNode>().Single();
        node.Branches.Should().HaveCount(1);
    }

    [Fact]
    public void SeekAfter_SetsIsAfterToTrue()
    {
        var query = new SelectQuery<User>().SeekAfter(new CursorKey("Id", 1, true));
        query.Nodes.OfType<CompositeCursorNode>().Single().IsAfter.Should().BeTrue();
    }

    [Fact]
    public void SeekBefore_SetsIsAfterToFalse()
    {
        var query = new SelectQuery<User>().SeekBefore(new CursorKey("Id", 1, true));
        query.Nodes.OfType<CompositeCursorNode>().Single().IsAfter.Should().BeFalse();
    }

    [Fact]
    public void Select_ScalarSubquery_AddsScalarSubquerySelectNodeAndCompiles()
    {
        var subquery = new RawQuery("SELECT COUNT(*) FROM orders WHERE customer_id = customers.id");
        var query = Sql.From<User>().Select(subquery, "order_count");

        query.Nodes.Should().ContainSingle(n => n is ScalarSubquerySelectNode);
        var node = query.Nodes.OfType<ScalarSubquerySelectNode>().Single();
        node.Alias.Should().Be("order_count");
        node.Subquery.Should().BeSameAs(subquery);

        var compiler = new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler();
        var result = query.Build(compiler);
        result.Sql.Should().Contain("(SELECT COUNT(*) FROM orders WHERE customer_id = customers.id) AS [order_count]");
    }

    [Fact]
    public void Select_ScalarSubquery_ThrowsOnNullOrEmpty()
    {
        var query = Sql.From<User>();
        var act1 = () => query.Select((ISqlQuery)null!, "alias");
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => query.Select(new RawQuery("SELECT 1"), "");
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhereColumns_AppendsRawWhereNodeWithColumnComparison()
    {
        var query = Sql.From<User>().WhereColumns("t1.category_id", "=", "t2.category_id");
        query.Nodes.Should().ContainSingle(n => n is RawWhereNode);
        var node = query.Nodes.OfType<RawWhereNode>().Single();
        node.Condition.Should().Be("t1.category_id = t2.category_id");
        node.Parameters.Should().BeNull();

        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var result = query.Build(compiler);
        result.Sql.Should().Contain("WHERE t1.category_id = t2.category_id");
    }

    [Fact]
    public void WhereColumns_ThrowsOnNullOrEmpty()
    {
        var query = Sql.From<User>();
        var act = () => query.WhereColumns("", "=", "col2");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhereDate_WhereYear_WhereMonth_WhereDay_AppendsCondition()
    {
        var date = new DateTime(2026, 8, 19);
        var query = Sql.From<User>()
            .WhereDate("created_at", ">=", date)
            .WhereYear("created_at", "=", 2026)
            .WhereMonth("created_at", "=", 8)
            .WhereDay("created_at", "=", 19);

        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var result = query.Build(compiler);
        result.Sql.Should().Contain("WHERE created_at >= @p0");
        result.Sql.Should().Contain("AND EXTRACT(YEAR FROM created_at) = @p1");
        result.Sql.Should().Contain("AND EXTRACT(MONTH FROM created_at) = @p2");
        result.Sql.Should().Contain("AND EXTRACT(DAY FROM created_at) = @p3");
        result.Parameters.Count.Should().Be(4);
    }

    [Fact]
    public void AggregateHelpers_AsCount_AsSum_AsAvg_AsMin_AsMax_AddProjection()
    {
        var countQuery = Sql.From<User>().AsCount();
        countQuery.Nodes.OfType<RawSelectNode>().Single().RawSql.Should().Be("COUNT(*) AS count");

        var sumQuery = Sql.From<User>().AsSum("amount", "total_sum");
        sumQuery.Nodes.OfType<RawSelectNode>().Single().RawSql.Should().Be("SUM(amount) AS total_sum");

        var avgQuery = Sql.From<User>().AsAvg("price");
        avgQuery.Nodes.OfType<RawSelectNode>().Single().RawSql.Should().Be("AVG(price)");

        var minQuery = Sql.From<User>().AsMin("age", "min_age");
        minQuery.Nodes.OfType<RawSelectNode>().Single().RawSql.Should().Be("MIN(age) AS min_age");

        var maxQuery = Sql.From<User>().AsMax("score", "max_score");
        maxQuery.Nodes.OfType<RawSelectNode>().Single().RawSql.Should().Be("MAX(score) AS max_score");
    }
}


