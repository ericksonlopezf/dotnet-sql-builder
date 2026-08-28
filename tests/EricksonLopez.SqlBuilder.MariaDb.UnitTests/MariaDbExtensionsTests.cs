// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using Xunit;

namespace EricksonLopez.SqlBuilder.MariaDb.Tests;

public class MariaDbExtensionsTests
{
    private readonly MariaDbCompiler _compiler = new();

    // ─── JSON Functions ───────────────────────────────────────────────────────

    [Fact]
    public void WhereJsonExtract_GeneratesCorrectSql()
    {
        var builder = Sql.From<DummyEntity>();
        var result = builder.WhereJsonExtract("data", "$.name", "test");
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)result;
        var compiled = query.Build(_compiler);

        compiled.Sql.Trim().Should().Be("SELECT * FROM `dummy_entity` WHERE JSON_EXTRACT(`data`, '$.name') = @p0");
        compiled.Parameters["p0"].Should().Be("test");
    }

    [Fact]
    public void SelectJsonArrayAgg_GeneratesCorrectSql()
    {
        var builder = Sql.From<DummyEntity>();
        var result = builder.SelectJsonArrayAgg("id", "ids");
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)result;
        var compiled = query.Build(_compiler);

        compiled.Sql.Trim().Should().Be("SELECT JSON_ARRAYAGG(`id`) AS `ids` FROM `dummy_entity`");
    }

    [Fact]
    public void SelectJsonObjectAgg_GeneratesCorrectSql()
    {
        var builder = Sql.From<DummyEntity>();
        var result = builder.SelectJsonObjectAgg("id", "name", "obj");
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)result;
        var compiled = query.Build(_compiler);

        compiled.Sql.Trim().Should().Be("SELECT JSON_OBJECTAGG(`id`, `name`) AS `obj` FROM `dummy_entity`");
    }

    // ─── Full-Text Search ─────────────────────────────────────────────────────

    [Fact]
    public void WhereFullText_GeneratesCorrectSql()
    {
        var builder = Sql.From<DummyEntity>();
        var result = builder.WhereFullText("keyword", "name", "bio");
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)result;
        var compiled = query.Build(_compiler);

        compiled.Sql.Trim().Should().Be("SELECT * FROM `dummy_entity` WHERE MATCH(`name`, `bio`) AGAINST (@p0 IN BOOLEAN MODE)");
        compiled.Parameters["p0"].Should().Be("keyword");
    }

    // ─── ON DUPLICATE KEY UPDATE ──────────────────────────────────────────────

    [Fact]
    public void BuildOnDuplicateKeyUpdate_ReturnsCorrectString()
    {
        var result = MariaDbExtensions.BuildOnDuplicateKeyUpdate("id", "name");

        result.Should().Be("`id` = VALUES(`id`), `name` = VALUES(`name`)");
    }

    [Fact]
    public void BuildOnDuplicateKeyUpdate_SingleColumn_ReturnsCorrectString()
    {
        var result = MariaDbExtensions.BuildOnDuplicateKeyUpdate("email");

        result.Should().Be("`email` = VALUES(`email`)");
    }

    // ─── Pagination ───────────────────────────────────────────────────────────

    [Fact]
    public void Page_CalculatesCorrectLimitOffset()
    {
        var builder = Sql.From<DummyEntity>().Page(2, 10);
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)builder;
        var compiled = query.Build(_compiler);

        compiled.Sql.Trim().Should().Contain("LIMIT 10 OFFSET 10");
    }

    [Fact]
    public void Page_FirstPage_HasZeroOffset()
    {
        var builder = Sql.From<DummyEntity>().Page(1, 25);
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)builder;
        var compiled = query.Build(_compiler);

        compiled.Sql.Trim().Should().Contain("LIMIT 25");
        compiled.Sql.Should().Contain("OFFSET 0");
    }

    [Fact]
    public void Page_PageSizeZero_FallsBackToDefault()
    {
        var builder = Sql.From<DummyEntity>().Page(2, 0);
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)builder;
        var compiled = query.Build(_compiler);

        // Default page size is 10
        compiled.Sql.Trim().Should().Contain("LIMIT 10");
    }

    [Fact]
    public void Page_PageNumberZero_FallsBackToPageOne()
    {
        var builder = Sql.From<DummyEntity>().Page(0, 10);
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)builder;
        var compiled = query.Build(_compiler);

        // Page 1 → OFFSET 0
        compiled.Sql.Trim().Should().Contain("OFFSET 0");
    }
}
