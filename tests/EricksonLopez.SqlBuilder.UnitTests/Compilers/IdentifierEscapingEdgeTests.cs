// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
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
/// Edge-case and branch coverage tests for identifier escaping, expressions, select/from/where nodes and visitor AST traversal.
/// </summary>
public class IdentifierEscapingEdgeTests
{
    private class UnannotatedDummy { }

    private class DefaultCompiler : SqlCompilerBase
    {
        protected override ISqlRenderer AotRenderer => null!;
        public override string EscapeIdentifier(string identifier) => $"\"{identifier}\"";
        public string PublicEscape(string identifier) => Escape(identifier);
        public new void CompileWheres(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext ctx)
            => base.CompileWheres(nodes, visitor, ctx);
        public new void CompileOrderBys(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext ctx)
            => base.CompileOrderBys(nodes, visitor, ctx);
        public new void CompileSelect(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext ctx)
            => base.CompileSelect(nodes, visitor, ctx);
    }

    private class UnknownTestNode : ISqlNode
    {
        public void Accept(ISqlVisitor visitor) => visitor.VisitUnknown(this);
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
    public void Parse_WhenExpressionTypeIsBlock_ThrowsNotSupportedException()
    {
        var parser = new SqlExpressionVisitor(new StringBuilder(), new ParameterManager());
        var expr = Expression.Block(Expression.Constant(1));
        Action act = () => parser.Parse(expr);
        act.Should().Throw<NotSupportedException>().WithMessage("*Expression of type Block is not supported.*");
    }

    [Fact]
    public void Parse_WhenOperatorIsUnsupportedPower_ThrowsNotSupportedException()
    {
        var parser = new SqlExpressionVisitor(new StringBuilder(), new ParameterManager());
        var expr = Expression.MakeBinary(ExpressionType.Power, Expression.Constant(1.0), Expression.Constant(2.0));
        Action act = () => parser.Parse(expr);
        act.Should().Throw<NotSupportedException>().WithMessage("*Operator Power is not supported.*");
    }

    [Fact]
    public void Parse_WhenExpressionIsConvertChecked_ShouldParseSuccessfully()
    {
        var parser = new SqlExpressionVisitor(new StringBuilder(), new ParameterManager());
        var expr = Expression.ConvertChecked(Expression.Constant(1), typeof(int));
        parser.Parse(expr);
    }

    [Fact]
    public void Construct_WhenEntityDoesNotImplementISqlEntity_ThrowsTypeInitializationException()
    {
        var act = () => { var query = new UpdateQuery<UnannotatedDummy>(); };
        act.Should().Throw<TypeInitializationException>()
            .WithInnerException<InvalidOperationException>()
            .WithMessage("*does not implement ISqlEntity*");
    }

    [Fact]
    public void Escape_WhenInputIsEmptyOrAsterisk_ShouldReturnUnmodified()
    {
        var compiler = new DefaultCompiler();
        compiler.PublicEscape("").Should().Be("");
        compiler.PublicEscape("*").Should().Be("*");
    }

    [Fact]
    public void Escape_WhenInputHasDotNotation_ShouldEscapeSegments()
    {
        var compiler = new DefaultCompiler();
        compiler.PublicEscape("table.column").Should().Be("\"table\".\"column\"");
    }

    [Fact]
    public void EscapeIdentifier_NonNull_UsesCompilerEscape()
    {
        var compiler = new DefaultCompiler();
        var escaped = compiler.EscapeIdentifier("test");
        escaped.Should().Be("\"test\"");
    }

    [Fact]
    public void Escape_WhenIdentifierContainsDot_ShouldEscapeEachPartSeparately()
    {
        var node = new SelectNode(new[] { "table.column" }, false);
        var query = new SelectQuery<DummyEntity>().From("table").AddNode(node);
        GetSql(query).Should().Be("SELECT \"table\".\"column\" FROM \"table\"");
    }

