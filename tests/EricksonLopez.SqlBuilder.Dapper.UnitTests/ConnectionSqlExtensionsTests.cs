// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.Pagination;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.Tests;

public class ConnectionSqlExtensionsTests
{
    private class DummyConnection : IDbConnection
    {
        public string ConnectionString { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int ConnectionTimeout => throw new NotImplementedException();
        public string Database => throw new NotImplementedException();
        public ConnectionState State => throw new NotImplementedException();
        public IDbTransaction BeginTransaction() => throw new NotImplementedException();
        public IDbTransaction BeginTransaction(IsolationLevel il) => throw new NotImplementedException();
        public void ChangeDatabase(string databaseName) => throw new NotImplementedException();
        public void Close() => throw new NotImplementedException();
        public IDbCommand CreateCommand() => throw new NotImplementedException();
        public void Dispose() => throw new NotImplementedException();
        public void Open() => throw new NotImplementedException();
    }

    private class User : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string GetTableName() => "users";
        public string[] GetColumnNames() => new[] { "id", "name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "Id", "id" }, { "Name", "name" } };
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    public ConnectionSqlExtensionsTests()
    {
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
    }

    [Fact]
    public void Sql_WithNullConnection_ThrowsArgumentNullException()
    {
        IDbConnection? connection = null;
        var act = () => connection!.Sql();
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public void Sql_WithValidConnection_ReturnsContext()
    {
        var connection = new DummyConnection();
        var context = connection.Sql();
        context.Connection.Should().BeSameAs(connection);
    }

    public class DummyUserDto { public string Name { get; set; } = ""; }

    [Fact]
    public void ProjectTo_CopiesNodes()
    {
        var query = Sql.From<User>().Where(u => u.Id == 1).OrderBy(u => u.Name);
        var projected = query.ProjectTo<User, DummyUserDto>();
        
        var ast = (IAstQuery)projected;
        var list = new List<ISqlNode>(ast.Nodes);
        list.Should().HaveCount(3);
    }

    [Fact]
    public void BoundSelectQuery_WithNulls_Throws()
    {
        var query = Sql.From<User>();
        var act1 = () => new BoundSelectQuery<User>(null!, new DummyConnection());
        act1.Should().Throw<ArgumentNullException>();

        var act2 = () => new BoundSelectQuery<User>(query, null!);
        act2.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ToResultAsync_OnSuccess_ReturnsItems()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT)");
        connection.Execute("INSERT INTO Users (Id, Name) VALUES (1, 'Erick')");

        var result = await Sql.From<User>().ToResultAsync(connection);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Erick");
    }

    [Fact]
    public async Task ToResultAsync_OnError_ReturnsFailure()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        var result = await Sql.From<User>().ToResultAsync(connection); // Table doesn't exist
        
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DbError");
    }

    [Fact]
    public async Task ToStreamAsync_OnSuccess_YieldsItems()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT)");
        connection.Execute("INSERT INTO Users (Id, Name) VALUES (1, 'Erick'), (2, 'Maria')");

        var stream = Sql.From<User>().ToStreamAsync(connection);
        
