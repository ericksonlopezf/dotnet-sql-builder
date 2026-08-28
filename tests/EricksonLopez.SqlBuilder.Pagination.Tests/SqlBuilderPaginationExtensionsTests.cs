// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Pagination;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.Pagination.Tests;

internal sealed class TestEntity : ISqlEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public string GetTableName() => "test_entities";
    public string[] GetColumnNames() => new[] { "id", "name", "price" };
    public object?[] GetValues() => new object?[] { Id, Name, Price };
    public string[] GetAllColumnNames() => new[] { "id", "name", "price" };
    public object?[] GetAllValues() => new object?[] { Id, Name, Price };
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Id"] = "id",
        ["Name"] = "name",
        ["Price"] = "price"
    };
    public string[] GetIndexedColumns() => new[] { "id" };
}

public class SqlBuilderPaginationExtensionsTests
{
    private readonly PostgreSqlCompiler _compiler = new();

    #region Paginate Tests

    [Fact]
    public void Paginate_NullQuery_ThrowsArgumentNullException()
    {
        SelectQuery<TestEntity> query = null!;
        var act1 = () => query.Paginate(new PaginationParameters { Page = 1, PageSize = 10 });
        var act2 = () => query.Paginate(1, 10);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Paginate_WithParameters_AppliesLimitAndOffsetCorrectly()
    {
        var query = Sql.From<TestEntity>();
        var parameters = new PaginationParameters { Page = 3, PageSize = 25 };

        var paginatedQuery = query.Paginate(parameters);
        var sqlResult = paginatedQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 25");
        sqlResult.Sql.Should().Contain("OFFSET 50");
    }

    [Fact]
    public void Paginate_WithExplicitNumbers_AppliesLimitAndOffsetCorrectly()
    {
        var query = Sql.From<TestEntity>();

        var paginatedQuery = query.Paginate(pageNumber: 2, pageSize: 10);
        var sqlResult = paginatedQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 10");
        sqlResult.Sql.Should().Contain("OFFSET 10");
    }

    [Fact]
    public void Paginate_WithInvalidArguments_ThrowsArgumentOutOfRangeException()
    {
        var query = Sql.From<TestEntity>();

        var act1 = () => query.Paginate(pageNumber: 0, pageSize: 10);
        var act2 = () => query.Paginate(pageNumber: 1, pageSize: 0);

        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region ApplyCursor Tests

    [Fact]
    public void ApplyCursor_NullQueryOrKeySelector_ThrowsArgumentNullException()
    {
        SelectQuery<TestEntity> query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters();

        var act1 = () => ((SelectQuery<TestEntity>)null!).ApplyCursor(parameters, x => x.Id);
        var act2 = () => query.ApplyCursor<TestEntity, int>(parameters, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ApplyCursor_Default_OrdersAscendingAndSetsLimitPlusOne()
    {
        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { First = 15 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("ORDER BY");
        sqlResult.Sql.Should().Contain("LIMIT 16");
    }

    [Fact]
    public void ApplyCursor_Descending_OrdersDescendingAndSetsLimitPlusOne()
    {
        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { First = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, ascending: false);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("ORDER BY");
        sqlResult.Sql.Should().Contain("DESC");
        sqlResult.Sql.Should().Contain("LIMIT 11");
    }

    [Fact]
    public void ApplyCursor_WithAfterCursor_DecodesAndAppliesSeekPredicate()
    {
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        var cursorToken = encoder.Encode("42");

        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { After = cursorToken, First = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder: encoder, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("WHERE (id > @p0)");
        sqlResult.Parameters.Values.Should().Contain(42);
    }

    [Fact]
    public void ApplyCursor_WithBeforeCursor_DecodesAndAppliesSeekPredicate()
    {
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        var cursorToken = encoder.Encode("100");

        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { Before = cursorToken, Last = 5 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder: encoder, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("WHERE (id < @p0)");
        sqlResult.Parameters.Values.Should().Contain(100);
    }

    #endregion

    #region Materialization Tests

    [Fact]
    public void ToCursorPagedList_NullArguments_ThrowsArgumentNullException()
    {
        IReadOnlyList<TestEntity> items = null!;
        var parameters = new CursorPaginationParameters();

        var act1 = () => items.ToCursorPagedList(parameters, x => x.Id);
        var act2 = () => new List<TestEntity>().ToCursorPagedList<TestEntity, int>(parameters, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToCursorPagedList_WhenItemsExceedPageSize_HasNextPageIsTrue()
    {
        var items = Enumerable.Range(1, 11).Select(i => new TestEntity { Id = i, Name = $"Item {i}" }).ToList();
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.Should().HaveCount(10);
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
        result.StartCursor.Should().NotBeNullOrEmpty();
        result.EndCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ToCursorPagedList_WhenItemsFitWithinPageSize_HasNextPageIsFalse()
    {
        var items = Enumerable.Range(1, 5).Select(i => new TestEntity { Id = i, Name = $"Item {i}" }).ToList();
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.Should().HaveCount(5);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ToCursorPagedList_EmptyItems_ReturnsEmptyPagedList()
    {
        var items = new List<TestEntity>();
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.Should().BeEmpty();
        result.StartCursor.Should().BeNull();
        result.EndCursor.Should().BeNull();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ToPagedList_WithParameters_MaterializesCorrectly()
    {
        var items = new List<TestEntity> { new() { Id = 1 }, new() { Id = 2 } };
        var parameters = new PaginationParameters { Page = 2, PageSize = 10 };

        var result = items.ToPagedList(totalCount: 50, parameters: parameters);

        result.Should().HaveCount(2);
        result.TotalCount.Should().Be(50);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(5);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void ToPagedList_WithExplicitNumbers_MaterializesCorrectly()
    {
        var items = new List<TestEntity> { new() { Id = 1 } };

        var result = items.ToPagedList(totalCount: 1, pageNumber: 1, pageSize: 10);

        result.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    #endregion
}
