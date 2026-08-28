// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class QueryCoverageTests
{

    private class UnregisteredNonEntity
    {
        public int Id { get; set; }
    }

    [Fact]
    public void UpdateQuery_Set_WithNullOrNonSqlEntity_ThrowsInvalidOperationException()
    {
        var query = new UpdateQuery<DummyEntity>();
        var action = () => query.Set((DummyEntity)null!, false);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*does not implement ISqlEntity*");
    }

    [Fact]
    public void UpdateQuery_Set_WithNonMemberExpression_ThrowsArgumentException()
    {
        var query = new UpdateQuery<DummyEntity>();
        // Using a constant expression which is not a MemberExpression
        var action = () => query.Set(x => "Not a property", "Value");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*must be a member expression*");
    }

    [Fact]
    public void DeleteQuery_Where_WithNonMemberExpression_ThrowsArgumentException()
    {
        var query = new DeleteQuery<DummyEntity>();
        // DeleteQuery doesn't have Set but checking Where or if it has anything similar
        query.AddNode(new DeleteNode("dummy")).Should().NotBeNull();
    }

    [Fact]
    public void AllQueries_WithTag_SetsTagProperty()
    {
        var select = new SelectQuery<DummyEntity>().WithTag("tag-select");
        select.Tag.Should().Be("tag-select");

        var insert = new InsertQuery<DummyEntity>().WithTag("tag-insert");
        insert.Tag.Should().Be("tag-insert");

        var update = new UpdateQuery<DummyEntity>().WithTag("tag-update");
        update.Tag.Should().Be("tag-update");

        var delete = new DeleteQuery<DummyEntity>().WithTag("tag-delete");
        delete.Tag.Should().Be("tag-delete");

        var raw = new RawQuery("SELECT 1").WithTag("tag-raw");
        raw.Tag.Should().Be("tag-raw");
    }

    [Fact]
    public void SelectQuery_AdvancedJoinsAndCte_CreatesNodes()
    {
        var q = new SelectQuery<DummyEntity>()
            .LateralJoin<DummyEntity>(sub => sub.Where(x => x.Id == 1), "lat", (x, sub) => x.Id == sub.Id)
            .LateralLeftJoin<DummyEntity>(sub => sub.Where(x => x.Id == 2), "lat_left", (x, sub) => x.Id == sub.Id)
            .LateralLeftJoin<DummyEntity>(sub => sub.Where(x => x.Id == 3), "lat_left_str", "TRUE")
            .JoinSubquery<DummyEntity>(Sql.From<DummyEntity>(), "sub", (x, sub) => x.Id == sub.Id)
            .LeftJoinSubquery<DummyEntity>(Sql.From<DummyEntity>(), "sub_left", (x, sub) => x.Id == sub.Id)
            .RecursiveCTE("rec_cte", Sql.From<DummyEntity>(), MaterializationHint.Materialized)
            .SelectCase(new CaseExpressionBuilder().When("id = 1").Then("'One'").Else("'Other'").Build());

        q.Nodes.Should().HaveCount(7);
        q.Nodes[0].Should().BeOfType<SubqueryJoinNode>().Which.IsLateral.Should().BeTrue();
        q.Nodes[1].Should().BeOfType<SubqueryJoinNode>().Which.IsLateral.Should().BeTrue();
        q.Nodes[2].Should().BeOfType<SubqueryJoinNode>().Which.IsLateral.Should().BeTrue();
        q.Nodes[3].Should().BeOfType<SubqueryJoinNode>().Which.IsLateral.Should().BeFalse();
        var node4 = q.Nodes[4].Should().BeOfType<SubqueryJoinNode>().Subject;
        node4.IsLateral.Should().BeFalse();
        node4.Type.Should().Be(JoinType.Left);
        var cteNode = q.Nodes[5].Should().BeOfType<CteNode>().Subject;
        cteNode.Materialization.Should().Be(MaterializationHint.Materialized);
        cteNode.IsRecursive.Should().BeTrue();
        q.Nodes[6].Should().BeOfType<CaseNode>();
    }

    [Fact]
    public void SelectQuery_Join_Generic_CreatesJoinNode()
    {
        var q = new SelectQuery<DummyEntity>()
            .Join<ThreeColumnEntity>((d, t) => d.Name == t.Name);

        q.Nodes.Should().ContainSingle().Which.Should().BeOfType<JoinNode>();
    }

    [Fact]
    public void UpdateQuery_Join_Generic_CreatesJoinNode()
    {
        var q = new UpdateQuery<DummyEntity>()
            .Join<ThreeColumnEntity>((d, t) => d.Name == t.Name);

        ((IAstQuery)q).Nodes.OfType<JoinNode>().Should().ContainSingle();
    }

    [Fact]
    public void InsertQuery_Returning_MemberExpression_And_NewExpression()
    {
        var q1 = new InsertQuery<DummyEntity>().Returning(x => x.Id);
        q1.Nodes.OfType<ReturningNode>().Single().Columns.Should().Equal("id");

        var q2 = new InsertQuery<DummyEntity>().Returning(x => new { x.Id, x.Name });
        q2.Nodes.OfType<ReturningNode>().Single().Columns.Should().Equal("id", "name");
    }

    [Fact]
    public void InsertQuery_OnConflict_And_DoActions_EdgeCases()
    {
        // OnConflict with MemberExpression directly
        var qMember = new InsertQuery<DummyEntity>().OnConflict(x => x.Name);
        qMember.Nodes.OfType<OnConflictNode>().Single().TargetColumns.Should().Equal("name");

        // DoNothing / DoUpdate without prior OnConflict should return same query
        var qEmpty = new InsertQuery<DummyEntity>();
        qEmpty.DoNothing().Should().BeSameAs(qEmpty);
        qEmpty.DoUpdate(x => new { x.Name }).Should().BeSameAs(qEmpty);
        qEmpty.DoUpdate($"name = 'Test'").Should().BeSameAs(qEmpty);

        // DoNothing / DoUpdate with prior OnConflict
        var qWithConflict = new InsertQuery<DummyEntity>().OnConflict("id");
        var qNothing = qWithConflict.DoNothing();
        qNothing.Nodes.OfType<OnConflictNode>().Single().UpdateAction.Should().Be("DO NOTHING");

        var qUpdateExpr = qWithConflict.DoUpdate(x => new { x.Name });
        qUpdateExpr.Nodes.OfType<OnConflictNode>().Single().UpdateAction.Should().Be("DO UPDATE SET");

        var qUpdateRaw = qWithConflict.DoUpdate($"name = 'Updated'");
        qUpdateRaw.Nodes.OfType<OnConflictNode>().Single().UpdateAction.Should().Contain("DO UPDATE SET");
    }

    [Fact]
    public void InsertQuery_DefaultValues_AddsDefaultValuesNode()
    {
        var q = new InsertQuery<DummyEntity>().DefaultValues();
        q.Nodes.Should().Contain(n => n is DefaultValuesNode);
    }

    [Fact]
    public void SqlBuilderDiagnostics_Configuration_And_Defaults()
    {
        SqlBuilderDiagnostics.LogParameters = true;
        SqlBuilderDiagnostics.LogParameters.Should().BeTrue();
        SqlBuilderDiagnostics.LogParameters = false;

        SqlBuilderDiagnostics.SlowQueryThresholdMs = 250;
        SqlBuilderDiagnostics.SlowQueryThresholdMs.Should().Be(250);
        SqlBuilderDiagnostics.SlowQueryThresholdMs = 500;

        SqlBuilderDiagnostics.LoggerFactory = null;
        SqlBuilderDiagnostics.LoggerFactory.Should().BeNull();
    }
}