    [Fact]
    public void Compile_WhenSelectNodeHasEmptyString_ShouldEscapeAsEmptyString()
    {
        var query = new SelectQuery<DummyEntity>().From("DummyEntity").AddNode(new SelectNode(new[] { "" }, false));
        GetSql(query).Should().Be("SELECT  FROM \"DummyEntity\"");
    }

    [Fact]
    public void Compile_WhenSelectNodeHasEmptyColumns_ShouldAppendAsterisk()
    {
        var node = new SelectNode(Array.Empty<string>(), false);
        var query = new SelectQuery<DummyEntity>().AddNode(node);
        var result = new PostgreSqlCompiler().Compile(query);
        result.Sql.Trim().Should().Be("SELECT *");
    }

    [Fact]
    public void Compile_WhenSelectHasEmptyAnonymousObject_ShouldAppendAsterisk()
    {
        var query = new SelectQuery<DummyEntity>().Select(x => new { });
        GetSql(query).Should().Be("SELECT *");
    }

    [Fact]
    public void Compile_WhenRawSelectIsDistinct_ShouldAppendDistinctKeyword()
    {
        var node = new RawSelectNode("1", null, true);
        var query = new SelectQuery<DummyEntity>().AddNode(node);
        GetSql(query).Should().Be("SELECT DISTINCT 1");
    }

    [Fact]
    public void Compile_WhenFromNodeContainsSubquery_ShouldRenderSubqueryWithAlias()
    {
        var subQuery = new SelectQuery<DummyEntity>().Select(x => x.Id);
        var query = new SelectQuery<DummyEntity>().From(subQuery, "sub");
        var compiler = new PostgreSqlCompiler();
        compiler.Compile(query).Sql.Should().Be("SELECT * FROM (SELECT id) AS \"sub\"");
    }

    [Fact]
    public void Compile_WhenFromNodeHasEmptyAlias_ShouldNotAppendAsClause()
    {
        var node = new FromNode("t", "");
        var query = new SelectQuery<DummyEntity>().AddNode(node);
        GetSql(query).Should().Be("SELECT * FROM \"t\"");
    }

    [Fact]
    public void Compile_WhenFromNodeHasAlias_ShouldRenderAsClause()
    {
        var query = new SelectQuery<DummyEntity>().AddNode(new FromNode("table", "t"));
        GetSql(query).Should().Be("SELECT * FROM \"table\" AS \"t\"");
    }

    [Fact]
    public void Compile_WhenRawOrderByIsDescending_ShouldAppendDescKeyword()
    {
        var node = new RawOrderByNode("id", true);
        var query = new SelectQuery<DummyEntity>().AddNode(node);
        GetSql(query).Should().Be("SELECT * ORDER BY id DESC");
    }

    [Fact]
    public void Compile_WhenOrderByExpressionIsNotMemberAccess_ShouldRenderOnlyOrderByClause()
    {
        var query = new SelectQuery<DummyEntity>().OrderBy(x => 1);
        GetSql(query).Should().Be("SELECT * ORDER BY");
    }

    [Fact]
    public void Compile_WhenOrderByNodeHasProperty_ShouldRenderOrderByClause()
    {
        var query = new SelectQuery<DummyEntity>().From("DummyEntity").AddNode(new OrderByNode((DummyEntity x) => x.Name, false));
        GetSql(query).Should().Be("SELECT * FROM \"DummyEntity\" ORDER BY \"name\"");
    }

    [Fact]
    public void Compile_WhenRawWhereNodeHasNullParameters_ShouldRenderWhereClause()
    {
        var query = new SelectQuery<DummyEntity>().From("DummyEntity").AddNode(new RawWhereNode("1=1", null, false));
        var result = new PostgreSqlCompiler().Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"DummyEntity\" WHERE 1=1");
    }

    [Fact]
    public void Compile_WhenMultipleWhereNodesWithOr_ShouldRenderCorrectLogicalOperators()
    {
        var w1 = new RawWhereNode("id = 1", null, false);
        var w2 = new RawWhereNode("id = 2", null, true);
        var w3 = new ExpressionWhereNode((DummyEntity x) => x.Name == "a", false);
        var w4 = new ExpressionWhereNode((DummyEntity x) => x.Name == "b", true);
        var query = new SelectQuery<DummyEntity>().From("t").AddNode(w1).AddNode(w2).AddNode(w3).AddNode(w4);
        GetSql(query).Should().Be("SELECT * FROM \"t\" WHERE id = 1 OR id = 2 AND (name = @p0) OR (name = @p1)");
    }

