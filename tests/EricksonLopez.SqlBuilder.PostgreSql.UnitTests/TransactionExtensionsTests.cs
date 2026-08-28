// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.PostgreSql;
using Npgsql;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class TransactionExtensionsTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_NullConnection_ThrowsArgumentNullException()
    {
        NpgsqlConnection conn = null!;
        var act = () => conn.ExecuteInTransactionAsync(tx => Task.CompletedTask);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_NullOperation_ThrowsArgumentNullException()
    {
        using var conn = new NpgsqlConnection();
        Func<NpgsqlTransaction, Task> op = null!;
        var act = () => conn.ExecuteInTransactionAsync(op);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_NullConnection_ThrowsArgumentNullException()
    {
        NpgsqlConnection conn = null!;
        var act = () => conn.ExecuteInTransactionAsync(tx => Task.FromResult(1));
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WithResult_NullOperation_ThrowsArgumentNullException()
    {
        using var conn = new NpgsqlConnection();
        Func<NpgsqlTransaction, Task<int>> op = null!;
        var act = () => conn.ExecuteInTransactionAsync(op);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}




