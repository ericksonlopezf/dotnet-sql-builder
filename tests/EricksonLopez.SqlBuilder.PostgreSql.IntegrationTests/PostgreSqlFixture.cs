// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Dapper;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.IntegrationTests;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer;

    public PostgreSqlFixture()
    {
        DapperExtensions.RegisterCompiler<NpgsqlConnection>(() => new PostgreSqlCompiler());
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15-alpine")
            .Build();
    }

    public string ConnectionString => _postgreSqlContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();

        using var connection = CreateConnection();
        await connection.ExecuteAsync(@"
            CREATE TABLE users (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                age INT NOT NULL,
                is_active BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMP NOT NULL DEFAULT NOW()
            );
        ");
    }

    public Task DisposeAsync()
    {
        return _postgreSqlContainer.DisposeAsync().AsTask();
    }

    public IDbConnection CreateConnection()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}
