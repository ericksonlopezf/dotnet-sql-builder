// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Security;

public class SqlInjectionRedTeamTests
{
    [Theory]
    [InlineData("users\" --")]
    [InlineData("users\"; DROP TABLE users; --")]
    [InlineData("col\"name")]
    [InlineData("schema.\"table\"")]
    public void PostgreSql_EscapeIdentifier_EscapesEmbeddedQuotes(string payload)
    {
        var compiler = new PostgreSqlCompiler();
        var escaped = compiler.EscapeIdentifier(payload);

        // Quotes inside the identifier must be doubled so it cannot escape the identifier boundary
        escaped.Should().StartWith("\"").And.EndWith("\"");
        var inner = escaped.Substring(1, escaped.Length - 2);
        inner.Should().Be(payload.Replace("\"", "\"\""));
    }

    [Theory]
    [InlineData("users] --")]
    [InlineData("users]; DROP TABLE users; --")]
    [InlineData("col]name")]
    public void SqlServer_EscapeIdentifier_EscapesEmbeddedClosingBrackets(string payload)
    {
        var compiler = new SqlServerCompiler();
        var escaped = compiler.EscapeIdentifier(payload);

        escaped.Should().StartWith("[").And.EndWith("]");
        var inner = escaped.Substring(1, escaped.Length - 2);
        inner.Should().Be(payload.Replace("]", "]]"));
    }

    [Theory]
    [InlineData("users` --")]
    [InlineData("users`; DROP TABLE users; --")]
    [InlineData("col`name")]
    public void MySql_EscapeIdentifier_EscapesEmbeddedBackticks(string payload)
    {
        var compiler = new MySqlCompiler();
        var escaped = compiler.EscapeIdentifier(payload);

        escaped.Should().StartWith("`").And.EndWith("`");
        var inner = escaped.Substring(1, escaped.Length - 2);
        inner.Should().Be(payload.Replace("`", "``"));
    }

    [Theory]
    [InlineData("users\" --")]
    [InlineData("users\"; DROP TABLE users; --")]
    public void Sqlite_EscapeIdentifier_EscapesEmbeddedQuotes(string payload)
    {
        var compiler = new SqliteCompiler();
        var escaped = compiler.EscapeIdentifier(payload);

        escaped.Should().StartWith("\"").And.EndWith("\"");
        var inner = escaped.Substring(1, escaped.Length - 2);
        inner.Should().Be(payload.Replace("\"", "\"\""));
    }

    [Theory]
    [InlineData("users\" --")]
    [InlineData("users\"; DROP TABLE users; --")]
    public void Oracle_EscapeIdentifier_EscapesEmbeddedQuotes(string payload)
    {
        var compiler = new OracleCompiler();
        var escaped = compiler.EscapeIdentifier(payload);

        escaped.Should().StartWith("\"").And.EndWith("\"");
        var inner = escaped.Substring(1, escaped.Length - 2);
        inner.Should().Be(payload.ToUpperInvariant().Replace("\"", "\"\""));
    }

    [Theory]
    [InlineData("count; DROP TABLE Users; --")]
    [InlineData("alias\"")]
    [InlineData("alias'")]
    [InlineData("alias`")]
    [InlineData("alias--")]
    [InlineData("alias/*comment*/")]
    public void AsCount_WithMaliciousAlias_ThrowsArgumentException(string maliciousAlias)
    {
        var query = Sql.From<DummyEntity>();
        Action act = () => query.AsCount(maliciousAlias);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*contains invalid characters*");
    }

    [Theory]
    [InlineData("amount); DROP TABLE Users; --", "valid_alias")]
    [InlineData("amount", "alias; DROP TABLE Users; --")]
    [InlineData("col'", "valid")]
    [InlineData("col\"", "valid")]
    [InlineData("col`", "valid")]
    public void AsSum_WithMaliciousInput_ThrowsArgumentException(string column, string? alias)
    {
        var query = Sql.From<DummyEntity>();
        Action act = () => query.AsSum(column, alias);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*contains invalid characters*");
    }

    [Theory]
    [InlineData("price); DROP TABLE Users; --", null)]
    [InlineData("price", "alias--")]
    public void AsAvg_WithMaliciousInput_ThrowsArgumentException(string column, string? alias)
    {
        var query = Sql.From<DummyEntity>();
        Action act = () => query.AsAvg(column, alias);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*contains invalid characters*");
    }

    [Theory]
    [InlineData("age); DROP TABLE Users; --", null)]
    [InlineData("age", "alias/*hack*/")]
    public void AsMin_WithMaliciousInput_ThrowsArgumentException(string column, string? alias)
    {
        var query = Sql.From<DummyEntity>();
        Action act = () => query.AsMin(column, alias);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*contains invalid characters*");
    }

    [Theory]
    [InlineData("score); DROP TABLE Users; --", null)]
    [InlineData("score", "alias;--")]
    public void AsMax_WithMaliciousInput_ThrowsArgumentException(string column, string? alias)
    {
        var query = Sql.From<DummyEntity>();
        Action act = () => query.AsMax(column, alias);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*contains invalid characters*");
    }

    [Fact]
    public void PostgreSqlCompiler_WithMaliciousTableName_EscapesCleanlyWithoutInjection()
    {
        var compiler = new PostgreSqlCompiler();
        var maliciousTable = "users\"; DROP TABLE secret; --";
        var query = Sql.From<DummyEntity>().From(maliciousTable);

        var result = compiler.Compile(query);
        result.Sql.Should().Contain("\"users\"\"; DROP TABLE secret; --\"");
        result.Sql.Should().NotContain(";\nDROP TABLE");
    }
}
