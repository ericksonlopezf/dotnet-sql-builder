// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Sqlite.Tests;

public class SqliteCompilerTests
{
    [Fact]
    public void Compile_WhenSelectWithLimitOffset_ShouldGenerateCorrectSyntax()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("Id", "Name").Offset(20).Limit(10);
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT \"Id\", \"Name\" FROM \"testentitys\" LIMIT 10 OFFSET 20");
    }
    
    [Fact]
    public void Compile_WhenInsert_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Insert(ObjectMother.CreateTestEntity());
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" (\"id\", \"name\", \"is_active\") VALUES (@p0, @p1, @p2)");
    }

    [Fact]
    public void Compile_WhenUpdate_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Update<TestEntity>().WhereAll();
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("UPDATE \"testentitys\"");
    }
    
    [Fact]
    public void Compile_InsertOnConflict_WithMultipleColumns_IncludesComma()
    {
        var query = Sql.Insert(ObjectMother.CreateTestEntity()).OnConflict("id", "name").DoNothing();
        var compiler = new SqliteCompiler();
        
        var result = compiler.Compile(query);
        
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" (\"id\", \"name\", \"is_active\") VALUES (@p0, @p1, @p2) ON CONFLICT (\"id\", \"name\") DO NOTHING");
    }

    [Fact]
    public void Compile_InsertOnConflict_WithReturning_ShouldIncludeBothClausesWithoutSyntaxError()
    {
        var query = Sql.Insert(ObjectMother.CreateTestEntity())
                       .OnConflict("id")
                       .DoUpdate(x => new { Name = x.Name })
                       .Returning("id");
        var compiler = new SqliteCompiler();
        
        var result = compiler.Compile(query);
        
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" (\"id\", \"name\", \"is_active\") VALUES (@p0, @p1, @p2) ON CONFLICT (\"id\") DO UPDATE SET \"name\" = EXCLUDED.\"name\" RETURNING \"id\"");
    }

    [Fact]
    public void Compile_InsertOnConflictDoNothing_WithReturning_ShouldIncludeBothClausesWithoutSyntaxError()
    {
        var query = Sql.Insert(ObjectMother.CreateTestEntity())
                       .OnConflict("id")
                       .DoNothing()
                       .Returning("id");
        var compiler = new SqliteCompiler();
        
        var result = compiler.Compile(query);
        
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" (\"id\", \"name\", \"is_active\") VALUES (@p0, @p1, @p2) ON CONFLICT (\"id\") DO NOTHING RETURNING \"id\"");
    }

    [Fact]
    public void Compile_WhenDelete_ShouldEscapeTableName()
    {
        // Arrange
        var query = Sql.Delete<TestEntity>().WhereAll();
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("DELETE FROM \"testentitys\"");
    }

    [Fact]
    public void Compile_WhenWhere_ShouldCompileSuccessfully()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("*").Where($"Id = {1}").Or(u => u.Name == "Admin");
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Id = @p0 OR (name = @p1)");
    }
    
    [Fact]
    public void Compile_WhenDistinct_ShouldIncludeDistinctKeyword()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("Name").Distinct();
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT DISTINCT \"Name\" FROM \"testentitys\"");
    }

    [Fact]
    public void Compile_WhenOffsetWithoutLimit_ShouldGenerateLimitMinusOne()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("Name").Offset(10);
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT \"Name\" FROM \"testentitys\" LIMIT -1 OFFSET 10");
    }

    [Fact]
    public void Compile_WhenLimitWithoutOffset_ShouldGenerateLimit()
    {
        // Arrange
        var query = Sql.From<TestEntity>().Select("Name").Limit(10);
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("SELECT \"Name\" FROM \"testentitys\" LIMIT 10");
    }

    [Fact]
    public void Compile_WhenDeleteWithReturning_ShouldGenerateReturningClause()
    {
        // Arrange
        var query = Sql.Delete<TestEntity>().Where(e => e.Id == 1).Returning("Id", "Name");
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("DELETE FROM \"testentitys\" WHERE (id = @p0) RETURNING \"Id\", \"Name\"");
    }

    [Fact]
    public void Compile_WhenDeleteWithReturningAll_ShouldGenerateReturningStar()
    {
        // Arrange
        var query = Sql.Delete<TestEntity>().Where(e => e.Id == 1).Returning();
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("DELETE FROM \"testentitys\" WHERE (id = @p0) RETURNING *");
    }

    [Fact]
    public void Compile_WhenInsertOnConflictDoNothing_ShouldGenerateCorrectSyntax()
    {
        // Arrange
        var query = Sql.Insert(ObjectMother.CreateTestEntity()).OnConflict(e => e.Id).DoNothing();
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" (\"id\", \"name\", \"is_active\") VALUES (@p0, @p1, @p2) ON CONFLICT (\"id\") DO NOTHING");
    }

    [Fact]
    public void Compile_WhenInsertOnConflictDoUpdateSetNewExpression_ShouldGenerateAssignments()
    {
        // Arrange
        var query = Sql.Insert(ObjectMother.CreateTestEntity()).OnConflict(e => e.Id).DoUpdate(e => new { e.Name, e.IsActive });
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" (\"id\", \"name\", \"is_active\") VALUES (@p0, @p1, @p2) ON CONFLICT (\"id\") DO UPDATE SET \"name\" = EXCLUDED.\"name\", \"is_active\" = EXCLUDED.\"is_active\"");
    }

    [Fact]
    public void Compile_WhenInsertOnConflictDoUpdateSetMemberExpression_ShouldGenerateSingleAssignment()
    {
        // Arrange
        var query = Sql.Insert(ObjectMother.CreateTestEntity()).OnConflict(e => e.Id).DoUpdate(e => e.Name);
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" (\"id\", \"name\", \"is_active\") VALUES (@p0, @p1, @p2) ON CONFLICT (\"id\") DO UPDATE SET \"name\" = EXCLUDED.\"name\"");
    }

    [Fact]
    public void Compile_WhenInsertOnConflictRawWithoutDoUpdateSet_ShouldPrependIt()
    {
        // Arrange
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] 
        { 
            new InsertNode("testentitys", Array.Empty<string>()), 
            new OnConflictNode(new[] { "name" }) { UpdateAction = "name = excluded.name" } 
        }.ToImmutableList());
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile((ISqlQuery)query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" ON CONFLICT (\"name\") DO UPDATE SET name = excluded.name");
    }

    [Fact]
    public void Compile_WhenInsertOnConflictRawWithDoUpdateSet_ShouldNotPrependIt()
    {
        // Arrange
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] 
        { 
            new InsertNode("testentitys", Array.Empty<string>()), 
            new OnConflictNode(new[] { "name" }) { UpdateAction = "DO UPDATE SET name = excluded.name" } 
        }.ToImmutableList());
        
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile((ISqlQuery)query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" ON CONFLICT (\"name\") DO UPDATE SET name = excluded.name");
    }

    [Fact]
    public void Compile_WhenInsertOnConflictWithoutColumns_ShouldNotIncludeParentheses()
    {
        // Arrange
        var query = Sql.Insert(ObjectMother.CreateTestEntity()).OnConflict(Array.Empty<string>()).DoNothing();
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile(query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" (\"id\", \"name\", \"is_active\") VALUES (@p0, @p1, @p2) ON CONFLICT DO NOTHING");
    }

    [Fact]
    public void Compile_WhenInsertOnConflictWithNullColumns_ShouldNotIncludeParentheses()
    {
        // Arrange
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] 
        { 
            new InsertNode("testentitys", Array.Empty<string>()), 
            new OnConflictNode((string[])null) { UpdateAction = "DO NOTHING" } 
        }.ToImmutableList());
        
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile((ISqlQuery)query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" ON CONFLICT DO NOTHING");
    }

    [Fact]
    public void Compile_WhenInsertOnConflictUnsupportedLambda_ShouldFallThrough()
    {
        // Arrange
        Expression<Func<TestEntity, object>> unsupportedLambda = e => 1;
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] 
        { 
            new InsertNode("testentitys", Array.Empty<string>()), 
            new OnConflictNode(new[] { "id" }) { UpdateExpression = unsupportedLambda } 
        }.ToImmutableList());
        
        var compiler = new SqliteCompiler();
        
        // Act
        var result = compiler.Compile((ISqlQuery)query);
        
        // Assert
        result.Sql.Trim().Should().Be("INSERT INTO \"testentitys\" ON CONFLICT (\"id\") DO UPDATE SET");
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

        var compiler = new SqliteCompiler();
        Action act = () => compiler.Compile((ISqlQuery)query);
        
        act.Should().Throw<NotSupportedException>()
           .WithMessage("*SQLite does not support the FILTER (WHERE ...) clause on window functions*");
    }

    [Fact]
    public void Compile_WindowFunction_WithFilterExpression_ThrowsNotSupportedException()
    {
        var expr = Expression.Lambda<Func<TestEntity, bool>>(
            Expression.Constant(true),
            Expression.Parameter(typeof(TestEntity), "x"));
        var node = new WindowFunctionNode(
            "SUM", "Amount", null, null,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(),
            "sum_val", FilterExpression: expr);

        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new FromNode("users"), node }.ToImmutableList());

        var compiler = new SqliteCompiler();
        Action act = () => compiler.Compile((ISqlQuery)query);
        
        act.Should().Throw<NotSupportedException>()
           .WithMessage("*SQLite does not support the FILTER (WHERE ...) clause on window functions*");
    }

    [Fact]
    public void Compile_WindowFunction_WithoutFilter_BuildsCorrectSql()
    {
        var node = new WindowFunctionNode(
            "SUM", "Amount", null, null,
            new[] { "Dept" }, new[] { "Salary" }, new[] { true },
            "sum_val");

        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new SelectNode(new[] { "id" }, false), new FromNode("users"), node }.ToImmutableList());

        var compiler = new SqliteCompiler();
        var result = compiler.Compile((ISqlQuery)query);
        
        result.Sql.Trim().Should().Contain("SUM(\"Amount\") OVER (PARTITION BY \"Dept\" ORDER BY \"Salary\" DESC) AS \"sum_val\"");
    }

    [Fact]
    public void Compile_GroupBy_Standard_BuildsCorrectSql()
    {
        var node = new GroupByNode(new[] { "dept" }, GroupByType.Standard);
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new SelectNode(new[] { "dept" }, false), new FromNode("users"), node }.ToImmutableList());

        var compiler = new SqliteCompiler();
        var result = compiler.Compile((ISqlQuery)query);
        
        result.Sql.Trim().Should().Be("SELECT \"dept\" FROM \"users\" GROUP BY \"dept\"");
    }

    [Fact]
    public void Compile_OrderBy_NonMemberExpression_BuildsCorrectSql()
    {
        Expression<Func<TestEntity, object>> orderExpr = x => 1;
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users"),
            new OrderByNode(orderExpr, false)
        }.ToImmutableList());

        var compiler = new SqliteCompiler();
        var result = compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"users\" ORDER BY");
    }

    [Fact]
    public void Compile_OrderBy_UnaryExpressionMember_BuildsCorrectSql()
    {
        Expression<Func<TestEntity, object>> orderExpr = x => x.Id;
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users"),
            new OrderByNode(orderExpr, true)
        }.ToImmutableList());

        var compiler = new SqliteCompiler();
        var result = compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"users\" ORDER BY \"id\" DESC");
    }

    [Fact]
    public void Compile_OrderBy_NullKeySelector_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users"),
            new OrderByNode(null!, true)
        }.ToImmutableList());

        var compiler = new SqliteCompiler();
        var result = compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"users\" ORDER BY  DESC");
    }
}






