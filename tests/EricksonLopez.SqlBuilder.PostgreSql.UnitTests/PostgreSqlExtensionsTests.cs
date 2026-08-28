// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class PostgreSqlExtensionsTests
{

    [Fact]
    public void WhereComposite_EmptyProperties_ThrowsArgumentException()
    {
        var query = new SelectQuery<TestEntity>();
        var values = new[] { new { Id = 1, Name = "A" } };
        
        Action act = () => query.WhereComposite("col", "type", Array.Empty<object>());
        
        act.Should().Throw<ArgumentException>()
           .WithMessage("Composite properties must not be empty. (Parameter 'properties')");
    }

    [Fact]
    public void JoinLateral_WithRawQuery_ThrowsArgumentException()
    {
        var query = new SelectQuery<TestEntity>();
        var rawSubquery = Sql.Raw("SELECT id FROM users");
        
        Action act = () => query.JoinLateral(rawSubquery, "alias");
        
        act.Should().Throw<ArgumentException>()
           .WithMessage("Subquery must be an AST query.");
    }
    
    private readonly PostgreSqlCompiler _compiler = new();

    [Fact]
    public void DistinctOn_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().Select("Id", "Name").DistinctOn("Id", "Name");
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT DISTINCT ON (\"Id\", \"Name\") \"Id\", \"Name\" FROM \"testentitys\"");
    }

    [Fact]
    public void WhereILike_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WhereILike("Name", "%test%");
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Name ILIKE @p0");
        result.Parameters["p0"].Should().Be("%test%");
    }

    [Fact]
    public void WhereJsonbContains_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WhereJsonbContains("Data", "{\"key\":\"value\"}");
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Data @> @p0::jsonb");
    }

    [Fact]
    public void WhereJsonbExists_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WhereJsonbExists("Data", "key");
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Data ? @p0");
    }

    [Fact]
    public void WhereAny_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WhereAny("Tags", new[] { "tag1" });
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Tags = ANY(@p0)");
    }

    [Fact]
    public void WhereAll_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WhereAll("Tags", new[] { "tag1" });
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Tags = ALL(@p0)");
    }

    [Fact]
    public void WhereArrayContains_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WhereArrayContains("Tags", new[] { "tag1" });
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Tags @> @p0");
    }

    [Fact]
    public void WhereArrayOverlaps_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WhereArrayOverlaps("Tags", new[] { "tag1" });
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Tags && @p0");
    }

    [Fact]
    public void WhereJsonPath_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WhereJsonPath("Data", "$.key");
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Data @@ @p0::jsonpath");
    }

    [Fact]
    public void JoinLateral_ShouldGenerateCorrectSql()
    {
        var subquery = Sql.From<TestEntity>().Select("Id");
        var query = Sql.From<TestEntity>().JoinLateral(subquery, "sub");
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" CROSS JOIN LATERAL (SELECT \"Id\" FROM \"testentitys\") AS \"sub\"");
    }

    [Fact]
    public void FromUnnest_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().FromUnnest("unnested", new[] { 1, 2 });
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" , UNNEST(@p0) AS \"unnested\"");
    }

    [Fact]
    public void SelectFilter_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().SelectFilter($"COUNT({1})", $"\"Status\" = {1}", "active_count");
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT COUNT(@p0) FILTER (WHERE \"Status\" = @p1) AS active_count FROM \"testentitys\"");
        result.Parameters["p0"].Should().Be(1);
        result.Parameters["p1"].Should().Be(1);
    }

    [Fact]
    public void WhereComposite_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WhereComposite("Price", "money_type", 100, "USD");
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Price = ROW(@p0, @p1)::money_type");
    }

    public enum StatusEnum { Active, Inactive }

    [Fact]
    public void WherePgEnum_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WherePgEnum("Status", "status_enum", StatusEnum.Active);
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Status = @p0::status_enum");
        result.Parameters["p0"].Should().Be("Active");
    }

    [Fact]
    public void WherePgEnum_WithString_ShouldGenerateCorrectSql()
    {
        var query = Sql.From<TestEntity>().WherePgEnum("Status", "status_enum", "Active");
        var result = _compiler.Compile(query);
        result.Sql.Trim().Should().Be("SELECT * FROM \"testentitys\" WHERE Status = @p0::status_enum");
        result.Parameters["p0"].Should().Be("Active");
    }

    [Fact]
    public void WhereComposite_WithEmptyProperties_ShouldThrowArgumentException()
    {
        var query = Sql.From<TestEntity>();
        var action = () => query.WhereComposite("Price", "money_type");
        action.Should().Throw<System.ArgumentException>();
    }

    [Fact]
    public void WhereComposite_WithNullProperties_ShouldThrowArgumentException()
    {
        var query = Sql.From<TestEntity>();
        var action = () => query.WhereComposite("Price", "money_type", (object[])null!);
        action.Should().Throw<System.ArgumentException>();
    }
}




