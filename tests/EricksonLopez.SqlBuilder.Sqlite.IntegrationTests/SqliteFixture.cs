// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Dapper;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.SqlBuilder.Sqlite.IntegrationTests;

public class SqliteFixture : IAsyncLifetime
{
    private string _dbFile;
    private SqliteConnection _keepAliveConnection;

    public SqliteFixture()
    {
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());
        _dbFile = System.IO.Path.GetTempFileName();
        ConnectionString = $"Data Source={_dbFile}";
    }

    public string ConnectionString { get; }

    public Task InitializeAsync()
    {
        _keepAliveConnection = new SqliteConnection(ConnectionString);
        _keepAliveConnection.Open();

        using var connection = CreateConnection();
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                age INTEGER NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ");

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _keepAliveConnection?.Dispose();
        if (System.IO.File.Exists(_dbFile))
        {
            try { System.IO.File.Delete(_dbFile); } catch { }
        }
        return Task.CompletedTask;
    }

    public IDbConnection CreateConnection()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}
