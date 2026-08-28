// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.MySql.Tests;

public class MySqlCompilerTests
{
    private readonly MySqlCompiler _compiler = new();

    [Fact]
    public void Compile_SelectQuery_WithDistinct_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id", "name" }, true),
            new FromNode("users", "u"),
            new LimitOffsetNode(10, 20)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT DISTINCT `id`, `name` FROM `users` AS `u` LIMIT 10 OFFSET 20");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Compile_SelectQuery_WithNoColumns_UsesAsterisk()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT *");
    }

    [Fact]
    public void Compile_InsertQuery_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", Array.Empty<string>()),
            new ValuesNode(new[] { new object[] { 1, "test" } })
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("INSERT INTO `users` VALUES (@p0, @p1)");
        result.Parameters.Count.Should().Be(2);
        result.Parameters["p0"].Should().Be(1);
        result.Parameters["p1"].Should().Be("test");
    }

    [Fact]
    public void Compile_UpdateQuery_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("UPDATE `users`");
    }

    [Fact]
    public void Compile_DeleteQuery_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("users")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("DELETE FROM `users`");
    }

    [Fact]
    public void Compile_WithWhereClauses_BuildsCorrectSql()
    {
        Expression<Func<TestEntity, bool>> expr = x => x.Id == 1;

        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", null),
            new RawWhereNode("status = 1", null, false),
            new ExpressionWhereNode(expr.Body, true) // OR x.Id == 1
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT * FROM `users` WHERE status = 1 OR (id = @p0)");
        result.Parameters.Should().ContainSingle();
        result.Parameters["p0"].Should().Be(1);
    }

    [Fact]
    public void Compile_WithAndWhereClauses_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", null),
            new RawWhereNode("status = 1", null, false),
            new RawWhereNode("type = 2", null, false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT * FROM `users` WHERE status = 1 AND type = 2");
    }

    [Fact]
    public void Compile_WithOrRawWhereClause_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", null),
            new RawWhereNode("status = 1", null, false),
            new RawWhereNode("type = 2", null, true)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT * FROM `users` WHERE status = 1 OR type = 2");
    }

    [Fact]
    public void Compile_WithAndExpressionWhereClause_BuildsCorrectSql()
    {
        Expression<Func<TestEntity, bool>> expr = x => x.Id == 1;
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", null),
            new RawWhereNode("status = 1", null, false),
            new ExpressionWhereNode(expr.Body, false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT * FROM `users` WHERE status = 1 AND (id = @p0)");
    }
    
    [Fact]
    public void Compile_NonAstQuery_ReturnsEmptyResult()
    {
        var query = Substitute.For<ISqlQuery>();
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("");
    }
    
    [Fact]
    public void Visit_UnknownNode_DoesNothing()
    {
        var query = Substitute.For<IAstQuery>();
        var unknownNode = Substitute.For<ISqlNode>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", null),
            unknownNode
        }.ToImmutableList());
        
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * FROM `users`");
    }

    [Fact]
    public void Compile_InsertQuery_WithoutValuesNode()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new InsertNode("users", Array.Empty<string>()) }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("INSERT INTO `users`");
    }

    [Fact]
    public void Compile_UnknownQueryType_DoesNotThrow()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { 
            new LimitOffsetNode(10, null),
            new LimitOffsetNode(null, 20)
        }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * LIMIT 10 OFFSET 20");
    }

    [Fact]
    public void Compile_OrderByNode_IsIgnoredForNow()
    {
        var query = Substitute.For<IAstQuery>();
        Expression<Func<TestEntity, object>> expr = x => x.Id;
        query.Nodes.Returns(new ISqlNode[] { new OrderByNode(expr, false) }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * ORDER BY `id`");
    }

    [Fact]
    public void Compile_ReturningNode_ThrowsNotSupportedException()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] 
        { 
            new InsertNode("users", Array.Empty<string>()),
            new ReturningNode(Array.Empty<string>()) 
        }.ToImmutableList());
        Action act = () => _compiler.Compile((ISqlQuery)query);
        
        act.Should().Throw<NotSupportedException>()
           .WithMessage("*RETURNING clause is not natively supported in MySQL 8.x*");
    }

    [Fact]
    public void Compile_WindowFunction_WithFilter_ThrowsNotSupportedException()
    {
        var node = new WindowFunctionNode(
            "SUM", "Amount", null, null,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(),
            "sum_val", FilterRaw: "Status = 'Active'");

        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new FromNode("users"), node }.ToImmutableList());

        Action act = () => _compiler.Compile((ISqlQuery)query);
        
        act.Should().Throw<NotSupportedException>()
           .WithMessage("*MySQL does not support the FILTER (WHERE ...) clause on window functions*");
    }
}




