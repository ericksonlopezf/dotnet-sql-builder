// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing.Domain;
using EricksonLopez.SqlBuilder.UnitTests.Infrastructure;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

[Collection("SqlBuilderDiagnosticsCollection")]
public class SqlBuilderDiagnosticsTests
{
    private readonly SqlBuilderDiagnosticsFixture _fixture;

    public SqlBuilderDiagnosticsTests(SqlBuilderDiagnosticsFixture fixture)
    {
        _fixture = fixture;
    }

    private Activity AssertActivityEmitted(ISqlQuery query, string expectedQueryType)
    {
        var activities = new List<Activity>();
        using var _ = _fixture.CaptureActivities(activity => activities.Add(activity));

        var compiler = new PostgreSqlCompiler();
        var result = compiler.Compile(query);

        var activity = activities.Last(a => (string?)a.GetTagItem("db.statement") == result.Sql);
        
        activity.OperationName.Should().Be("SqlCompiler.Compile");
        activity.GetTagItem("sqlbuilder.query_type").Should().Be(expectedQueryType);
        
        return activity;
    }

    [Fact]
    public void ActivitySource_HasCorrectVersionAndName()
    {
        SqlBuilderDiagnostics.ActivitySource.Name.Should().Be("EricksonLopez.SqlBuilder");
        SqlBuilderDiagnostics.ActivitySource.Version.Should().Be("1.0.0.0");
    }

    [Fact]
    public void Properties_SetAndGet_FunctionCorrectly()
    {
        var prevLog = SqlBuilderDiagnostics.LogParameters;
        var prevThreshold = SqlBuilderDiagnostics.SlowQueryThresholdMs;
        var prevLogger = SqlBuilderDiagnostics.LoggerFactory;

        try
        {
            SqlBuilderDiagnostics.LogParameters = true;
            SqlBuilderDiagnostics.LogParameters.Should().BeTrue();

            SqlBuilderDiagnostics.SlowQueryThresholdMs = 1200;
            SqlBuilderDiagnostics.SlowQueryThresholdMs.Should().Be(1200);

            var mockFactory = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILoggerFactory>();
            SqlBuilderDiagnostics.LoggerFactory = mockFactory;
            SqlBuilderDiagnostics.LoggerFactory.Should().BeSameAs(mockFactory);

            SqlBuilderDiagnostics.Meter.Name.Should().Be("EricksonLopez.SqlBuilder");
            SqlBuilderDiagnostics.Meter.Version.Should().Be("1.0.0");
            SqlBuilderDiagnostics.QueryExecutionCounter.Name.Should().Be("sql_builder.query.count");
            SqlBuilderDiagnostics.QueryExecutionCounter.Unit.Should().Be("queries");
            SqlBuilderDiagnostics.QueryExecutionCounter.Description.Should().Be("Total number of SQL queries executed.");

            SqlBuilderDiagnostics.QueryDurationHistogram.Name.Should().Be("sql_builder.query.duration");
            SqlBuilderDiagnostics.QueryDurationHistogram.Unit.Should().Be("ms");
            SqlBuilderDiagnostics.QueryDurationHistogram.Description.Should().Be("Duration of SQL queries in milliseconds.");

            SqlBuilderDiagnostics.SlowQueryCounter.Name.Should().Be("sql_builder.query.slow.count");
            SqlBuilderDiagnostics.SlowQueryCounter.Unit.Should().Be("queries");
            SqlBuilderDiagnostics.SlowQueryCounter.Description.Should().Be("Total number of slow SQL queries executed.");

            SqlBuilderDiagnostics.ErrorQueryCounter.Name.Should().Be("sql_builder.query.error.count");
            SqlBuilderDiagnostics.ErrorQueryCounter.Unit.Should().Be("errors");
            SqlBuilderDiagnostics.ErrorQueryCounter.Description.Should().Be("Total number of SQL query execution errors.");

            SqlBuilderDiagnostics.ReinitializeMetersForTesting();
            SqlBuilderDiagnostics.Meter.Should().NotBeNull();
            SqlBuilderDiagnostics.Meter.Name.Should().Be("EricksonLopez.SqlBuilder");
            SqlBuilderDiagnostics.Meter.Version.Should().Be("1.0.0");
            SqlBuilderDiagnostics.QueryExecutionCounter.Name.Should().Be("sql_builder.query.count");
            SqlBuilderDiagnostics.QueryExecutionCounter.Unit.Should().Be("queries");
            SqlBuilderDiagnostics.QueryExecutionCounter.Description.Should().Be("Total number of SQL queries executed.");

            SqlBuilderDiagnostics.QueryDurationHistogram.Name.Should().Be("sql_builder.query.duration");
            SqlBuilderDiagnostics.QueryDurationHistogram.Unit.Should().Be("ms");
            SqlBuilderDiagnostics.QueryDurationHistogram.Description.Should().Be("Duration of SQL queries in milliseconds.");

            SqlBuilderDiagnostics.SlowQueryCounter.Name.Should().Be("sql_builder.query.slow.count");
            SqlBuilderDiagnostics.SlowQueryCounter.Unit.Should().Be("queries");
            SqlBuilderDiagnostics.SlowQueryCounter.Description.Should().Be("Total number of slow SQL queries executed.");

            SqlBuilderDiagnostics.ErrorQueryCounter.Name.Should().Be("sql_builder.query.error.count");
            SqlBuilderDiagnostics.ErrorQueryCounter.Unit.Should().Be("errors");
            SqlBuilderDiagnostics.ErrorQueryCounter.Description.Should().Be("Total number of SQL query execution errors.");
        }
        finally
        {
            SqlBuilderDiagnostics.LogParameters = prevLog;
            SqlBuilderDiagnostics.SlowQueryThresholdMs = prevThreshold;
            SqlBuilderDiagnostics.LoggerFactory = prevLogger;
        }
    }

    [Fact]
    public void SqlCompiler_WhenCompiling_EmitsActivity()
    {
        // Arrange
        var query = Sql.From<User>().Where(u => u.Username == "Test");

        // Act & Assert
        var activity = AssertActivityEmitted(query, "SELECT");
        activity.GetTagItem("sqlbuilder.parameter_count").Should().Be(1);
    }

    [Fact]
    public void SqlCompiler_WhenCompilingInsert_EmitsActivityWithInsertType()
    {
        // Arrange
        var query = Sql.Insert(new User { Username = "Test" });

        // Act & Assert
        AssertActivityEmitted(query, "INSERT");
    }

    [Fact]
    public void SqlCompiler_WhenCompilingUpdate_EmitsActivityWithUpdateType()
    {
        // Arrange
        var query = Sql.Update<User>().Set(u => u.Username, "Test").WhereAll();

        // Act & Assert
        AssertActivityEmitted(query, "UPDATE");
    }

    [Fact]
    public void SqlCompiler_WhenCompilingDelete_EmitsActivityWithDeleteType()
    {
        // Arrange
        var query = Sql.Delete<User>().Where(u => u.Username == "Test");

        // Act & Assert
        AssertActivityEmitted(query, "DELETE");
    }

    [Fact]
    public void SqlCompiler_WhenCompilingRaw_EmitsActivityWithRawType()
    {
        // Arrange
        var query = Sql.Raw($"SELECT 1");

        // Act & Assert
        AssertActivityEmitted(query, "RAW");
    }
}
