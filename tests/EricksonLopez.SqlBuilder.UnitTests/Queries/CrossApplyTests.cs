// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.SqlServer;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

/// <summary>
/// Tests for CROSS APPLY / OUTER APPLY / LATERAL JOIN.
/// SQL Server: CROSS APPLY / OUTER APPLY
/// PostgreSQL: CROSS JOIN LATERAL / LEFT JOIN LATERAL
/// </summary>
public class CrossApplyTests
{
    private readonly SqlServerCompiler _sqlServer = new();
    private readonly PostgreSqlCompiler _postgreSql = new();

    private SelectQuery<TestEntity> BuildSubquery()
        => Sql.From<TestEntity>().Select("id", "val");

    [Fact]
    public void CrossApply_SqlServer_EmitsCrossApplyKeyword()
    {
        var subquery = BuildSubquery();
        var query = Sql.From<TestEntity>().CrossApply(subquery, "s");
        var result = query.Build(_sqlServer);

        result.Sql.Should().Contain("CROSS APPLY (");
        result.Sql.Should().NotContain("CROSS APPLY JOIN");
        result.Sql.Should().Contain(") AS [s]");
    }

    [Fact]
    public void OuterApply_SqlServer_EmitsOuterApplyKeyword()
    {
        var subquery = BuildSubquery();
        var query = Sql.From<TestEntity>().OuterApply(subquery, "s");
        var result = query.Build(_sqlServer);

        result.Sql.Should().Contain("OUTER APPLY (");
        result.Sql.Should().NotContain("OUTER APPLY JOIN");
        result.Sql.Should().Contain(") AS [s]");
    }

    [Fact]
    public void CrossApply_PostgreSql_EmitsCrossJoinLateral()
    {
        var subquery = BuildSubquery();
        var query = Sql.From<TestEntity>().CrossApply(subquery, "s");
        var result = query.Build(_postgreSql);

        result.Sql.Should().Contain("CROSS JOIN LATERAL (");
        result.Sql.Should().NotContain("CROSS APPLY");
        result.Sql.Should().Contain(") AS \"s\"");
    }

    [Fact]
    public void OuterApply_PostgreSql_EmitsLeftJoinLateral()
    {
        var subquery = BuildSubquery();
        var query = Sql.From<TestEntity>().OuterApply(subquery, "s");
        var result = query.Build(_postgreSql);

        result.Sql.Should().Contain("LEFT JOIN LATERAL (");
        result.Sql.Should().NotContain("OUTER APPLY");
        result.Sql.Should().Contain(") AS \"s\"");
    }

    [Fact]
    public void CrossApply_SqlServer_WithSelectColumns_ReturnsFullQuery()
    {
        var subquery = BuildSubquery();
        var query = Sql.From<TestEntity>()
            .CrossApply(subquery, "s");
        var result = query.Build(_sqlServer);

        result.Sql.Should().Contain("CROSS APPLY (");
        result.Sql.Should().Contain(") AS [s]");
        result.Sql.Should().Contain("FROM [");
    }

    [Fact]
    public void RegularSubqueryJoin_SqlServer_StillWorksAfterFix()
    {
        var subquery = BuildSubquery();
        var query = Sql.From<TestEntity>().JoinSubquery(subquery, "sub", "sub.id = u.id");
        var result = query.Build(_sqlServer);

        result.Sql.Should().Contain("INNER JOIN (");
        result.Sql.Should().Contain(") AS [sub] ON sub.id = u.id");
    }

    [Fact]
    public void LateralJoin_PostgreSql_StillWorksAfterFix()
    {
        var subquery = BuildSubquery();
        var query = Sql.From<TestEntity>().LateralJoin(subquery, "lj");
        var result = query.Build(_postgreSql);

        result.Sql.Should().Contain("INNER JOIN LATERAL (");
        result.Sql.Should().Contain(") AS \"lj\"");
    }
}
