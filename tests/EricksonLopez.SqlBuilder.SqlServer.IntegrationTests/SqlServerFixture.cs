// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Dapper;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.SqlServer.IntegrationTests;

public class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _MsSqlContainer;

    public SqlServerFixture()
    {
        DapperExtensions.RegisterCompiler<SqlConnection>(() => new SqlServerCompiler());
        _MsSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
    }

    public string ConnectionString => _MsSqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _MsSqlContainer.StartAsync();

        using var connection = CreateConnection();
        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INT IDENTITY(1,1) PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                age INT NOT NULL,
                is_active BIT NOT NULL DEFAULT 1,
                created_at DATETIME2 NOT NULL DEFAULT SYSDATETIME()
            );
        ");
    }

    public Task DisposeAsync()
    {
        return _MsSqlContainer.DisposeAsync().AsTask();
    }

    public IDbConnection CreateConnection()
    {
        var conn = new SqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}
