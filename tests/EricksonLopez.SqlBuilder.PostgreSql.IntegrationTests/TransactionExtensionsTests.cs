// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using Npgsql;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.IntegrationTests;

[Collection("PostgreSqlCollection")]
[Trait("Category", "Integration")]
public class TransactionExtensionsTests
{
    private readonly PostgreSqlFixture _fixture;

    public TransactionExtensionsTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_Commits_OnSuccess()
    {
        // Arrange
        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS test_trx (id int)");

        // Act
        await connection.ExecuteInTransactionAsync(async trx =>
        {
            await connection.ExecuteAsync("INSERT INTO test_trx (id) VALUES (1)", transaction: trx);
        });

        // Assert
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM test_trx WHERE id = 1");
        count.Should().Be(1);

        // Cleanup
        await connection.ExecuteAsync("DROP TABLE test_trx");
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_RollsBack_OnException()
    {
        // Arrange
        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS test_trx2 (id int)");

        // Act
        var act = async () =>
        {
            await connection.ExecuteInTransactionAsync(async trx =>
            {
                await connection.ExecuteAsync("INSERT INTO test_trx2 (id) VALUES (2)", transaction: trx);
                throw new InvalidOperationException("Force rollback");
            });
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM test_trx2 WHERE id = 2");
        count.Should().Be(0);

        // Cleanup
        await connection.ExecuteAsync("DROP TABLE test_trx2");
    }

    [Fact]
    public async Task ExecuteInTransactionAsyncWithResult_Commits_OnSuccess()
    {
        // Arrange
        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS test_trx3 (id int)");

        // Act
        var result = await connection.ExecuteInTransactionAsync(async trx =>
        {
            await connection.ExecuteAsync("INSERT INTO test_trx3 (id) VALUES (3)", transaction: trx);
            return "success";
        });

        // Assert
        result.Should().Be("success");
        var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM test_trx3 WHERE id = 3");
        count.Should().Be(1);

        // Cleanup
        await connection.ExecuteAsync("DROP TABLE test_trx3");
    }
}





