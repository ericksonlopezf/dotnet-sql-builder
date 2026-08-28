// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.Tests;

[Collection("Sequential")]
public class DapperExtensionsTests
{
    public DapperExtensionsTests()
    {
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
    }
    [Fact]
    public async Task QueryAsync_PassesSqlAndParametersToDapper()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // Create table
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
        }
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO test (id, name) VALUES (1, 'erick')";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT id, name FROM test WHERE id = @p0", new Dictionary<string, object?> { { "@p0", 1 } }));

        var result = await connection.QueryAsync<TestUser>(query);
        
        var list = new List<TestUser>(result);
        list.Should().ContainSingle();
        list[0].Id.Should().Be(1);
        list[0].Name.Should().Be("erick");
    }

    [Fact]
    public async Task ExecuteAsync_PassesSqlAndParametersToDapper()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("INSERT INTO test (id, name) VALUES (@p0, @p1)", new Dictionary<string, object?> { { "@p0", 2 }, { "@p1", "john" } }));

        var rows = await connection.ExecuteAsync(query);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_NoParameters_PassesSqlToDapper()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
        }
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO test (id, name) VALUES (1, 'erick')";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT id, name FROM test", new Dictionary<string, object?>()));

        var result = await connection.QueryAsync<TestUser>(query);
        
        var list = new List<TestUser>(result);
        list.Should().ContainSingle();
    }
    
    [Fact]
    public async Task ExecuteAsync_NoParameters_PassesSqlToDapper()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("INSERT INTO test (id, name) VALUES (2, 'john')", new Dictionary<string, object?>()));

        var rows = await connection.ExecuteAsync(query);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_NullParameter_PassesSqlToDapper()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
        }
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "INSERT INTO test (id, name) VALUES (1, NULL)";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT id, name FROM test WHERE name IS NULL OR name = @p0", new Dictionary<string, object?> { { "@p0", null } }));

        var result = await connection.QueryAsync<TestUser>(query);
        var list = new List<TestUser>(result);
        list.Should().ContainSingle();
    }

    [Fact]
    public async Task ExecuteAsync_NullParameter_PassesSqlToDapper()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE test (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("INSERT INTO test (id, name) VALUES (@p0, @p1)", new Dictionary<string, object?> { { "@p0", 2 }, { "@p1", null } }));

        var rows = await connection.ExecuteAsync(query);
        rows.Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_OnError_ThrowsException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT * FROM invalid_table", new Dictionary<string, object?>()));

        await Assert.ThrowsAnyAsync<Exception>(() => connection.QueryAsync<dynamic>(query));
    }

    [Fact]
    public async Task ExecuteAsync_OnError_ThrowsException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("INSERT INTO invalid_table (id) VALUES (1)", new Dictionary<string, object?>()));

        await Assert.ThrowsAnyAsync<Exception>(() => connection.ExecuteAsync(query));
    }

    [Fact]
    public async Task QueryAotAsync_DbCommand_ExecutesAndMapsProperly()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE testaot (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO testaot (id, name) VALUES (1, 'erick')";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT id, name FROM testaot WHERE id = @p0", new Dictionary<string, object?> { { "@p0", 1 } }));

        var result = await connection.QueryAotAsync<TestUser>(query, reader => new TestUser 
        { 
            Id = reader.GetInt32(0), 
            Name = reader.GetString(1) 
        });
        
        var list = new List<TestUser>(result);
        list.Should().ContainSingle();
        list[0].Id.Should().Be(1);
        list[0].Name.Should().Be("erick");
    }

    [Fact]
    public async Task QueryAotAsync_IDbCommand_ExecutesAndMapsProperly()
    {
        // Mock an IDbConnection that returns a non-DbCommand IDbCommand to test the else branch
        var connection = Substitute.For<IDbConnection>();
        typeof(DapperExtensions)
            .GetMethod("RegisterCompiler")!
            .MakeGenericMethod(connection.GetType())
            .Invoke(null, new object[] { (Func<ISqlCompiler>)(() => Substitute.For<ISqlCompiler>()) });
            
        var command = Substitute.For<IDbCommand>();
        var parameters = Substitute.For<IDataParameterCollection>();
        var reader = Substitute.For<IDataReader>();
        
        connection.CreateCommand().Returns(command);
        command.Parameters.Returns(parameters);
        
        // Setup reader to return one row
        reader.Read().Returns(true, false);
        reader.GetInt32(0).Returns(99);
        reader.GetString(1).Returns("mocked");
        command.ExecuteReader().Returns(reader);
        
        var paramMock = Substitute.For<IDbDataParameter>();
        command.CreateParameter().Returns(paramMock);

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT id, name FROM testaot", new Dictionary<string, object?>()));

        var result = await connection.QueryAotAsync<TestUser>(query, r => new TestUser 
        { 
            Id = r.GetInt32(0), 
            Name = r.GetString(1) 
        });

        var list = new List<TestUser>(result);
        list.Should().ContainSingle();
        list[0].Id.Should().Be(99);
        list[0].Name.Should().Be("mocked");
    }

    [Fact]
    public async Task QueryAotAsync_OnError_ThrowsException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT * FROM invalid_table", new Dictionary<string, object?>()));

        await Assert.ThrowsAnyAsync<Exception>(() => connection.QueryAotAsync<TestUser>(query, r => new TestUser()));
    }

    [Fact]
    public async Task QuerySequentialAsync_DbCommand_ExecutesAndStreamsProperly()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "CREATE TABLE testseq (id INTEGER, name TEXT)";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO testseq (id, name) VALUES (1, 'erick')";
            cmd.ExecuteNonQuery();
            cmd.CommandText = "INSERT INTO testseq (id, name) VALUES (2, 'john')";
            cmd.ExecuteNonQuery();
        }

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT id, name FROM testseq ORDER BY id", new Dictionary<string, object?>()));

        var list = new List<TestUser>();
        var results = await connection.QuerySequentialAsync<TestUser>(query, reader => new TestUser 
        { 
            Id = reader.GetInt32(0), 
            Name = reader.GetString(1) 
        });
        foreach (var user in results)
        {
            list.Add(user);
        }
        
        list.Should().HaveCount(2);
        list[0].Id.Should().Be(1);
        list[1].Id.Should().Be(2);
    }

    [Fact]
    public async Task QuerySequentialAsync_IDbCommand_ExecutesAndStreamsProperly()
    {
        // Mock an IDbConnection that returns a non-DbCommand IDbCommand
        var connection = Substitute.For<IDbConnection>();
        typeof(DapperExtensions)
            .GetMethod("RegisterCompiler")!
            .MakeGenericMethod(connection.GetType())
            .Invoke(null, new object[] { (Func<ISqlCompiler>)(() => Substitute.For<ISqlCompiler>()) });
            
        var command = Substitute.For<IDbCommand>();
        var parameters = Substitute.For<IDataParameterCollection>();
        var reader = Substitute.For<IDataReader>();
        
        connection.CreateCommand().Returns(command);
        command.Parameters.Returns(parameters);
        
        // Setup reader to return one row
        reader.Read().Returns(true, false);
        reader.GetInt32(0).Returns(99);
        reader.GetString(1).Returns("mocked");
        command.ExecuteReader(CommandBehavior.SequentialAccess).Returns(reader);
        
        var paramMock = Substitute.For<IDbDataParameter>();
        command.CreateParameter().Returns(paramMock);

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT id, name FROM testaot", new Dictionary<string, object?>()));

        var list = new List<TestUser>();
        var results = await connection.QuerySequentialAsync<TestUser>(query, r => new TestUser 
        { 
            Id = r.GetInt32(0), 
            Name = r.GetString(1) 
        });
        foreach (var user in results)
        {
            list.Add(user);
        }

        list.Should().ContainSingle();
        list[0].Id.Should().Be(99);
    }

    [Fact]
    public async Task QuerySequentialAsync_OnError_ThrowsException()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT * FROM invalid_table", new Dictionary<string, object?>()));

        await Assert.ThrowsAnyAsync<Exception>(() => connection.QuerySequentialAsync<TestUser>(query, r => new TestUser()));
    }

    private class TestCustomType
    {
        public string Value { get; set; } = string.Empty;
    }

    private class CustomTypeHandler : EricksonLopez.SqlBuilder.Abstractions.ITypeHandler
    {
        public object? Parse(Type destinationType, object? value) => new TestCustomType { Value = value?.ToString() ?? "" };
        public void SetValue(System.Data.IDbDataParameter parameter, object value) => parameter.Value = ((TestCustomType)value).Value;
    }

    [Fact]
    public void RegisterTypeHandler_AdapterDelegatesToCustomHandler()
    {
        // This test ensures the DapperTypeHandlerAdapter works by verifying that
        // DapperExtensions.RegisterTypeHandler correctly registers it with Dapper.
        var handler = new CustomTypeHandler();
        DapperExtensions.RegisterTypeHandler<TestCustomType>(handler);

        // Since it's registered globally in Dapper, we can verify by letting Dapper query it
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var result = connection.QuerySingle<TestCustomType>("SELECT 'Hello' AS Value");
        result.Should().NotBeNull();
        result.Value.Should().Be("Hello");

        // Now test SetValue by passing it as a parameter
        var count = connection.QuerySingle<int>("SELECT 1 WHERE @p = 'World'", new { p = new TestCustomType { Value = "World" } });
        count.Should().Be(1);
    }

    private class UnregisteredConnection : IDbConnection
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

    [Fact]
    public void GetCompiler_UnregisteredConnection_ThrowsInvalidOperationException()
    {
        var connection = new UnregisteredConnection();
        var act = () => DapperExtensions.GetCompiler(connection);
        act.Should().Throw<InvalidOperationException>().WithMessage("*No SQL compiler registered*");
    }

    private class TestUser : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public string GetTableName() => "test";
        public string[] GetColumnNames() => new[] { "id", "name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "Id", "id" }, { "Name", "name" } };
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }
}








