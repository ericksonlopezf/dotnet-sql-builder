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

internal sealed class NullToStringKey
{
    public override string? ToString() => null;
}

internal sealed class CustomTestEncoder : ICursorEncoder
{
    public bool EncodeCalled { get; private set; }
    public bool DecodeCalled { get; private set; }

    public string Encode(string rawValue)
    {
        EncodeCalled = true;
        return "CUSTOM_" + rawValue;
    }

    public string? Decode(string cursor)
    {
        DecodeCalled = true;
        return cursor.StartsWith("CUSTOM_", StringComparison.Ordinal) ? cursor["CUSTOM_".Length..] : null;
    }
}

internal sealed class EmptyDecodingTestEncoder : ICursorEncoder
{
    public string Encode(string rawValue) => "TOKEN";
    public string? Decode(string cursor) => string.Empty;
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
        sqlResult.Sql.Should().NotContain("DESC");
        sqlResult.Sql.Should().Contain("LIMIT 16");
    }

    [Fact]
    public void ApplyCursor_WhenOnlyLastProvided_SetsLimitBasedOnLast()
    {
        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { Last = 7 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 8");
    }

    [Fact]
    public void ApplyCursor_WhenNeitherFirstNorLastProvided_DefaultsToPageSizeTen()
    {
        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters();

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("LIMIT 11");
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
    public void ApplyCursor_WithAfterCursor_Ascending_AppliesGreaterThanPredicate()
    {
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        var cursorToken = encoder.Encode("42");

        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { After = cursorToken, First = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder: encoder, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("WHERE (id > @p0)");
        sqlResult.Sql.Should().NotContain("DESC");
        sqlResult.Parameters.Values.Should().Contain(42);
    }

    [Fact]
    public void ApplyCursor_WithAfterCursor_Descending_AppliesLessThanPredicate()
    {
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        var cursorToken = encoder.Encode("42");

        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { After = cursorToken, First = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder: encoder, ascending: false);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("WHERE (id < @p0)");
        sqlResult.Sql.Should().Contain("DESC");
        sqlResult.Parameters.Values.Should().Contain(42);
    }

    [Fact]
    public void ApplyCursor_WithBeforeCursor_Ascending_AppliesLessThanPredicate()
    {
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        var cursorToken = encoder.Encode("100");

        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { Before = cursorToken, Last = 5 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder: encoder, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("WHERE (id < @p0)");
        sqlResult.Sql.Should().NotContain("DESC");
        sqlResult.Parameters.Values.Should().Contain(100);
    }

    [Fact]
    public void ApplyCursor_WithBeforeCursor_Descending_AppliesGreaterThanPredicate()
    {
        var encoder = HmacCursorEncoder.DevelopmentDefault;
        var cursorToken = encoder.Encode("100");

        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { Before = cursorToken, Last = 5 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder: encoder, ascending: false);
        var sqlResult = cursorQuery.Build(_compiler);

        sqlResult.Sql.Should().Contain("WHERE (id > @p0)");
        sqlResult.Sql.Should().Contain("DESC");
        sqlResult.Parameters.Values.Should().Contain(100);
    }

    [Fact]
    public void ApplyCursor_WithCustomEncoder_UsesPassedEncoderInstance()
    {
        var customEncoder = new CustomTestEncoder();
        var cursorToken = customEncoder.Encode("99");

        var query = Sql.From<TestEntity>();
        var parameters = new CursorPaginationParameters { After = cursorToken, First = 10 };

        var cursorQuery = query.ApplyCursor(parameters, x => x.Id, encoder: customEncoder, ascending: true);
        var sqlResult = cursorQuery.Build(_compiler);

        customEncoder.DecodeCalled.Should().BeTrue();
        sqlResult.Sql.Should().Contain("WHERE (id > @p0)");
        sqlResult.Parameters.Values.Should().Contain(99);
    }

    [Fact]
    public void ApplyCursor_WithEmptyDecodedCursor_DoesNotApplySeekPredicate()
    {
        var emptyEncoder = new EmptyDecodingTestEncoder();
        var query = Sql.From<TestEntity>();
        var parametersAfter = new CursorPaginationParameters { After = "TOKEN", First = 10 };
        var parametersBefore = new CursorPaginationParameters { Before = "TOKEN", Last = 10 };

        var queryAfter = query.ApplyCursor(parametersAfter, x => x.Id, encoder: emptyEncoder);
        var queryBefore = query.ApplyCursor(parametersBefore, x => x.Id, encoder: emptyEncoder);

        queryAfter.Build(_compiler).Sql.Should().NotContain("WHERE");
        queryBefore.Build(_compiler).Sql.Should().NotContain("WHERE");
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
    public void ToCursorPagedList_WhenItemsCountEqualsPageSize_HasNextPageIsFalse()
    {
        var items = Enumerable.Range(1, 10).Select(i => new TestEntity { Id = i, Name = $"Item {i}" }).ToList();
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.Should().HaveCount(10);
        result.HasNextPage.Should().BeFalse();
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
    public void ToCursorPagedList_WithAfterParameter_HasPreviousPageIsTrue()
    {
        var items = Enumerable.Range(1, 5).Select(i => new TestEntity { Id = i, Name = $"Item {i}" }).ToList();
        var parameters = new CursorPaginationParameters { First = 10, After = "valid_token" };

        var result = items.ToCursorPagedList(parameters, x => x.Id);

        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void ToCursorPagedList_WithCustomEncoder_EncodesUsingPassedInstance()
    {
        var customEncoder = new CustomTestEncoder();
        var items = new List<TestEntity> { new() { Id = 77, Name = "Item 77" } };
        var parameters = new CursorPaginationParameters { First = 10 };

        var result = items.ToCursorPagedList(parameters, x => x.Id, encoder: customEncoder);

        customEncoder.EncodeCalled.Should().BeTrue();
        result.StartCursor.Should().Be("CUSTOM_77");
        result.EndCursor.Should().Be("CUSTOM_77");
    }

    [Fact]
    public void ToCursorPagedList_WithNullKeyToString_EncodesEmptyString()
    {
        var items = new List<TestEntity> { new() { Id = 1 } };
        var customEncoder = new CustomTestEncoder();

        var result = items.ToCursorPagedList(new CursorPaginationParameters(), _ => new NullToStringKey(), encoder: customEncoder);

        result.StartCursor.Should().Be("CUSTOM_");
        result.EndCursor.Should().Be("CUSTOM_");
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
        result.HasPreviousPage.Should().BeFalse();
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
    public void ToPagedList_WithExplicitNumbers_NonDefaultValues_SetsPropertiesCorrectly()
    {
        var items = new List<TestEntity> { new() { Id = 1 } };

        var result = items.ToPagedList(totalCount: 100, pageNumber: 4, pageSize: 25);

        result.Should().HaveCount(1);
        result.TotalCount.Should().Be(100);
        result.Page.Should().Be(4);
        result.PageSize.Should().Be(25);
        result.TotalPages.Should().Be(4);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void ToPagedList_FromEnumerable_MaterializesListCorrectly()
    {
        static IEnumerable<TestEntity> GenerateItems()
        {
            yield return new TestEntity { Id = 10 };
            yield return new TestEntity { Id = 20 };
        }

        var result = GenerateItems().ToPagedList(totalCount: 2, pageNumber: 1, pageSize: 10);

        result.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    #endregion
}
