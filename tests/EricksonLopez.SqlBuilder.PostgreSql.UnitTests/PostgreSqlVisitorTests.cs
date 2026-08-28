// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing.Domain;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class PostgreSqlVisitorTests
{
    [Fact]
    public void VisitUnknown_CallsBaseAndThrows()
    {
        var compiler = new PostgreSqlCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new PostgreSqlVisitor(compiler, context);

        var node = Substitute.For<ISqlNode>();
        Action act = () => visitor.VisitUnknown(node);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void VisitDistinctOnNode_ShouldAppendDistinctOn()
    {
        var compiler = new PostgreSqlCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new PostgreSqlVisitor(compiler, context);

        var node = new DistinctOnNode(new[] { "col1", "col2" });
        visitor.Visit(node);

        context.Sql.ToString().Should().Be("DISTINCT ON (\"col1\", \"col2\") ");
    }

    [Fact]
    public void VisitWindowFunctionNode_WithFilter_ShouldAppendFilterClause()
    {
        var compiler = new PostgreSqlCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new PostgreSqlVisitor(compiler, context);

        var node = new WindowFunctionNode(
            "SUM",
            "amount",
            null,
            null,
            new[] { "dept" },
            new[] { "salary" },
            new[] { true },
            "total",
            FilterRaw: "status = {0}",
            FilterRawArgs: new object?[] { "active" }
        );

        visitor.Visit(node);

        context.Sql.ToString().Should().Be("SUM(\"amount\") FILTER (WHERE status = @p0) OVER (PARTITION BY \"dept\" ORDER BY \"salary\" DESC) AS \"total\"");
        context.Parameters.GetParameters()["p0"].Should().Be("active");
    }

    [Fact]
    public void VisitSubqueryJoinNode_Lateral_WithOnCondition_AppendsOnClause()
    {
        var compiler = new PostgreSqlCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new PostgreSqlVisitor(compiler, context);

        var subquery = Sql.From<User>().Select("Id");
        var node = new SubqueryJoinNode(JoinType.Left, (IAstQuery)subquery, "sub", OnCondition: "t.id = sub.id", IsLateral: true);
        visitor.Visit(node);

        context.Sql.ToString().Should().Be("LEFT JOIN LATERAL (SELECT \"Id\" FROM \"users\") AS \"sub\" ON t.id = sub.id ");
    }

    [Fact]
    public void VisitSubqueryJoinNode_Lateral_WithExpressionCondition_AppendsOnClauseWithTrailingSpace()
    {
        var compiler = new PostgreSqlCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new PostgreSqlVisitor(compiler, context);

        var subquery = Sql.From<User>().Select("Id");
        var expr = System.Linq.Expressions.Expression.Lambda<Func<User, bool>>(
            System.Linq.Expressions.Expression.Equal(
                System.Linq.Expressions.Expression.Property(
                    System.Linq.Expressions.Expression.Parameter(typeof(User), "x"),
                    nameof(User.Id)),
                System.Linq.Expressions.Expression.Constant(10)),
            System.Linq.Expressions.Expression.Parameter(typeof(User), "x"));

        var node = new SubqueryJoinNode(JoinType.Cross, (IAstQuery)subquery, "sub", ExpressionCondition: expr, IsLateral: true);
        visitor.Visit(node);

        context.Sql.ToString().Should().Be("CROSS JOIN LATERAL (SELECT \"Id\" FROM \"users\") AS \"sub\" ON (id = @p0) ");
        context.Parameters.GetParameters()["p0"].Should().Be(10);
    }
}


