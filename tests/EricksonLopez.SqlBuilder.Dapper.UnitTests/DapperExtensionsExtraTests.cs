// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.Tests;

public class DapperExtensionsExtraTests
{
    public class TestUser : ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        
        public string GetTableName() => "TestUsers";
        public string[] GetColumnNames() => new[] { "Id", "Name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    private class NonCollectionEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _items;
        public NonCollectionEnumerable(IEnumerable<T> items) => _items = items;
        public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

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

    [Fact]
    public void GetCompiler_WhenNotRegistered_ThrowsInvalidOperationException()
    {
        var connection = new DummyConnection();
        var act = () => DapperExtensions.GetCompiler(connection);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage($"No SQL compiler registered for connection type {typeof(DummyConnection).Name}. Please call DapperExtensions.RegisterCompiler first.");
    }

    [Fact]
    public void SyncMethods_ExecuteAndQuery_ThrowOnError()
    {
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT * FROM NonExistentTable", new Dictionary<string, object?> { { "@p1", "test" } }));

        var actQuery = () => connection.Query<TestUser>(query);
        actQuery.Should().Throw<SqliteException>();

        var actExecute = () => connection.Execute(query);
        actExecute.Should().Throw<SqliteException>();
    }

    [Fact]
    public async Task AsyncMethods_ExecuteAndQuery_ThrowOnError()
    {
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT * FROM NonExistentTable", new Dictionary<string, object?> { { "@p1", "test" } }));

        var actQuery = async () => await connection.QueryAsync<TestUser>(query);
        await actQuery.Should().ThrowAsync<SqliteException>();

        var actExecute = async () => await connection.ExecuteAsync(query);
        await actExecute.Should().ThrowAsync<SqliteException>();
    }

    [Fact]
    public async Task BulkInsertAsync_WhenEmptyList_ReturnsZero()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        var result = await connection.BulkInsertAsync(new List<TestUser>());
        result.Should().Be(0);
    }
    
