// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
/// Edge-case and branch coverage tests for DML query compilation: INSERT, UPDATE, DELETE, MERGE, ON CONFLICT, RETURNING, UNNEST and CompositeCursor.
/// </summary>
public class DmlCompilationEdgeTests
{
    private class DefaultCompiler : SqlCompilerBase
    {
        protected override ISqlRenderer AotRenderer => null!;
        public override string EscapeIdentifier(string identifier) => $"\"{identifier}\"";
        public new void CompileInsert(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext ctx)
            => base.CompileInsert(nodes, visitor, ctx);
        public new void CompileUpdate(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext ctx)
            => base.CompileUpdate(nodes, visitor, ctx);
        public new void CompileDelete(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext ctx)
            => base.CompileDelete(nodes, visitor, ctx);
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
    public void Compile_WhenUpdateNodeHasNoColumns_ShouldRenderOnlyUpdateTable()
    {
        var node = new UpdateNode("t");
        var query = new UpdateQuery<DummyEntity>().AddNode(node);
        GetSql(query).Should().Be("UPDATE \"t\"");
    }

    [Fact]
    public void Compile_WhenOnConflictHasEmptyColumns_ShouldRenderDoNothingWithoutTarget()
    {
        var node = new OnConflictNode(Array.Empty<string>(), "DO NOTHING", null, null);
        var query = new InsertQuery<DummyEntity>().Into("t").AddNode(node);
        GetSql(query).Should().Be("INSERT INTO \"t\" ON CONFLICT DO NOTHING");
    }

    [Fact]
    public void Compile_WhenOnConflictHasUpdateExpression_ShouldRenderDoUpdateSet()
    {
        var expr = (Expression<Func<DummyEntity, bool>>)(x => x.Name == "test");
        var node = new OnConflictNode(new[] { "id" }, "DO UPDATE SET", expr);
        var query = new InsertQuery<DummyEntity>().Into("t").AddNode(node);
        GetSql(query).Should().Be("INSERT INTO \"t\" ON CONFLICT (\"id\") DO UPDATE SET (name = @p0)");
    }

    [Fact]
    public void Compile_WhenInsertOnConflictHasExpression_ShouldNotHaveTrailingSpace()
    {
        Expression<Func<DummyEntity, bool>> expr = x => x.Id == 1;
        var query = new InsertQuery<DummyEntity>()
            .AddNode(new InsertNode("DummyEntity", new[] { "Id" }))
            .AddNode(new OnConflictNode(new[] { "id" }, "DO UPDATE SET Name = @p0", expr, new object[] { "test" }));
        var result = new PostgreSqlCompiler().Compile(query);
        result.Sql.EndsWith(' ').Should().BeFalse();
    }

    [Fact]
    public void Compile_WhenReturningNodeHasEmptyColumns_ShouldAppendAsterisk()
    {
        var node = new ReturningNode(Array.Empty<string>());
        var query = new DeleteQuery<DummyEntity>().AddNode(new DeleteNode("t")).AddNode(node);
        GetSql(query).Should().Be("DELETE FROM \"t\" RETURNING *");
    }

    [Fact]
    public void Compile_WhenDeleteQueryHasWhereClause_ShouldRenderWhereClause()
    {
        var query = new DeleteQuery<DummyEntity>()
            .AddNode(new DeleteNode("DummyEntity"))
            .AddNode(new ExpressionWhereNode((DummyEntity x) => x.Id == 1, false));
        GetSql(query).Should().Be("DELETE FROM \"DummyEntity\" WHERE (id = @p0)");
    }

    [Fact]
    public void Compile_WhenDeleteQueryHasReturningClause_ShouldRenderReturningClause()
    {
        var query = new DeleteQuery<DummyEntity>()
            .AddNode(new DeleteNode("DummyEntity"))
            .AddNode(new ReturningNode(new[] { "Id" }));
        GetSql(query).Should().Be("DELETE FROM \"DummyEntity\" RETURNING \"Id\"");
    }

