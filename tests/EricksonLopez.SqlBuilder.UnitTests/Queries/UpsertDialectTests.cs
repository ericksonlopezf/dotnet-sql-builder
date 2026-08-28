// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

/// <summary>
/// Unit tests for upsert (INSERT ... ON CONFLICT) functionality per SQL dialect.
/// Verifies each dialect emits the correct SQL syntax.
/// </summary>
public class UpsertDialectTests
{

    // ─────────────────────────────────────────────────────────────────────────
    // PostgreSQL — INSERT ... ON CONFLICT DO UPDATE SET ...
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PostgreSql_OnConflict_DoUpdateSet_EmitsCorrectSyntax()
    {
        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var node = new OnConflictNode(new[] { "email" }, "DO UPDATE SET", null, null);
        var query = new InsertQuery<User>().Into("users").AddNode(node);

        var result = compiler.Compile(query);

        result.Sql.Should().Contain("ON CONFLICT (\"email\")");
        result.Sql.Should().Contain("DO UPDATE SET");
    }

    [Fact]
    public void PostgreSql_OnConflict_DoNothing_EmitsDoNothing()
    {
        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var node = new OnConflictNode(new[] { "email" }, "DO NOTHING", null, null);
        var query = new InsertQuery<User>().Into("users").AddNode(node);

        var result = compiler.Compile(query);

        result.Sql.Should().Contain("ON CONFLICT");
        result.Sql.Should().Contain("DO NOTHING");
    }

    [Fact]
    public void PostgreSql_OnConflict_NoTargetColumns_EmitsConflictWithoutTarget()
    {
        var compiler = new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler();
        var node = new OnConflictNode(Array.Empty<string>(), "DO NOTHING", null, null);
        var query = new InsertQuery<User>().Into("users").AddNode(node);

        var result = compiler.Compile(query);

        // When no conflict target, just emits "ON CONFLICT DO NOTHING"
        result.Sql.Should().Contain("ON CONFLICT");
        result.Sql.Should().Contain("DO NOTHING");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MySQL — INSERT ... ON DUPLICATE KEY UPDATE
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void MySql_OnConflict_DoUpdateSet_EmitsOnDuplicateKeyUpdate()
    {
        var compiler = new EricksonLopez.SqlBuilder.MySql.MySqlCompiler();
        var node = new OnConflictNode(new[] { "email" }, "DO UPDATE SET", null, null);
        var query = new InsertQuery<User>().Into("users").AddNode(node);

        var result = compiler.Compile(query);

        result.Sql.Should().Contain("ON DUPLICATE KEY UPDATE");
    }

    [Fact]
    public void MySql_OnConflict_DoNothing_EmitsOnDuplicateKeyPattern()
    {
        var compiler = new EricksonLopez.SqlBuilder.MySql.MySqlCompiler();
        var node = new OnConflictNode(Array.Empty<string>(), "DO NOTHING", null, null);
        var query = new InsertQuery<User>().Into("users").AddNode(node);

        var result = compiler.Compile(query);

        // MySQL DO NOTHING emits: ON DUPLICATE KEY UPDATE `id` = `id`
        result.Sql.Should().Contain("ON DUPLICATE KEY UPDATE");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SQLite — INSERT OR REPLACE / INSERT OR IGNORE
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Sqlite_OnConflict_DoUpdateSet_EmitsOnConflictDoUpdateSet()
    {
        var compiler = new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler();
        var node = new OnConflictNode(new[] { "id" }, "DO UPDATE SET", null, null);
        var query = new InsertQuery<User>().Into("users").AddNode(node);

        var result = compiler.Compile(query);

        // SQLite uses standard ON CONFLICT ... DO UPDATE SET syntax
        result.Sql.Should().Contain("ON CONFLICT");
        result.Sql.Should().Contain("DO UPDATE SET");
    }

    [Fact]
    public void Sqlite_OnConflict_DoNothing_EmitsOnConflictDoNothing()
    {
        var compiler = new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler();
        var node = new OnConflictNode(new[] { "id" }, "DO NOTHING", null, null);
        var query = new InsertQuery<User>().Into("users").AddNode(node);

        var result = compiler.Compile(query);

        // SQLite uses standard ON CONFLICT ... DO NOTHING syntax
        result.Sql.Should().Contain("ON CONFLICT");
        result.Sql.Should().Contain("DO NOTHING");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SQL Server — MERGE or OUTPUT-based upsert
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SqlServer_OnConflict_ThrowsNotSupported_UseMergeInstead()
    {
        var compiler = new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler();
        var node = new OnConflictNode(new[] { "id" }, "DO UPDATE SET", null, null);
        var query = new InsertQuery<User>().Into("users").AddNode(node);

        var act = () => compiler.Compile(query);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*SQL Server*MERGE*");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Oracle — MERGE INTO
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Oracle_OnConflict_ThrowsNotSupported_UseMergeInstead()
    {
        var compiler = new EricksonLopez.SqlBuilder.Oracle.OracleCompiler();
        var node = new OnConflictNode(new[] { "id" }, "DO UPDATE SET", null, null);
        var query = new InsertQuery<User>().Into("users").AddNode(node);

        // Oracle does not support ON CONFLICT — use Sql.Merge<T>() instead
        var act = () => compiler.Compile(query);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*Oracle*");
    }
}



