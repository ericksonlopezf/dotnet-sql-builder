// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Dapper;
using MySqlConnector;
using Testcontainers.MySql;
using Xunit;

namespace EricksonLopez.SqlBuilder.MySql.IntegrationTests;

public class MySqlFixture : IAsyncLifetime
{
    private readonly MySqlContainer _MySqlContainer;

    public MySqlFixture()
    {
        DapperExtensions.RegisterCompiler<MySqlConnection>(() => new MySqlCompiler());
        _MySqlContainer = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .Build();
    }

    public string ConnectionString => _MySqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _MySqlContainer.StartAsync();

        using var connection = CreateConnection();
        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id INT AUTO_INCREMENT PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                age INT NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            );
        ");
    }

    public Task DisposeAsync()
    {
        return _MySqlContainer.DisposeAsync().AsTask();
    }

    public IDbConnection CreateConnection()
    {
        var conn = new MySqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}