        var items = new List<User>();
        await foreach (var item in stream)
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
        items[0].Name.Should().Be("Erick");
        items[1].Name.Should().Be("Maria");
    }

    [Fact]
    public async Task ToStreamAsync_OnError_ThrowsException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        var stream = Sql.From<User>().ToStreamAsync(connection); // Table doesn't exist
        
        var act = async () => 
        {
            await foreach (var _ in stream) { }
        };

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ToPagedListAsync_OnSuccess_ReturnsPagedList()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT)");
        for (int i = 1; i <= 20; i++)
        {
            connection.Execute($"INSERT INTO Users (Id, Name) VALUES ({i}, 'User {i}')");
        }

        var result = await Sql.From<User>().ToPagedListAsync(connection, 2, 5);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(20);
        result.Value.TotalPages.Should().Be(4);
    }

    [Fact]
    public async Task ToPagedListAsync_OnError_ReturnsFailure()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        var result = await Sql.From<User>().ToPagedListAsync(connection, 1, 10);
        
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DbError");
    }

    [Fact]
    public async Task ToPagedListAsync_WithVariousNodes_FiltersProperlyForCountQuery()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT)");
        for (int i = 1; i <= 20; i++)
        {
            connection.Execute($"INSERT INTO Users (Id, Name) VALUES ({i}, 'User {i}')");
        }
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var tags = new Dictionary<string, object?>();
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

        // Add various nodes that should be filtered out or handled properly
        var query = Sql.From<User>()
            .Select("Id")
            .Select(u => u.Id)
            .RawSelect($"Name AS CustomName")
            .Where(u => u.Id > 0)
            .OrderBy(u => u.Id)
            .ThenByDescending(u => u.Name)
            .OrderBy($"Id ASC")
            .Limit(10)
            .Offset(0);
            
        var result = await query.ToPagedListAsync(connection, 2, 5);
        
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty(); 
        result.Value.TotalCount.Should().Be(20);

        var countQuery = executedStatements.FirstOrDefault(s => s.Contains("COUNT(*)"));
        countQuery.Should().NotBeNull();
        countQuery.Should().NotContain("ORDER BY");
        countQuery.Should().NotContain("LIMIT");
        countQuery.Should().NotContain("CustomName");
        countQuery.ToLowerInvariant().Should().NotContain("name");
    }

    [Fact]
    public async Task ToPagedListAsync_WithSelectNodeOnly_FiltersCorrectly()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT)");
        for (int i = 1; i <= 10; i++)
        {
            connection.Execute($"INSERT INTO Users (Id, Name) VALUES ({i}, 'User {i}')");
        }
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

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

        var query = Sql.From<User>().Select("Id", "Name");
        var result = await query.ToPagedListAsync(connection, 1, 5);
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(10);

        var countQuery = executedStatements.FirstOrDefault(s => s.Contains("COUNT(*)"));
        countQuery.Should().NotBeNull();
        countQuery.ToLowerInvariant().Should().NotContain("name");
    }

    [Fact]
    public async Task ToPagedListAsync_WithExpressionSelectNodeOnly_FiltersCorrectly()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT)");
        for (int i = 1; i <= 10; i++)
        {
            connection.Execute($"INSERT INTO Users (Id, Name) VALUES ({i}, 'User {i}')");
        }
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

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

        var query = Sql.From<User>().Select(u => u.Name);
        var result = await query.ToPagedListAsync(connection, 1, 5);
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(10);

        var countQuery = executedStatements.FirstOrDefault(s => s.Contains("COUNT(*)"));
        countQuery.Should().NotBeNull();
        countQuery.ToLowerInvariant().Should().NotContain("name");
    }

    [Fact]
    public async Task ToPagedListAsync_WithRawSelectNodeOnly_FiltersCorrectly()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE Users (Id INTEGER, Name TEXT)");
        for (int i = 1; i <= 10; i++)
        {
            connection.Execute($"INSERT INTO Users (Id, Name) VALUES ({i}, 'User {i}')");
        }
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

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

        var query = Sql.From<User>().RawSelect($"Name AS CustomUser");
        var result = await query.ToPagedListAsync(connection, 1, 5);
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(10);

        var countQuery = executedStatements.FirstOrDefault(s => s.Contains("COUNT(*)"));
        countQuery.Should().NotBeNull();
        countQuery.Should().NotContain("CustomUser");
    }

    [Fact]
    public void BoundSelectQuery_OrderBy_GeneratesAscendingOrder()
    {
        var connection = new SqliteConnection();
        var query = connection.Sql().Select<User>().OrderBy(u => u.Name);
        var result = query.Build(new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
        result.Sql.Should().Contain("ORDER BY \"name\"");
        result.Sql.Should().NotContain("DESC");
    }

    [Fact]
    public void BoundSelectQuery_OrderByDescending_GeneratesDescendingOrder()
    {
        var connection = new SqliteConnection();
        var query = connection.Sql().Select<User>().OrderByDescending(u => u.Name);
        var result = query.Build(new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
        result.Sql.Should().Contain("ORDER BY \"name\" DESC");
    }
}







