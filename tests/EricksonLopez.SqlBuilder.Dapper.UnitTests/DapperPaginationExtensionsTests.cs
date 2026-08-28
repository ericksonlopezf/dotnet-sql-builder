// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Pagination;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.UnitTests;

public class DapperPaginationExtensionsTests
{
    private static async Task<IDbConnection> GetOpenConnectionAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        // Create table and seed data
        await connection.ExecuteAsync("CREATE TABLE Users (Id INTEGER PRIMARY KEY, Name TEXT)");

        var sql = "INSERT INTO Users (Id, Name) VALUES (@Id, @Name)";
        var users = Enumerable.Range(1, 25).Select(i => new { Id = i, Name = $"User {i}" }).ToList();
        
        await connection.ExecuteAsync(sql, users);

        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());
        
        return connection;
    }

    private class User : global::EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string GetTableName() => "Users";
        public string[] GetColumnNames() => new[] { "Id", "Name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "Id", "Id" }, { "Name", "Name" } };
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }

    [Fact]
    public async Task QueryPagedAsync_WithCount_ReturnsCorrectMetadata()
    {
        // Arrange
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().From("Users").OrderBy(u => u.Id);
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);

        // Act
        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: true);

        // Assert
        paged.TotalCount.Should().Be(25);
        paged.TotalPages.Should().Be(3);
        paged.Count.Should().Be(10);
        paged[0].Id.Should().Be(11);
        paged.HasNextPage.Should().BeTrue();
        paged.HasPreviousPage.Should().BeTrue();
    }
    
    [Fact]
    public async Task QueryPagedAsync_WithExpressionSelect_ReturnsCorrectMetadata()
    {
        // Arrange
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().Select(u => u.Id).From("Users").OrderBy(u => u.Id);
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);

        // Act
        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: true);

        // Assert
        paged.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task QueryPagedAsync_WithoutCount_EvaluatesHasNextPageCorrectly()
    {
        // Arrange
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().From("Users").OrderBy(u => u.Id);
        var parameters = PaginationParameters.Create(page: 3, pageSize: 10);

        // Act
        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: false);

        // Assert
        paged.TotalCount.Should().BeNull();
        paged.TotalPages.Should().BeNull();
        paged.Count.Should().Be(5); // Only 5 items on the last page
        paged[0].Id.Should().Be(21);
        paged.HasNextPage.Should().BeFalse();
        paged.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public async Task QueryPagedAsync_WithRawSelect_ReturnsCorrectMetadata()
    {
        // Arrange
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().RawSelect($"id, name").From("Users").OrderBy(u => u.Id);
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);

        // Act
        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: true);

        // Assert
        paged.TotalCount.Should().Be(25);
    }
    
    [Fact]
    public async Task QueryPagedAsync_WithoutCount_HasNextPage_EvaluatesTrue()
    {
        // Arrange
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().From("Users").OrderBy(u => u.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        // Act
        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: false);

        // Assert
        paged.TotalCount.Should().BeNull();
        paged.TotalPages.Should().BeNull();
        paged.Count.Should().Be(10);
        paged.HasNextPage.Should().BeTrue();
        paged.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryPagedAsync_WithCount_EmptyResult_ReturnsEmpty()
    {
        // Arrange
        using var connection = await GetOpenConnectionAsync();
        // clear table
        await connection.ExecuteAsync("DELETE FROM Users");

        var query = new SelectQuery<User>().From("Users").OrderBy(u => u.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        // Act
        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: true);

        // Assert
        paged.Should().BeAssignableTo<IPagedList<User>>();
        paged.TotalCount.Should().Be(0);
        paged.TotalPages.Should().Be(0);
        paged.Count.Should().Be(0);
        paged.Should().BeEmpty();
    }

    [Fact]
    public async Task QueryPagedAsync_WithoutCount_ExactPageSize_HasNextPageIsFalse()
    {
        // Arrange
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE Users (Id INTEGER PRIMARY KEY, Name TEXT)");
        var users = Enumerable.Range(1, 10).Select(i => new { Id = i, Name = $"User {i}" }).ToList();
        await connection.ExecuteAsync("INSERT INTO Users (Id, Name) VALUES (@Id, @Name)", users);
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());
        
        var query = new SelectQuery<User>().From("Users").OrderBy(u => u.Id);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        // Act
        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: false);

        // Assert
        paged.Count.Should().Be(10);
        paged.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task QueryPagedAsync_WithCount_FiltersNodesProperly()
    {
        // Arrange
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().From("Users")
            .Select(u => u.Id)
            .RawSelect($"Name AS CustomName")
            .OrderBy(u => u.Id)
            .OrderBy($"Name DESC")
            .Limit(10)
            .Offset(5);
            
        var parameters = PaginationParameters.Create(page: 2, pageSize: 10);

        var executedStatements = new List<string>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                var stmt = activity.TagObjects.FirstOrDefault(t => t.Key == "db.statement").Value?.ToString();
                if (stmt != null)
                {
                    executedStatements.Add(stmt);
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        // Act
        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: true);

        // Assert
        paged.TotalCount.Should().Be(25);
        
        var countQuery = executedStatements.FirstOrDefault(s => s.Contains("COUNT(*)"));
        countQuery.Should().NotBeNull();
        countQuery.Should().NotContain("ORDER BY");
        countQuery.Should().NotContain("LIMIT");
        countQuery.Should().NotContain("OFFSET");
        countQuery.Should().NotContain("CustomName");
        countQuery.ToLowerInvariant().Should().NotContain("name");
    }

    [Fact]
    public async Task QueryPagedAsync_WithSelectNodeOnly_FiltersCorrectly()
    {
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().From("Users").Select("Id", "Name");
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var executedStatements = new List<string>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                var stmt = activity.TagObjects.FirstOrDefault(t => t.Key == "db.statement").Value?.ToString();
                if (stmt != null)
                {
                    executedStatements.Add(stmt);
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: true);
        paged.TotalCount.Should().Be(25);

        var countQuery = executedStatements.FirstOrDefault(s => s.Contains("COUNT(*)"));
        countQuery.Should().NotBeNull();
        countQuery.ToLowerInvariant().Should().NotContain("name");
    }

    [Fact]
    public async Task QueryPagedAsync_WithExpressionSelectNodeOnly_FiltersCorrectly()
    {
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().From("Users").Select(u => u.Name);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var executedStatements = new List<string>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                var stmt = activity.TagObjects.FirstOrDefault(t => t.Key == "db.statement").Value?.ToString();
                if (stmt != null)
                {
                    executedStatements.Add(stmt);
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: true);
        paged.TotalCount.Should().Be(25);

        var countQuery = executedStatements.FirstOrDefault(s => s.Contains("COUNT(*)"));
        countQuery.Should().NotBeNull();
        countQuery.ToLowerInvariant().Should().NotContain("name");
    }

    [Fact]
    public async Task QueryPagedAsync_WithRawSelectNodeOnly_FiltersCorrectly()
    {
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().From("Users").RawSelect($"Name AS CustomUser");
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var executedStatements = new List<string>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                var stmt = activity.TagObjects.FirstOrDefault(t => t.Key == "db.statement").Value?.ToString();
                if (stmt != null)
                {
                    executedStatements.Add(stmt);
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: true);
        paged.TotalCount.Should().Be(25);

        var countQuery = executedStatements.FirstOrDefault(s => s.Contains("COUNT(*)"));
        countQuery.Should().NotBeNull();
        countQuery.Should().NotContain("CustomUser");
    }

    [Fact]
    public async Task QueryPagedAsync_WithOrderByNodeOnly_FiltersCorrectly()
    {
        using var connection = await GetOpenConnectionAsync();
        var query = new SelectQuery<User>().From("Users").OrderBy(u => u.Name);
        var parameters = PaginationParameters.Create(page: 1, pageSize: 10);

        var executedStatements = new List<string>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                var stmt = activity.TagObjects.FirstOrDefault(t => t.Key == "db.statement").Value?.ToString();
                if (stmt != null)
                {
                    executedStatements.Add(stmt);
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var paged = await connection.QueryPagedAsync(query, parameters, countTotal: true);
        paged.TotalCount.Should().Be(25);

        var countQuery = executedStatements.FirstOrDefault(s => s.Contains("COUNT(*)"));
        countQuery.Should().NotBeNull();
        countQuery.Should().NotContain("ORDER BY");
    }
}