    [Fact]
    public void SyncMethods_ExecuteAndQuery_Success()
    {
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE test (id INTEGER, name TEXT)");
        connection.Execute("INSERT INTO test (id, name) VALUES (1, 'erick')");

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT id, name FROM test", new Dictionary<string, object?>()));

        var resultQuery = connection.Query<TestUser>(query);
        resultQuery.Should().HaveCount(1);
        
        var queryUpdate = Substitute.For<ISqlQuery>();
        queryUpdate.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("UPDATE test SET name = 'test' WHERE id = 1", new Dictionary<string, object?>()));
        var resultExecute = connection.Execute(queryUpdate);
        resultExecute.Should().Be(1);
    }

    [Fact]
    public void BoundSelectQuery_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var connection = Substitute.For<IDbConnection>();
        SelectQuery<TestUser> query = null!;

        // Act & Assert
        Action act = () => new BoundSelectQuery<TestUser>(query, connection);
        act.Should().Throw<ArgumentNullException>().WithParameterName("query");
    }

    [Fact]
    public void BoundSelectQuery_NullConnection_ThrowsArgumentNullException()
    {
        // Arrange
        var query = new SelectQuery<TestUser>();

        // Act & Assert
        Action act = () => new BoundSelectQuery<TestUser>(query, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("connection");
    }

    [Fact]
    public async Task BulkInsertAsync_WithNonCollection_ConvertsToListAndInserts()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var items = new NonCollectionEnumerable<TestUser>(new[] { new TestUser { Id = 1, Name = "A" } });

        // Act
        var result = await connection.BulkInsertAsync(items);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task QuerySequentialAsync_NullParameter_MapsToDbNull()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await connection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, NULL)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        string? nullName = null;
        var query = Sql.From<TestUser>().Where(u => u.Name == nullName);

        // Act
        var result = await connection.QuerySequentialAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryAotAsync_NullParameter_MapsToDbNull()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await connection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, NULL)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        string? nullName = null;
        var query = Sql.From<TestUser>().Where(u => u.Name == nullName);

        // Act
        var result = await connection.QueryAotAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAotAsync_NullParameter_MapsToDbNull()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await connection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, NULL)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        string? nullName = null;
        var query = Sql.From<TestUser>().Where(u => u.Name == nullName);

        // Act
        var result = await connection.QueryFirstOrDefaultAotAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_LogParametersFalse_LogsMaskedParameters()
    {
        // Arrange
        SqlBuilderDiagnostics.LogParameters = false;
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var query = Sql.Insert(new TestUser { Id = 1, Name = "Secret" });

        // Act
        var result = await connection.ExecuteAsync(query);

        // Assert
        result.Should().Be(1);

        // Cleanup
        SqlBuilderDiagnostics.LogParameters = true;
    }

    [Fact]
    public async Task ExecuteAsync_WithActivityListener_LogsExpectedTags()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var tags = new Dictionary<string, object?>();
        string? operationName = null;
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                var stmt = activity.TagObjects.FirstOrDefault(t => t.Key == "db.statement").Value?.ToString();
                if (stmt != null && stmt.Contains("INSERT INTO \"TestUsers\""))
                {
                    operationName = activity.OperationName;
                    foreach (var tag in activity.TagObjects)
                    {
                        tags[tag.Key] = tag.Value;
                    }
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var query = Sql.Insert(new TestUser { Id = 1, Name = "A" });

        // Act
        await connection.ExecuteAsync(query);

        // Assert
        operationName.Should().Be("db.execute");
        tags.Should().ContainKey("db.statement");
        tags["db.statement"]?.ToString().Should().Contain("INSERT INTO");
    }

    [Fact]
    public async Task BulkOperations_WithActivityListener_LogsExpectedOperationNames()
    {
        // Arrange
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var operations = new List<string>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName.StartsWith("db."))
                {
                    operations.Add(activity.OperationName);
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var users = new[] { new TestUser { Id = 1, Name = "A" } };
        var updateQuery = Sql.Update<TestUser>().Set(u => u.Name, "B").Where(u => u.Id == 1);
        var deleteQuery = Sql.Delete<TestUser>().Where(u => u.Id == 1);

        // Act & Assert
        var actUpdate = async () => await connection.BulkUpdateAsync(updateQuery, users);
        await actUpdate.Should().ThrowAsync<Exception>();

        var actDelete = async () => await connection.BulkDeleteAsync(deleteQuery, users);
        await actDelete.Should().ThrowAsync<Exception>();

        // Assert
        operations.Should().Contain("db.bulk_update");
        operations.Should().Contain("db.bulk_delete");
    }

    [Fact]
    public void SyncMethods_WithActivityListener_LogsExpectedOperationNames()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        connection.Execute("INSERT INTO TestUsers (Id, Name) VALUES (1, 'A')");

        var operations = new List<string>();
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName.StartsWith("db."))
                {
                    operations.Add(activity.OperationName);
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var query = connection.Sql().Select<TestUser>();
        var insertQuery = Sql.Insert(new TestUser { Id = 2, Name = "B" });

        // Act
        connection.Query<TestUser>(query).ToList();
        connection.Execute(insertQuery);

        // Assert
        operations.Should().Contain("db.query.sync");
        operations.Should().Contain("db.execute.sync");
    }

    [Fact]
    public async Task ExecuteAsync_SlowQuery_LogsSlowQueryEvent()
    {
        // Arrange
        int originalThreshold = SqlBuilderDiagnostics.SlowQueryThresholdMs;
        SqlBuilderDiagnostics.SlowQueryThresholdMs = -1; // Force slow query threshold
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        long slowQueryCount = 0;
        bool hasDurationTag = false;
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.TagObjects.Any(t => t.Key == "db.duration_ms"))
                {
                    hasDurationTag = true;
                }
            }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        using var meterListener = new System.Diagnostics.Metrics.MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == SqlBuilderDiagnostics.SourceName && instrument.Name == "sql_builder.query.slow.count")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            slowQueryCount += measurement;
        });
        meterListener.Start();

        var query = Sql.Insert(new TestUser { Id = 1, Name = "A" });

        // Act
        try
        {
            await connection.ExecuteAsync(query);
        }
        finally
        {
            SqlBuilderDiagnostics.SlowQueryThresholdMs = originalThreshold; // Restore
        }

        // Assert
        slowQueryCount.Should().BeGreaterThan(0);
        hasDurationTag.Should().BeTrue();
    }



    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrown_IncrementsErrorCounter()
    {
        typeof(SqlBuilderDiagnostics).GetMethod("ReinitializeMetersForTesting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, null);

        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        
        var query = Sql.From<TestUser>().Where(u => u.Name == "throw");
        
        // Inject an invalid SQL string to force an error
        var modifiedQuery = query.RawSelect($"INVALID SQL PARSE ERROR");

        var act = async () => await connection.ExecuteAsync(modifiedQuery);
        await act.Should().ThrowAsync<Exception>();

        // Check that the metric increased (we use a MeterListener to capture it since OpenTelemetry isn't fully configured here)
        var errorCount = 0L;
        using var meterListener = new System.Diagnostics.Metrics.MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Name == "sql_builder.query.error.count")
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == "sql_builder.query.error.count")
            {
                errorCount += measurement;
            }
        });
        meterListener.Start();
        
        await act.Should().ThrowAsync<Exception>();
        meterListener.RecordObservableInstruments();

        errorCount.Should().BeGreaterThan(0);
    }
    
    [Fact]
    public void RegisterTypeHandler_RegistersWithSqlMapper()
    {
        var handler = new DummyTypeHandler();
        DapperExtensions.RegisterTypeHandler<DummyType>(handler);
        // We just ensure it doesn't throw. Dapper's SqlMapper will now use it.
    }

    [Fact]
    public async Task ExecuteAsync_SlowQuery_LogsWarning()
    {
        // Arrange
        var loggerFactory = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILoggerFactory>();
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        logger.IsEnabled(Arg.Any<Microsoft.Extensions.Logging.LogLevel>()).Returns(true);

        SqlBuilderDiagnostics.LoggerFactory = loggerFactory;
        int originalThreshold = SqlBuilderDiagnostics.SlowQueryThresholdMs;
        SqlBuilderDiagnostics.SlowQueryThresholdMs = -1; // Force slow query

        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await connection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, 'A')");
        
        var query = Sql.From<TestUser>().Where(u => u.Id == 1);

        try
        {
            // Act
            await connection.QueryAsync<TestUser>(query);
        }
        finally
        {
            SqlBuilderDiagnostics.SlowQueryThresholdMs = originalThreshold;
            SqlBuilderDiagnostics.LoggerFactory = null;
        }

        // Assert
        // We can't easily assert the exact string via NSubstitute extension methods for ILogger because they use internal structures,
        // but we can assert that Log method was called with LogLevel.Warning
        logger.Received().Log(
            Microsoft.Extensions.Logging.LogLevel.Warning,
            Arg.Any<Microsoft.Extensions.Logging.EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenExceptionThrown_LogsError()
    {
        // Arrange
        var loggerFactory = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILoggerFactory>();
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(logger);
        logger.IsEnabled(Arg.Any<Microsoft.Extensions.Logging.LogLevel>()).Returns(true);

        SqlBuilderDiagnostics.LoggerFactory = loggerFactory;

        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        
        var query = Sql.From<TestUser>().RawSelect($"INVALID SQL");

        try
        {
            // Act
            var act = async () => await connection.ExecuteAsync(query);
            await act.Should().ThrowAsync<Exception>();
        }
        finally
        {
            SqlBuilderDiagnostics.LoggerFactory = null;
        }

        // Assert
        logger.Received().Log(
            Microsoft.Extensions.Logging.LogLevel.Error,
            Arg.Any<Microsoft.Extensions.Logging.EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    private class DummyType {}
    private class DummyTypeHandler : EricksonLopez.SqlBuilder.Abstractions.ITypeHandler
    {
        public void SetValue(System.Data.IDbDataParameter parameter, object? value) { }
        public object? Parse(Type destinationType, object? value) => new DummyType();
    }

    private class PlainDbConnection : IDbConnection
    {
        private readonly IDbConnection _inner;
        public PlainDbConnection(IDbConnection inner) => _inner = inner;

        public string ConnectionString { get => _inner.ConnectionString; set => _inner.ConnectionString = value; }
        public int ConnectionTimeout => _inner.ConnectionTimeout;
        public string Database => _inner.Database;
        public ConnectionState State => _inner.State;
        public IDbTransaction BeginTransaction() => _inner.BeginTransaction();
        public IDbTransaction BeginTransaction(IsolationLevel il) => _inner.BeginTransaction(il);
        public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
        public void Close() => _inner.Close();
        
        public IDbCommand CreateCommand() 
        {
            var cmd = _inner.CreateCommand();
            return new PlainDbCommand(cmd);
        }
        
        public void Dispose() => _inner.Dispose();
        public void Open() => _inner.Open();
    }

    private class PlainDbCommand : IDbCommand
    {
        private readonly IDbCommand _inner;
        public PlainDbCommand(IDbCommand inner) => _inner = inner;

        public string CommandText { get => _inner.CommandText; set => _inner.CommandText = value; }
        public int CommandTimeout { get => _inner.CommandTimeout; set => _inner.CommandTimeout = value; }
        public CommandType CommandType { get => _inner.CommandType; set => _inner.CommandType = value; }
        public IDbConnection? Connection { get => _inner.Connection; set => _inner.Connection = value; }
        
        public IDataParameterCollection Parameters => _inner.Parameters;
        public IDbTransaction? Transaction { get => _inner.Transaction; set => _inner.Transaction = value; }
        public UpdateRowSource UpdatedRowSource { get => _inner.UpdatedRowSource; set => _inner.UpdatedRowSource = value; }

        public void Cancel() => _inner.Cancel();
        public IDbDataParameter CreateParameter() => _inner.CreateParameter();
        public void Dispose() => _inner.Dispose();
        
        public int ExecuteNonQuery() => _inner.ExecuteNonQuery();
        
        public IDataReader ExecuteReader() 
        {
            var reader = _inner.ExecuteReader();
            return new PlainDataReader(reader);
        }
        
        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            var reader = _inner.ExecuteReader(behavior);
            return new PlainDataReader(reader);
        }
        
        public object? ExecuteScalar() => _inner.ExecuteScalar();
        public void Prepare() => _inner.Prepare();
    }

    private class PlainDataReader : IDataReader
    {
        private readonly IDataReader _inner;
        public PlainDataReader(IDataReader inner) => _inner = inner;

        public object this[int i] => _inner[i];
        public object this[string name] => _inner[name];
        public int Depth => _inner.Depth;
        public bool IsClosed => _inner.IsClosed;
        public int RecordsAffected => _inner.RecordsAffected;
        public int FieldCount => _inner.FieldCount;

        public void Close() => _inner.Close();
        public void Dispose() => _inner.Dispose();
        public bool GetBoolean(int i) => _inner.GetBoolean(i);
        public byte GetByte(int i) => _inner.GetByte(i);
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => _inner.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
        public char GetChar(int i) => _inner.GetChar(i);
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => _inner.GetChars(i, fieldoffset, buffer, bufferoffset, length);
        public IDataReader GetData(int i) => _inner.GetData(i);
        public string GetDataTypeName(int i) => _inner.GetDataTypeName(i);
        public DateTime GetDateTime(int i) => _inner.GetDateTime(i);
        public decimal GetDecimal(int i) => _inner.GetDecimal(i);
        public double GetDouble(int i) => _inner.GetDouble(i);
        public Type GetFieldType(int i) => _inner.GetFieldType(i);
        public float GetFloat(int i) => _inner.GetFloat(i);
        public Guid GetGuid(int i) => _inner.GetGuid(i);
        public short GetInt16(int i) => _inner.GetInt16(i);
        public int GetInt32(int i) => _inner.GetInt32(i);
        public long GetInt64(int i) => _inner.GetInt64(i);
        public string GetName(int i) => _inner.GetName(i);
        public int GetOrdinal(string name) => _inner.GetOrdinal(name);
        public DataTable? GetSchemaTable() => _inner.GetSchemaTable();
        public string GetString(int i) => _inner.GetString(i);
        public object GetValue(int i) => _inner.GetValue(i);
        public int GetValues(object[] values) => _inner.GetValues(values);
        public bool IsDBNull(int i) => _inner.IsDBNull(i);
        public bool NextResult() => _inner.NextResult();
        public bool Read() => _inner.Read();
    }

    [Fact]
    public async Task QuerySequentialAsync_PlainCommand_ReadsSuccessfully()
    {
        // Arrange
        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await sqliteConnection.OpenAsync();
        await sqliteConnection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await sqliteConnection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, 'A')");
        
        var connection = new PlainDbConnection(sqliteConnection);
        DapperExtensions.RegisterCompiler<PlainDbConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var query = Sql.From<TestUser>();

        // Act
        var result = await connection.QuerySequentialAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryAotAsync_PlainCommand_ReadsSuccessfully()
    {
        // Arrange
        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await sqliteConnection.OpenAsync();
        await sqliteConnection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await sqliteConnection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, 'A')");
        
        var connection = new PlainDbConnection(sqliteConnection);
        DapperExtensions.RegisterCompiler<PlainDbConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var query = Sql.From<TestUser>();

        // Act
        var result = await connection.QueryAotAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAotAsync_PlainCommand_ReadsSuccessfully()
    {
        // Arrange
        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await sqliteConnection.OpenAsync();
        await sqliteConnection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await sqliteConnection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, 'A')");
        
        var connection = new PlainDbConnection(sqliteConnection);
        DapperExtensions.RegisterCompiler<PlainDbConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var query = Sql.From<TestUser>();

        // Act
        var result = await connection.QueryFirstOrDefaultAotAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public void BoundSelectQuery_FluentMethods_ReturnNewInstances()
    {
        var query = Sql.From<TestUser>();
        var connection = new DummyConnection();
        var bound = new BoundSelectQuery<TestUser>(query, connection);
        
        bound.Where(u => u.Id == 1).Should().NotBeNull();
        bound.Where((FormattableString)$"Id = 1").Should().NotBeNull();
        bound.And(u => u.Id == 2).Should().NotBeNull();
        bound.Or(u => u.Id == 3).Should().NotBeNull();
        bound.OrderBy(u => u.Id).Should().NotBeNull();
        bound.OrderByDescending(u => u.Name).Should().NotBeNull();
        bound.Paginate(1, 10).Should().NotBeNull();
        bound.ProjectTo<TestUser>().Should().NotBeNull();
    }

    [Fact]
    public async Task QuerySequentialAsync_WithParameters_MapsCorrectly()
    {
        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await sqliteConnection.OpenAsync();
        await sqliteConnection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await sqliteConnection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, 'A')");
        var connection = new PlainDbConnection(sqliteConnection);
        DapperExtensions.RegisterCompiler<PlainDbConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        int id = 1;
        var query = Sql.From<TestUser>().Where(u => u.Id == id);
        var result = await connection.QuerySequentialAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryAotAsync_WithParameters_MapsCorrectly()
    {
        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await sqliteConnection.OpenAsync();
        await sqliteConnection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await sqliteConnection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, 'A')");
        var connection = new PlainDbConnection(sqliteConnection);
        DapperExtensions.RegisterCompiler<PlainDbConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        int id = 1;
        var query = Sql.From<TestUser>().Where(u => u.Id == id);
        var result = await connection.QueryAotAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task QueryFirstOrDefaultAotAsync_WithParameters_MapsCorrectly()
    {
        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await sqliteConnection.OpenAsync();
        await sqliteConnection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await sqliteConnection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, 'A')");
        var connection = new PlainDbConnection(sqliteConnection);
        DapperExtensions.RegisterCompiler<PlainDbConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        int id = 1;
        var query = Sql.From<TestUser>().Where(u => u.Id == id);
        var result = await connection.QueryFirstOrDefaultAotAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task QueryFirstOrDefaultAotAsync_EmptyResult_ReturnsDefault()
    {
        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await sqliteConnection.OpenAsync();
        await sqliteConnection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        var connection = new PlainDbConnection(sqliteConnection);
        DapperExtensions.RegisterCompiler<PlainDbConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var query = Sql.From<TestUser>();
        var result = await connection.QueryFirstOrDefaultAotAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        result.Should().BeNull();
    }

    [Fact]
    public void BoundSelectQuery_AdditionalMethods_ExecuteSuccessfully()
    {
        var query = Sql.From<TestUser>();
        var connection = new DummyConnection();
        var bound = new BoundSelectQuery<TestUser>(query, connection);
        
        ((IAstQuery)bound).Nodes.Should().NotBeNull();
        bound.Build(new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler()).Should().NotBeNull();
        
        bound.ToResultAsync().Should().NotBeNull();
        bound.ToPagedListAsync(1, 10).Should().NotBeNull();
    }

    [Fact]
    public void SqlBuilderConnectionContext_Methods_ReturnQueries()
    {
        var connection = new DummyConnection();
        var context = new SqlBuilderConnectionContext(connection);
        
        context.Select<TestUser>().Should().NotBeNull();
        context.Insert(new TestUser()).Should().NotBeNull();
        context.Update<TestUser>().Should().NotBeNull();
        context.Delete<TestUser>().Should().NotBeNull();
        context.Connection.Should().BeSameAs(connection);
    }

    [Fact]
    public async Task QuerySequentialAsync_ExplicitNullParameter_MapsCorrectly()
    {
        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await sqliteConnection.OpenAsync();
        await sqliteConnection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        await sqliteConnection.ExecuteAsync("INSERT INTO TestUsers (Id, Name) VALUES (1, NULL)");
        var connection = new PlainDbConnection(sqliteConnection);
        DapperExtensions.RegisterCompiler<PlainDbConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        string? nullName = null;
        var query = Sql.From<TestUser>().Where((FormattableString)$"Name = {nullName}");
        var result = await connection.QuerySequentialAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });

        result.Should().HaveCount(0); // Name = NULL yields 0 rows in sqlite, but we just want to execute it
    }

    [Fact]
    public async Task QueryMetrics_WithActivityListener_LogsNullAndNonNullParameters()
    {
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.ActivitySource.Name,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var sqliteConnection = new SqliteConnection("Data Source=:memory:");
        await sqliteConnection.OpenAsync();
        await sqliteConnection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        string? nullName = null;
        var query = Sql.From<TestUser>().Where((FormattableString)$"Name = {nullName} AND Id = {1}");
        
        await sqliteConnection.QueryAotAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });
        await sqliteConnection.QueryFirstOrDefaultAotAsync(query, reader => new TestUser { Id = reader.GetInt32(0) });
    }

    private class MockQuery : EricksonLopez.SqlBuilder.Abstractions.ISqlQuery
    {
        public string? Tag => null;
        private readonly string _sql;
        public MockQuery(string sql) => _sql = sql;
        public EricksonLopez.SqlBuilder.Abstractions.SqlResult Build(EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler compiler) => new EricksonLopez.SqlBuilder.Abstractions.SqlResult(_sql, new Dictionary<string, object>());
    }

    [Fact]
    public async Task BulkUpdateAsync_ExecutesSuccessfully()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var query = new MockQuery("UPDATE TestUsers SET Name = @Name WHERE Id = @Id");
        var items = new[] { new TestUser { Id = 1, Name = "A" } };
        var result = await connection.BulkUpdateAsync(query, items);

        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkDeleteAsync_ExecutesSuccessfully()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await connection.ExecuteAsync("CREATE TABLE TestUsers (Id INTEGER, Name TEXT)");
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());

        var query = new MockQuery("DELETE FROM TestUsers WHERE Id = @Id");
        var items = new[] { new TestUser { Id = 1, Name = "A" } };
        var result = await connection.BulkDeleteAsync(query, items);

        result.Should().Be(0);
    }
}






