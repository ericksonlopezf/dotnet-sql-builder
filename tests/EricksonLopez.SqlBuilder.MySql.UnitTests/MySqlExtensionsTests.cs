// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.MySql.UnitTests;

public class MySqlExtensionsTests
{

    [Fact]
    public void WhereJsonExtract_ReturnsQuery()
    {
        var builder = Sql.From<DummyEntity>();
        var result = builder.WhereJsonExtract("data", "$.name", "test");
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)result;
        var compiled = query.Build(new MySqlCompiler());
        compiled.Sql.Trim().Should().Be("SELECT * FROM `dummy_entity` WHERE JSON_EXTRACT(`data`, '$.name') = @p0");
        compiled.Parameters["p0"].Should().Be("test");
    }

    [Fact]
    public void SelectJsonArrayAgg_ReturnsQuery()
    {
        var builder = Sql.From<DummyEntity>();
        var result = builder.SelectJsonArrayAgg("id", "ids");
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)result;
        var compiled = query.Build(new MySqlCompiler());
        compiled.Sql.Trim().Should().Be("SELECT JSON_ARRAYAGG(`id`) AS `ids` FROM `dummy_entity`");
    }
    
    [Fact]
    public void SelectJsonObjectAgg_ReturnsQuery()
    {
        var builder = Sql.From<DummyEntity>();
        var result = builder.SelectJsonObjectAgg("id", "name", "obj");
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)result;
        var compiled = query.Build(new MySqlCompiler());
        compiled.Sql.Trim().Should().Be("SELECT JSON_OBJECTAGG(`id`, `name`) AS `obj` FROM `dummy_entity`");
    }

    [Fact]
    public void WhereFullText_ReturnsQuery()
    {
        var builder = Sql.From<DummyEntity>();
        var result = builder.WhereFullText("keyword", "name", "bio");
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)result;
        var compiled = query.Build(new MySqlCompiler());
        compiled.Sql.Trim().Should().Be("SELECT * FROM `dummy_entity` WHERE MATCH(`name`, `bio`) AGAINST (@p0 IN BOOLEAN MODE)");
        compiled.Parameters["p0"].Should().Be("keyword");
    }

    [Fact]
    public void BuildOnDuplicateKeyUpdate_ReturnsString()
    {
        var result = MySqlExtensions.BuildOnDuplicateKeyUpdate("id", "name");
        result.Should().Be("`id` = VALUES(`id`), `name` = VALUES(`name`)");
    }

    [Fact]
    public void Page_CalculatesCorrectLimitOffset()
    {
        var builder = Sql.From<DummyEntity>().Page(2, 10);
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)builder;
        var limitNode = query.Nodes.OfType<LimitOffsetNode>().FirstOrDefault(n => n.Limit.HasValue);
        var offsetNode = query.Nodes.OfType<LimitOffsetNode>().FirstOrDefault(n => n.Offset.HasValue);
        limitNode.Should().NotBeNull();
        offsetNode.Should().NotBeNull();
        limitNode!.Limit.Should().Be(10);
        offsetNode!.Offset.Should().Be(10);
    }
    
    [Fact]
    public void Page_PageSizeZero_FallsBackToDefault()
    {
        var builder = Sql.From<DummyEntity>();
        var paged = builder.Page(2, 0);
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)paged;
        var limitNode = query.Nodes.OfType<LimitOffsetNode>().FirstOrDefault(n => n.Limit.HasValue);
        limitNode.Should().NotBeNull();
        limitNode!.Limit.Should().Be(10);
    }

    [Fact]
    public void Page_PageNumberZero_FallsBackToDefault()
    {
        var builder = Sql.From<DummyEntity>();
        var paged = builder.Page(0, 10);
        var query = (EricksonLopez.SqlBuilder.Abstractions.IAstQuery)paged;
        var offsetNode = query.Nodes.OfType<LimitOffsetNode>().FirstOrDefault(n => n.Offset.HasValue);
        offsetNode.Should().NotBeNull();
        offsetNode!.Offset.Should().Be(0);
    }
}





