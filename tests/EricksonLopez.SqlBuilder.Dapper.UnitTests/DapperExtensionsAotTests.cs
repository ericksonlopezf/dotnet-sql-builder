// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.Tests;

public class DapperExtensionsAotTests
{
    private class TestUser : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
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

    private class DummyCommand : IDbCommand
    {
        public string CommandText { get; set; } = string.Empty;
        public int CommandTimeout { get; set; }
        public CommandType CommandType { get; set; }
        public IDbConnection? Connection { get; set; }
        public IDataParameterCollection Parameters { get; } = Substitute.For<IDataParameterCollection>();
        public IDbTransaction? Transaction { get; set; }
        public UpdateRowSource UpdatedRowSource { get; set; }

        public void Cancel() { }
        public IDbDataParameter CreateParameter() => Substitute.For<IDbDataParameter>();
        public void Dispose() { }
        public int ExecuteNonQuery() => 0;

        public IDataReader ExecuteReader()
        {
            var reader = Substitute.For<IDataReader>();
            reader.Read().Returns(true, false);
            return reader;
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            return ExecuteReader();
        }

        public object? ExecuteScalar() => null;
        public void Prepare() { }
    }

    private class DummyConnection : IDbConnection
    {
        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 0;
        public string Database => "";
        public ConnectionState State => ConnectionState.Open;

        public IDbTransaction BeginTransaction() => null!;
        public IDbTransaction BeginTransaction(IsolationLevel il) => null!;
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public IDbCommand CreateCommand() => new DummyCommand();
        public void Dispose() { }
        public void Open() { }
    }

    [Fact]
    public async Task QuerySequentialAsync_WithNonDbCommand_UsesSyncReader()
    {
        DapperExtensions.RegisterCompiler<DummyConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
        var connection = new DummyConnection();
        var query = Sql.From<TestUser>();

        var result = await connection.QuerySequentialAsync(query, reader => new TestUser());
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryAotAsync_WithNonDbCommand_UsesSyncReader()
    {
        DapperExtensions.RegisterCompiler<DummyConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
        var connection = new DummyConnection();
        var query = Sql.From<TestUser>();

        var result = await connection.QueryAotAsync(query, reader => new TestUser());
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAotAsync_WithNonDbCommand_UsesSyncReader()
    {
        DapperExtensions.RegisterCompiler<DummyConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
        var connection = new DummyConnection();
        var query = Sql.From<TestUser>();

        var result = await connection.QueryFirstOrDefaultAotAsync(query, reader => new TestUser());
        result.Should().NotBeNull();
    }
}





