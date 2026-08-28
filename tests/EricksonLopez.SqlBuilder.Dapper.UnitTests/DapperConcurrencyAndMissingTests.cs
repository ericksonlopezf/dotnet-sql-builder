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
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.UnitTests;

public sealed class DapperConcurrencyAndMissingTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ISqlCompiler _compiler = new SqliteCompiler();

    public DapperConcurrencyAndMissingTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => _compiler);

        CreateSchema();
        SeedData();
    }

    public void Dispose() => _connection.Dispose();

    private void CreateSchema()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS products (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                category_id INTEGER NOT NULL DEFAULT 0,
                name        TEXT    NOT NULL,
                sku         TEXT    NOT NULL DEFAULT '',
                description TEXT,
                price       REAL    NOT NULL DEFAULT 0,
                cost_price  REAL    NOT NULL DEFAULT 0,
                stock       INTEGER NOT NULL DEFAULT 0,
                min_stock   INTEGER NOT NULL DEFAULT 0,
                is_active   INTEGER NOT NULL DEFAULT 1,
                created_at  TEXT    NOT NULL,
                updated_at  TEXT
            )";
        cmd.ExecuteNonQuery();
    }

    private void SeedData()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO products (category_id, name, sku, price, cost_price, stock, min_stock, is_active, created_at)
            VALUES
                (1, 'Widget A', 'WGT-001', 9.99,  5.00, 100, 10, 1, '2026-01-01'),
                (1, 'Widget B', 'WGT-002', 14.99, 7.00, 50,  5,  1, '2026-01-02'),
                (2, 'Gadget X', 'GDG-001', 29.99, 15.0, 0,   0,  0, '2026-01-03')";
        cmd.ExecuteNonQuery();
    }

    // ─── Concurrency Checks ───────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteWithConcurrencyCheckAsync_WhenRowsAffectedGreaterThanZero_ReturnsCount()
    {
        var update = Sql.Update<Product>()
            .Set(p => p.Price, 10.99m)
            .Where(p => p.Sku == "WGT-001");

        var rows = await _connection.ExecuteWithConcurrencyCheckAsync<Product>(update, _compiler);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithConcurrencyCheckAsync_WithRegisteredCompiler_WhenRowsAffectedGreaterThanZero_ReturnsCount()
    {
        var update = Sql.Update<Product>()
            .Set(p => p.Price, 12.99m)
            .Where(p => p.Sku == "WGT-001");

        var rows = await _connection.ExecuteWithConcurrencyCheckAsync<Product>(update);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteWithConcurrencyCheckAsync_WhenZeroRowsAffected_ThrowsDbConcurrencyException()
    {
        var update = Sql.Update<Product>()
            .Set(p => p.Price, 99.99m)
            .Where(p => p.Sku == "NON_EXISTENT");

        var ex = await Assert.ThrowsAsync<DbConcurrencyException>(() =>
            _connection.ExecuteWithConcurrencyCheckAsync<Product>(update, _compiler));

        ex.EntityTypeName.Should().Be(nameof(Product));
        ex.RowsAffected.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWithConcurrencyCheckAsync_WithRegisteredCompiler_WhenZeroRowsAffected_ThrowsDbConcurrencyException()
    {
        var update = Sql.Update<Product>()
            .Set(p => p.Price, 99.99m)
            .Where(p => p.Sku == "NON_EXISTENT");

        var ex = await Assert.ThrowsAsync<DbConcurrencyException>(() =>
            _connection.ExecuteWithConcurrencyCheckAsync<Product>(update));

        ex.EntityTypeName.Should().Be(nameof(Product));
    }

    // ─── QueryStreamAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task QueryStreamAsync_StreamsResultsCorrectly()
    {
        var query = Sql.From<Product>().OrderBy(p => p.Id);

        var list = new List<Product>();
        await foreach (var item in _connection.QueryStreamAsync<Product>(query))
        {
            list.Add(item);
        }

        list.Should().HaveCount(3);
        list[0].Name.Should().Be("Widget A");
    }

    [Fact]
    public async Task QueryStreamAsync_WithTransactionAndCancellationToken_StreamsResults()
    {
        using var tx = _connection.BeginTransaction();
        var query = Sql.From<Product>().Where(p => p.CategoryId == 2);

        var list = new List<Product>();
        await foreach (var item in _connection.QueryStreamAsync<Product>(query, tx, CancellationToken.None))
        {
            list.Add(item);
        }

        list.Should().HaveCount(1);
        list[0].Sku.Should().Be("GDG-001");
        tx.Rollback();
    }

    [Fact]
    public async Task QueryStreamAsync_WhenNonDbConnection_ThrowsNotSupportedException()
    {
        DapperExtensions.RegisterCompiler<NonDbConnectionMock>(() => _compiler);
        var nonDbConn = new NonDbConnectionMock();
        var query = Sql.From<Product>();

        var stream = nonDbConn.QueryStreamAsync<Product>(query);
        await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await using var enumerator = stream.GetAsyncEnumerator();
            await enumerator.MoveNextAsync();
        });
    }

    [Fact]
    public void BoundSelectQuery_Tag_ReturnsQueryTag()
    {
        var query = Sql.From<Product>().WithTag("analytics-query");
        var bound = new BoundSelectQuery<Product>(query, _connection);
        bound.Tag.Should().Be("analytics-query");
    }

    [Fact]
    public async Task BulkInsertAsync_WithRegisteredStrategy_ExecutesStrategy()
    {
        var mockStrategy = new CustomBulkStrategy();
        DapperExtensions.RegisterBulkStrategy(mockStrategy);

        var customConn = new CustomBulkConnection();
        var products = new[] { new Product { Sku = "S1" } };
        var inserted = await customConn.BulkInsertAsync(products);
        inserted.Should().Be(999);
    }

    [Fact]
    public async Task BulkInsertAsync_WhenRegisteredStrategyCannotHandle_FallsBackToInsertQuery()
    {
        var mockStrategy = new CustomBulkStrategy();
        DapperExtensions.RegisterBulkStrategy(mockStrategy);

        var products = new[]
        {
            new Product { Name = "Fallback Product 1", Sku = "FB-001", CreatedAt = DateTime.UtcNow },
            new Product { Name = "Fallback Product 2", Sku = "FB-002", CreatedAt = DateTime.UtcNow }
        };

        var count = await _connection.BulkInsertAsync(products);
        count.Should().Be(2);
    }

    [Fact]
    public void GetCompiler_WhenUnregistered_ThrowsInvalidOperationException()
    {
        var unregConn = new UnregisteredConnectionMock();
        Assert.Throws<InvalidOperationException>(() => DapperExtensions.GetCompiler(unregConn));
    }

    [Fact]
    public async Task QueryStreamAsync_WhenSqlInvalid_ThrowsAndRecordsMetrics()
    {
        var query = Sql.Raw("SELECT * FROM non_existent_table_xyz");

        await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await foreach (var _ in _connection.QueryStreamAsync<Product>(query))
            {
            }
        });
    }

    // ─── BoundSelectQuery Execution ───────────────────────────────────────────

    [Fact]
    public async Task Connection_Sql_BoundSelectQuery_ExecutesSuccessfully()
    {
        var bound = _connection.Sql().Select<Product>()
            .Where(p => p.CategoryId == 1)
            .And(p => p.Price > 0)
            .Or(p => p.Stock > 0)
            .OrderBy(p => p.Price)
            .OrderByDescending(p => p.Id);

        bound.Tag.Should().BeNull();
        ((IAstQuery)bound).Nodes.Should().NotBeEmpty();
        bound.Build(_compiler).Sql.Should().NotBeNullOrEmpty();

        var result = await bound.ToResultAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var streamList = new List<Product>();
        await foreach (var item in bound.ToStreamAsync())
        {
            streamList.Add(item);
        }
        streamList.Should().HaveCount(2);

        var paged = await bound.ToPagedListAsync(1, 10);
        paged.IsSuccess.Should().BeTrue();
        paged.Value.Count.Should().Be(2);
    }

    [Fact]
    public async Task BoundSelectQuery_WithRawWhereAndPaginateAndProjectTo()
    {
        var bound = _connection.Sql().Select<Product>()
            .Where($"category_id = {1}")
            .Paginate(1, 2)
            .ProjectTo<ProductDto>();

        var result = await bound.ToResultAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Name.Should().Be("Widget A");
    }

    [Fact]
    public async Task ToResultAsync_And_ToPagedListAsync_WhenSqlErrors_ReturnFailure()
    {
        var badQuery = new SelectQuery<Product>().From("invalid_table_abc");
        var bound = new BoundSelectQuery<Product>(badQuery, _connection);

        var res = await bound.ToResultAsync();
        res.IsFailure.Should().BeTrue();
        res.Error.Code.Should().Be("DbError");

        var pagedRes = await bound.ToPagedListAsync(1, 10);
        pagedRes.IsFailure.Should().BeTrue();
        pagedRes.Error.Code.Should().Be("DbError");
    }

    [Fact]
    public void Connection_Sql_NullConnection_ThrowsArgumentNullException()
    {
        IDbConnection nullConn = null!;
        Assert.Throws<ArgumentNullException>(() => nullConn.Sql());

        var query = Sql.From<Product>();
        Assert.Throws<ArgumentNullException>(() => new BoundSelectQuery<Product>(null!, _connection));
        Assert.Throws<ArgumentNullException>(() => new BoundSelectQuery<Product>(query, null!));
    }

    [Fact]
    public void Connection_Sql_Insert_Update_Delete_CreateBuilders()
    {
        var ctx = _connection.Sql();
        ctx.Connection.Should().BeSameAs(_connection);

        var insert = ctx.Insert(new Product { Sku = "NEW-1" });
        insert.Should().NotBeNull();

        var update = ctx.Update<Product>();
        update.Should().NotBeNull();

        var updateEntity = ctx.Update(new Product { Sku = "UPD-1" });
        updateEntity.Should().NotBeNull();

        var delete = ctx.Delete<Product>();
        delete.Should().NotBeNull();
    }

    // ─── Bulk Operations ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteBulkAsync_ExecutesAllBatchesCorrectly()
    {
        var batch1 = new SqlResult("INSERT INTO products (name, sku, category_id, created_at) VALUES ('Bulk 1', 'BLK-001', 1, '2026-01-01')",
            new Dictionary<string, object?> { ["p0"] = 1 });
        var batch2 = new SqlResult("INSERT INTO products (name, sku, category_id, created_at) VALUES ('Bulk 2', 'BLK-002', 1, '2026-01-01')",
            null);

        var bulkResult = new BulkSqlResult(new[] { batch1, batch2 });

        var affected = await _connection.ExecuteBulkAsync(bulkResult);
        affected.Should().Be(2);
    }

    // ─── SqlResultExtensions ──────────────────────────────────────────────────

    [Fact]
    public void ToDynamicParameters_ConvertsParametersCorrectly()
    {
        var result = new SqlResult("SELECT * FROM products WHERE id = @id", new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["name"] = "Widget"
        });

        var dynParams = result.ToDynamicParameters();
        dynParams.Should().NotBeNull();
        dynParams.ParameterNames.Should().Contain(new[] { "id", "name" });
    }

    // ─── PagedList & CountedPagedList ─────────────────────────────────────────

    [Fact]
    public void PagedList_WithoutCount_PropertiesAndPaginationLogic()
    {
        var items = new List<string> { "Item1", "Item2", "Item3" };
        var param = PaginationParameters.Create(2, 2);
        var paged = PagedList<string>.WithoutCount(items, param, hasNextPage: true, hasPreviousPage: true);

        paged.Page.Should().Be(2);
        paged.PageSize.Should().Be(2);
        paged.HasNextPage.Should().BeTrue();
        paged.HasPreviousPage.Should().BeTrue();
        paged.TotalCount.Should().BeNull();
        paged.TotalPages.Should().BeNull();
        paged.Count.Should().Be(3);
        paged[0].Should().Be("Item1");
        paged.ToList().Should().HaveCount(3);

        var mapped = paged.Map(s => s.ToUpperInvariant());
        mapped[0].Should().Be("ITEM1");
        mapped.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedList_WithCount_PropertiesAndTotalPages()
    {
        var items = new List<string> { "A", "B" };
        var param = PaginationParameters.Create(1, 2);
        var counted = PagedList<string>.WithCount(items, param, totalCount: 5);

        counted.Page.Should().Be(1);
        counted.PageSize.Should().Be(2);
        counted.TotalCount.Should().Be(5);
        counted.ExactTotalCount.Should().Be(5);
        ((ICountedPagedList<string>)counted).TotalCount.Should().Be(5);
        counted.TotalPages.Should().Be(3);
        counted.HasNextPage.Should().BeTrue();
        counted.HasPreviousPage.Should().BeFalse();

        var mapped = counted.Map(s => s + "!");
        mapped[0].Should().Be("A!");
        mapped.ExactTotalCount.Should().Be(5);
    }

    [Fact]
    public void PagedList_Empty_HasZeroCounts()
    {
        var param = PaginationParameters.Create(1, 10);
        var empty = PagedList<string>.Empty(param);

        empty.Count.Should().Be(0);
        empty.TotalCount.Should().Be(0);
        empty.ExactTotalCount.Should().Be(0);
        empty.TotalPages.Should().Be(0);
        empty.HasNextPage.Should().BeFalse();
        empty.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PagedList_ArgumentValidation()
    {
        var param = PaginationParameters.Create(1, 10);
        Assert.Throws<ArgumentNullException>(() => PagedList<string>.WithCount(null!, param, 10));
        Assert.Throws<ArgumentNullException>(() => PagedList<string>.WithoutCount(null!, param, true));

        var paged = PagedList<string>.Empty(param);
        Assert.Throws<ArgumentNullException>(() => paged.Map<int>(null!));
        Assert.Throws<ArgumentNullException>(() => ((CountedPagedList<string>)paged).Map<int>(null!));

        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedList<string>(Array.Empty<string>(), null, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedList<string>(Array.Empty<string>(), null, 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PagedList<string>(Array.Empty<string>(), -1, 1, 10));
    }

    public class ProductDto
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class NonDbConnectionMock : IDbConnection
    {
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 30;
        public string Database => "Mock";
        public ConnectionState State => ConnectionState.Open;

        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() {}
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Dispose() {}
        public void Open() {}
    }

    private sealed class UnregisteredConnectionMock : IDbConnection
    {
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 30;
        public string Database => "Unregistered";
        public ConnectionState State => ConnectionState.Open;

        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() {}
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Dispose() {}
        public void Open() {}
    }

    private sealed class CustomBulkConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = "";
        public int ConnectionTimeout => 30;
        public string Database => "BulkMock";
        public ConnectionState State => ConnectionState.Open;

        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() {}
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Dispose() {}
        public void Open() {}
    }

    private sealed class CustomBulkStrategy : IBulkStrategy
    {
        public bool CanHandle(IDbConnection connection) => connection is CustomBulkConnection;

        public Task<int> BulkInsertAsync<T>(IDbConnection connection, IEnumerable<T> entities, IDbTransaction? transaction = null, CancellationToken cancellationToken = default) where T : class, new()
        {
            return Task.FromResult(999);
        }
    }
}
