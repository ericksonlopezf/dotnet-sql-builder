// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.SqlServer.Tests;

public class SqlServerCompilerTestsExtended
{
    private readonly SqlServerCompiler _compiler = new();
    
    [Fact]
    public void Compile_SelectNodes_AllTypes()
    {
        var query = Substitute.For<IAstQuery>();
        Expression<Func<TestEntity, object>> expr = x => new { x.Id, x.Name };
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "col1" }, false),
            new ExpressionSelectNode(expr, false),
            new RawSelectNode("MAX(col2)", null, true)
        }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT DISTINCT MAX(col2)");
    }
    
    [Fact]
    public void Compile_JoinNodes_AllTypes()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new JoinNode(JoinType.Inner, "table1", "t1", "t1.id = u.id", null),
            new JoinNode(JoinType.Left, "table2", null, null, Expression.Constant(true)),
            new RawJoinNode("CROSS JOIN table3", null)
        }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * INNER JOIN [table1] AS [t1] ON t1.id = u.id LEFT JOIN [table2] ON @p0 CROSS JOIN table3");
    }

    [Fact]
    public void Compile_HavingNodes_AllTypes()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new RawHavingNode("COUNT(1) > 1", null, false),
            new ExpressionHavingNode(Expression.Constant(true), true)
        }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * HAVING COUNT(1) > 1 OR @p0");
    }

    [Fact]
    public void Compile_OrderByNodes_AllTypes()
    {
        var query = Substitute.For<IAstQuery>();
        Expression<Func<TestEntity, object>> expr = x => x.Id;
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new OrderByNode(expr, true),
            new RawOrderByNode("status", false, null)
        }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * ORDER BY [id] DESC, status");
    }

    [Fact]
    public void Compile_SubqueryAndCte()
    {
        var subQuery = Substitute.For<IAstQuery>();
        subQuery.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());
        
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new SubqueryFromNode(subQuery, "sub"),
            new CteNode("cte1", subQuery),
            new WindowNode("win1", new[] { "col" }, new[] { "col DESC" })
        }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("WITH [cte1] AS (SELECT 1) SELECT * FROM (SELECT 1) AS [sub] WINDOW [win1] AS (PARTITION BY [col] ORDER BY [col] DESC)");
    }
    
    [Fact]
    public void Compile_SetOperation()
    {
        var subQuery = Substitute.For<IAstQuery>();
        subQuery.Nodes.Returns(new ISqlNode[] { new RawSelectNode("2", null, false) }.ToImmutableList());
        
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new SetOperationNode("UNION", subQuery)
        }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * UNION SELECT 2");
    }
    
    [Fact]
    public void Compile_UpdateSetNodes()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new SetNode("status", 1, null, null),
            new SetNode(null, null, "count = count + 1", null)
        }.ToImmutableList());
        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("UPDATE [users] SET [status] = @p0, count = count + 1");
    }
    
    [Fact]
    public void Compile_RawQuery()
    {
        var query = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "p0", 1 } });
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT 1");
        result.Parameters.Should().ContainKey("p0");
    }
        [Fact]
    public void Compile_LimitAndOffset_Various()
    {
        var query1 = Substitute.For<IAstQuery>();
        query1.Nodes.Returns(new ISqlNode[] { new LimitOffsetNode(10, null) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)query1).Sql.Should().Contain("FETCH NEXT 10");

        var query2 = Substitute.For<IAstQuery>();
        query2.Nodes.Returns(new ISqlNode[] { new LimitOffsetNode(null, 20) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)query2).Sql.Should().Contain("OFFSET 20");
    }

        [Fact]
    public void Compile_InsertWithReturningColumns()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new InsertNode("Users", new[] { "Name" }), new ValuesNode(new[] { new object[] { "A" } }), new ReturningNode(new[] { "Id", "Name" }) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)query).Sql.Should().Contain("OUTPUT INSERTED.[Id], INSERTED.[Name]");

        var query2 = Substitute.For<IAstQuery>();
        query2.Nodes.Returns(new ISqlNode[] { new InsertNode("Users", new[] { "Name" }), new DefaultValuesNode(), new ReturningNode(Array.Empty<string>()) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)query2).Sql.Should().Contain("OUTPUT INSERTED.*");
    }

    [Fact]
    public void Compile_UpdateWithReturningColumns()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new UpdateNode("Users"), new SetNode("Name", "A"), new ReturningNode(new[] { "Id", "Name" }), new RawJoinNode("JOIN table ON 1=1") }.ToImmutableList());
        _compiler.Compile((ISqlQuery)query).Sql.Should().Contain("OUTPUT INSERTED.[Id], INSERTED.[Name]");

        var query2 = Substitute.For<IAstQuery>();
        query2.Nodes.Returns(new ISqlNode[] { new UpdateNode("Users"), new SetNode("Name", "A"), new ReturningNode(Array.Empty<string>()) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)query2).Sql.Should().Contain("OUTPUT INSERTED.*");
    }

    [Fact]
    public void Compile_DeleteWithReturningColumns()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new DeleteNode("Users"), new ReturningNode(new[] { "Id", "Name" }) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)query).Sql.Should().Contain("OUTPUT DELETED.[Id], DELETED.[Name]");

        var query2 = Substitute.For<IAstQuery>();
        query2.Nodes.Returns(new ISqlNode[] { new DeleteNode("Users"), new ReturningNode(Array.Empty<string>()) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)query2).Sql.Should().Contain("OUTPUT DELETED.*");
    }

    [Fact]
    public void Compile_UpdateWithFromNode()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("Users"),
            new SetNode("Name", "A"),
            new FromNode("table", null)
        }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Contain("FROM [table]");
    }

    [Fact]
    public void Compile_DeleteWithFromAndJoinNodes()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("Users"),
            new FromNode("Users", "u"),
            new RawJoinNode("JOIN X ON 1=1")
        }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Contain("FROM [Users] AS [u] JOIN X ON 1=1");
    }

    [Fact]
    public void Compile_LimitAndOffset_EmptyLimitNodes()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new LimitOffsetNode(null, null) }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Be("SELECT * OFFSET 0 ROWS");
    }

    [Fact]
    public void Compile_InsertWithEmptyColumns_ExactMatch()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new InsertNode("Users", Array.Empty<string>()) }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Be("INSERT INTO [Users]");
    }

    [Fact]
    public void Compile_UpdateWithRawAndSubqueryJoin_ExactMatch()
    {
        var query = Substitute.For<IAstQuery>();
        var subq = Substitute.For<IAstQuery>();
        subq.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());
        subq.Build(Arg.Any<EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler>()).Returns(new EricksonLopez.SqlBuilder.Abstractions.SqlResult("SELECT 1", null));
        query.Nodes.Returns(new ISqlNode[] {
            new UpdateNode("Users"),
            new SetNode("Age", "30", null),
            new RawJoinNode("JOIN A ON B", null),
            new SubqueryJoinNode(JoinType.Inner, subq, "SQ", "C = D")
        }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Be("UPDATE [Users] SET [Age] = @p0 JOIN A ON B INNER JOIN (SELECT 1) AS [SQ] ON C = D");
    }

    [Fact]
    public void Compile_Delete_ExactMatch()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new DeleteNode("Users") }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Be("DELETE FROM [Users]");
    }

    [Fact]
    public void RenderUpdate_MultipleWhereColumns_ShouldGenerateAnd()
    {
        var entity = new EricksonLopez.SqlBuilder.Testing.ThreeColumnEntity { Id = "1", Name = "Test", Status = "A" };
        var setMask = new[] { false, false, false }.AsSpan();
        var whereMask = new[] { false, true, true }.AsSpan();
        
        var result = _compiler.RenderUpdate(entity, setMask, whereMask);
        
        result.Sql.Should().Be("UPDATE [TestEntity] SET  OUTPUT INSERTED.* WHERE [Name] = @p0 AND [Status] = @p1");
        result.Parameters.Should().ContainKey("p0");
        result.Parameters.Should().ContainKey("p1");
    }

    [Fact]
    public void Compile_Insert_WithAllNodes_ExactMatch()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] {
            new InsertNode("Users", new[] { "Name" }),
            new ReturningNode(new[] { "Id", "Name" }),
            new DefaultValuesNode()
        }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Be("INSERT INTO [Users] ([Name]) OUTPUT INSERTED.[Id], INSERTED.[Name] DEFAULT VALUES");
    }
    
    [Fact]
    public void Compile_Insert_WithValuesNode_ExactMatch()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] {
            new InsertNode("Users", new[] { "Name" }),
            new ValuesNode(new[] { new object[] { 1 } })
        }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Be("INSERT INTO [Users] ([Name]) VALUES (@p0)");
    }

    [Fact]
    public void Compile_Update_WithAllNodes_ExactMatch()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] {
            new UpdateNode("Users"),
            new SetNode("Age", "30", null),
            new ReturningNode(new[] { "Id" }),
            new FromNode("Users", "U"),
            new RawJoinNode("JOIN A ON B", null),
            new RawWhereNode("Id = 1", null, false)
        }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Be("UPDATE [Users] SET [Age] = @p0 OUTPUT INSERTED.[Id] FROM [Users] AS [U] JOIN A ON B WHERE Id = 1");
    }
    
    [Fact]
    public void Compile_Delete_WithAllNodes_ExactMatch()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] {
            new DeleteNode("Users"),
            new ReturningNode(Array.Empty<string>()),
            new FromNode("Users", "U"),
            new RawJoinNode("JOIN A ON B", null),
            new RawWhereNode("Id = 1", null, false)
        }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Should().Be("DELETE FROM [Users] OUTPUT DELETED.* FROM [Users] AS [U] JOIN A ON B WHERE Id = 1");
    }
}





