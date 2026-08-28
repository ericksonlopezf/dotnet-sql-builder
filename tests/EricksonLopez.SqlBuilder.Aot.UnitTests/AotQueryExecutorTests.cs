// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Aot;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Aot.UnitTests;

/// <summary>
/// End-to-end tests for <see cref="AotQueryExecutor"/> and <see cref="AotConnectionExtensions"/>
/// using SQLite in-memory. Verifies the full NativeAOT execution path (no Dapper, no reflection).
/// </summary>
public sealed class AotQueryExecutorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ISqlCompiler _compiler = new SqliteCompiler();

    public AotQueryExecutorTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        CreateSchema();
        SeedData();
    }

    public void Dispose() => _connection.Dispose();

    // ─── Schema & seed ───────────────────────────────────────────────────────

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

    // ─── AotQueryExecutor — direct SqlResult overloads ───────────────────────

    [Fact]
    public async Task QueryAsync_WhenSqlResultIsValid_ShouldReturnAllMappedRows()
    {
        var query = Sql.From<Product>();
        var result = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var products = await AotQueryExecutor.QueryAsync(_connection, result, mapper);

        products.Should().HaveCount(3);
        products.Should().AllSatisfy(p => p.Id.Should().BeGreaterThan(0));
    }

    [Fact]
    public async Task QueryAsync_WhenWhereClauseProvided_ShouldReturnFilteredRows()
    {
        var query = Sql.From<Product>().Where(p => p.CategoryId == 1);
        var result = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var products = await AotQueryExecutor.QueryAsync(_connection, result, mapper);

        products.Should().HaveCount(2);
        products.Should().AllSatisfy(p => p.CategoryId.Should().Be(1));
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenRowMatches_ShouldReturnEntity()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "WGT-001");
        var result = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var product = await AotQueryExecutor.QueryFirstOrDefaultAsync(
            _connection, result, mapper);

        product.Should().NotBeNull();
        product!.Name.Should().Be("Widget A");
        product.Price.Should().BeApproximately(9.99m, 0.001m);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenNoRowMatches_ShouldReturnNull()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "DOES-NOT-EXIST");
        var result = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var product = await AotQueryExecutor.QueryFirstOrDefaultAsync(
            _connection, result, mapper);

        product.Should().BeNull();
    }

    [Fact]
    public async Task QuerySingleAsync_WhenExactlyOneRowMatches_ShouldReturnEntity()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "GDG-001");
        var result = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var product = await AotQueryExecutor.QuerySingleAsync(_connection, result, mapper);

        product.Name.Should().Be("Gadget X");
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task QuerySingleAsync_WhenNoRowMatches_ThrowsInvalidOperationException()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "NONE");
        var result = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AotQueryExecutor.QuerySingleAsync(_connection, result, mapper));
        ex.Message.Should().Be("Query returned no results.");
    }

    [Fact]
    public async Task ExecuteAsync_WhenInsertQueryExecuted_ShouldIncrementRowCount()
    {
        var before = await CountProducts();

        var newProduct = new Product
        {
            CategoryId = 3,
            Name = "New Product",
            Sku = "NEW-001",
            Price = 5.00m,
            CostPrice = 2.50m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var insert = Sql.Insert(newProduct);
        var result = _compiler.Compile(insert);

        var rowsAffected = await AotQueryExecutor.ExecuteAsync(_connection, result);

        rowsAffected.Should().Be(1);
        (await CountProducts()).Should().Be(before + 1);
    }

    [Fact]
    public async Task QueryScalarAsync_WhenCountQueryExecuted_ShouldReturnCorrectScalarCount()
    {
        var rawCount = Sql.Raw("SELECT COUNT(*) FROM products WHERE is_active = 1");
        var result = _compiler.Compile(rawCount);

        var count = await AotQueryExecutor.QueryScalarAsync<long>(_connection, result);

        count.Should().Be(2); // Widget A and Widget B are active, Gadget X is not
    }

    // ─── AotConnectionExtensions — fluent compile+execute overloads ──────────

    [Fact]
    public async Task AotQueryAsync_WhenQueryExtensionInvoked_ShouldReturnMappedResults()
    {
        var query = Sql.From<Product>().Where(p => p.IsActive == true);
        var mapper = Product.GetReaderParser();

        var products = await _connection.AotQueryAsync(query, _compiler, mapper);

        products.Should().HaveCount(2);
    }

    [Fact]
    public async Task AotQueryFirstOrDefaultAsync_WhenQueryExtensionInvoked_ShouldReturnFirstMatch()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "WGT-002");
        var mapper = Product.GetReaderParser();

        var product = await _connection.AotQueryFirstOrDefaultAsync(query, _compiler, mapper);

        product.Should().NotBeNull();
        product!.Name.Should().Be("Widget B");
    }

    [Fact]
    public async Task AotExecuteAsync_WhenUpdateQueryExecuted_ShouldAffectExpectedRows()
    {
        var update = Sql.Update<Product>()
            .Set(p => p.IsActive, false)
            .Where(p => p.CategoryId == 1);

        var rowsAffected = await _connection.AotExecuteAsync(update, _compiler);

        rowsAffected.Should().Be(2); // Widget A + Widget B
    }

    [Fact]
    public async Task AotQueryAsync_WhenConnectionIsNull_ThrowsArgumentNullException()
    {
        IDbConnection nullConnection = null!;
        var query = Sql.From<Product>();
        var mapper = Product.GetReaderParser();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            nullConnection.AotQueryAsync(query, _compiler, mapper));
    }

    [Fact]
    public async Task AotQueryAsync_WhenQueryIsNull_ThrowsArgumentNullException()
    {
        ISqlQuery nullQuery = null!;
        var mapper = Product.GetReaderParser();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryAsync(nullQuery, _compiler, mapper));
    }

    [Fact]
    public async Task AotQueryAsync_WhenCompilerIsNull_ThrowsArgumentNullException()
    {
        var query = Sql.From<Product>();
        var mapper = Product.GetReaderParser();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryAsync(query, null!, mapper));
    }

    [Fact]
    public async Task AotQueryAsync_WhenPrecompiledSqlResultProvided_ShouldExecuteWithoutRecompiling()
    {
        // Verifies the SqlResult overload (pre-compiled / cached query plan pattern)
        var query = Sql.From<Product>().Where(p => p.CategoryId == 2);
        var precompiled = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var products = await _connection.AotQueryAsync(precompiled, mapper);

        products.Should().HaveCount(1);
        products[0].Sku.Should().Be("GDG-001");
    }

    [Fact]
    public async Task AotQueryAsync_WhenInferredParserUsed_ShouldReturnAllRowsWithoutExplicitMapper()
    {
        var query = Sql.From<Product>().Where(p => p.CategoryId == 1);

        // Uses the new AOT-005 inferred parser overload (T.GetReaderParser() called automatically)
        var products = await _connection.AotQueryAsync<Product>(query, _compiler);

        products.Should().HaveCount(2);
        products.Should().AllSatisfy(p => p.CategoryId.Should().Be(1));
    }

    [Fact]
    public async Task AotQueryFirstOrDefaultAsync_WhenInferredParserUsed_ShouldReturnEntityWithoutExplicitMapper()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "WGT-001");

        var product = await _connection.AotQueryFirstOrDefaultAsync<Product>(query, _compiler);

        product.Should().NotBeNull();
        product!.Name.Should().Be("Widget A");
    }

    [Fact]
    public async Task AotQuerySingleAsync_WhenInferredParserUsed_ShouldReturnSingleEntityWithoutExplicitMapper()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "GDG-001");

        var product = await _connection.AotQuerySingleAsync<Product>(query, _compiler);

        product.Should().NotBeNull();
        product.Name.Should().Be("Gadget X");
    }

    [Fact]
    public async Task AotQueryAsync_WhenPrecompiledWithInferredParser_ShouldExecuteAndMapCorrectly()
    {
        var query = Sql.From<Product>().Where(p => p.CategoryId == 2);
        var precompiled = _compiler.Compile(query);

        var products = await _connection.AotQueryAsync<Product>(precompiled);

        products.Should().HaveCount(1);
        products[0].Sku.Should().Be("GDG-001");
    }

    [Fact]
    public async Task QuerySingleAsync_WhenMoreThanOneRowMatches_ThrowsInvalidOperationException()
    {
        var query = Sql.From<Product>().Where(p => p.CategoryId == 1);
        var result = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AotQueryExecutor.QuerySingleAsync(_connection, result, mapper));
        ex.Message.Should().Contain("more than one result");
    }

    [Fact]
    public async Task QueryScalarAsync_WhenResultIsEmpty_ShouldReturnDefault()
    {
        var rawCount = Sql.Raw("SELECT price FROM products WHERE sku = 'NON_EXISTENT'");
        var result = _compiler.Compile(rawCount);

        var price = await AotQueryExecutor.QueryScalarAsync<decimal?>(_connection, result);
        price.Should().BeNull();
    }

    [Fact]
    public async Task QueryScalarAsync_WhenResultIsDbNull_ShouldReturnDefault()
    {
        var rawNull = Sql.Raw("SELECT description FROM products WHERE sku = 'WGT-001'");
        var result = _compiler.Compile(rawNull);

        var desc = await AotQueryExecutor.QueryScalarAsync<string>(_connection, result);
        desc.Should().BeNull();
    }

    [Fact]
    public async Task QueryAsync_WithNullParameterValue_ShouldBindDbNull()
    {
        var query = Sql.Raw("SELECT * FROM products WHERE description = @desc", new Dictionary<string, object?> { ["desc"] = null });
        var result = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var products = await AotQueryExecutor.QueryAsync(_connection, result, mapper);
        products.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryAsync_WhenConnectionOrMapperIsNull_ThrowsArgumentNullException()
    {
        var result = new SqlResult("SELECT 1", null);
        var mapper = Product.GetReaderParser();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AotQueryExecutor.QueryAsync<Product>(null!, result, mapper));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AotQueryExecutor.QueryAsync<Product>(_connection, result, null!));
    }

    [Fact]
    public async Task QueryFirstOrDefaultAsync_WhenConnectionOrMapperIsNull_ThrowsArgumentNullException()
    {
        var result = new SqlResult("SELECT 1", null);
        var mapper = Product.GetReaderParser();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AotQueryExecutor.QueryFirstOrDefaultAsync<Product>(null!, result, mapper));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AotQueryExecutor.QueryFirstOrDefaultAsync<Product>(_connection, result, null!));
    }

    [Fact]
    public async Task QuerySingleAsync_WhenConnectionOrMapperIsNull_ThrowsArgumentNullException()
    {
        var result = new SqlResult("SELECT 1", null);
        var mapper = Product.GetReaderParser();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AotQueryExecutor.QuerySingleAsync<Product>(null!, result, mapper));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AotQueryExecutor.QuerySingleAsync<Product>(_connection, result, null!));
    }

    [Fact]
    public async Task ExecuteAsync_WhenConnectionIsNull_ThrowsArgumentNullException()
    {
        var result = new SqlResult("SELECT 1", null);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AotQueryExecutor.ExecuteAsync(null!, result));
    }

    [Fact]
    public async Task QueryScalarAsync_WhenConnectionIsNull_ThrowsArgumentNullException()
    {
        var result = new SqlResult("SELECT 1", null);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AotQueryExecutor.QueryScalarAsync<int>(null!, result));
    }

    [Fact]
    public async Task QueryAsync_WithCustomTimeoutAndTransaction_ExecutesSuccessfully()
    {
        using var tx = _connection.BeginTransaction();
        var query = Sql.From<Product>().Where(p => p.CategoryId == 1);
        var result = _compiler.Compile(query);
        var mapper = Product.GetReaderParser();

        var products = await AotQueryExecutor.QueryAsync(_connection, result, mapper, tx, commandTimeout: 60);
        products.Should().HaveCount(2);

        var first = await AotQueryExecutor.QueryFirstOrDefaultAsync(_connection, result, mapper, tx, commandTimeout: 60);
        first.Should().NotBeNull();

        var scalar = await AotQueryExecutor.QueryScalarAsync<long>(_connection, _compiler.Compile(Sql.Raw("SELECT 42")), tx, commandTimeout: 60);
        scalar.Should().Be(42);

        var singleResult = _compiler.Compile(Sql.From<Product>().Where(p => p.Sku == "GDG-001"));
        var single = await AotQueryExecutor.QuerySingleAsync(_connection, singleResult, mapper, tx, commandTimeout: 60);
        single.Should().NotBeNull();

        var executeResult = _compiler.Compile(Sql.Update<Product>().Set(p => p.Stock, 1).Where(p => p.Sku == "GDG-001"));
        var affected = await AotQueryExecutor.ExecuteAsync(_connection, executeResult, tx, commandTimeout: 60);
        affected.Should().Be(1);

        tx.Rollback();
    }

    [Fact]
    public async Task QueryAsync_WithCancellationToken_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = new SqlResult("SELECT 1", null);
        var mapper = (IDataReader r) => r.GetInt64(0);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AotQueryExecutor.QueryAsync(_connection, result, mapper, cancellationToken: cts.Token));
    }

    // ─── AotConnectionExtensions — additional overloads ─────────────────────

    [Fact]
    public async Task AotQuerySingleAsync_WithExplicitMapper_ShouldReturnEntity()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "GDG-001");
        var mapper = Product.GetReaderParser();

        var product = await _connection.AotQuerySingleAsync(query, _compiler, mapper);
        product.Name.Should().Be("Gadget X");
    }

    [Fact]
    public async Task AotQueryScalarAsync_Extension_ShouldReturnScalarValue()
    {
        var query = Sql.Raw("SELECT COUNT(*) FROM products");
        var count = await _connection.AotQueryScalarAsync<long>(query, _compiler);
        count.Should().Be(3);
    }

    [Fact]
    public async Task AotQueryFirstOrDefaultAsync_PrecompiledInferred_ShouldReturnEntity()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "WGT-001");
        var precompiled = _compiler.Compile(query);

        var product = await _connection.AotQueryFirstOrDefaultAsync<Product>(precompiled);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Widget A");
    }

    [Fact]
    public async Task AotQuerySingleAsync_PrecompiledInferred_ShouldReturnEntity()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "GDG-001");
        var precompiled = _compiler.Compile(query);

        var product = await _connection.AotQuerySingleAsync<Product>(precompiled);
        product.Should().NotBeNull();
        product.Name.Should().Be("Gadget X");
    }

    [Fact]
    public async Task AotExecuteAsync_Precompiled_ShouldExecuteCorrectly()
    {
        var update = Sql.Update<Product>().Set(p => p.Stock, 99).Where(p => p.Sku == "GDG-001");
        var precompiled = _compiler.Compile(update);

        var rows = await _connection.AotExecuteAsync(precompiled);
        rows.Should().Be(1);
    }

    // ─── Non-DbCommand Fallback tests ───────────────────────────────────────

    [Fact]
    public async Task DbCommand_EnforcesAsyncExecution()
    {
        var trackingConn = new TrackingDbConnection(_connection);
        var updateResult = new SqlResult("UPDATE products SET price = price + 1 WHERE id = 1", null);

        var rows = await AotQueryExecutor.ExecuteAsync(trackingConn, updateResult);
        rows.Should().Be(1);

        var selectResult = new SqlResult("SELECT 1", null);
        var mapper = (IDataReader r) => r.GetInt64(0);
        var items = await AotQueryExecutor.QueryAsync(trackingConn, selectResult, mapper);
        items.Should().ContainSingle().Which.Should().Be(1);
    }

    [Fact]
    public async Task NonDbCommand_Fallback_ExecuteReaderAndExecuteNonQuery()
    {
        var customConn = new NonDbCustomConnection(_connection);
        var updateResult = new SqlResult("UPDATE products SET price = price + 1 WHERE id = 1", null);

        var rows = await AotQueryExecutor.ExecuteAsync(customConn, updateResult);
        rows.Should().Be(1);

        var selectResult = new SqlResult("SELECT 1", null);
        var mapper = (IDataReader r) => r.GetInt64(0);
        var items = await AotQueryExecutor.QueryAsync(customConn, selectResult, mapper);
        items.Should().ContainSingle().Which.Should().Be(1);
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private async Task<long> CountProducts()
    {
        var result = await AotQueryExecutor.QueryScalarAsync<long>(
            _connection,
            _compiler.Compile(Sql.Raw("SELECT COUNT(*) FROM products")));
        return result;
    }

    private sealed class TrackingDbConnection : System.Data.Common.DbConnection
    {
        private readonly SqliteConnection _inner;

        public TrackingDbConnection(SqliteConnection inner) => _inner = inner;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString { get => _inner.ConnectionString; set => _inner.ConnectionString = value ?? string.Empty; }
        public override string Database => _inner.Database;
        public override string DataSource => _inner.DataSource;
        public override string ServerVersion => _inner.ServerVersion;
        public override ConnectionState State => _inner.State;

        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
        public override void Close() => _inner.Close();
        public override void Open() => _inner.Open();

        protected override System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => _inner.BeginTransaction(isolationLevel);

        protected override System.Data.Common.DbCommand CreateDbCommand()
            => new TrackingDbCommand(_inner.CreateCommand());
    }

    private sealed class TrackingDbCommand : System.Data.Common.DbCommand
    {
        private readonly SqliteCommand _inner;

        public TrackingDbCommand(SqliteCommand inner) => _inner = inner;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string CommandText { get => _inner.CommandText; set => _inner.CommandText = value; }
        public override int CommandTimeout { get => _inner.CommandTimeout; set => _inner.CommandTimeout = value; }
        public override CommandType CommandType { get => _inner.CommandType; set => _inner.CommandType = value; }
        public override bool DesignTimeVisible { get => _inner.DesignTimeVisible; set => _inner.DesignTimeVisible = value; }
        public override UpdateRowSource UpdatedRowSource { get => _inner.UpdatedRowSource; set => _inner.UpdatedRowSource = value; }

        protected override System.Data.Common.DbConnection? DbConnection
        {
            get => _inner.Connection;
            set => _inner.Connection = (SqliteConnection?)value;
        }

        protected override System.Data.Common.DbParameterCollection DbParameterCollection => _inner.Parameters;

        protected override System.Data.Common.DbTransaction? DbTransaction
        {
            get => _inner.Transaction;
            set => _inner.Transaction = (SqliteTransaction?)value;
        }

        public override void Cancel() => _inner.Cancel();
        protected override System.Data.Common.DbParameter CreateDbParameter() => _inner.CreateParameter();

        public override int ExecuteNonQuery()
            => throw new InvalidOperationException("Sync ExecuteNonQuery should not be called on DbCommand");

        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            => _inner.ExecuteNonQueryAsync(cancellationToken);

        public override object? ExecuteScalar()
            => throw new InvalidOperationException("Sync ExecuteScalar should not be called on DbCommand");

        public override void Prepare() => _inner.Prepare();

        protected override System.Data.Common.DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            => throw new InvalidOperationException("Sync ExecuteDbDataReader should not be called on DbCommand");

        protected override async Task<System.Data.Common.DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
        {
            var reader = await _inner.ExecuteReaderAsync(behavior, cancellationToken);
            return reader;
        }
    }

    private sealed class NonDbCustomConnection : IDbConnection
    {
        private readonly IDbConnection _inner;

        public NonDbCustomConnection(IDbConnection inner) => _inner = inner;

        public string ConnectionString { get => _inner.ConnectionString; set => _inner.ConnectionString = value; }
        public int ConnectionTimeout => _inner.ConnectionTimeout;
        public string Database => _inner.Database;
        public ConnectionState State => _inner.State;

        public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
        public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
        public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
        public void Close() => _inner.Close();
        public void Open() => _inner.Open();
        public void Dispose() => _inner.Dispose();

        public IDbCommand CreateCommand() => new NonDbCustomCommand(_inner.CreateCommand());
    }

    private sealed class NonDbCustomCommand : IDbCommand
    {
        private readonly IDbCommand _inner;

        public NonDbCustomCommand(IDbCommand inner) => _inner = inner;

        public string CommandText { get => _inner.CommandText; set => _inner.CommandText = value; }
        public int CommandTimeout { get => _inner.CommandTimeout; set => _inner.CommandTimeout = value; }
        public CommandType CommandType { get => _inner.CommandType; set => _inner.CommandType = value; }
        public IDbConnection? Connection { get => _inner.Connection; set => _inner.Connection = value; }
        public IDataParameterCollection Parameters => _inner.Parameters;
        public IDbTransaction? Transaction { get => _inner.Transaction; set => _inner.Transaction = value; }
        public UpdateRowSource UpdatedRowSource { get => _inner.UpdatedRowSource; set => _inner.UpdatedRowSource = value; }

        public void Cancel() => _inner.Cancel();
        public IDbDataParameter CreateParameter() => _inner.CreateParameter();
        public void Dispose() => _inner.Dispose();
        public int ExecuteNonQuery() => _inner.ExecuteNonQuery();
        public IDataReader ExecuteReader() => _inner.ExecuteReader();
        public IDataReader ExecuteReader(CommandBehavior behavior) => _inner.ExecuteReader(behavior);
        public object? ExecuteScalar() => _inner.ExecuteScalar();
        public void Prepare() => _inner.Prepare();
    }
}





