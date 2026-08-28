// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.Aot.UnitTests;

public class AotDapperExtensionsTests
{
    private record Product(int Id, string Name, decimal Price);

    private static Func<IDataReader, Product> ProductMapper => r => new Product(
        r.GetInt32(r.GetOrdinal("id")),
        r.GetString(r.GetOrdinal("name")),
        r.GetDecimal(r.GetOrdinal("price"))
    );

    [Fact]
    public async Task AotQueryAsync_ExecutesAndMaterializesRows()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price NUMERIC); INSERT INTO products VALUES (1, 'Widget', 9.99), (2, 'Gadget', 19.99);";
            await cmd.ExecuteNonQueryAsync();
        }

        var query = new RawQuery("SELECT id, name, price FROM products ORDER BY id");
        var compiler = new SqliteCompiler();

        var products = await connection.AotQueryAsync(query, compiler, ProductMapper);

        products.Should().HaveCount(2);
        products[0].Name.Should().Be("Widget");
        products[1].Price.Should().Be(19.99m);
    }

    [Fact]
    public async Task AotQueryFirstOrDefaultAsync_ReturnsFirstRowOrDefault()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price NUMERIC); INSERT INTO products VALUES (1, 'Widget', 9.99);";
            await cmd.ExecuteNonQueryAsync();
        }

        var query = new RawQuery("SELECT id, name, price FROM products WHERE id = 1");
        var compiler = new SqliteCompiler();

        var product = await connection.AotQueryFirstOrDefaultAsync(query, compiler, ProductMapper);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Widget");

        var emptyQuery = new RawQuery("SELECT id, name, price FROM products WHERE id = 999");
        var notFound = await connection.AotQueryFirstOrDefaultAsync(emptyQuery, compiler, ProductMapper);
        notFound.Should().BeNull();
    }

    [Fact]
    public async Task AotExecuteAsync_ExecutesNonQuerySuccessfully()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price NUMERIC);";
            await cmd.ExecuteNonQueryAsync();
        }

        var query = new RawQuery("INSERT INTO products VALUES (1, 'Widget', 9.99)");
        var compiler = new SqliteCompiler();

        var affected = await connection.AotExecuteAsync(query, compiler);
        affected.Should().Be(1);
    }

    [Fact]
    public async Task AotExecuteScalarAsync_ReturnsScalarValue()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price NUMERIC); INSERT INTO products VALUES (1, 'Widget', 9.99);";
            await cmd.ExecuteNonQueryAsync();
        }

        var query = new RawQuery("SELECT COUNT(*) FROM products");
        var compiler = new SqliteCompiler();

        var count = await connection.AotExecuteScalarAsync<long>(query, compiler);
        count.Should().Be(1);
    }

    [Fact]
    public async Task AotQueryAsync_WithSqlResult_ExecutesAndMaterializesRows()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price NUMERIC); INSERT INTO products VALUES (1, 'Widget', 9.99);";
            await cmd.ExecuteNonQueryAsync();
        }

        var result = new SqlResult("SELECT id, name, price FROM products WHERE id = @id", new System.Collections.Generic.Dictionary<string, object?> { ["id"] = 1 });
        var products = await connection.AotQueryAsync(result, ProductMapper);
        products.Should().HaveCount(1);
        products[0].Name.Should().Be("Widget");

        var productFirst = await connection.AotQueryFirstOrDefaultAsync(result, ProductMapper);
        productFirst.Should().NotBeNull();
        productFirst!.Name.Should().Be("Widget");

        var emptyResult = new SqlResult("SELECT id, name, price FROM products WHERE id = @id", new System.Collections.Generic.Dictionary<string, object?> { ["id"] = 999 });
        var notFound = await connection.AotQueryFirstOrDefaultAsync(emptyResult, ProductMapper);
        notFound.Should().BeNull();
    }

    [Fact]
    public async Task AotExecuteAsync_WithSqlResult_Executes()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE products (id INTEGER PRIMARY KEY, name TEXT, price NUMERIC);";
            await cmd.ExecuteNonQueryAsync();
        }

        var insertResult = new SqlResult("INSERT INTO products VALUES (@id, @name, @price)", new System.Collections.Generic.Dictionary<string, object?>
        {
            ["id"] = 1,
            ["name"] = "Widget",
            ["price"] = 9.99m
        });
        var affected = await connection.AotExecuteAsync(insertResult);
        affected.Should().Be(1);

        var scalarResult = new SqlResult("SELECT COUNT(*) FROM products", new System.Collections.Generic.Dictionary<string, object?>());
        var count = await connection.AotExecuteScalarAsync<long>(scalarResult);
        count.Should().Be(1);
    }

    [Fact]
    public void AotExtensions_NullGuards_ThrowArgumentNullException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var query = new RawQuery("SELECT 1");
        var compiler = new SqliteCompiler();
        var result = new SqlResult("SELECT 1", new System.Collections.Generic.Dictionary<string, object?>());

        // ISqlQuery overloads
        Action act1 = () => ((IDbConnection)null!).AotQueryAsync(query, compiler, ProductMapper);
        act1.Should().Throw<ArgumentNullException>();

        Action act2 = () => connection.AotQueryAsync(null!, compiler, ProductMapper);
        act2.Should().Throw<ArgumentNullException>();

        Action act3 = () => connection.AotQueryAsync(query, null!, ProductMapper);
        act3.Should().Throw<ArgumentNullException>();

        Action act4 = () => connection.AotQueryAsync<Product>(query, compiler, null!);
        act4.Should().Throw<ArgumentNullException>();

        Action act5 = () => ((IDbConnection)null!).AotExecuteAsync(query, compiler);
        act5.Should().Throw<ArgumentNullException>();

        Action act6 = () => connection.AotExecuteAsync(null!, compiler);
        act6.Should().Throw<ArgumentNullException>();

        Action act7 = () => connection.AotExecuteAsync(query, null!);
        act7.Should().Throw<ArgumentNullException>();

        Action act8 = () => ((IDbConnection)null!).AotExecuteScalarAsync<int>(query, compiler);
        act8.Should().Throw<ArgumentNullException>();

        Action act9 = () => connection.AotExecuteScalarAsync<int>(null!, compiler);
        act9.Should().Throw<ArgumentNullException>();

        Action act10 = () => connection.AotExecuteScalarAsync<int>(query, null!);
        act10.Should().Throw<ArgumentNullException>();

        // SqlResult overloads
        Action act11 = () => ((IDbConnection)null!).AotQueryAsync(result, ProductMapper);
        act11.Should().Throw<ArgumentNullException>();

        Action act12 = () => connection.AotQueryAsync(null!, ProductMapper);
        act12.Should().Throw<ArgumentNullException>();

        Action act13 = () => connection.AotQueryAsync<Product>(result, null!);
        act13.Should().Throw<ArgumentNullException>();

        Action act14 = () => ((IDbConnection)null!).AotExecuteAsync(result);
        act14.Should().Throw<ArgumentNullException>();

        Action act15 = () => connection.AotExecuteAsync(null!);
        act15.Should().Throw<ArgumentNullException>();

        Action act16 = () => ((IDbConnection)null!).AotExecuteScalarAsync<int>(result);
        act16.Should().Throw<ArgumentNullException>();

        Action act17 = () => connection.AotExecuteScalarAsync<int>(null!);
        act17.Should().Throw<ArgumentNullException>();
    }
}
