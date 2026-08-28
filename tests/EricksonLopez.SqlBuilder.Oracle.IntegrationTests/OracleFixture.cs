// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Dapper;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;
using Xunit;

namespace EricksonLopez.SqlBuilder.Oracle.IntegrationTests;

public class OracleFixture : IAsyncLifetime
{
    private readonly OracleContainer _OracleContainer;

    public OracleFixture()
    {
        DapperExtensions.RegisterCompiler<OracleConnection>(() => new OracleCompiler());
        _OracleContainer = new OracleBuilder()
            .Build();
    }

    public string ConnectionString => _OracleContainer.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _OracleContainer.StartAsync();

        using var connection = CreateConnection();
        await connection.ExecuteAsync(@"
            CREATE TABLE ""TEST_USERS"" (
                ""ID"" NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                ""NAME"" VARCHAR2(100) NOT NULL,
                ""AGE"" INT NOT NULL,
                ""IS_ACTIVE"" NUMBER(1) DEFAULT 1 NOT NULL,
                ""CREATED_AT"" TIMESTAMP DEFAULT SYSTIMESTAMP NOT NULL
            )
        ");
    }

    public Task DisposeAsync()
    {
        return _OracleContainer.DisposeAsync().AsTask();
    }

    public IDbConnection CreateConnection()
    {
        var conn = new OracleConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}