    [Fact]
    public void Compile_WhenUnnestNodeHasMultipleArrays_ShouldRenderUnnestFunction()
    {
        var node = new UnnestNode(new object[] { new[] { 1 }, new[] { 2 } }, "alias");
        var query = new SelectQuery<DummyEntity>().AddNode(node);
        GetSql(query).Should().Be("SELECT * FROM UNNEST(@p0, @p1) AS \"alias\"");
    }

    [Fact]
    public void Compile_WhenUnnestNodeFollowedByWhere_ShouldRenderSpaceBeforeWhere()
    {
        var query = new SelectQuery<DummyEntity>()
            .AddNode(new UnnestNode(new object[] { new[] { 1, 2 } }, "alias"))
            .AddNode(new ExpressionWhereNode((DummyEntity x) => x.Id == 1, false));
        GetSql(query).Should().Be("SELECT * FROM UNNEST(@p0) AS \"alias\" WHERE (id = @p1)");
    }

    [Fact]
    public void CompileUpdate_NoSetNodes_DoesNotAppendSet()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode> { new UpdateNode("t") };
        c.CompileUpdate(nodes, v, ctx);
        ctx.Sql.ToString().Should().NotContain("SET ");
    }

    [Fact]
    public void CompileUpdate_WithSetNodeOnly_AppendsSet()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode> { new UpdateNode("t"), new SetNode("name", "x") };
        c.CompileUpdate(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain("SET ");
    }

    [Fact]
    public void CompileUpdate_WithTokenNodeOnly_AppendsSet()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode>
        {
            new UpdateNode("t"),
            new ConcurrencyTokenNode("Version", ExpectedValue: 1, AutoIncrement: true)
        };
        c.CompileUpdate(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain("SET ");
    }

    [Fact]
    public void CompileUpdate_SetKeyword_IsExactlySet()
    {
        var (c, ctx, v) = CreateCtx();
        var nodes = new List<ISqlNode> { new UpdateNode("t"), new SetNode("x", 1) };
        c.CompileUpdate(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain("SET ");
        ctx.Sql.ToString().Should().NotContain("SETX");
    }

    [Fact]
    public void Compile_WhenUpdateQueryHasMultipleSetNodes_ShouldRenderCommaSeparatedAssignments()
    {
        var s1 = new SetNode("name", "test");
        var s2 = new SetNode(null, null, "age = 20");
        var query = new UpdateQuery<DummyEntity>().AddNode(new UpdateNode("t")).AddNode(s1).AddNode(s2);
        GetSql(query).Should().Be("UPDATE \"t\" SET \"name\" = @p0, age = 20");
    }

    [Fact]
    public void Compile_WhenUpdateQueryHasFromAndJoin_ShouldRenderFromAndJoinClauses()
    {
        var query = new UpdateQuery<DummyEntity>().AddNode(new UpdateNode("t")).AddNode(new SetNode("name", "x"))
            .AddNode(new FromNode("t2", null))
            .AddNode(new JoinNode(JoinType.Inner, "t3", null, "t.id = t3.id"));
        GetSql(query).Should().Be("UPDATE \"t\" SET \"name\" = @p0 FROM \"t2\" INNER JOIN \"t3\" ON t.id = t3.id");
    }

    [Fact]
    public void CompileSelect_ReturningOnInsert_AppendsSpaceBeforeReturning()
    {
        var (c, ctx, v) = CreateCtx();
        ctx.Sql.Append("INSERT INTO \"t\" (\"id\")");
        var nodes = new List<ISqlNode> { new ReturningNode(new[] { "id" }) };
        c.CompileInsert(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain(" RETURNING");
    }

    [Fact]
    public void CompileInsert_NoTrailingSpaceOnContext_AppendsSpaceBeforeReturning()
    {
        var (c, ctx, v) = CreateCtx();
        ctx.Sql.Append("SOMETHING");
        var nodes = new List<ISqlNode> { new ReturningNode(new[] { "id" }) };
        c.CompileInsert(nodes, v, ctx);
        ctx.Sql.ToString().Should().Contain("SOMETHING RETURNING");
    }

    [Fact]
    public void CompileInsert_WithTrailingSpaceOnContext_DoesNotDoubleSpace()
    {
        var (c, ctx, v) = CreateCtx();
        ctx.Sql.Append("SOMETHING ");
        var nodes = new List<ISqlNode> { new ReturningNode(new[] { "id" }) };
        c.CompileInsert(nodes, v, ctx);
        ctx.Sql.ToString().Should().NotContain("SOMETHING  RETURNING");
    }

    [Fact]
    public void Visit_CompositeCursorNode_AscendingIsAfter_UsesGreaterThan()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        var keys = new[] { new CursorKey("id", 5, false) };
        var node = new CompositeCursorNode(keys, IsAfter: true);
        visitor.Visit(node);
        ctx.Sql.ToString().Should().Contain("\"id\" > @p0");
    }

    [Fact]
    public void Visit_CompositeCursorNode_AscendingIsBeforeNotAfter_UsesLessThan()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        var keys = new[] { new CursorKey("id", 5, false) };
        var node = new CompositeCursorNode(keys, IsAfter: false);
        visitor.Visit(node);
        ctx.Sql.ToString().Should().Contain("\"id\" < @p0");
    }

