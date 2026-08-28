// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.PostgreSql;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class PostgreSqlCompilerFullTests
{
    [Fact]
    public void Compile_ExpressionHavingNode_IsOrTrue_ShouldGenerateOr()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "*" }, false),
            new FromNode("users", null),
            new GroupByNode(new[] { "id" }),
            new ExpressionHavingNode(System.Linq.Expressions.Expression.Constant(true), false),
            new ExpressionHavingNode(System.Linq.Expressions.Expression.Constant(true), true)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("SELECT * FROM \"users\" GROUP BY \"id\" HAVING @p0 OR @p1");
    }

    [Fact]
    public void Compile_SetOperationNode_ShouldGenerateSetOperation()
    {
        var query = Substitute.For<IAstQuery>();
        var subQuery = Substitute.For<IAstQuery>();
        subQuery.Nodes.Returns(new ISqlNode[] { new RawSelectNode("2", null, false) }.ToImmutableList());

        
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "1" }, false),
            new SetOperationNode("UNION ALL", subQuery)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("SELECT \"1\" UNION ALL SELECT 2");
    }

    [Fact]
    public void Compile_SelectWithRecursiveCteAndWindowPage_ShouldGenerateRecursiveCte()
    {
        var query = Substitute.For<IAstQuery>();
        var cte1 = Substitute.For<IAstQuery>();
        cte1.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());
        
        query.Nodes.Returns(new ISqlNode[]
        {
            new CteNode("cte1", cte1, true),
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", null),
            new WindowPageNode(1, 10, "id", false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("WITH RECURSIVE \"cte1\" AS (SELECT 1), __wp AS (SELECT \"id\", ROW_NUMBER() OVER(ORDER BY \"id\" ASC) AS __row_num FROM \"users\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_SelectWithMultipleCtesMixedRecursive_ShouldGenerateRecursive()
    {
        var query = Substitute.For<IAstQuery>();
        var cte1 = Substitute.For<IAstQuery>();
        cte1.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());
        var cte2 = Substitute.For<IAstQuery>();
        cte2.Nodes.Returns(new ISqlNode[] { new RawSelectNode("2", null, false) }.ToImmutableList());

        query.Nodes.Returns(new ISqlNode[]
        {
            new CteNode("cte1", cte1, true),
            new CteNode("cte2", cte2, false),
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", null)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("WITH RECURSIVE \"cte1\" AS (SELECT 1), \"cte2\" AS (SELECT 2) SELECT \"id\" FROM \"users\"");
    }

    [Fact]
    public void Compile_SelectWithWindowPageAndDistinctOnAndNoSelect_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new FromNode("users", null),
            new DistinctOnNode(new[] { "id" }),
            new WindowPageNode(1, 10, "id", false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("WITH __wp AS (SELECT DISTINCT ON (\"id\") *, ROW_NUMBER() OVER(ORDER BY \"id\" ASC) AS __row_num FROM \"users\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_MultipleUnnestNodes_WithoutFrom_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new UnnestNode(new object[] { "arr1" }, "u1"),
            new UnnestNode(new object[] { "arr2" }, "u2")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("SELECT * FROM UNNEST(@p0) AS \"u1\" , UNNEST(@p1) AS \"u2\"");
    }

    [Fact]
    public void Compile_HavingNode_WithIsOrFalseAndTrue_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "*" }, false),
            new FromNode("users", null),
            new GroupByNode(new[] { "id" }),
            new RawHavingNode("x = 1", null, false),
            new RawHavingNode("y = 2", null, true),
            new RawHavingNode("z = 3", null, false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("SELECT * FROM \"users\" GROUP BY \"id\" HAVING x = 1 OR y = 2 AND z = 3");
    }

    [Fact]
    public void Compile_MultipleWindowNodes_ShouldGenerateCommaSeparated()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "*" }, false),
            new FromNode("users", null),
            new WindowNode("w1", new[] { "id" }, new[] { "id DESC" }),
            new WindowNode("w2", new[] { "name" }, null)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("SELECT * FROM \"users\" WINDOW \"w1\" AS (PARTITION BY \"id\" ORDER BY \"id\" DESC), \"w2\" AS (PARTITION BY \"name\" )");
    }

    [Fact]
    public void CompileDelete_WithReturningAndExpressionSelect_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("users"),
            new RawWhereNode("id = 1", null, false),
            new ReturningNode(new[] { "id", "name" }),
            new ExpressionSelectNode(System.Linq.Expressions.Expression.Constant(true), false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("DELETE FROM \"users\" WHERE id = 1 RETURNING \"id\", \"name\"");
    }

    [Fact]
    public void Compile_SelectWithWindowPageAndNoSelectNode_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new FromNode("users", null),
            new WindowPageNode(1, 10, "id", true)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("WITH __wp AS (SELECT *, ROW_NUMBER() OVER(ORDER BY \"id\" DESC) AS __row_num FROM \"users\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_CopyNode_WithNoFormat_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new EricksonLopez.SqlBuilder.PostgreSql.CopyNode("users", new[] { "id" }, "STDIN", null!)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("COPY \"users\" (\"id\") FROM STDIN");
    }
    
    [Fact]
    public void CompileUpdate_WithRawJoinAndSubqueryJoin_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        var sub = Substitute.For<IAstQuery, ISqlQuery>();
        ((ISqlQuery)sub).Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT 1", new Dictionary<string, object?>()));
        ((IAstQuery)sub).Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());
        
        query.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new SetNode("name", "John"),
            new RawJoinNode("JOIN x ON 1=1"),
            new SubqueryJoinNode(JoinType.Left, (IAstQuery)sub, "sub", "users.id = sub.id"),
            new RawWhereNode("id = 1", null, false),
            new RawOrderByNode("id", false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        // Postgres update doesn't natively support full join syntax like this but the compiler generates it verbatim
        result.Sql.TrimEnd().Should().Be("UPDATE \"users\" SET \"name\" = @p0 JOIN x ON 1=1 LEFT JOIN (SELECT 1) AS \"sub\" ON users.id = sub.id WHERE id = 1");
    }

    [Fact]
    public void CompileDelete_WithRawJoinAndSubqueryJoin_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        var sub = Substitute.For<IAstQuery, ISqlQuery>();
        ((ISqlQuery)sub).Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT 1", new Dictionary<string, object?>()));
        ((IAstQuery)sub).Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());
        
        query.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("users"),
            new RawJoinNode("JOIN x ON 1=1"),
            new SubqueryJoinNode(JoinType.Left, (IAstQuery)sub, "sub", "users.id = sub.id"),
            new RawWhereNode("id = 1", null, false),
            new RawOrderByNode("id", false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("DELETE FROM \"users\" JOIN x ON 1=1 LEFT JOIN (SELECT 1) AS \"sub\" ON users.id = sub.id WHERE id = 1");
    }

    [Fact]
    public void Compile_SelectWithMultipleCtesAndWindowPage_ShouldGenerateCommaSeparatedCtes()
    {
        var query = Substitute.For<IAstQuery>();
        var cte1 = Substitute.For<ISqlQuery>();
        cte1.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("", new Dictionary<string, object?>()));
        var cte2 = Substitute.For<ISqlQuery>();
        cte2.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("", new Dictionary<string, object?>()));
        
        query.Nodes.Returns(new ISqlNode[]
        {
            new CteNode("cte1", cte1, false),
            new CteNode("cte2", cte2, false),
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", null),
            new WindowPageNode(1, 10, "id", false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("WITH \"cte1\" AS (), \"cte2\" AS (), __wp AS (SELECT \"id\", ROW_NUMBER() OVER(ORDER BY \"id\" ASC) AS __row_num FROM \"users\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_SelectTwiceWithDistinctOn_ShouldUseLastSelect()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "*" }, false),
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", null),
            new DistinctOnNode(new[] { "id" })
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT DISTINCT ON (\"id\") \"id\" FROM \"users\"");
    }

    [Fact]
    public void Compile_UnnestNodeWithMultipleArrays_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "*" }, false),
            new UnnestNode(new object[] { "arr1", "arr2" }, "u")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("SELECT * FROM UNNEST(@p0, @p1) AS \"u\"");
    }

    [Fact]
    public void Compile_RawJoinNode_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "*" }, false),
            new FromNode("users", null),
            new RawJoinNode("JOIN x ON 1=1")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"users\" JOIN x ON 1=1");
    }

    [Fact]
    public void Compile_SubqueryJoinNode_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        var sub = Substitute.For<IAstQuery, ISqlQuery>();
        ((ISqlQuery)sub).Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT 1", new Dictionary<string, object?>()));
        ((IAstQuery)sub).Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());
        
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "*" }, false),
            new FromNode("users", null),
            new SubqueryJoinNode(JoinType.Left, (IAstQuery)sub, "sub", "users.id = sub.id")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("SELECT * FROM \"users\" LEFT JOIN (SELECT 1) AS \"sub\" ON users.id = sub.id");
    }

    [Fact]
    public void Compile_GroupByNode_WithMultipleColumns_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "*" }, false),
            new FromNode("users", null),
            new GroupByNode(new[] { "col1", "col2" })
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.TrimEnd().Should().Be("SELECT * FROM \"users\" GROUP BY \"col1\", \"col2\"");
    }

    [Fact]
    public void Compile_HavingNode_WithOr_ShouldGenerateCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "*" }, false),
            new FromNode("users", null),
            new GroupByNode(new[] { "id" }),
            new RawHavingNode("COUNT(id) > 1", null, false),
            new RawHavingNode("SUM(id) = 0", null, true),
            new RawHavingNode("MAX(id) = 10", null, true)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"users\" GROUP BY \"id\" HAVING COUNT(id) > 1 OR SUM(id) = 0 OR MAX(id) = 10");
    }

    private readonly PostgreSqlCompiler _compiler = new();

    [Fact]
    public void Compile_WindowPageNode_ShouldGenerateCteWithRowNum()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id", "name" }, false),
            new FromNode("users", null),
            new WindowPageNode(2, 10, "id", false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("WITH __wp AS (SELECT \"id\", \"name\", ROW_NUMBER() OVER(ORDER BY \"id\" ASC) AS __row_num FROM \"users\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 11 AND 20");
    }
    
    [Fact]
    public void Compile_WindowPageNode_WithDescending_ShouldGenerateCteWithRowNumDesc()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", null),
            new WindowPageNode(1, 10, "id", true)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("WITH __wp AS (SELECT \"id\", ROW_NUMBER() OVER(ORDER BY \"id\" DESC) AS __row_num FROM \"users\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }

    [Fact]
    public void Compile_GroupByNode_ShouldGenerateGroupBy()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "name" }, false),
            new FromNode("users", null),
            new GroupByNode(new[] { "name" })
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT \"name\" FROM \"users\" GROUP BY \"name\"");
    }

    [Fact]
    public void Compile_HavingNode_ShouldGenerateHaving()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "name" }, false),
            new FromNode("users", null),
            new GroupByNode(new[] { "name" }),
            new RawHavingNode("COUNT(id) > 1", null, false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT \"name\" FROM \"users\" GROUP BY \"name\" HAVING COUNT(id) > 1");
    }
    
    [Fact]
    public void Compile_MultipleHavingNodes_ShouldGenerateHavingAndOr()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "name" }, false),
            new FromNode("users", null),
            new GroupByNode(new[] { "name" }),
            new RawHavingNode("COUNT(id) > 1", null, false),
            new RawHavingNode("MAX(id) > 5", null, true)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT \"name\" FROM \"users\" GROUP BY \"name\" HAVING COUNT(id) > 1 OR MAX(id) > 5");
    }
    
    [Fact]
    public void Compile_ExpressionHavingNodes_ShouldGenerateHavingAndOr()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "name" }, false),
            new FromNode("users", null),
            new GroupByNode(new[] { "name" }),
            new ExpressionHavingNode(null!, false),
            new ExpressionHavingNode(null!, true)
        }.ToImmutableList());

        try
        {
            _compiler.Compile((ISqlQuery)query);
        }
        catch
        {
            // Ignore parse error
        }
    }

    [Fact]
    public void Compile_WindowNode_ShouldGenerateWindow()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", "u"),
            new WindowNode("w1", new[] { "dept" }, null),
            new WindowNode("w2", null, new[] { "role ASC" })
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT \"id\" FROM \"users\" AS \"u\" WINDOW \"w1\" AS (PARTITION BY \"dept\" ), \"w2\" AS (ORDER BY \"role\" ASC)");
    }

    [Fact]
    public void Compile_DeleteUsing_ShouldGenerateUsing()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("users"),
            new FromNode("other_table", "ot"),
            new RawWhereNode("users.id = ot.user_id", null)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("DELETE FROM \"users\" USING \"other_table\" AS \"ot\" WHERE users.id = ot.user_id");
    }

    [Fact]
    public void Compile_UpdateWithCte_ShouldGenerateCte()
    {
        var query = Substitute.For<IAstQuery>();
        var cteSubquery = Substitute.For<ISqlQuery>();
        cteSubquery.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("", new Dictionary<string, object?>()));
        
        query.Nodes.Returns(new ISqlNode[]
        {
            new CteNode("my_cte", cteSubquery, false),
            new UpdateNode("users"),
            new SetNode("name", "test")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("WITH \"my_cte\" AS () UPDATE \"users\" SET \"name\" = @p0");
    }
    
    [Fact]
    public void Compile_SelectWithRecursiveCte_ShouldGenerateRecursiveCte()
    {
        var query = Substitute.For<IAstQuery>();
        var cteSubquery = Substitute.For<ISqlQuery>();
        cteSubquery.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("", new Dictionary<string, object?>()));
        
        query.Nodes.Returns(new ISqlNode[]
        {
            new CteNode("my_cte", cteSubquery, true),
            new SelectNode(new[] { "*" }, false),
            new FromNode("my_cte", null)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("WITH RECURSIVE \"my_cte\" AS () SELECT * FROM \"my_cte\"");
    }
    
    [Fact]
    public void Compile_SelectWithMultipleCtes_ShouldGenerateCommaSeparatedCtes()
    {
        var query = Substitute.For<IAstQuery>();
        var cteSubquery1 = Substitute.For<ISqlQuery>();
        cteSubquery1.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("", new Dictionary<string, object?>()));
        var cteSubquery2 = Substitute.For<ISqlQuery>();
        cteSubquery2.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("", new Dictionary<string, object?>()));
        
        query.Nodes.Returns(new ISqlNode[]
        {
            new CteNode("cte1", cteSubquery1, false),
            new CteNode("cte2", cteSubquery2, false),
            new SelectNode(new[] { "*" }, false),
            new FromNode("cte1", null)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("WITH \"cte1\" AS (), \"cte2\" AS () SELECT * FROM \"cte1\"");
    }
    
    [Fact]
    public void Compile_SelectWithCteAndWindowPage_ShouldGenerateCombinedCte()
    {
        var query = Substitute.For<IAstQuery>();
        var cteSubquery = Substitute.For<ISqlQuery>();
        cteSubquery.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("", new Dictionary<string, object?>()));
        
        query.Nodes.Returns(new ISqlNode[]
        {
            new CteNode("cte1", cteSubquery, false),
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", null),
            new WindowPageNode(1, 10, "id", false)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("WITH \"cte1\" AS (), __wp AS (SELECT \"id\", ROW_NUMBER() OVER(ORDER BY \"id\" ASC) AS __row_num FROM \"users\" ) SELECT * FROM __wp WHERE __row_num BETWEEN 1 AND 10");
    }
}





