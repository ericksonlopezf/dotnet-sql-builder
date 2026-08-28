// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.Tests;

public class ObservabilityExtraTests : IDisposable
{
    public ObservabilityExtraTests()
    {
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new EricksonLopez.SqlBuilder.Sqlite.SqliteCompiler());
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        SqlBuilderDiagnostics.LoggerFactory = null;
    }

    [Fact]
    public async Task QueryMetrics_Error_LogsError_WhenLoggerFactoryIsConfigured()
    {
        var factory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        factory.CreateLogger("EricksonLopez.SqlBuilder.Dapper").Returns(logger);
        SqlBuilderDiagnostics.LoggerFactory = factory;

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT * FROM invalid_table", new Dictionary<string, object?>()));

        await Assert.ThrowsAnyAsync<Exception>(() => connection.QueryAsync<dynamic>(query));

        // It should log an error since _logger is not null
        logger.ReceivedWithAnyArgs().Log(LogLevel.Error, default, default, default, default!);
    }

    [Fact]
    public async Task QueryMetrics_SlowQuery_LogsWarning_WhenLoggerFactoryIsConfigured()
    {
        var factory = Substitute.For<ILoggerFactory>();
        var logger = Substitute.For<ILogger>();
        factory.CreateLogger("EricksonLopez.SqlBuilder.Dapper").Returns(logger);
        SqlBuilderDiagnostics.LoggerFactory = factory;
        SqlBuilderDiagnostics.SlowQueryThresholdMs = 0; // Force slow query

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var query = Substitute.For<ISqlQuery>();
        query.Build(Arg.Any<ISqlCompiler>()).Returns(new SqlResult("SELECT 1", new Dictionary<string, object?>()));

        await connection.QueryAsync<dynamic>(query);

        // It should log a warning since _logger is not null and it was a "slow query"
        logger.ReceivedWithAnyArgs().Log(LogLevel.Warning, default, default, default, default!);
        
        SqlBuilderDiagnostics.SlowQueryThresholdMs = 500; // Reset
    }
}




