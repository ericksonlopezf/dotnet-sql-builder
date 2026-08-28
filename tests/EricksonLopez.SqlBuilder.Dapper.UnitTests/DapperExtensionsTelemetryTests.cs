// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.Tests;

public class DapperExtensionsTelemetryTests
{
    private class TestUser : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        public string GetTableName() => "users";
        public string[] GetColumnNames() => new[] { "id", "name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "Id", "id" }, { "Name", "name" } };
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Theory]
    [InlineData("QueryAsync")]
    [InlineData("ExecuteAsync")]
    [InlineData("QuerySequentialAsync")]
    [InlineData("QueryAotAsync")]
    [InlineData("QueryFirstOrDefaultAotAsync")]
    public async Task AsyncRunners_WhenExceptionThrown_LogsErrorAndThrows(string methodName)
    {
        var factory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        factory.CreateLogger("EricksonLopez.SqlBuilder.Dapper").Returns(logger);
        SqlBuilderDiagnostics.LoggerFactory = factory;
        
        // Use an in-memory db with NO tables to force exceptions
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var query = Sql.From<TestUser>().Where(u => u.Id == 1);
        
        Func<Task> action = methodName switch
        {
            "QueryAsync" => async () => await connection.QueryAsync<TestUser>(query),
            "ExecuteAsync" => async () => await connection.ExecuteAsync(query),
            "QuerySequentialAsync" => async () => await connection.QuerySequentialAsync(query, r => new TestUser()),
            "QueryAotAsync" => async () => await connection.QueryAotAsync(query, r => new TestUser()),
            "QueryFirstOrDefaultAotAsync" => async () => await connection.QueryFirstOrDefaultAotAsync(query, r => new TestUser()),
            _ => throw new NotImplementedException()
        };

        await action.Should().ThrowAsync<SqliteException>();

        // Assert metrics.SetError was called, which calls LogError
        logger.ReceivedWithAnyArgs().Log(LogLevel.Error, default, default, default, default!);
    }

    [Theory]
    [InlineData("Query")]
    [InlineData("Execute")]
    public void SyncRunners_WhenExceptionThrown_LogsErrorAndThrows(string methodName)
    {
        var factory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        factory.CreateLogger("EricksonLopez.SqlBuilder.Dapper").Returns(logger);
        SqlBuilderDiagnostics.LoggerFactory = factory;
        
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var query = Sql.From<TestUser>().Where(u => u.Id == 1);
        
        Action action = methodName switch
        {
            "Query" => () => connection.Query<TestUser>(query),
            "Execute" => () => connection.Execute(query),
            _ => throw new NotImplementedException()
        };

        action.Should().Throw<SqliteException>();

        logger.ReceivedWithAnyArgs().Log(LogLevel.Error, default, default, default, default!);
    }
}





