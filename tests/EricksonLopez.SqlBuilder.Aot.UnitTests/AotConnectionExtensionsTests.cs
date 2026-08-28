// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
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

public sealed class AotConnectionExtensionsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ISqlCompiler _compiler = new SqliteCompiler();

    public AotConnectionExtensionsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
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

    [Fact]
    public async Task AotQueryAsync_WithExplicitMapper_ThrowsWhenQueryOrCompilerNull()
    {
        var mapper = Product.GetReaderParser();
        var query = Sql.From<Product>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryAsync((ISqlQuery)null!, _compiler, mapper));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryAsync(query, (ISqlCompiler)null!, mapper));
    }

    [Fact]
    public async Task AotQueryFirstOrDefaultAsync_ThrowsWhenQueryOrCompilerNull()
    {
        var mapper = Product.GetReaderParser();
        var query = Sql.From<Product>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryFirstOrDefaultAsync<Product>((ISqlQuery)null!, _compiler));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryFirstOrDefaultAsync<Product>(query, (ISqlCompiler)null!));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryFirstOrDefaultAsync((ISqlQuery)null!, _compiler, mapper));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryFirstOrDefaultAsync(query, (ISqlCompiler)null!, mapper));
    }

    [Fact]
    public async Task AotQuerySingleAsync_ThrowsWhenQueryOrCompilerNull()
    {
        var mapper = Product.GetReaderParser();
        var query = Sql.From<Product>();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQuerySingleAsync<Product>((ISqlQuery)null!, _compiler));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQuerySingleAsync<Product>(query, (ISqlCompiler)null!));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQuerySingleAsync((ISqlQuery)null!, _compiler, mapper));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQuerySingleAsync(query, (ISqlCompiler)null!, mapper));
    }

    [Fact]
    public async Task AotExecuteAsync_ThrowsWhenQueryOrCompilerNull()
    {
        var query = Sql.Raw("UPDATE products SET stock = 10");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotExecuteAsync((ISqlQuery)null!, _compiler));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotExecuteAsync(query, (ISqlCompiler)null!));
    }

    [Fact]
    public async Task AotQueryScalarAsync_ThrowsWhenQueryOrCompilerNull()
    {
        var query = Sql.Raw("SELECT 1");

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryScalarAsync<int>((ISqlQuery)null!, _compiler));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _connection.AotQueryScalarAsync<int>(query, (ISqlCompiler)null!));
    }

    [Fact]
    public async Task AotQueryAsync_PrecompiledWithExplicitMapper_ReturnsEntities()
    {
        var result = _compiler.Compile(Sql.From<Product>().Where(p => p.CategoryId == 1));
        var mapper = Product.GetReaderParser();

        var products = await _connection.AotQueryAsync(result, mapper);
        products.Should().HaveCount(2);
    }

    [Fact]
    public async Task AotQueryFirstOrDefaultAsync_ExplicitMapper_ReturnsEntity()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "WGT-001");
        var mapper = Product.GetReaderParser();

        var product = await _connection.AotQueryFirstOrDefaultAsync(query, _compiler, mapper);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Widget A");
    }

    [Fact]
    public async Task AotQueryFirstOrDefaultAsync_ExplicitMapper_WhenNoMatch_ReturnsNull()
    {
        var query = Sql.From<Product>().Where(p => p.Sku == "NOT_FOUND");
        var mapper = Product.GetReaderParser();

        var product = await _connection.AotQueryFirstOrDefaultAsync(query, _compiler, mapper);
        product.Should().BeNull();
    }

    [Fact]
    public async Task AotQueryFirstOrDefaultAsync_PrecompiledExplicitMapper_ReturnsEntity()
    {
        var result = _compiler.Compile(Sql.From<Product>().Where(p => p.Sku == "WGT-001"));
        var mapper = Product.GetReaderParser();

        var product = await AotConnectionExtensions.AotQueryFirstOrDefaultAsync<Product>(_connection, result);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Widget A");
    }

    [Fact]
    public async Task AotQuerySingleAsync_PrecompiledExplicitMapper_ReturnsEntity()
    {
        var result = _compiler.Compile(Sql.From<Product>().Where(p => p.Sku == "GDG-001"));

        var product = await _connection.AotQuerySingleAsync<Product>(result);
        product.Should().NotBeNull();
        product.Name.Should().Be("Gadget X");
    }

    [Fact]
    public async Task CustomTransactionAndTimeout_PropagateCorrectly()
    {
        using var tx = _connection.BeginTransaction();
        var query = Sql.From<Product>().Where(p => p.CategoryId == 1);

        var products = await _connection.AotQueryAsync<Product>(query, _compiler, tx, commandTimeout: 45);
        products.Should().HaveCount(2);

        var first = await _connection.AotQueryFirstOrDefaultAsync<Product>(query, _compiler, tx, commandTimeout: 45);
        first.Should().NotBeNull();

        var singleQuery = Sql.From<Product>().Where(p => p.Sku == "GDG-001");
        var single = await _connection.AotQuerySingleAsync<Product>(singleQuery, _compiler, tx, commandTimeout: 45);
        single.Should().NotBeNull();

        var scalarQuery = Sql.Raw("SELECT 100");
        var scalar = await _connection.AotQueryScalarAsync<long>(scalarQuery, _compiler, tx, commandTimeout: 45);
        scalar.Should().Be(100);

        var updateQuery = Sql.Update<Product>().Set(p => p.Stock, 50).Where(p => p.Sku == "GDG-001");
        var rows = await _connection.AotExecuteAsync(updateQuery, _compiler, tx, commandTimeout: 45);
        rows.Should().Be(1);

        tx.Rollback();
    }
}
