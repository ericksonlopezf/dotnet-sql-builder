// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Text;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Core;

public class NonSqlEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class EntityWithReservedKeyword : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
{
    public int Order { get; set; }
    public string User { get; set; } = "";

    public string GetTableName() => "items";
    public string[] GetColumnNames() => new[] { "order", "user" };
    public object?[] GetValues() => new object?[] { Order, User };
    public string[] GetAllColumnNames() => GetColumnNames();
    public object?[] GetAllValues() => GetValues();
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
    {
        { "Order", "order" }, { "User", "user" }
    };
    public string[] GetIndexedColumns() => Array.Empty<string>();
}

public class CoreMutationTestingCoverageTests
{
    private readonly PostgreSqlCompiler _compiler = new();

    #region SelectQuery Mutation Coverage

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectQuery_From_AliasWhitespace_ThrowsArgumentException(string alias)
    {
        var query = Sql.From<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.From("table1", alias));
        ex.ParamName.Should().Be("alias");
        ex.Message.Should().Contain("Alias cannot be whitespace.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectQuery_InnerJoin_InvalidAliasOrOn_ThrowsArgumentException(string? invalidVal)
    {
        var query = Sql.From<TestingUser>();
        Assert.ThrowsAny<ArgumentException>(() => query.InnerJoin("table1", invalidVal!, "table1.id = testingusers.id"));
        Assert.ThrowsAny<ArgumentException>(() => query.InnerJoin("table1", "t1", invalidVal!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectQuery_LeftJoin_InvalidAliasOrOn_ThrowsArgumentException(string? invalidVal)
    {
        var query = Sql.From<TestingUser>();
        Assert.ThrowsAny<ArgumentException>(() => query.LeftJoin("table1", invalidVal!, "table1.id = testingusers.id"));
        Assert.ThrowsAny<ArgumentException>(() => query.LeftJoin("table1", "t1", invalidVal!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectQuery_RightJoin_InvalidAliasOrOn_ThrowsArgumentException(string? invalidVal)
    {
        var query = Sql.From<TestingUser>();
        Assert.ThrowsAny<ArgumentException>(() => query.RightJoin("table1", invalidVal!, "table1.id = testingusers.id"));
        Assert.ThrowsAny<ArgumentException>(() => query.RightJoin("table1", "t1", invalidVal!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectQuery_CrossJoin_InvalidAlias_ThrowsArgumentException(string? invalidVal)
    {
        var query = Sql.From<TestingUser>();
        Assert.ThrowsAny<ArgumentException>(() => query.CrossJoin("table1", invalidVal!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectQuery_FullJoin_InvalidAliasOrOn_ThrowsArgumentException(string? invalidVal)
    {
        var query = Sql.From<TestingUser>();
        Assert.ThrowsAny<ArgumentException>(() => query.FullJoin("table1", invalidVal!, "table1.id = testingusers.id"));
        Assert.ThrowsAny<ArgumentException>(() => query.FullJoin("table1", "t1", invalidVal!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectQuery_WhereColumns_InvalidOperator_ThrowsArgumentException(string? invalidOp)
    {
        var query = Sql.From<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.WhereColumns("col1", invalidOp!, "col2"));
        ex.ParamName.Should().Be("operator");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID_OP")]
    public void SelectQuery_WhereDate_InvalidOperator_ThrowsArgumentException(string? invalidOp)
    {
        var query = Sql.From<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.WhereDate("created_at", invalidOp!, DateTime.UtcNow));
        ex.ParamName.Should().Be("operator");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID_OP")]
    public void SelectQuery_WhereYear_InvalidOperator_ThrowsArgumentException(string? invalidOp)
    {
        var query = Sql.From<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.WhereYear("created_at", invalidOp!, 2026));
        ex.ParamName.Should().Be("operator");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID_OP")]
    public void SelectQuery_WhereMonth_InvalidOperator_ThrowsArgumentException(string? invalidOp)
    {
        var query = Sql.From<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.WhereMonth("created_at", invalidOp!, 9));
        ex.ParamName.Should().Be("operator");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID_OP")]
    public void SelectQuery_WhereDay_InvalidOperator_ThrowsArgumentException(string? invalidOp)
    {
        var query = Sql.From<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.WhereDay("created_at", invalidOp!, 24));
        ex.ParamName.Should().Be("operator");
    }

    [Fact]
    public void SelectQuery_WhereExists_NullSubquery_ThrowsArgumentNullException()
    {
        var query = Sql.From<TestingUser>();
        Assert.Throws<ArgumentNullException>(() => query.WhereExists(null!));
        Assert.Throws<ArgumentNullException>(() => query.WhereNotExists(null!));
        Assert.Throws<ArgumentNullException>(() => query.OrExists(null!));
        Assert.Throws<ArgumentNullException>(() => query.OrNotExists(null!));
    }

    [Fact]
    public void SelectQuery_LimitOffset_Negative_ThrowsArgumentOutOfRangeException()
    {
        var query = Sql.From<TestingUser>();
        Assert.Throws<ArgumentOutOfRangeException>(() => query.Limit(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => query.Offset(-1));
    }

    [Fact]
    public void SelectQuery_CTE_InvalidNameOrNullQuery_ThrowsException()
    {
        var query = Sql.From<TestingUser>();
        var validSub = Sql.From<TestingUser>();

        Assert.ThrowsAny<ArgumentException>(() => query.CTE("", validSub));
        Assert.ThrowsAny<ArgumentException>(() => query.CTE("   ", validSub));
        Assert.Throws<ArgumentNullException>(() => query.CTE("cte1", null!));

        Assert.ThrowsAny<ArgumentException>(() => query.CTE("", validSub, MaterializationHint.Materialized));
        Assert.ThrowsAny<ArgumentException>(() => query.CTE("   ", validSub, MaterializationHint.Materialized));
        Assert.Throws<ArgumentNullException>(() => query.CTE("cte1", null!, MaterializationHint.Materialized));

        Assert.ThrowsAny<ArgumentException>(() => query.RecursiveCTE("", validSub));
        Assert.ThrowsAny<ArgumentException>(() => query.RecursiveCTE("   ", validSub));
        Assert.Throws<ArgumentNullException>(() => query.RecursiveCTE("cte1", null!));

        Assert.ThrowsAny<ArgumentException>(() => query.RecursiveCTE("", validSub, MaterializationHint.Materialized));
        Assert.ThrowsAny<ArgumentException>(() => query.RecursiveCTE("   ", validSub, MaterializationHint.Materialized));
        Assert.Throws<ArgumentNullException>(() => query.RecursiveCTE("cte1", null!, MaterializationHint.Materialized));
    }

    [Fact]
    public void SelectQuery_Window_InvalidArguments_ThrowsArgumentException()
    {
        var query = Sql.From<TestingUser>();
        Assert.ThrowsAny<ArgumentException>(() => query.Window(""));
        Assert.ThrowsAny<ArgumentException>(() => query.Window("   "));
        Assert.Throws<ArgumentException>(() => query.Window("w", orderBy: new[] { "" }));
        Assert.Throws<ArgumentException>(() => query.Window("w", orderBy: new[] { "   " }));
        Assert.Throws<ArgumentException>(() => query.Window("w", orderBy: new[] { "col INVALID_DIR" }));

        // Valid directions should succeed
        var q1 = query.Window("w", orderBy: new[] { "col ASC" });
        q1.Should().NotBeNull();
        var q2 = query.Window("w", orderBy: new[] { "col DESC" });
        q2.Should().NotBeNull();
    }

    [Fact]
    public void SelectQuery_SetOperations_NullQuery_ThrowsArgumentNullException()
    {
        var query = Sql.From<TestingUser>();
        Assert.Throws<ArgumentNullException>(() => query.Union(null!));
        Assert.Throws<ArgumentNullException>(() => query.UnionAll(null!));
        Assert.Throws<ArgumentNullException>(() => query.Intersect(null!));
        Assert.Throws<ArgumentNullException>(() => query.IntersectAll(null!));
        Assert.Throws<ArgumentNullException>(() => query.Except(null!));
        Assert.Throws<ArgumentNullException>(() => query.ExceptAll(null!));
    }

    [Fact]
    public void SelectQuery_AsCount_WithAndWithoutAlias()
    {
        var queryWithAlias = Sql.From<TestingUser>().AsCount("total");
        var res1 = _compiler.Compile(queryWithAlias);
        res1.Sql.Should().Contain("COUNT(*) AS total");

        var queryWithoutAlias1 = Sql.From<TestingUser>().AsCount("");
        var res2 = _compiler.Compile(queryWithoutAlias1);
        res2.Sql.Should().Contain("COUNT(*)");
        res2.Sql.Should().NotContain("AS");

        var queryWithoutAlias2 = Sql.From<TestingUser>().AsCount(null!);
        var res3 = _compiler.Compile(queryWithoutAlias2);
        res3.Sql.Should().Contain("COUNT(*)");
        res3.Sql.Should().NotContain("AS");
    }

    #endregion

    #region DeleteQuery Mutation Coverage

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeleteQuery_Using_WhitespaceAlias_ThrowsArgumentException(string alias)
    {
        var query = new DeleteQuery<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.Using("other_table", alias));
        ex.ParamName.Should().Be("alias");
        ex.Message.Should().Contain("Alias cannot be whitespace.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void DeleteQuery_Join_InvalidAliasOrOn_ThrowsArgumentException(string? invalidVal)
    {
        var query = new DeleteQuery<TestingUser>();
        Assert.ThrowsAny<ArgumentException>(() => query.Join("table1", invalidVal!, "table1.id = testingusers.id"));
        Assert.ThrowsAny<ArgumentException>(() => query.Join("table1", "t1", invalidVal!));
    }

    [Fact]
    public void DeleteQuery_WhereNotExists_NullSubquery_ThrowsArgumentNullException()
    {
        var query = new DeleteQuery<TestingUser>();
        Assert.Throws<ArgumentNullException>(() => query.WhereNotExists(null!));
    }

    [Fact]
    public void DeleteQuery_Returning_NullColumns_ThrowsArgumentNullException()
    {
        var query = new DeleteQuery<TestingUser>();
        Assert.Throws<ArgumentNullException>(() => query.Returning(null!));
    }

    #endregion

    #region UpdateQuery Mutation Coverage

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateQuery_From_WhitespaceAlias_ThrowsArgumentException(string alias)
    {
        var query = new UpdateQuery<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.From("other_table", alias));
        ex.ParamName.Should().Be("alias");
        ex.Message.Should().Contain("Alias cannot be whitespace.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateQuery_Join_InvalidAliasOrOn_ThrowsArgumentException(string? invalidVal)
    {
        var query = new UpdateQuery<TestingUser>();
        Assert.ThrowsAny<ArgumentException>(() => query.Join("table1", invalidVal!, "table1.id = testingusers.id"));
        Assert.ThrowsAny<ArgumentException>(() => query.Join("table1", "t1", invalidVal!));
    }

    [Fact]
    public void UpdateQuery_WhereNotExists_NullSubquery_ThrowsArgumentNullException()
    {
        var query = new UpdateQuery<TestingUser>();
        Assert.Throws<ArgumentNullException>(() => query.WhereNotExists(null!));
    }

    [Fact]
    public void UpdateQuery_Returning_NullColumns_ThrowsArgumentNullException()
    {
        var query = new UpdateQuery<TestingUser>();
        Assert.Throws<ArgumentNullException>(() => query.Returning(null!));
    }

    #endregion

    #region InsertQuery Mutation Coverage

    [Fact]
    public void InsertQuery_Values_NonSqlEntity_ThrowsInvalidOperationException()
    {
        var query = new InsertQuery<NonSqlEntity>();
        var nonEntity = new NonSqlEntity { Id = 1, Name = "Test" };
        var ex = Assert.Throws<InvalidOperationException>(() => query.Values(nonEntity));
        ex.Message.Should().Contain("does not implement ISqlEntity");
        ex.Message.Should().Contain("Ensure the entity class is decorated with [SqlEntity]");
    }

    [Fact]
    public void InsertQuery_Returning_NullColumns_ThrowsArgumentNullException()
    {
        var query = new InsertQuery<TestingUser>();
        Assert.Throws<ArgumentNullException>(() => query.Returning(null!));
    }

    #endregion

    #region DynamicSortingExtensions Mutation Coverage

    [Fact]
    public void DynamicSortingExtensions_TooManyQualifiers_ThrowsArgumentException()
    {
        var query = Sql.From<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.OrderByDynamic("a.b.c"));
        ex.ParamName.Should().Be("sortBy");
        ex.Message.Should().Contain("Sort expression contains too many qualifiers.");
    }

    [Fact]
    public void DynamicSortingExtensions_InvalidTableAlias_ThrowsArgumentException()
    {
        var query = Sql.From<TestingUser>();
        var ex = Assert.Throws<ArgumentException>(() => query.OrderByDynamic("invalid-alias!.Name"));
        ex.ParamName.Should().Be("sortBy");
        ex.Message.Should().Contain("Invalid table alias in sort expression.");
    }

    #endregion

    #region SqlCompilerBase & SqlCompilerVisitor Mutation Coverage

    [Fact]
    public void SqlCompilerBase_EscapeIdentifier_HandlesEmbeddedQuotes()
    {
        var escaped = _compiler.EscapeIdentifier("col\"name");
        escaped.Should().Be("\"col\"\"name\"");

        var sb = new StringBuilder();
        _compiler.EscapeIdentifier(sb, "col\"name".AsSpan());
        sb.ToString().Should().Be("\"col\"\"name\"");
    }

    [Fact]
    public void SqlCompilerBase_SelectWildcardElimination_WhenExplicitColumnsSpecified()
    {
        // When wildcard * is combined with explicit columns, wildcard should be removed
        var query1 = Sql.From<TestingUser>().Select("*").Select("id", "name");
        var res1 = _compiler.Compile(query1);
        res1.Sql.Should().Be("SELECT \"id\", \"name\" FROM \"testingusers\"");

        // Reverse order: explicit column then wildcard
        var query2 = Sql.From<TestingUser>().Select("id").Select("*");
        var res2 = _compiler.Compile(query2);
        res2.Sql.Should().Be("SELECT \"id\" FROM \"testingusers\"");
    }

    [Fact]
    public void SqlCompilerBase_SelectDistinct_WithMultipleNodes_PreservesDistinct()
    {
        var query = Sql.From<TestingUser>().Select("id").Distinct().Select("name");
        var res = _compiler.Compile(query);
        res.Sql.Should().StartWith("SELECT DISTINCT ");
    }

    [Fact]
    public void SqlCompilerBase_MultipleProjections_ContextClearedBetweenNodes()
    {
        var query = Sql.From<TestingUser>().Select("id").Select("name").Select("age");
        var res = _compiler.Compile(query);
        res.Sql.Should().Be("SELECT \"id\", \"name\", \"age\" FROM \"testingusers\"");
    }

    [Fact]
    public void SqlCompilerVisitor_ExpressionSelect_Distinct_EmitsDistinct()
    {
        var query = Sql.From<TestingUser>().Select(u => new { u.Id, u.Name }).Distinct();
        var res = _compiler.Compile(query);
        res.Sql.Should().StartWith("SELECT DISTINCT id, name FROM \"testingusers\"");
    }

    [Fact]
    public void SqlCompilerVisitor_ExpressionSelect_SingleMemberReservedKeyword_EscapesColumn()
    {
        var query = Sql.From<EntityWithReservedKeyword>().Select(e => e.Order);
        var res = _compiler.Compile(query);
        res.Sql.Should().Be("SELECT \"order\" FROM \"items\"");
    }

    #endregion
}
