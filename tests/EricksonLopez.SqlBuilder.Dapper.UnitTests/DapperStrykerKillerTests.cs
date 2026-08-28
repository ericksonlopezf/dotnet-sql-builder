// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.UnitTests;

public class DapperStrykerKillerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ISqlCompiler _compiler;

    public DapperStrykerKillerTests()
    {
        _compiler = new SqliteCompiler();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => _compiler);

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Sku TEXT NOT NULL,
                Price DECIMAL(18,2) NOT NULL DEFAULT 0,
                Version INTEGER NOT NULL DEFAULT 1
            );
            INSERT INTO Products (Sku, Price, Version) VALUES ('P1', 10.0, 1);
            INSERT INTO Products (Sku, Price, Version) VALUES ('P2', 20.0, 1);
            INSERT INTO Products (Sku, Price, Version) VALUES ('P3', 30.0, 1);
            INSERT INTO Products (Sku, Price, Version) VALUES ('P4', 40.0, 1);
            INSERT INTO Products (Sku, Price, Version) VALUES ('P5', 50.0, 1);
        ";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public class TestProduct : ISqlEntity
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Version { get; set; }

        public string GetTableName() => "Products";
        public string[] GetColumnNames() => new[] { "Id", "Sku", "Price", "Version" };
        public object?[] GetValues() => new object?[] { Id, Sku, Price, Version };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    private class CustomType
    {
        public string Value { get; set; } = string.Empty;
    }

    private class MockTypeHandler : ITypeHandler
    {
        public void SetValue(IDbDataParameter parameter, object? value)
        {
            parameter.Value = (value as CustomType)?.Value ?? (object)DBNull.Value;
        }

        public object? Parse(Type destinationType, object? value)
        {
            return value is string s ? new CustomType { Value = s } : null;
        }
    }

    // 1. ConnectionSqlExtensions
    [Fact]
    public void Paginate_Arithmetic_CalculatesCorrectLimitAndOffset()
    {
        var query = Sql.From<TestProduct>().Paginate(3, 10);
        var ast = (IAstQuery)query;
        var limitNodes = ast.Nodes.OfType<LimitOffsetNode>().ToList();

        limitNodes.Should().HaveCount(2);
        limitNodes[0].Limit.Should().Be(10);
        limitNodes[1].Offset.Should().Be(20); // (3 - 1) * 10 = 20
    }

    private IDisposable CaptureExecutedSql(List<string> sqlList)
    {
        var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == "EricksonLopez.SqlBuilder",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                var stmt = activity.TagObjects.FirstOrDefault(t => t.Key == "db.statement").Value?.ToString();
                if (stmt != null)
                {
                    sqlList.Add(stmt);
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public async Task ToPagedListAsync_WithExplicitSelect_ExcludesSelectNodesFromCount()
    {
        var executedSql = new List<string>();
        using var listener = CaptureExecutedSql(executedSql);

        var queryExpr = Sql.From<TestProduct>().Select(p => p.Sku);
        var resultExpr = await queryExpr.ToPagedListAsync(_connection, 1, 2);
        resultExpr.IsSuccess.Should().BeTrue();
        resultExpr.Value.TotalCount.Should().Be(5);
        var countExprSql = executedSql.First(s => s.Contains("COUNT(*)"));
        countExprSql.Should().NotContain("Sku");
        executedSql.Clear();

        var queryString = Sql.From<TestProduct>().Select("Sku");
        var resultString = await queryString.ToPagedListAsync(_connection, 1, 2);
        resultString.IsSuccess.Should().BeTrue();
        resultString.Value.TotalCount.Should().Be(5);
        var countStringSql = executedSql.First(s => s.Contains("COUNT(*)"));
        countStringSql.Should().NotContain("Sku");
        executedSql.Clear();

        var queryRaw = Sql.From<TestProduct>().RawSelect($"Sku AS CustomSku");
        var resultRaw = await queryRaw.ToPagedListAsync(_connection, 1, 2);
        resultRaw.IsSuccess.Should().BeTrue();
        resultRaw.Value.TotalCount.Should().Be(5);
        var countRawSql = executedSql.First(s => s.Contains("COUNT(*)"));
        countRawSql.Should().NotContain("CustomSku");
    }

    [Fact]
    public void RegisterTypeHandler_RegistersInBothSqlAndDapper()
    {
        var handler = new MockTypeHandler();
        DapperExtensions.RegisterTypeHandler<CustomType>(handler);

        Sql.TypeHandlers.TryGetValue(typeof(CustomType), out var registered).Should().BeTrue();
        registered.Should().BeSameAs(handler);
    }

    // 2. DapperMultiMappingExtensions - Cancellation
    [Fact]
    public async Task MultiMap_QueryAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var query = Sql.From<TestProduct>();

        // 2-way
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _connection.QueryAsync<TestProduct, TestProduct, TestProduct>(query, (p1, p2) => p1, cancellationToken: cts.Token);
        });

        // 3-way
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _connection.QueryAsync<TestProduct, TestProduct, TestProduct, TestProduct>(query, (p1, p2, p3) => p1, cancellationToken: cts.Token);
        });

        // 4-way
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _connection.QueryAsync<TestProduct, TestProduct, TestProduct, TestProduct, TestProduct>(query, (p1, p2, p3, p4) => p1, cancellationToken: cts.Token);
        });

        // 5-way
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _connection.QueryAsync<TestProduct, TestProduct, TestProduct, TestProduct, TestProduct, TestProduct>(query, (p1, p2, p3, p4, p5) => p1, cancellationToken: cts.Token);
        });

        // 6-way
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _connection.QueryAsync<TestProduct, TestProduct, TestProduct, TestProduct, TestProduct, TestProduct, TestProduct>(query, (p1, p2, p3, p4, p5, p6) => p1, cancellationToken: cts.Token);
        });

        // 7-way
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await _connection.QueryAsync<TestProduct, TestProduct, TestProduct, TestProduct, TestProduct, TestProduct, TestProduct, TestProduct>(query, (p1, p2, p3, p4, p5, p6, p7) => p1, cancellationToken: cts.Token);
        });
    }

    // 3. DapperPaginationExtensions
    [Fact]
    public async Task QueryPagedAsync_WithExpressionSelectAndRawSelect_FiltersProperly()
    {
        var parameters = PaginationParameters.Create(1, 2);
        var executedSql = new List<string>();
        using var listener = CaptureExecutedSql(executedSql);

        // Expression Select only
        var queryExpr = Sql.From<TestProduct>().Select(p => p.Sku).Where(p => p.Price > 15);
        var pagedExpr = await _connection.QueryPagedAsync(queryExpr, parameters, countTotal: true);
        pagedExpr.TotalCount.Should().Be(4);
        var countExprSql = executedSql.First(s => s.Contains("COUNT(*)"));
        countExprSql.Should().NotContain("Sku");
        executedSql.Clear();

        // Raw Select only
        var queryRaw = Sql.From<TestProduct>().RawSelect($"Sku AS CustomSku").Where(p => p.Price > 15);
        var pagedRaw = await _connection.QueryPagedAsync(queryRaw, parameters, countTotal: true);
        pagedRaw.TotalCount.Should().Be(4);
        var countRawSql = executedSql.First(s => s.Contains("COUNT(*)"));
        countRawSql.Should().NotContain("CustomSku");
        executedSql.Clear();

        // String Select only
        var queryString = Sql.From<TestProduct>().Select("Sku").Where(p => p.Price > 15);
        var pagedString = await _connection.QueryPagedAsync(queryString, parameters, countTotal: true);
        pagedString.TotalCount.Should().Be(4);
        var countStringSql = executedSql.First(s => s.Contains("COUNT(*)"));
        countStringSql.Should().NotContain("Sku");
        executedSql.Clear();

        // OrderByNode
        var queryOrder = Sql.From<TestProduct>().OrderBy(p => p.Price).Where(p => p.Price > 15);
        var pagedOrder = await _connection.QueryPagedAsync(queryOrder, parameters, countTotal: true);
        pagedOrder.TotalCount.Should().Be(4);
        var countOrderSql = executedSql.First(s => s.Contains("COUNT(*)"));
        countOrderSql.Should().NotContain("ORDER BY");
        executedSql.Clear();

        // LimitOffsetNode
        var queryLimit = Sql.From<TestProduct>().Limit(2).Offset(1).Where(p => p.Price > 15);
        var pagedLimit = await _connection.QueryPagedAsync(queryLimit, parameters, countTotal: true);
        pagedLimit.TotalCount.Should().Be(4);
        var countLimitSql = executedSql.First(s => s.Contains("COUNT(*)"));
        countLimitSql.Should().NotContain("LIMIT");
        countLimitSql.Should().NotContain("OFFSET");
        executedSql.Clear();

        // Both
        var queryBoth = Sql.From<TestProduct>().Select(p => p.Sku).RawSelect($"1 AS Flag").Where(p => p.Price > 15);
        var pagedBoth = await _connection.QueryPagedAsync(queryBoth, parameters, countTotal: true);
        pagedBoth.TotalCount.Should().Be(4);
        pagedBoth.Count.Should().Be(2);
    }

    [Fact]
    public async Task QueryPagedAsync_WhenCountIsZero_ReturnsEmptyPagedListDirectly()
    {
        var executedSql = new List<string>();
        using var listener = CaptureExecutedSql(executedSql);

        var query = Sql.From<TestProduct>().Where(p => p.Price > 9999);
        var parameters = PaginationParameters.Create(1, 10);

        var pagedList = await _connection.QueryPagedAsync(query, parameters, countTotal: true);

        pagedList.TotalCount.Should().Be(0);
        pagedList.Count.Should().Be(0);
        pagedList.HasNextPage.Should().BeFalse();
        executedSql.Should().NotBeEmpty();
        executedSql.Should().OnlyContain(s => s.Contains("COUNT(*)"));
        executedSql.Should().NotContain(s => s.Contains("LIMIT"));
    }

    [Theory]
    [InlineData(null, "SELECT COUNT(*) FROM Products")]
    [InlineData("", "SELECT COUNT(*) FROM Products")]
    [InlineData("   ", "SELECT COUNT(*) FROM Products")]
    [InlineData("SELECT * FROM Products", null)]
    [InlineData("SELECT * FROM Products", "")]
    [InlineData("SELECT * FROM Products", "   ")]
    public async Task QueryPagedRawAsync_NullOrWhiteSpace_ThrowsArgumentException(string? sql, string? countSql)
    {
        var parameters = PaginationParameters.Create(1, 10);
        await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
        {
            await _connection.QueryPagedRawAsync<TestProduct>(sql!, countSql!, parameters);
        });
    }

    // 4. JsonbTypeHandler & Registrar
    [Fact]
    public void SetJsonb_WithNpgsqlParameter_SetsJsonbTypeOnFirstAndSubsequentCalls()
    {
        var helperType = typeof(DapperExtensions).Assembly.GetType("EricksonLopez.SqlBuilder.Dapper.NpgsqlParameterHelper");
        helperType?.GetField("_initialized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.SetValue(null, false);
        helperType?.GetField("_npgsqlDbTypeProperty", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.SetValue(null, null);

        var handler = new JsonbTypeHandler<TestProduct>();

        var p1 = new NpgsqlParameter();
        handler.SetValue(p1, new TestProduct { Sku = "SKU1" });
        p1.NpgsqlDbType.Should().Be(NpgsqlDbType.Jsonb);

        var p2 = new NpgsqlParameter();
        handler.SetValue(p2, new TestProduct { Sku = "SKU2" });
        p2.NpgsqlDbType.Should().Be(NpgsqlDbType.Jsonb);

        // Non-NpgsqlParameter should return early
        var sqliteParam = new SqliteParameter();
        handler.SetValue(sqliteParam, new TestProduct { Sku = "SKU3" });
    }

    [Fact]
    public void JsonbTypeHandler_Parse_CaseInsensitiveDeserialization_Succeeds()
    {
        var handler = new JsonbTypeHandler<TestProduct>();
        var json = "{\"SKU\":\"UPPERCASE_SKU\",\"PRICE\":99.9}";

        var product = handler.Parse(json);

        product.Should().NotBeNull();
        product!.Sku.Should().Be("UPPERCASE_SKU");
        product.Price.Should().Be(99.9m);
    }

    [Fact]
    public void RegisterJsonbHandlers_ExecutesAllPassedRegistrations()
    {
        int count1 = 0;
        int count2 = 0;
        int count3 = 0;

        PostgreSqlTypeHandlerRegistrar.RegisterJsonbHandlers(
            () => count1++,
            () => count2++,
            () => count3++
        );

        count1.Should().Be(1);
        count2.Should().Be(1);
        count3.Should().Be(1);
    }

    // 5. Activity Operation Name Verifications (Kill Mutants 155, 170, 185)
    [Fact]
    public async Task QuerySequentialAsync_RecordsCorrectActivityOperationName()
    {
        string? activityName = null;
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == "EricksonLopez.SqlBuilder",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => activityName = a.OperationName
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var query = Sql.From<TestProduct>();
        var results = await _connection.QuerySequentialAsync(query, r => new TestProduct { Sku = r.GetString(1) });
        results.Should().NotBeEmpty();
        activityName.Should().Be("db.query_sequential");
    }

    [Fact]
    public async Task QueryAotAsync_RecordsCorrectActivityOperationName()
    {
        string? activityName = null;
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == "EricksonLopez.SqlBuilder",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => activityName = a.OperationName
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var query = Sql.From<TestProduct>();
        var results = await _connection.QueryAotAsync(query, r => new TestProduct { Sku = r.GetString(1) });
        results.Should().NotBeEmpty();
        activityName.Should().Be("db.query_aot");
    }

    [Fact]
    public async Task QueryFirstOrDefaultAotAsync_RecordsCorrectActivityOperationName()
    {
        string? activityName = null;
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == "EricksonLopez.SqlBuilder",
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => activityName = a.OperationName
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var query = Sql.From<TestProduct>();
        var result = await _connection.QueryFirstOrDefaultAotAsync(query, r => new TestProduct { Sku = r.GetString(1) });
        result.Should().NotBeNull();
        activityName.Should().Be("db.query_first_aot");
    }

    // 6. PagedList Bounds & Guards
    [Fact]
    public void PagedList_PageSizeEqualsOne_Succeeds()
    {
        var list = PagedList<int>.WithCount(new[] { 42 }, PaginationParameters.Create(1, 1), 1);
        list.PageSize.Should().Be(1);
        list.Page.Should().Be(1);
        list.TotalCount.Should().Be(1);
        list.TotalPages.Should().Be(1);
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedList_PageEqualsTotalPages_HasNextPageIsFalse()
    {
        // 2 items total, page 2 of pageSize 1 -> TotalPages = 2, Page = 2
        var list = PagedList<int>.WithCount(new[] { 2 }, PaginationParameters.Create(2, 1), 2);
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PagedList_ConstructorGuards_ValidateArgumentNames()
    {
        var actPage = () => new PagedList<int>(new[] { 1 }, 10, 0, 10);
        var exPage = actPage.Should().Throw<ArgumentOutOfRangeException>().Which;
        exPage.ParamName.Should().Be("page");
        exPage.Message.Should().Contain("Page must be greater than or equal to 1.");

        var actPageSize = () => new PagedList<int>(new[] { 1 }, 10, 1, 0);
        var exPageSize = actPageSize.Should().Throw<ArgumentOutOfRangeException>().Which;
        exPageSize.ParamName.Should().Be("pageSize");
        exPageSize.Message.Should().Contain("PageSize must be greater than or equal to 1.");

        var actTotal = () => new PagedList<int>(new[] { 1 }, -1, 1, 10);
        var exTotal = actTotal.Should().Throw<ArgumentOutOfRangeException>().Which;
        exTotal.ParamName.Should().Be("totalCount");
        exTotal.Message.Should().Contain("TotalCount cannot be negative.");
    }

    [Fact]
    public void PagedList_Map_NullSelector_ThrowsArgumentNullException()
    {
        // CountedPagedList Map
        var countedList = PagedList<int>.WithCount(new[] { 1, 2 }, PaginationParameters.Create(1, 10), 2);
        var actCounted = () => countedList.Map<string>(null!);
        actCounted.Should().Throw<ArgumentNullException>().WithParameterName("selector");

        // Base PagedList Map (without count)
        var uncountedList = PagedList<int>.WithoutCount(new[] { 1, 2 }, PaginationParameters.Create(1, 10), false);
        var actUncounted = () => uncountedList.Map<string>(null!);
        actUncounted.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void PagedList_NonGenericEnumerable_GetEnumerator_ReturnsNonNull()
    {
        var list = PagedList<int>.WithCount(new[] { 1, 2, 3 }, PaginationParameters.Create(1, 10), 3);
        var enumerable = (System.Collections.IEnumerable)list;
        var enumerator = enumerable.GetEnumerator();
        enumerator.Should().NotBeNull();
        enumerator.MoveNext().Should().BeTrue();
        enumerator.Current.Should().Be(1);
    }

    [Fact]
    public void PagedList_NullItemsConstructor_InitializesEmptyList()
    {
        var list = new PagedList<int>(null!, null, 1, 10);
        list.Count.Should().Be(0);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryPagedAsync_WithRawOrderBy_FiltersRawOrderByFromCount()
    {
        var query = Sql.From<TestProduct>().OrderBy($"Id DESC");
        var parameters = PaginationParameters.Create(1, 10);
        var result = await _connection.QueryPagedAsync(query, parameters, countTotal: true);
        result.TotalCount.Should().Be(5);
        result.Count.Should().Be(5);
    }
}

