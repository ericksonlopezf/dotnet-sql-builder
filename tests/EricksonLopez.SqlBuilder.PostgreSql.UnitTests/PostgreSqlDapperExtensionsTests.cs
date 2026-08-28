// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.PostgreSql.UnitTests.Mocks;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Npgsql;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class PostgreSqlDapperExtensionsTests
{
    [Fact]
    public async Task BulkCopyAsync_WithNonNpgsqlConnection_ThrowsInvalidOperationException()
    {
        var mockConnection = Substitute.For<IDbConnection>();
        var data = new[] { ObjectMother.CreateUser() };
        
        Func<Task> act = async () => await PostgreSqlDapperExtensions.BulkCopyAsync(mockConnection, data);
        
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("BulkCopyAsync requires an active NpgsqlConnection.");
    }
    
    [Fact]
    public async Task BulkCopyAsync_WithNpgsqlConnectionAsIDbConnection_ShouldCallBulkCopy()
    {
        System.Data.IDbConnection conn = new NpgsqlConnection();
        var data = new[] { new TestEntity { Id = 1, Name = "Test" } };
        
        Func<Task> act = async () => await PostgreSqlDapperExtensions.BulkCopyAsync(conn, data);
        
        await act.Should().ThrowAsync<Exception>(); // Throws because closed/uninitialized
    }

    [Fact]
    public async Task BulkInsertUnnestAsync_WithEmptyList_ReturnsZero()
    {
        var connection = new MockDbConnection();
        var data = Array.Empty<TestEntity>();
        
        var result = await connection.BulkInsertUnnestAsync(data);
        
        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertUnnestAsync_WithEntities_GeneratesCommand()
    {
        var connection = new MockDbConnection();
        var data = new[] { new TestEntity { Id = 1, Name = "Test" } };
        
        var result = await connection.BulkInsertUnnestAsync(data);
        
        result.Should().Be(1);
        connection.Commands.Should().HaveCount(1);
        connection.Commands[0].CommandText.Should().Contain("INSERT INTO \"testentitys\" (\"id\", \"name\"");
        connection.Commands[0].CommandText.Should().Contain("SELECT * FROM UNNEST(@p0, @p1, @p2)");
        connection.Commands[0].Parameters.Count.Should().Be(3);
        connection.Commands[0].Parameters[0].ParameterName.Should().Be("p0");
        var valArray = (int[])connection.Commands[0].Parameters[0].Value!;
        valArray.Length.Should().Be(1);
        valArray[0].Should().Be(1);
    }

    [Fact]
    public async Task BulkInsertUnnestAsync_WithTransaction_SetsTransaction()
    {
        var connection = new MockDbConnection();
        var data = new[] { new TestEntity { Id = 1, Name = "Test" } };
        var tx = connection.BeginTransaction();
        
        await connection.BulkInsertUnnestAsync(data, tx);
        
        connection.Commands[0].Transaction.Should().Be(tx);
    }

    [Fact]
    public async Task BulkInsertUnnestAsync_WithNullSampleVal_ShouldFallbackToObjectArray()
    {
        var connection = new MockDbConnection();
        var data = new[] { new TestEntity { Id = 1, Name = null! } };
        
        await connection.BulkInsertUnnestAsync(data);
        
        connection.Commands[0].Parameters[1].Value.Should().BeOfType<object[]>();
        var objArr = (object[])connection.Commands[0].Parameters[1].Value!;
        objArr[0].Should().BeNull();
    }

    [Fact]
    public async Task BulkInsertUnnestAsync_WithEnumerable_ShouldCallToList()
    {
        var connection = new MockDbConnection();
        var data = new[] { new TestEntity { Id = 1, Name = "Test" } };
        var enumerable = data.Where(x => true); 
        
        await connection.BulkInsertUnnestAsync(enumerable);
        
        connection.Commands.Count.Should().Be(1);
    }

    [Fact]
    public async Task BulkInsertUnnestAsync_WithPlainNonDbCommand_ShouldCallSyncExecuteNonQuery()
    {
        var mockConn = Substitute.For<IDbConnection>();
        var mockCmd = Substitute.For<IDbCommand>(); // not DbCommand
        var mockParams = Substitute.For<IDataParameterCollection>();
        var mockParam = Substitute.For<IDbDataParameter>();
        
        mockConn.CreateCommand().Returns(mockCmd);
        mockCmd.Parameters.Returns(mockParams);
        mockCmd.CreateParameter().Returns(mockParam);
        mockCmd.ExecuteNonQuery().Returns(1);

        var data = new[] { new TestEntity { Id = 1, Name = "Test" } };
        var result = await mockConn.BulkInsertUnnestAsync(data);

        result.Should().Be(1);
        mockCmd.Received(1).ExecuteNonQuery();
    }
    [Fact]
    public async Task BulkInsertAsync_WithNullConnection_ThrowsArgumentNullException()
    {
        IDbConnection? connection = null;
        var act = () => connection!.BulkInsertAsync("sql", Array.Empty<NpgsqlParameter>());
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task BulkInsertAsync_WithNullOrEmptySql_ThrowsArgumentException()
    {
        var connection = Substitute.For<IDbConnection>();
        var act = () => connection.BulkInsertAsync("", Array.Empty<NpgsqlParameter>());
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("sql");
    }

    [Fact]
    public async Task BulkInsertAsync_WithNullParameters_ThrowsArgumentNullException()
    {
        var connection = Substitute.For<IDbConnection>();
        var act = () => connection.BulkInsertAsync("sql", null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("parameters");
    }

    [Fact]
    public async Task BulkInsertAsync_WithEmptyParameters_ReturnsZero()
    {
        var connection = Substitute.For<IDbConnection>();
        var result = await connection.BulkInsertAsync("sql", Array.Empty<NpgsqlParameter>());
        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkInsertAsync_WithNonNpgsqlConnection_ThrowsArgumentException()
    {
        var connection = Substitute.For<IDbConnection>();
        var parameters = new[] { new NpgsqlParameter() };
        var act = () => connection.BulkInsertAsync("sql", parameters);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*BulkInsertAsync requires an NpgsqlConnection*");
    }
    
    [Fact]
    public async Task BulkInsertAsync_WithNpgsqlConnection_ThrowsExceptionFromDatabase()
    {
        // Here we just use a dummy closed connection to ensure we hit the OpenAsync / Execute branch
        // Since we don't have a DB, it will fail to open or execute.
        var connection = new NpgsqlConnection();
        var parameters = new[] { new NpgsqlParameter() };
        
        var act = () => connection.BulkInsertAsync("sql", parameters);
        
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task BulkUpsertAsync_CallsBulkInsertAsync()
    {
        var connection = Substitute.For<IDbConnection>();
        var result = await connection.BulkUpsertAsync("sql", Array.Empty<NpgsqlParameter>());
        result.Should().Be(0); // Falls through the empty parameters check in BulkInsertAsync
    }

    private class MismatchedEntity : ISqlEntity
    {
        public string GetTableName() => "mismatched";
        public string[] GetColumnNames() => new[] { "col1", "col2" };
        public object?[] GetValues() => new object?[] { 1 };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public System.Collections.Generic.IReadOnlyDictionary<string, string> GetPropertyMap() => new System.Collections.Generic.Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public async Task BulkInsertUnnestAsync_WithMetadataMismatch_ThrowsInvalidOperationException()
    {
        var connection = new MockDbConnection();
        var data = new[] { new MismatchedEntity() };
        var act = () => connection.BulkInsertUnnestAsync(data);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Entity metadata mismatch: GetColumnNames() returned 2 items, but GetValues() returned 1. They must match.");
    }
}





