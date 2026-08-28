// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

[SqlEntity("testusers")]
public partial class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class PostgreSqlAdvancedFeaturesTests
{
    private readonly PostgreSqlCompiler _compiler = new();

    [Fact]
    public void ILikeExpression_GeneratesILikeKeyword()
    {
        var query = Sql.From<TestUser>()
            .Where(x => Sql.ILike(x.Name, "john%"));

        var result = query.Build(_compiler);
        Assert.Contains("name ILIKE", result.Sql);
        Assert.Equal("john%", result.Parameters["p0"]);
    }

    [Fact]
    public void SqlAnyExpression_GeneratesAnySyntax()
    {
        var roles = new[] { "Admin", "User" };
        var query = Sql.From<TestUser>()
            .Where(x => Sql.Any(x.Role, roles));

        var result = query.Build(_compiler);
        Assert.Contains("role = ANY(@p0)", result.Sql);
        Assert.Equal(roles, result.Parameters["p0"]);
    }

    [Fact]
    public void SqlAllExpression_GeneratesAllSyntax()
    {
        var roles = new[] { "Admin" };
        var query = Sql.From<TestUser>()
            .Where(x => Sql.All(x.Role, roles));

        var result = query.Build(_compiler);
        Assert.Contains("role = ALL(@p0)", result.Sql);
    }

    [Fact]
    public void LateralJoin_GeneratesCrossJoinLateral()
    {
        var subquery = Sql.From<TestUser>().Where(u => u.IsActive);
        var query = Sql.From<TestUser>().JoinLateral(subquery, "active_users");

        var result = query.Build(_compiler);
        Assert.Contains("CROSS JOIN LATERAL (SELECT * FROM \"testusers\" WHERE is_active) AS \"active_users\"", result.Sql);
    }

    [Fact]
    public void SelectFilter_GeneratesFilterWhereClause()
    {
        var query = Sql.From<TestUser>()
            .SelectFilter($"COUNT(*)", $"is_active = {true}", "active_count");

        var result = query.Build(_compiler);
        Assert.Contains("COUNT(*) FILTER (WHERE is_active = @p0) AS active_count", result.Sql);
    }
}



