// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.SqlBuilder.Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.UnitTests;

/// <summary>
/// Tests for the raw-SQL pagination overloads added to <see cref="DapperPaginationExtensions"/>:
/// <see cref="DapperPaginationExtensions.QueryPagedRawAsync{T}"/> and
/// <see cref="DapperPaginationExtensions.QueryPagedMultipleAsync{T}"/>.
/// </summary>
public class DapperPaginationRawExtensionsTests
{
    private sealed class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Returns an <see cref="IDbConnection"/> (not the concrete type) so that
    /// extension methods on the interface are resolved unambiguously by the compiler.</summary>
    private static async Task<IDbConnection> GetSeededConnectionAsync(int rowCount = 25)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE Users (Id INTEGER PRIMARY KEY, Name TEXT)");
        var users = Enumerable.Range(1, rowCount)
            .Select(i => new { Id = i, Name = $"User {i}" })
            .ToList();
        await connection.ExecuteAsync("INSERT INTO Users (Id, Name) VALUES (@Id, @Name)", users);
        return connection;
    }

    // ── QueryPagedRawAsync — guard clauses ─────────────────────────────────────

    [Fact]
    public async Task QueryPagedRawAsync_WithNullOrWhiteSpaceSql_ThrowsArgumentException()
    {
        using var conn = await GetSeededConnectionAsync();
        Func<Task> act = async () => await conn.QueryPagedRawAsync<User>(
            "   ",
            "SELECT COUNT(*) FROM Users",
            PaginationParameters.Create(1, 10));
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task QueryPagedRawAsync_WithNullOrWhiteSpaceCountSql_ThrowsArgumentException()
    {
        using var conn = await GetSeededConnectionAsync();
        Func<Task> act = async () => await conn.QueryPagedRawAsync<User>(
            "SELECT * FROM Users ORDER BY Id",
            "",
            PaginationParameters.Create(1, 10));
        await act.Should().ThrowAsync<ArgumentException>();
    }



    // ── QueryPagedRawAsync — happy path ──────────────────────────────────────

    [Fact]
    public async Task QueryPagedRawAsync_Page1_ReturnsCorrectItems()
    {
        using var conn = await GetSeededConnectionAsync();
        var pagination = PaginationParameters.Create(page: 1, pageSize: 10);

        var result = await conn.QueryPagedRawAsync<User>(
            "SELECT Id, Name FROM Users ORDER BY Id",
            "SELECT COUNT(*) FROM Users",
            pagination);

        result.Count.Should().Be(10);
        result.TotalCount.Should().Be(25);
        result[0].Id.Should().Be(1);
        result[9].Id.Should().Be(10);
    }

    [Fact]
    public async Task QueryPagedRawAsync_Page2_CorrectOffset()
    {
        using var conn = await GetSeededConnectionAsync();
        var pagination = PaginationParameters.Create(page: 2, pageSize: 10);

        var result = await conn.QueryPagedRawAsync<User>(
            "SELECT Id, Name FROM Users ORDER BY Id",
            "SELECT COUNT(*) FROM Users",
            pagination);

        result.Count.Should().Be(10);
        result[0].Id.Should().Be(11);
        result[9].Id.Should().Be(20);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task QueryPagedRawAsync_LastPage_ReturnsRemainingItems()
    {
        using var conn = await GetSeededConnectionAsync();
        var pagination = PaginationParameters.Create(page: 3, pageSize: 10);

        var result = await conn.QueryPagedRawAsync<User>(
            "SELECT Id, Name FROM Users ORDER BY Id",
            "SELECT COUNT(*) FROM Users",
            pagination);

        result.Count.Should().Be(5);
        result[0].Id.Should().Be(21);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task QueryPagedRawAsync_WithParameters_FiltersCorrectly()
    {
        using var conn = await GetSeededConnectionAsync();
        var pagination = PaginationParameters.Create(page: 1, pageSize: 10);

        var result = await conn.QueryPagedRawAsync<User>(
            "SELECT Id, Name FROM Users WHERE Id <= @Max ORDER BY Id",
            "SELECT COUNT(*) FROM Users WHERE Id <= @Max",
            pagination,
            param: new { Max = 5 });

        result.TotalCount.Should().Be(5);
        result.Count.Should().Be(5);
        result.All(u => u.Id <= 5).Should().BeTrue();
    }

    [Fact]
    public async Task QueryPagedRawAsync_EmptyTable_ReturnsTotalCountZero()
    {
        using var conn = await GetSeededConnectionAsync(0);
        var pagination = PaginationParameters.Create(page: 1, pageSize: 10);

        var result = await conn.QueryPagedRawAsync<User>(
            "SELECT Id, Name FROM Users ORDER BY Id",
            "SELECT COUNT(*) FROM Users",
            pagination);

        result.TotalCount.Should().Be(0);
        result.Count.Should().Be(0);
    }

    // ── QueryPagedMultipleAsync — guard clauses ───────────────────────────────

    [Fact]
    public async Task QueryPagedMultipleAsync_WithNullOrWhiteSpaceSql_ThrowsArgumentException()
    {
        using var conn = await GetSeededConnectionAsync();
        Func<Task> act = async () => await conn.QueryPagedMultipleAsync<User>(
            "  ",
            PaginationParameters.Create(1, 10));
        await act.Should().ThrowAsync<ArgumentException>();
    }



    // ── QueryPagedMultipleAsync — happy path ─────────────────────────────────

    [Fact]
    public async Task QueryPagedMultipleAsync_Page1_ReturnsCorrectMetadata()
    {
        using var conn = await GetSeededConnectionAsync();
        var pagination = PaginationParameters.Create(page: 1, pageSize: 10);

        var sql = "SELECT Id, Name FROM Users ORDER BY Id LIMIT @Limit OFFSET @Offset;" +
                  "SELECT COUNT(*) FROM Users;";

        var result = await conn.QueryPagedMultipleAsync<User>(
            sql,
            pagination,
            param: new { Limit = 10, Offset = 0 });

        result.TotalCount.Should().Be(25);
        result.Count.Should().Be(10);
        result[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task QueryPagedMultipleAsync_Page2_CorrectOffset()
    {
        using var conn = await GetSeededConnectionAsync();
        var pagination = PaginationParameters.Create(page: 2, pageSize: 10);

        var sql = "SELECT Id, Name FROM Users ORDER BY Id LIMIT @Limit OFFSET @Offset;" +
                  "SELECT COUNT(*) FROM Users;";

        var result = await conn.QueryPagedMultipleAsync<User>(
            sql,
            pagination,
            param: new { Limit = 10, Offset = 10 });

        result.Count.Should().Be(10);
        result[0].Id.Should().Be(11);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task QueryPagedMultipleAsync_WithFilters_ReturnsFilteredCount()
    {
        using var conn = await GetSeededConnectionAsync();
        var pagination = PaginationParameters.Create(page: 1, pageSize: 10);

        var sql = "SELECT Id, Name FROM Users WHERE Id <= @Max ORDER BY Id LIMIT @Limit OFFSET @Offset;" +
                  "SELECT COUNT(*) FROM Users WHERE Id <= @Max;";

        var result = await conn.QueryPagedMultipleAsync<User>(
            sql,
            pagination,
            param: new { Max = 5, Limit = 10, Offset = 0 });

        result.TotalCount.Should().Be(5);
        result.Count.Should().Be(5);
    }
}





