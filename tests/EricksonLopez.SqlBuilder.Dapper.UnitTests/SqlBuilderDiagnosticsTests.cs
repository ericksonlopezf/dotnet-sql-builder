// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.Tests;

public class SqlBuilderDiagnosticsTests
{
    [Fact]
    public void Constants_HaveExpectedValues()
    {
        typeof(SqlBuilderDiagnostics).GetMethod("ReinitializeMetersForTesting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, null);
        
        SqlBuilderDiagnostics.SourceName.Should().Be("EricksonLopez.SqlBuilder");
        
        SqlBuilderDiagnostics.ActivitySource.Name.Should().Be("EricksonLopez.SqlBuilder");
        var expectedVersion = typeof(SqlBuilderDiagnostics).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        SqlBuilderDiagnostics.ActivitySource.Version.Should().Be(expectedVersion);
        
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
        
        SqlBuilderDiagnostics.SlowQueryThresholdMs.Should().Be(500);
        
        var defaultLogParams = SqlBuilderDiagnostics.LogParameters;
        try
        {
            SqlBuilderDiagnostics.LogParameters = true;
            SqlBuilderDiagnostics.LogParameters.Should().BeTrue();
        }
        finally
        {
            SqlBuilderDiagnostics.LogParameters = defaultLogParams;
        }
    }
}


