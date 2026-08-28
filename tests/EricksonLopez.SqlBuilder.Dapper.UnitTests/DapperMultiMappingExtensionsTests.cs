// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.UnitTests;

/// <summary>
/// Integration tests for multi-mapping extensions 2-7 entities using SQLite.
/// </summary>
[Collection("MultiMapping")]
public class DapperMultiMappingExtensionsTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE author  (id INTEGER PRIMARY KEY, name TEXT NOT NULL);
            CREATE TABLE book    (id INTEGER PRIMARY KEY, author_id INTEGER, title TEXT);
            CREATE TABLE genre   (id INTEGER PRIMARY KEY, book_id INTEGER, label TEXT);

            INSERT INTO author VALUES (1, 'Alice');
            INSERT INTO book   VALUES (10, 1, 'Book A');
            INSERT INTO genre  VALUES (100, 10, 'Fiction');
        ";
        await cmd.ExecuteNonQueryAsync();

        DapperExtensions.RegisterCompiler<SqliteConnection>(
            () => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
    }

    public async Task DisposeAsync() => await _connection.DisposeAsync();

    // ──────────────────────────────────────────────────────────────────────────
    // 2-entity mapping
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_TwoEntities_MapsCorrectly()
    {
        var query = new RawQuery(
            "SELECT a.id, a.name, b.id, b.title " +
            "FROM author a JOIN book b ON b.author_id = a.id");

        var results = await _connection.QueryAsync<Author, Book, (Author, Book)>(
            query,
            map: (a, b) => (a, b),
            splitOn: "id");

        var list = results.ToList();
        list.Should().HaveCount(1);
        list[0].Item1.Name.Should().Be("Alice");
        list[0].Item2.Title.Should().Be("Book A");
    }

    [Fact]
    public void Query_Sync_TwoEntities_MapsCorrectly()
    {
        var query = new RawQuery(
            "SELECT a.id, a.name, b.id, b.title " +
            "FROM author a JOIN book b ON b.author_id = a.id");

        var results = _connection.Query<Author, Book, (Author, Book)>(
            query,
            map: (a, b) => (a, b),
            splitOn: "id");

        var list = results.ToList();
        list.Should().HaveCount(1);
        list[0].Item1.Name.Should().Be("Alice");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3-entity mapping
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_ThreeEntities_MapsCorrectly()
    {
        var query = new RawQuery(
            "SELECT a.id, a.name, b.id, b.title, g.id, g.label " +
            "FROM author a " +
            "JOIN book  b ON b.author_id = a.id " +
            "JOIN genre g ON g.book_id = b.id");

        var results = await _connection.QueryAsync<Author, Book, Genre, string>(
            query,
            map: (a, b, g) => $"{a.Name}|{b.Title}|{g.Label}",
            splitOn: "id");

        var item = results.Single();
        item.Should().Be("Alice|Book A|Fiction");
    }

    [Fact]
    public void Query_Sync_ThreeEntities_MapsCorrectly()
    {
        var query = new RawQuery(
            "SELECT a.id, a.name, b.id, b.title, g.id, g.label " +
            "FROM author a " +
            "JOIN book  b ON b.author_id = a.id " +
            "JOIN genre g ON g.book_id = b.id");

        var results = _connection.Query<Author, Book, Genre, string>(
            query,
            map: (a, b, g) => $"{a.Name}|{b.Title}|{g.Label}",
            splitOn: "id");

        results.Single().Should().Be("Alice|Book A|Fiction");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GetCompiler invocation (verifies compiler resolution is used for all overloads)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task QueryAsync_4_MapsCorrectly_WithInlineSubquery()
    {
        // Use a raw query to avoid requiring 4 joined tables in test schema
        var query = new RawQuery(
            "SELECT 1 as id, 'a' as name, 2 as id, 'b' as name, 3 as id, 'c' as name, 4 as id, 'd' as name");

        var results = await _connection.QueryAsync<Author, Author, Author, Author, string>(
            query,
            map: (a, b, c, d) => $"{a.Id},{b.Id},{c.Id},{d.Id}",
            splitOn: "id");

        results.Single().Should().Be("1,2,3,4");
    }

    [Fact]
    public async Task QueryAsync_5_Compiles_AndReturns()
    {
        var query = new RawQuery(
            "SELECT 1 as id, '' as name, 2 as id, '' as name, 3 as id, '' as name, 4 as id, '' as name, 5 as id, '' as name");

        var results = await _connection.QueryAsync<Author, Author, Author, Author, Author, string>(
            query,
            map: (a, b, c, d, e) => $"{a.Id},{b.Id},{c.Id},{d.Id},{e.Id}",
            splitOn: "id");

        results.Single().Should().Be("1,2,3,4,5");
    }

    [Fact]
    public async Task QueryAsync_6_Compiles_AndReturns()
    {
        var query = new RawQuery(
            "SELECT 1 as id, '' as name, 2 as id, '' as name, 3 as id, '' as name, 4 as id, '' as name, 5 as id, '' as name, 6 as id, '' as name");

        var results = await _connection.QueryAsync<Author, Author, Author, Author, Author, Author, string>(
            query,
            map: (a, b, c, d, e, f) => $"{a.Id},{b.Id},{c.Id},{d.Id},{e.Id},{f.Id}",
            splitOn: "id");

        results.Single().Should().Be("1,2,3,4,5,6");
    }

    [Fact]
    public async Task QueryAsync_7_Compiles_AndReturns()
    {
        var query = new RawQuery(
            "SELECT 1 as id, '' as name, 2 as id, '' as name, 3 as id, '' as name, 4 as id, '' as name, 5 as id, '' as name, 6 as id, '' as name, 7 as id, '' as name");

        var results = await _connection.QueryAsync<Author, Author, Author, Author, Author, Author, Author, string>(
            query,
            map: (a, b, c, d, e, f, g) => $"{a.Id},{b.Id},{c.Id},{d.Id},{e.Id},{f.Id},{g.Id}",
            splitOn: "id");

        results.Single().Should().Be("1,2,3,4,5,6,7");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Sync overloads 4-7
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Query_Sync_4_Compiles_AndReturns()
    {
        var query = new RawQuery(
            "SELECT 1 as id, 'a' as name, 2 as id, 'b' as name, 3 as id, 'c' as name, 4 as id, 'd' as name");

        var result = _connection.Query<Author, Author, Author, Author, string>(
            query,
            map: (a, b, c, d) => $"{a.Id},{b.Id},{c.Id},{d.Id}",
            splitOn: "id");

        result.Single().Should().Be("1,2,3,4");
    }

    [Fact]
    public void Query_Sync_5_Compiles_AndReturns()
    {
        var query = new RawQuery(
            "SELECT 1 as id, '' as name, 2 as id, '' as name, 3 as id, '' as name, 4 as id, '' as name, 5 as id, '' as name");

        var result = _connection.Query<Author, Author, Author, Author, Author, string>(
            query,
            map: (a, b, c, d, e) => $"{a.Id},{b.Id},{c.Id},{d.Id},{e.Id}",
            splitOn: "id");

        result.Single().Should().Be("1,2,3,4,5");
    }

    [Fact]
    public void Query_Sync_6_Compiles_AndReturns()
    {
        var query = new RawQuery(
            "SELECT 1 as id, '' as name, 2 as id, '' as name, 3 as id, '' as name, 4 as id, '' as name, 5 as id, '' as name, 6 as id, '' as name");

        var result = _connection.Query<Author, Author, Author, Author, Author, Author, string>(
            query,
            map: (a, b, c, d, e, f) => $"{a.Id},{b.Id},{c.Id},{d.Id},{e.Id},{f.Id}",
            splitOn: "id");

        result.Single().Should().Be("1,2,3,4,5,6");
    }

    [Fact]
    public void Query_Sync_7_Compiles_AndReturns()
    {
        var query = new RawQuery(
            "SELECT 1 as id, '' as name, 2 as id, '' as name, 3 as id, '' as name, 4 as id, '' as name, 5 as id, '' as name, 6 as id, '' as name, 7 as id, '' as name");

        var result = _connection.Query<Author, Author, Author, Author, Author, Author, Author, string>(
            query,
            map: (a, b, c, d, e, f, g) => $"{a.Id},{b.Id},{c.Id},{d.Id},{e.Id},{f.Id},{g.Id}",
            splitOn: "id");

        result.Single().Should().Be("1,2,3,4,5,6,7");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Models
    // ──────────────────────────────────────────────────────────────────────────

    private class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private class Genre
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }
}