    [Fact]
    public void Visit_CompositeCursorNode_DescendingIsAfter_UsesLessThan()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        var keys = new[] { new CursorKey("id", 5, true) };
        var node = new CompositeCursorNode(keys, IsAfter: true);
        visitor.Visit(node);
        ctx.Sql.ToString().Should().Contain("\"id\" < @p0");
    }

    [Fact]
    public void Visit_CompositeCursorNode_DescendingNotAfter_UsesGreaterThan()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        var keys = new[] { new CursorKey("id", 5, true) };
        var node = new CompositeCursorNode(keys, IsAfter: false);
        visitor.Visit(node);
        ctx.Sql.ToString().Should().Contain("\"id\" > @p0");
    }

    [Fact]
    public void Visit_CompositeCursorNode_TwoKeys_RecursiveOr()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        var keys = new[]
        {
            new CursorKey("a", 1, false),
            new CursorKey("b", 2, false)
        };
        var node = new CompositeCursorNode(keys, IsAfter: true);
        visitor.Visit(node);
        ctx.Sql.ToString().Should().Be("(\"a\" > @p0 OR (\"a\" = @p0 AND \"b\" > @p1))");
    }

    [Fact]
    public void Visit_CompositeCursorNode_NullKeys_DoesNotThrow()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        var node = new CompositeCursorNode(null!, IsAfter: true);
        Action act = () => visitor.Visit(node);
        act.Should().NotThrow();
        ctx.Sql.ToString().Should().Be("");
    }

    [Fact]
    public void Visit_CompositeCursorNode_EmptyKeys_ProducesEmptySql()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(new DefaultCompiler(), ctx);
        var node = new CompositeCursorNode(Array.Empty<CursorKey>(), IsAfter: true);
        visitor.Visit(node);
        ctx.Sql.ToString().Should().Be("");
    }

    [Fact]
    public void Compile_RawQuery_WithNoParams_AppendsRawSqlOnly()
    {
        var q = new RawQuery("SELECT 1 FROM dual", null);
        var result = new DefaultCompiler().Compile(q);
        result.Sql.Should().Be("SELECT 1 FROM dual");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Compile_RawQuery_WithParams_AppendsParams()
    {
        var dict = new Dictionary<string, object?> { { "@p0", 42 } };
        var q = new RawQuery("SELECT @p0", dict);
        var result = new DefaultCompiler().Compile(q);
        result.Sql.Should().Contain("SELECT");
        result.Parameters.Should().NotBeEmpty();
    }
}