    [Fact]
    public void CompileSelect_SelectNode_RemovesSelectPrefix()
    {
        var q = new SelectQuery<DummyEntity>().From("t").Select(x => x.Id);
        var sql = GetSql(q);
        sql.Should().NotContain("SELECT SELECT");
        sql.Should().StartWith("SELECT ");
    }

    [Fact]
    public void CompileSelect_SelectNode_WithTrailingSpace_IsTrimmed()
    {
        var q = new SelectQuery<DummyEntity>().From("t").Select(x => x.Id);
        var sql = GetSql(q);
        sql.Should().NotContain("  FROM");
    }

    [Fact]
    public void CompileWheres_FirstNode_AppendsWhere()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode> { new RawWhereNode("id = 1", null, false) };
        c.CompileWheres(nodes, v, ctx);
        ctx.Sql.ToString().Should().StartWith("WHERE ");
    }

    [Fact]
    public void CompileWheres_SecondNodeRawOr_AppendsOr()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode>
        {
            new RawWhereNode("a = 1", null, false),
            new RawWhereNode("b = 2", null, true)
        };
        c.CompileWheres(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain("OR b = 2");
    }

    [Fact]
    public void CompileWheres_SecondNodeRawAnd_AppendsAnd()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode>
        {
            new RawWhereNode("a = 1", null, false),
            new RawWhereNode("b = 2", null, false)
        };
        c.CompileWheres(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain("AND b = 2");
    }

    [Fact]
    public void CompileWheres_ExpressionWhereOr_AppendsOr()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode>
        {
            new RawWhereNode("a = 1", null, false),
            new ExpressionWhereNode((DummyEntity x) => x.Id == 5, true)
        };
        c.CompileWheres(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain("OR ");
    }

    [Fact]
    public void CompileWheres_ExpressionWhereAnd_AppendsAnd()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode>
        {
            new RawWhereNode("a = 1", null, false),
            new ExpressionWhereNode((DummyEntity x) => x.Id == 5, false)
        };
        c.CompileWheres(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain("AND ");
    }

    [Fact]
    public void CompileWheres_ExistsWhereOr_AppendsOr()
    {
        var (c, ctx, v) = CreateCtx();
        var subq = new SelectQuery<DummyEntity>().From("other").Where(x => x.Id == 1);
        var nodes = new List<ISqlNode>
        {
            new RawWhereNode("a = 1", null, false),
            new ExistsWhereNode(subq, IsNot: false, IsOr: true)
        };
        c.CompileWheres(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain("OR EXISTS");
    }

    [Fact]
    public void CompileWheres_NonWhereNode_IsSkipped()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode>
        {
            new FromNode("t", null),
            new RawWhereNode("id = 1", null, false)
        };
        c.CompileWheres(nodes, v, ctx);
        var sql = ctx.Sql.ToString();
        sql.Should().StartWith("WHERE ");
        sql.Should().NotContain("FROM");
    }

    [Fact]
    public void CompileOrderBys_SingleNode_AppendsOrderBy()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode> { new RawOrderByNode("id", false) };
        c.CompileOrderBys(nodes, v, ctx);
        ctx.Sql.ToString().Should().StartWith("ORDER BY ");
    }

    [Fact]
    public void CompileOrderBys_TwoNodes_AppendsComma()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode>
        {
            new RawOrderByNode("id", false),
            new RawOrderByNode("name", true)
        };
        c.CompileOrderBys(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain(", ");
    }

    [Fact]
    public void CompileOrderBys_AfterIteration_AppendsTrailingSpace()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode> { new RawOrderByNode("id", false) };
        c.CompileOrderBys(nodes, v, ctx);
        ctx.Sql.ToString().Should().EndWith(" ");
    }

    [Fact]
    public void CompileOrderBys_EmptyNodes_NoTrailingSpace()
    {
        var (c, ctx, v) = CreateCtx();
        c.CompileOrderBys(new List<ISqlNode>(), v, ctx);
        ctx.Sql.ToString().Should().Be("");
    }

    [Fact]
    public void SqlCompilerVisitor_VisitUnknown_ThrowsNotSupportedWithTypeName()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var compiler = new DefaultCompiler();
        var visitor = new SqlCompilerVisitor(compiler, ctx);

        var unknownNode = new UnknownTestNode();
        Action act = () => visitor.VisitUnknown(unknownNode);
        act.Should().Throw<NotSupportedException>()
           .WithMessage("*UnknownTestNode*");
    }

    [Fact]
    public void AppendRaw_WithNullParameters_AppendsRawConditionDirectly()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var compiler = new DefaultCompiler();
        var visitor = new SqlCompilerVisitor(compiler, ctx);
        var node = new RawWhereNode("id = 1", null, false);
        visitor.Visit(node);
        ctx.Sql.ToString().Should().Be("id = 1 ");
    }

    [Fact]
    public void AppendRaw_WithParameters_FormatsParameters()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var compiler = new DefaultCompiler();
        var visitor = new SqlCompilerVisitor(compiler, ctx);
        var node = new RawWhereNode("id = {0}", new object[] { 42 }, false);
        visitor.Visit(node);
        ctx.Sql.ToString().Should().Contain("id = @p0 ");
    }

    [Fact]
    public void Visit_SelectNode_AppendsSelectKeyword()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new SelectNode(new[] { "id" }, false));
        ctx.Sql.ToString().Should().StartWith("SELECT ");
    }

    [Fact]
    public void Visit_SelectNode_Distinct_AppendsDistinct()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new SelectNode(new[] { "id" }, true));
        ctx.Sql.ToString().Should().Contain("DISTINCT ");
    }

    [Fact]
    public void Visit_SelectNode_EmptyColumns_AppendsStar()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new SelectNode(Array.Empty<string>(), false));
        ctx.Sql.ToString().Should().Contain("*");
    }

    [Fact]
    public void Visit_SelectNode_MultipleColumns_InsertsComma()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new SelectNode(new[] { "id", "name" }, false));
        ctx.Sql.ToString().Should().Be("SELECT \"id\", \"name\" ");
    }

    [Fact]
    public void Visit_SelectNode_TrailingSpace()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new SelectNode(new[] { "id" }, false));
        ctx.Sql.ToString().Should().EndWith(" ");
    }

    [Fact]
    public void Visit_RawSelectNode_AppendsSelectAndRaw()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new RawSelectNode("COUNT(*)", null, false));
        ctx.Sql.ToString().Should().Be("SELECT COUNT(*)");
    }

    [Fact]
    public void Visit_RawSelectNode_Distinct_AppendsDistinct()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new RawSelectNode("COUNT(*)", null, true));
        ctx.Sql.ToString().Should().Contain("SELECT DISTINCT");
    }

    [Fact]
    public void Visit_ExpressionSelectNode_NullLambdaBody_AppendsStar()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new ExpressionSelectNode((DummyEntity x) => new DummyEntity(), false));
        ctx.Sql.ToString().Should().Contain("*");
    }

    [Fact]
    public void Visit_RawSelectNode_SelectKeywordIsExact()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new RawSelectNode("1", null, false));
        ctx.Sql.ToString().Should().StartWith("SELECT ");
        ctx.Sql.ToString().Should().NotStartWith("SELECTX");
    }

    [Fact]
    public void Visit_ExpressionWhereNode_AppendsTrailingSpace()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new ExpressionWhereNode((DummyEntity x) => x.Id == 1, false));
        ctx.Sql.ToString().Should().EndWith(" ");
    }

    [Fact]
    public void Visit_RawWhereNode_AppendsTrailingSpace()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        visitor.Visit(new RawWhereNode("x = 1", null, false));
        ctx.Sql.ToString().Should().EndWith(" ");
    }
}




