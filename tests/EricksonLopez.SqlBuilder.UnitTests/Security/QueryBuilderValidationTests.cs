// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Security;

public class QueryBuilderValidationTests
{
    [Fact]
    public void SelectQuery_From_NullOrWhitespace_ThrowsArgumentException()
    {
        var builder = new SelectQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.From(null!));
        Assert.ThrowsAny<ArgumentException>(() => builder.From(""));
        Assert.ThrowsAny<ArgumentException>(() => builder.From("   "));
        Assert.ThrowsAny<ArgumentException>(() => builder.From("users", "   "));
    }

    [Fact]
    public void SelectQuery_FromSubquery_NullOrWhitespace_ThrowsException()
    {
        var builder = new SelectQuery<DummyEntity>();

        Assert.Throws<ArgumentNullException>(() => builder.From((ISqlQuery)null!, "sub"));
        Assert.ThrowsAny<ArgumentException>(() => builder.From(new SelectQuery<DummyEntity>(), ""));
    }

    [Fact]
    public void SelectQuery_Alias_NullOrWhitespace_ThrowsArgumentException()
    {
        var builder = new SelectQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.Alias(""));
        Assert.ThrowsAny<ArgumentException>(() => builder.Alias("   "));
    }

    [Fact]
    public void SelectQuery_Join_NullOrWhitespace_ThrowsArgumentException()
    {
        var builder = new SelectQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.Join("", "u", "1=1"));
        Assert.ThrowsAny<ArgumentException>(() => builder.Join("users", "", "1=1"));
        Assert.ThrowsAny<ArgumentException>(() => builder.Join("users", "u", ""));

        Assert.ThrowsAny<ArgumentException>(() => builder.InnerJoin("", "u", "1=1"));
        Assert.ThrowsAny<ArgumentException>(() => builder.LeftJoin("", "u", "1=1"));
        Assert.ThrowsAny<ArgumentException>(() => builder.RightJoin("", "u", "1=1"));
        Assert.ThrowsAny<ArgumentException>(() => builder.FullJoin("", "u", "1=1"));
        Assert.ThrowsAny<ArgumentException>(() => builder.CrossJoin("", "u"));
    }

    [Fact]
    public void SelectQuery_WhereColumns_InvalidIdentifierOrOperator_ThrowsArgumentException()
    {
        var builder = new SelectQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.WhereColumns("id; DROP TABLE users; --", "=", "other_id"));
        Assert.ThrowsAny<ArgumentException>(() => builder.WhereColumns("id", "= 1; DROP TABLE users; --", "other_id"));
        Assert.ThrowsAny<ArgumentException>(() => builder.WhereColumns("id", "=", "other_id; --"));
    }

    [Fact]
    public void SelectQuery_WhereDateAndParts_InvalidIdentifierOrOperator_ThrowsArgumentException()
    {
        var builder = new SelectQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.WhereDate("created_at; DROP TABLE users; --", "=", DateTime.UtcNow));
        Assert.ThrowsAny<ArgumentException>(() => builder.WhereDate("created_at", "MALICIOUS", DateTime.UtcNow));

        Assert.ThrowsAny<ArgumentException>(() => builder.WhereYear("created_at; --", "=", 2026));
        Assert.ThrowsAny<ArgumentException>(() => builder.WhereYear("created_at", "DROP", 2026));

        Assert.ThrowsAny<ArgumentException>(() => builder.WhereMonth("created_at; --", "=", 9));
        Assert.ThrowsAny<ArgumentException>(() => builder.WhereDay("created_at; --", "=", 5));
    }

    [Fact]
    public void SelectQuery_CTE_InvalidNameOrNullQuery_ThrowsException()
    {
        var builder = new SelectQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.CTE("", new SelectQuery<DummyEntity>()));
        Assert.ThrowsAny<ArgumentException>(() => builder.CTE("bad;cte", new SelectQuery<DummyEntity>()));
        Assert.Throws<ArgumentNullException>(() => builder.CTE("valid_cte", null!));
        Assert.ThrowsAny<ArgumentException>(() => builder.RecursiveCTE("bad;cte", new SelectQuery<DummyEntity>()));
    }

    [Fact]
    public void SelectQuery_Window_InvalidIdentifier_ThrowsArgumentException()
    {
        var builder = new SelectQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.Window("bad;win"));
        Assert.ThrowsAny<ArgumentException>(() => builder.Window("valid_win", partitionBy: new[] { "bad;col" }));
        Assert.ThrowsAny<ArgumentException>(() => builder.Window("valid_win", orderBy: new[] { "bad;col" }));
        Assert.ThrowsAny<ArgumentException>(() => builder.WindowPage(1, 10, "bad;col"));
    }

    [Fact]
    public void DeleteQuery_Validations_EnforceFailFast()
    {
        var builder = new DeleteQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.Delete(""));
        Assert.ThrowsAny<ArgumentException>(() => builder.Using(""));
        Assert.ThrowsAny<ArgumentException>(() => builder.Join("", "u", "1=1"));
        Assert.ThrowsAny<ArgumentException>(() => builder.Returning("bad;col"));
        Assert.Throws<ArgumentNullException>(() => builder.WhereExists(null!));
    }

    [Fact]
    public void UpdateQuery_Validations_EnforceFailFast()
    {
        var builder = new UpdateQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.From(""));
        Assert.ThrowsAny<ArgumentException>(() => builder.Join("", "u", "1=1"));
        Assert.ThrowsAny<ArgumentException>(() => builder.Returning("bad;col"));
        Assert.Throws<ArgumentNullException>(() => builder.WhereExists(null!));
    }

    [Fact]
    public void InsertQuery_Validations_EnforceFailFast()
    {
        var builder = new InsertQuery<DummyEntity>();

        Assert.ThrowsAny<ArgumentException>(() => builder.Into(""));
        Assert.ThrowsAny<ArgumentException>(() => builder.Returning("bad;col"));
        Assert.ThrowsAny<ArgumentException>(() => builder.OnConflict("bad;col"));
    }
}
