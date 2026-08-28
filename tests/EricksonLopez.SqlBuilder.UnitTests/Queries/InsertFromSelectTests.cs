// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

/// <summary>
/// Tests for INSERT INTO ... SELECT syntax.
/// Verifies Sql.InsertFrom and InsertQuery.FromSelect produce correct INSERT SQL.
/// </summary>
public class InsertFromSelectTests
{
    private readonly SqlServerCompiler _sqlServer = new();
    private readonly PostgreSqlCompiler _postgreSql = new();

    private SelectQuery<TestEntity> BuildArchiveSelect()
        => Sql.From<TestEntity>();

    [Fact]
    public void InsertFrom_SqlServer_WithColumns_GeneratesCorrectSql()
    {
        var selectQuery = BuildArchiveSelect();
        var query = Sql.InsertFrom<TestEntity>(selectQuery, "Id", "Name");
        var result = query.Build(_sqlServer);

        result.Sql.Should().Contain("INSERT INTO");
        result.Sql.Should().Contain("[Id]");
        result.Sql.Should().Contain("[Name]");
        result.Sql.Should().Contain("SELECT");
    }

    [Fact]
    public void InsertFrom_SqlServer_WithoutColumns_GeneratesInsertStatement()
    {
        var selectQuery = BuildArchiveSelect();
        var query = Sql.InsertFrom<TestEntity>(selectQuery);
        var result = query.Build(_sqlServer);

        result.Sql.Should().Contain("INSERT INTO");
        result.Sql.Should().Contain("SELECT");
    }

    [Fact]
    public void InsertFrom_PostgreSql_UsesDoubleQuoteEscape()
    {
        var selectQuery = BuildArchiveSelect();
        var query = Sql.InsertFrom<TestEntity>(selectQuery, "Id", "Name");
        var result = query.Build(_postgreSql);

        result.Sql.Should().Contain("INSERT INTO");
        result.Sql.Should().Contain("\"Id\"");
        result.Sql.Should().Contain("\"Name\"");
        result.Sql.Should().Contain("SELECT");
    }

    [Fact]
    public void InsertFrom_AllDialects_CompileWithoutException()
    {
        var compilers = new ISqlCompiler[]
        {
            _sqlServer, _postgreSql, new MySqlCompiler(), new SqliteCompiler(), new OracleCompiler()
        };
        var selectQuery = BuildArchiveSelect();

        foreach (var compiler in compilers)
        {
            var query = Sql.InsertFrom<TestEntity>(selectQuery, "Id", "Name");
            var act = () => query.Build(compiler);
            act.Should().NotThrow($"Compiler {compiler.GetType().Name} should handle InsertFrom");
        }
    }

    [Fact]
    public void InsertQuery_FromSelect_ChainedWithInto_UsesTargetTable()
    {
        var selectQuery = BuildArchiveSelect();
        var query = new InsertQuery<TestEntity>()
            .Into("archive_table")
            .FromSelect(selectQuery, "Id", "Name");
        var result = query.Build(_sqlServer);

        result.Sql.Should().Contain("[archive_table]");
        result.Sql.Should().Contain("INSERT INTO");
    }

    [Fact]
    public void InsertQuery_FromSelect_WithoutExplicitColumns_OmitsColumnList()
    {
        var selectQuery = BuildArchiveSelect();
        var query = Sql.InsertFrom<TestEntity>(selectQuery);
        var result = query.Build(_sqlServer);

        result.Sql.Should().Contain("INSERT INTO");
        result.Sql.Should().Contain("SELECT");
    }
}
