// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class NewRoadmapFeaturesTests
{
    public class TestUser : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? AlternateEmail { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Salary { get; set; }

        public string GetTableName() => "users";
        public string[] GetColumnNames() => new[] { "id", "name", "email", "alternate_email", "status", "salary" };
        public object?[] GetValues() => new object?[] { Id, Name, Email, AlternateEmail, Status, Salary };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
        {
            { "Id", "id" }, { "Name", "name" }, { "Email", "email" }, { "AlternateEmail", "alternate_email" }, { "Status", "status" }, { "Salary", "salary" }
        };
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }

    [Fact]
    public void IntersectAll_EmitsCorrectSetOperationNode()
    {
        var q1 = new SelectQuery<TestUser>().From("users").Select("id");
        var q2 = new SelectQuery<TestUser>().From("archive_users").Select("id");

        var combined = q1.IntersectAll(q2);

        var node = combined.Nodes.OfType<SetOperationNode>().Single();
        node.Operation.Should().Be("INTERSECT ALL");
        node.Query.Should().Be(q2);

        var sql = new SqlServerCompiler().Compile(combined).Sql;
        sql.Should().Contain("INTERSECT ALL");
    }

    [Fact]
    public void ExceptAll_EmitsCorrectSetOperationNode()
    {
        var q1 = new SelectQuery<TestUser>().From("users").Select("id");
        var q2 = new SelectQuery<TestUser>().From("banned_users").Select("id");

        var combined = q1.ExceptAll(q2);

        var node = combined.Nodes.OfType<SetOperationNode>().Single();
        node.Operation.Should().Be("EXCEPT ALL");
        node.Query.Should().Be(q2);

        var sql = new SqlServerCompiler().Compile(combined).Sql;
        sql.Should().Contain("EXCEPT ALL");
    }

    [Fact]
    public void GroupByRollup_EmitsRollupSyntax()
    {
        var query = new SelectQuery<TestUser>().From("users").Select("status").GroupByRollup("status", "name");

        var compiler = new SqlServerCompiler();
        var sql = compiler.Compile(query).Sql;

        sql.Should().Contain("GROUP BY ROLLUP([status], [name])");
    }

    [Fact]
    public void GroupByCube_EmitsCubeSyntax()
    {
        var query = new SelectQuery<TestUser>().From("users").Select("status").GroupByCube("status", "name");

        var compiler = new SqlServerCompiler();
        var sql = compiler.Compile(query).Sql;

        sql.Should().Contain("GROUP BY CUBE([status], [name])");
    }

    [Fact]
    public void GroupingSets_EmitsGroupingSetsSyntax()
    {
        var query = new SelectQuery<TestUser>().From("users")
            .Select("status")
            .GroupingSets(new[] { "status" }, new[] { "status", "name" });

        var compiler = new SqlServerCompiler();
        var sql = compiler.Compile(query).Sql;

        sql.Should().Contain("GROUP BY GROUPING SETS (([status]), ([status], [name]))");
    }

    [Fact]
    public void Sqlite_AnalyticalGroupBy_ThrowsNotSupportedException()
    {
        var query = new SelectQuery<TestUser>().From("users").Select("status").GroupByRollup("status");
        var compiler = new SqliteCompiler();

        Action act = () => compiler.Compile(query);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*SQLite does not support Rollup*");
    }

    [Fact]
    public void Predicate_IsDistinctFrom_EmitsCorrectSql()
    {
        var query = new SelectQuery<TestUser>().From("users")
            .Where(u => Sql.IsDistinctFrom(u.Email, "test@example.com"));

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("email IS DISTINCT FROM @p0");
        result.Parameters.Values.Should().Contain("test@example.com");
    }

    [Fact]
    public void Predicate_IsNotDistinctFrom_EmitsCorrectSql()
    {
        var query = new SelectQuery<TestUser>().From("users")
            .Where(u => Sql.IsNotDistinctFrom(u.Email, "test@example.com"));

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("email IS NOT DISTINCT FROM @p0");
        result.Parameters.Values.Should().Contain("test@example.com");
    }

    [Fact]
    public void Predicate_NullIf_EmitsCorrectSql()
    {
        var query = new SelectQuery<TestUser>().From("users")
            .Where(u => Sql.NullIf(u.Email, "") == "test@example.com");

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("NULLIF(email, @p0)");
    }

    [Fact]
    public void Predicate_MultiArgCoalesce_EmitsCorrectSql()
    {
        var query = new SelectQuery<TestUser>().From("users")
            .Where(u => Sql.Coalesce(u.Email, u.AlternateEmail, "fallback@example.com") == "test@example.com");

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("COALESCE(email, alternate_email, @p0)");
    }

    [Fact]
    public void Predicate_Outer_EmitsEscapedColumnIdentifier()
    {
        var query = new SelectQuery<TestUser>().From("users")
            .Where(u => u.Id == Sql.Outer<TestUser, int>(o => o.Id));

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        result.Sql.Should().Contain("id = \"id\"");
    }

    [Fact]
    public void CTE_Materialized_EmitsMaterializedInPostgreSql()
    {
        var subquery = new SelectQuery<TestUser>().From("users").Select("id");
        var query = new SelectQuery<TestUser>()
            .CTE("active_users", subquery, MaterializationHint.Materialized)
            .From("active_users");

        var compiler = new PostgreSqlCompiler();
        var sql = compiler.Compile(query).Sql;

        sql.Should().Contain("WITH \"active_users\" AS MATERIALIZED (");
    }

    [Fact]
    public void CTE_NotMaterialized_EmitsNotMaterializedInPostgreSql()
    {
        var subquery = new SelectQuery<TestUser>().From("users").Select("id");
        var query = new SelectQuery<TestUser>()
            .CTE("active_users", subquery, MaterializationHint.NotMaterialized)
            .From("active_users");

        var compiler = new PostgreSqlCompiler();
        var sql = compiler.Compile(query).Sql;

        sql.Should().Contain("WITH \"active_users\" AS NOT MATERIALIZED (");
    }

    [Fact]
    public void CTE_Materialized_IgnoredInSqlServer()
    {
        var subquery = new SelectQuery<TestUser>().From("users").Select("id");
        var query = new SelectQuery<TestUser>()
            .CTE("active_users", subquery, MaterializationHint.Materialized)
            .From("active_users");

        var compiler = new SqlServerCompiler();
        var sql = compiler.Compile(query).Sql;

        sql.Should().Contain("WITH [active_users] AS (");
        sql.Should().NotContain("MATERIALIZED");
    }

    [Fact]
    public void WindowFunction_Filter_ThrowsOnSqlServer()
    {
        var window = Window.Sum<TestUser, decimal>(u => u.Salary)
            .Filter(u => u.Status == "active")
            .As("active_salary");

        var query = new SelectQuery<TestUser>().From("users").Select(window);
        var compiler = new SqlServerCompiler();

        Action act = () => compiler.Compile(query);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*SQL Server does not support the FILTER (WHERE ...) clause*");
    }
}



