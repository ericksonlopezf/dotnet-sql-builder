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

namespace EricksonLopez.SqlBuilder.MariaDb.IntegrationTests;

/// <summary>
/// Test fixture that spins up a MariaDB container via Testcontainers.
/// </summary>
/// <remarks>
/// Uses the <c>Testcontainers.MySql</c> package with a <c>mariadb:10.11</c> image.
/// <c>MySqlConnector</c> is wire-protocol-compatible with MariaDB, so no separate
/// driver is required.
/// </remarks>
public class MariaDbFixture : IAsyncLifetime
{
    private readonly MySqlContainer _mariaDbContainer;

    public MariaDbFixture()
    {
        DapperExtensions.RegisterCompiler<MySqlConnection>(() => new MariaDbCompiler());
        _mariaDbContainer = new MySqlBuilder()
            .WithImage("mariadb:10.11")
            .Build();
    }

    public string ConnectionString => _mariaDbContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _mariaDbContainer.StartAsync();

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
        return _mariaDbContainer.DisposeAsync().AsTask();
    }

    public IDbConnection CreateConnection()
    {
        var conn = new MySqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}
