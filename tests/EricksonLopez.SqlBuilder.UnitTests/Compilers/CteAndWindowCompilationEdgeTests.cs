// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Compilers;

/// <summary>
/// Verifies CTEs, Window functions, WindowPage pagination, GroupBy/Having, Set operations and Limit/Offset compilation.
/// </summary>
public class CteAndWindowCompilationEdgeTests
{
    private class DefaultCompiler : SqlCompilerBase
    {
        protected override ISqlRenderer AotRenderer => null!;
        public override string EscapeIdentifier(string identifier) => $"\"{identifier}\"";
        public new void CompileSelect(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext ctx)
            => base.CompileSelect(nodes, visitor, ctx);
    }

    private class FakeQuery : IAstQuery
    {
        public string? Tag => null;
        private readonly ISqlNode[] _nodes;
        public FakeQuery(params ISqlNode[] nodes) => _nodes = nodes;
        IReadOnlyList<ISqlNode> IAstQuery.Nodes => _nodes;
        public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);
    }

    private (DefaultCompiler compiler, CompilationContext ctx, ISqlVisitor visitor) CreateCtx()
    {
        var c = new DefaultCompiler();
        var ctx = new CompilationContext(new ParameterManager());
        var v = c.CreateVisitor(ctx);
        return (c, ctx, v);
    }

    private string GetSql(ISqlQuery query) => new DefaultCompiler().Compile(query).Sql.Trim();

    [Fact]
    public void Compile_WhenMultipleCtesIncludeRecursive_ShouldRenderWithRecursiveKeyword()
    {
        var cte1 = new CteNode("c1", new SelectQuery<DummyEntity>().From("t"), true);
        var cte2 = new CteNode("c2", new SelectQuery<DummyEntity>().From("t"), false);
        var query = new SelectQuery<DummyEntity>().AddNode(cte1).AddNode(cte2);
        GetSql(query).Should().Be("WITH RECURSIVE \"c1\" AS (SELECT * FROM \"t\"), \"c2\" AS (SELECT * FROM \"t\") SELECT *");
    }

    [Fact]
    public void Compile_WhenWindowPageCombinedWithRecursiveCte_ShouldRenderBothCtes()
    {
        var cte = new CteNode("c", new SelectQuery<DummyEntity>().From("t"), true);
        var wp = new WindowPageNode(2, 10, "id", true);
        var query = new SelectQuery<DummyEntity>().AddNode(cte).AddNode(wp);
        GetSql(query).Should().Be("WITH RECURSIVE \"c\" AS (SELECT * FROM \"t\"), __wp AS (SELECT *, ROW_NUMBER() OVER(ORDER BY \"id\" DESC) AS __row_num ) SELECT * FROM __wp WHERE __row_num BETWEEN 11 AND 20");
    }

    [Fact]
    public void Compile_WhenMultipleWindowNodesProvided_ShouldRenderWindowClauseWithComma()
    {
        var w1 = new WindowNode("w1", new[] { "id" }, new[] { "name" });
        var w2 = new WindowNode("w2", Array.Empty<string>(), Array.Empty<string>());
        var query = new SelectQuery<DummyEntity>().AddNode(w1).AddNode(w2);
        GetSql(query).Should().Be("SELECT * WINDOW \"w1\" AS (PARTITION BY \"id\" ORDER BY \"name\"), \"w2\" AS ()");
    }

    [Fact]
    public void Compile_WhenWindowPageCombinedWithMultipleCtes_ShouldRenderAllCtes()
    {
        var query = new SelectQuery<DummyEntity>()
            .From("DummyEntity")
            .AddNode(new CteNode("C1", new SelectQuery<DummyEntity>().From("DummyEntity"), false))
            .AddNode(new CteNode("C2", new SelectQuery<DummyEntity>().From("DummyEntity"), false))
            .AddNode(new WindowPageNode(1, 10, "Id", false));
        GetSql(query).Should().Be("WITH \"C1\" AS (SELECT * FROM \"DummyEntity\"), \"C2\" AS (SELECT * FROM \"DummyEntity\"), __wp AS (SELECT *, ROW_NUMBER() OVER(ORDER BY \"Id\" ASC) AS __row_num FROM \"DummyEntity\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_WhenMultipleCtesProvided_ShouldRenderAllCtes()
    {
        var query = new SelectQuery<DummyEntity>()
            .From("DummyEntity")
            .AddNode(new CteNode("C1", new SelectQuery<DummyEntity>().From("DummyEntity"), false))
            .AddNode(new CteNode("C2", new SelectQuery<DummyEntity>().From("DummyEntity"), false));
        GetSql(query).Should().Be("WITH \"C1\" AS (SELECT * FROM \"DummyEntity\"), \"C2\" AS (SELECT * FROM \"DummyEntity\") SELECT * FROM \"DummyEntity\"");
    }

    [Fact]
    public void Compile_WhenWindowPageIsAscending_ShouldRenderAscOrdering()
    {
        var query = new SelectQuery<DummyEntity>().From("DummyEntity").AddNode(new WindowPageNode(1, 10, "Id", false));
        GetSql(query).Should().Be("WITH __wp AS (SELECT *, ROW_NUMBER() OVER(ORDER BY \"Id\" ASC) AS __row_num FROM \"DummyEntity\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_WhenWindowPageHasCustomSelect_ShouldRenderCustomProjection()
    {
        var query = new SelectQuery<DummyEntity>()
            .From("DummyEntity")
            .AddNode(new SelectNode(new[] { "Name" }, false))
            .AddNode(new WindowPageNode(1, 10, "Id", false));
        GetSql(query).Should().Be("WITH __wp AS (SELECT \"Name\", ROW_NUMBER() OVER(ORDER BY \"Id\" ASC) AS __row_num FROM \"DummyEntity\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_WhenGroupByHasMultipleColumns_ShouldRenderCommaSeparatedColumns()
    {
        var query = new SelectQuery<DummyEntity>().From("DummyEntity").AddNode(new GroupByNode(new[] { "Id", "Name", "Age" }));
        GetSql(query).Should().Be("SELECT * FROM \"DummyEntity\" GROUP BY \"Id\", \"Name\", \"Age\"");
    }

    [Fact]
    public void Compile_WhenMultipleHavingNodesCombined_ShouldRenderWithAndKeywords()
    {
        var query = new SelectQuery<DummyEntity>()
            .From("DummyEntity")
            .AddNode(new RawHavingNode("Id > 1", null, false))
            .AddNode(new RawHavingNode("Id < 10", null, false))
            .AddNode(new ExpressionHavingNode((DummyEntity x) => x.Id != 5, false));
        GetSql(query).Should().Be("SELECT * FROM \"DummyEntity\" HAVING Id > 1 AND Id < 10 AND (id != @p0)");
    }

    [Fact]
    public void Compile_WhenMultipleNamedWindowsProvided_ShouldRenderWindowDefinitions()
    {
        var query = new SelectQuery<DummyEntity>()
            .From("DummyEntity")
            .AddNode(new WindowNode("w1", new[] { "Name" }, null))
            .AddNode(new WindowNode("w2", new[] { "Age" }, null));
        GetSql(query).Should().Be("SELECT * FROM \"DummyEntity\" WINDOW \"w1\" AS (PARTITION BY \"Name\" ), \"w2\" AS (PARTITION BY \"Age\" )");
    }

    [Fact]
    public void Compile_WhenMultipleSetOperationsProvided_ShouldRenderSequentialSetClauses()
    {
        var query = new SelectQuery<DummyEntity>()
            .From("DummyEntity")
            .AddNode(new SetOperationNode("UNION", new SelectQuery<DummyEntity>().From("DummyEntity")))
            .AddNode(new SetOperationNode("UNION ALL", new SelectQuery<DummyEntity>().From("DummyEntity")));
        GetSql(query).Should().Be("SELECT * FROM \"DummyEntity\" UNION SELECT * FROM \"DummyEntity\" UNION ALL SELECT * FROM \"DummyEntity\"");
    }

    [Fact]
    public void Compile_WhenMultipleNonRecursiveCtesProvided_ShouldRenderWithClause()
    {
        var cte1 = new CteNode("c1", new SelectQuery<DummyEntity>().From("t"), false);
        var cte2 = new CteNode("c2", new SelectQuery<DummyEntity>().From("t"), false);
        var query = new SelectQuery<DummyEntity>().From("dummy_entity").AddNode(cte1).AddNode(cte2);
        GetSql(query).Should().Be("WITH \"c1\" AS (SELECT * FROM \"t\"), \"c2\" AS (SELECT * FROM \"t\") SELECT * FROM \"dummy_entity\"");
    }

    [Fact]
    public void Compile_WhenWindowPageNodeHasNoExplicitSelect_ShouldRenderAsterisk()
    {
        var wp = new WindowPageNode(1, 10, "id", false);
        var query = new SelectQuery<DummyEntity>().From("dummy_entity").AddNode(wp);
        GetSql(query).Should().Be("WITH __wp AS (SELECT *, ROW_NUMBER() OVER(ORDER BY \"id\" ASC) AS __row_num FROM \"dummy_entity\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_WhenHavingNodeIsOr_ShouldRenderOrKeyword()
    {
        var h1 = new RawHavingNode("id = 1", null, false);
        var h2 = new RawHavingNode("id = 2", null, true);
        var query = new SelectQuery<DummyEntity>().From("dummy").AddNode(h1).AddNode(h2);
        GetSql(query).Should().Be("SELECT * FROM \"dummy\" HAVING id = 1 OR id = 2");
    }

    [Fact]
    public void Compile_WhenExpressionHavingIsOr_ShouldRenderOrKeyword()
    {
        var h1 = new ExpressionHavingNode((DummyEntity x) => x.Id == 1, false);
        var h2 = new ExpressionHavingNode((DummyEntity x) => x.Id == 2, true);
        var query = new SelectQuery<DummyEntity>().From("dummy").AddNode(h1).AddNode(h2);
        GetSql(query).Should().Be("SELECT * FROM \"dummy\" HAVING (id = @p0) OR (id = @p1)");
    }

    [Fact]
    public void Compile_WhenSetOperationNodeProvided_ShouldRenderSetKeyword()
    {
        var s1 = new SetOperationNode("UNION", new SelectQuery<DummyEntity>().From("t2"));
        var query = new SelectQuery<DummyEntity>().From("dummy").AddNode(s1);
        GetSql(query).Should().Be("SELECT * FROM \"dummy\" UNION SELECT * FROM \"t2\"");
    }

    [Fact]
    public void Compile_WhenAnyCteIsRecursive_ShouldRenderWithRecursive()
    {
        var cte1 = new CteNode("c1", new SelectQuery<DummyEntity>().From("t"), true);
        var cte2 = new CteNode("c2", new SelectQuery<DummyEntity>().From("t"), false);
        var query = new SelectQuery<DummyEntity>().From("dummy_entity").AddNode(cte1).AddNode(cte2);
        GetSql(query).Should().Be("WITH RECURSIVE \"c1\" AS (SELECT * FROM \"t\"), \"c2\" AS (SELECT * FROM \"t\") SELECT * FROM \"dummy_entity\"");
    }

    [Fact]
    public void Compile_WhenLimitOffsetNodeProvided_ShouldVisitNode()
    {
        var compiler = new DefaultCompiler();
        compiler.Compile(new FakeQuery(new LimitOffsetNode(10, null)));
        compiler.Should().NotBeNull();
    }

    [Fact]
    public void Compile_WhenWindowPageFollowsRecursiveCte_ShouldRenderWithRecursiveAndWindowPage()
    {
        var cte1 = new CteNode("c1", new SelectQuery<DummyEntity>().From("t"), true);
        var wp = new WindowPageNode(1, 10, "id", false);
        var query = new SelectQuery<DummyEntity>().From("dummy_entity").AddNode(cte1).AddNode(wp);
        GetSql(query).Should().Be("WITH RECURSIVE \"c1\" AS (SELECT * FROM \"t\"), __wp AS (SELECT *, ROW_NUMBER() OVER(ORDER BY \"id\" ASC) AS __row_num FROM \"dummy_entity\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_WhenWindowPageIsDescending_ShouldRenderDescOrdering()
    {
        var query = new SelectQuery<DummyEntity>().From("DummyEntity").AddNode(new WindowPageNode(1, 10, "Id", true));
        GetSql(query).Should().Be("WITH __wp AS (SELECT *, ROW_NUMBER() OVER(ORDER BY \"Id\" DESC) AS __row_num FROM \"DummyEntity\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_WhenWindowPageHasCustomSelectDescending_ShouldRenderCustomProjection()
    {
        var query = new SelectQuery<DummyEntity>()
            .From("DummyEntity")
            .AddNode(new SelectNode(new[] { "Name" }, false))
            .AddNode(new WindowPageNode(1, 10, "Id", true));
        GetSql(query).Should().Be("WITH __wp AS (SELECT \"Name\", ROW_NUMBER() OVER(ORDER BY \"Id\" DESC) AS __row_num FROM \"DummyEntity\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void CompileSelect_OneCteRecursiveOneNot_AppendsRecursiveKeyword()
    {
        var cteR = new CteNode("r", new SelectQuery<DummyEntity>().From("t"), true);
        var cteN = new CteNode("n", new SelectQuery<DummyEntity>().From("t"), false);
        var q = new SelectQuery<DummyEntity>().From("t2").AddNode(cteR).AddNode(cteN);
        GetSql(q).Should().Contain("WITH RECURSIVE");
    }

    [Fact]
    public void CompileSelect_NoCteRecursive_DoesNotAppendRecursiveKeyword()
    {
        var cte1 = new CteNode("a", new SelectQuery<DummyEntity>().From("t"), false);
        var cte2 = new CteNode("b", new SelectQuery<DummyEntity>().From("t"), false);
        var q = new SelectQuery<DummyEntity>().From("t2").AddNode(cte1).AddNode(cte2);
        GetSql(q).Should().NotContain("RECURSIVE");
    }

    [Fact]
    public void CompileSelect_ZeroCtes_DoesNotAppendWith()
    {
        var q = new SelectQuery<DummyEntity>().From("t");
        GetSql(q).Should().NotContain("WITH");
    }

    [Fact]
    public void CompileSelect_OneCte_AppendsWithExactlyOnce()
    {
        var cte = new CteNode("c", new SelectQuery<DummyEntity>().From("t"), false);
        var q = new SelectQuery<DummyEntity>().From("t2").AddNode(cte);
        var sql = GetSql(q);
        sql.Split("WITH ").Length.Should().Be(2, "exactly one WITH clause");
    }

    [Fact]
    public void CompileSelect_NoWindowPage_DoesNotAppendRowNumber()
    {
        var q = new SelectQuery<DummyEntity>().From("t").Select(x => x.Id);
        GetSql(q).Should().NotContain("ROW_NUMBER");
    }

    [Fact]
    public void CompileSelect_WithWindowPage_AppendsRowNumber()
    {
        var q = new SelectQuery<DummyEntity>().From("t")
            .AddNode(new SelectNode(new[] { "Name" }, false))
            .AddNode(new WindowPageNode(1, 5, "Id", false));
        GetSql(q).Should().Contain("ROW_NUMBER() OVER(ORDER BY");
    }

    [Fact]
    public void CompileSelect_NoSelectNoWindowPage_AppendsStar()
    {
        var q = new SelectQuery<DummyEntity>().From("t");
        GetSql(q).Should().Be("SELECT * FROM \"t\"");
    }

    [Fact]
    public void CompileSelect_NoSelectWithWindowPage_AppendsStarRowNumber()
    {
        var q = new SelectQuery<DummyEntity>().From("t").AddNode(new WindowPageNode(2, 3, "Id", true));
        var sql = GetSql(q);
        sql.Should().Contain("SELECT *, ROW_NUMBER() OVER(ORDER BY \"Id\" DESC)");
        sql.Should().Contain("__row_num BETWEEN 4 AND 6");
    }

    [Fact]
    public void CompileLimitOffset_LimitOnly_AppendsLimit()
    {
        var q = new SelectQuery<DummyEntity>().From("t").AddNode(new LimitOffsetNode(10, null));
        GetSql(q).Should().Contain("LIMIT 10");
    }

    [Fact]
    public void CompileLimitOffset_OffsetOnly_AppendsOffset()
    {
        var q = new SelectQuery<DummyEntity>().From("t").AddNode(new LimitOffsetNode(null, 5));
        GetSql(q).Should().Contain("OFFSET 5");
    }

    [Fact]
    public void CompileLimitOffset_BothLimitAndOffset_AppendsBoth()
    {
        var q = new SelectQuery<DummyEntity>().From("t").AddNode(new LimitOffsetNode(10, 20));
        var sql = GetSql(q);
        sql.Should().Contain("LIMIT 10");
        sql.Should().Contain("OFFSET 20");
    }

    [Fact]
    public void CompileSelect_WindowPage_PageTwoSize5_CorrectBetween()
    {
        var q = new SelectQuery<DummyEntity>().From("t").AddNode(new WindowPageNode(2, 5, "Id", false));
        GetSql(q).Should().Contain("__row_num BETWEEN 6 AND 10");
    }

    [Fact]
    public void CompileSelect_WindowPage_Page1Size10_CorrectBetween()
    {
        var q = new SelectQuery<DummyEntity>().From("t").AddNode(new WindowPageNode(1, 10, "Id", false));
        GetSql(q).Should().Contain("__row_num BETWEEN 1 AND 10");
    }
}



