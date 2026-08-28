// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using NpgsqlTypes;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.IntegrationTests;

[Collection("PostgreSqlCollection")]
[Trait("Category", "Integration")]
public class PostgreSqlBulkExtensionsTests
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlBulkExtensionsTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    private class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    [Fact]
    public async Task BulkInsertAsync_WithParameters_InsertsSuccessfully()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS test_products (id int, name text, price numeric)");
        await connection.ExecuteAsync("TRUNCATE TABLE test_products");

        var products = new[]
        {
            new Product { Id = 1, Name = "Product A", Price = 10.5m },
            new Product { Id = 2, Name = "Product B", Price = 20.0m }
        };

        var parameters = BulkParameters.From(products)
            .Add("Ids", p => p.Id, NpgsqlDbType.Integer)
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Add("Prices", p => p.Price, NpgsqlDbType.Numeric)
            .Build();

        var sql = "INSERT INTO test_products (id, name, price) SELECT * FROM UNNEST(@Ids, @Names, @Prices)";

        // Act
        var rowsAffected = await connection.BulkInsertAsync(sql, parameters);

        // Assert
        rowsAffected.Should().Be(2);

        var inserted = (await connection.QueryAsync<Product>("SELECT id, name, price FROM test_products ORDER BY id")).ToList();
        inserted.Should().HaveCount(2);
        inserted[0].Name.Should().Be("Product A");
        inserted[1].Price.Should().Be(20.0m);

        // Cleanup
        await connection.ExecuteAsync("DROP TABLE test_products");
    }

    [Fact]
    public async Task BulkUpsertAsync_WithParameters_UpsertsSuccessfully()
    {
        // Arrange
        using var connection = _fixture.CreateConnection();
        await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS test_products_upsert (id int PRIMARY KEY, name text)");
        await connection.ExecuteAsync("TRUNCATE TABLE test_products_upsert");
        await connection.ExecuteAsync("INSERT INTO test_products_upsert (id, name) VALUES (1, 'Old Name')");

        var products = new[]
        {
            new Product { Id = 1, Name = "New Name" },
            new Product { Id = 2, Name = "Product C" }
        };

        var parameters = BulkParameters.From(products)
            .Add("Ids", p => p.Id, NpgsqlDbType.Integer)
            .Add("Names", p => p.Name, NpgsqlDbType.Text)
            .Build();

        var sql = @"
            INSERT INTO test_products_upsert (id, name) 
            SELECT * FROM UNNEST(@Ids, @Names)
            ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name";

        // Act
        var rowsAffected = await connection.BulkUpsertAsync(sql, parameters);

        // Assert
        rowsAffected.Should().Be(2); // Depending on Postgres version this could be 2

        var inserted = (await connection.QueryAsync<Product>("SELECT id, name FROM test_products_upsert ORDER BY id")).ToList();
        inserted.Should().HaveCount(2);
        inserted[0].Name.Should().Be("New Name");
        inserted[1].Name.Should().Be("Product C");

        // Cleanup
        await connection.ExecuteAsync("DROP TABLE test_products_upsert");
    }

    [Fact]
    public void BulkParameters_ThrowsInvalidOperation_WhenEmpty()
    {
        // Arrange
        var products = new[] { new Product { Id = 1 } };
        var builder = BulkParameters.From(products);

        // Act
        var ex = Record.Exception(() => builder.Build());

        // Assert
        ex.Should().BeOfType<InvalidOperationException>();
    }
}




