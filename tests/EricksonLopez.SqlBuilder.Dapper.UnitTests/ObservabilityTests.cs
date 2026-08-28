// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.Tests;

[Collection("Sequential")]
public class ObservabilityTests : IDisposable
{
    private readonly ActivityListener _listener;
    private readonly List<Activity> _activities = new();

    public ObservabilityTests()
    {
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => _activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _listener.Dispose();
    }

    [Fact]
    public async Task QueryAsync_WithListener_RecordsTagsAndDuration()
    {
        SqlBuilderDiagnostics.LogParameters = true;
        try
        {
            _activities.Clear();
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
                cmd.ExecuteNonQuery();
            }

            var query = Substitute.For<ISqlQuery>();
            query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT 1 as Id", new Dictionary<string, object?> { { "@p0", 1 } }));

            await connection.QueryAsync<dynamic>(query);

            var activity = _activities.SingleOrDefault(a => a.OperationName == "db.query" && a.Tags.Any(t => (string?)t.Value == "SELECT 1 as Id"));
            activity.Should().NotBeNull();
            activity!.OperationName.Should().Be("db.query");
            Assert.Contains(activity.Tags, t => t.Key == "db.statement" && (string?)t.Value == "SELECT 1 as Id");
            Assert.Contains(activity.Tags, t => t.Key == "db.parameter.@p0" && (string?)t.Value == "1");
        }
        finally
        {
            SqlBuilderDiagnostics.LogParameters = false;
        }
    }

    [Fact]
    public async Task QueryAsync_WithListener_NullParameter_SanitizesParameter()
    {
        SqlBuilderDiagnostics.LogParameters = false;
        _activities.Clear();
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT 1 as Id", new Dictionary<string, object?> { { "@p0", null } }));

        await connection.QueryAsync<dynamic>(query);

        var activity = _activities.SingleOrDefault(a => a.OperationName == "db.query" && a.Tags.Any(t => (string?)t.Value == "SELECT 1 as Id"));
        activity.Should().NotBeNull();
        Assert.Contains(activity!.Tags, t => t.Key == "db.parameter.@p0" && (string?)t.Value == "***");
    }

    [Fact]
    public async Task ExecuteAsync_WithListener_RecordsTagsAndDuration()
    {
        SqlBuilderDiagnostics.LogParameters = true;
        try
        {
            _activities.Clear();
            using var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
                cmd.ExecuteNonQuery();
            }

            var query = Substitute.For<ISqlQuery>();
            query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("INSERT INTO test (id) VALUES (@p0)", new Dictionary<string, object?> { { "@p0", 1 } }));

            await connection.ExecuteAsync(query);

            var activity = _activities.SingleOrDefault(a => a.OperationName == "db.execute" && a.Tags.Any(t => (string?)t.Value == "INSERT INTO test (id) VALUES (@p0)"));
            activity.Should().NotBeNull();
            activity!.OperationName.Should().Be("db.execute");
            Assert.Contains(activity.Tags, t => t.Key == "db.statement" && (string?)t.Value == "INSERT INTO test (id) VALUES (@p0)");
            Assert.Contains(activity.Tags, t => t.Key == "db.parameter.@p0" && (string?)t.Value == "1");
        }
        finally
        {
            SqlBuilderDiagnostics.LogParameters = false;
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithListener_NullParameter_SanitizesParameter()
    {
        SqlBuilderDiagnostics.LogParameters = false;
        _activities.Clear();
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("INSERT INTO test (id, name) VALUES (1, @p0)", new Dictionary<string, object?> { { "@p0", null } }));

        await connection.ExecuteAsync(query);

        var activity = _activities.SingleOrDefault(a => a.OperationName == "db.execute" && a.Tags.Any(t => (string?)t.Value == "INSERT INTO test (id, name) VALUES (1, @p0)"));
        activity.Should().NotBeNull();
        Assert.Contains(activity!.Tags, t => t.Key == "db.parameter.@p0" && (string?)t.Value == "***");
    }

    [Fact]
    public async Task QueryAsync_OnError_SetsErrorStatus()
    {
        _activities.Clear();
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT * FROM invalid_table", new Dictionary<string, object?>()));

        await Assert.ThrowsAnyAsync<Exception>(() => connection.QueryAsync<dynamic>(query));

        var activity = _activities.SingleOrDefault(a => a.OperationName == "db.query" && a.Tags.Any(t => (string?)t.Value == "SELECT * FROM invalid_table"));
        activity.Should().NotBeNull();
        activity!.Status.Should().Be(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task ExecuteAsync_OnError_SetsErrorStatus()
    {
        _activities.Clear();
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("INSERT INTO invalid_table (id) VALUES (1)", new Dictionary<string, object?>()));

        await Assert.ThrowsAnyAsync<Exception>(() => connection.ExecuteAsync(query));

        var activity = _activities.SingleOrDefault(a => a.OperationName == "db.execute" && a.Tags.Any(t => (string?)t.Value == "INSERT INTO invalid_table (id) VALUES (1)"));
        activity.Should().NotBeNull();
        activity!.Status.Should().Be(ActivityStatusCode.Error);
    }
}






