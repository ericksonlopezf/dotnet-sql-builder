// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;
using EricksonLopez.SqlBuilder.Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.UnitTests;

public class SqlBuilderDapperBulkExtensionsTests
{
    [Fact]
    public async Task ExecuteBulkAsync_ShouldExecuteAllBatches()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        // Setup test table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT);";
            await cmd.ExecuteNonQueryAsync();
        }

        var param1 = new Dictionary<string, object?> { { "p0", 1 }, { "p1", "Test1" } };
        var param2 = new Dictionary<string, object?> { { "p0", 2 }, { "p1", "Test2" } };

        var batch1 = new SqlResult("INSERT INTO test (id, name) VALUES (@p0, @p1)", param1);
        var batch2 = new SqlResult("INSERT INTO test (id, name) VALUES (@p0, @p1)", param2);

        var bulkResult = new BulkSqlResult(new[] { batch1, batch2 });

        var rowsAffected = await connection.ExecuteBulkAsync(bulkResult);

        rowsAffected.Should().Be(2);

        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM test;";
            var count = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
            count.Should().Be(2);
        }
    }
    
    [Fact]
    public async Task ExecuteBulkAsync_NullParameters_ExecutesSuccessfully()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        // Setup test table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER);";
            await cmd.ExecuteNonQueryAsync();
        }

        var batch = new SqlResult("INSERT INTO test (id) VALUES (99)", null!);

        var bulkResult = new BulkSqlResult(new[] { batch });

        var rowsAffected = await connection.ExecuteBulkAsync(bulkResult);

        rowsAffected.Should().Be(1);
    }
}




