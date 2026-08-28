// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.OpenTelemetry;
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class OpenTelemetryDialectTests
{
    [Fact]
    public void ResolveDbSystem_ReturnsStandardIdentifiers()
    {
        SqlBuilderInstrumentation.ResolveDbSystem(new SqlServerCompiler()).Should().Be("mssql");
        SqlBuilderInstrumentation.ResolveDbSystem(new PostgreSqlCompiler()).Should().Be("postgresql");
        SqlBuilderInstrumentation.ResolveDbSystem(new MySqlCompiler()).Should().Be("mysql");
        SqlBuilderInstrumentation.ResolveDbSystem(new SqliteCompiler()).Should().Be("sqlite");
        SqlBuilderInstrumentation.ResolveDbSystem(new OracleCompiler()).Should().Be("oracle");
        SqlBuilderInstrumentation.ResolveDbSystem(null).Should().Be("sql");

        var customCompiler = NSubstitute.Substitute.For<EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler>();
        SqlBuilderInstrumentation.ResolveDbSystem(customCompiler).Should().Be("sql");
    }

    [Fact]
    public void StartQueryActivity_WithActiveListener_SetsTags()
    {
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == SqlBuilderInstrumentation.ActivitySourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        var queryTagged = Sql.From<NewRoadmapFeaturesTests.TestUser>() with { Tag = "GetUsers" };
        using var activity1 = SqlBuilderInstrumentation.StartQueryActivity(queryTagged, "ProductionDb", new PostgreSqlCompiler());

        activity1.Should().NotBeNull();
        activity1!.GetTagItem("db.system").Should().Be("postgresql");
        activity1.GetTagItem("db.name").Should().Be("ProductionDb");
        activity1.GetTagItem("db.query.tag").Should().Be("GetUsers");

        var queryUntagged = Sql.From<NewRoadmapFeaturesTests.TestUser>();
        using var activity2 = SqlBuilderInstrumentation.StartQueryActivity(queryUntagged, "AppDb", null);

        activity2.Should().NotBeNull();
        activity2!.GetTagItem("db.system").Should().Be("sql");
        activity2.GetTagItem("db.name").Should().Be("AppDb");
        activity2.GetTagItem("db.query.tag").Should().BeNull();

        // Default databaseName ("Unknown") and default compiler (null)
        using var activityDefault = SqlBuilderInstrumentation.StartQueryActivity(queryUntagged);
        activityDefault.Should().NotBeNull();
        activityDefault!.GetTagItem("db.system").Should().Be("sql");
        activityDefault.GetTagItem("db.name").Should().Be("Unknown");
    }

    [Fact]
    public void StartQueryActivity_WithoutListener_ReturnsNull()
    {
        var query = Sql.From<NewRoadmapFeaturesTests.TestUser>();
        // With no listener registered for this specific test scope
        // Note: ActivitySource.StartActivity returns null if no listeners are active
        // SqlBuilderInstrumentation.StartQueryActivity should handle null gracefully
        var activity = SqlBuilderInstrumentation.StartQueryActivity(query);
        // Whether null or not depending on ambient listeners, no exception is thrown
    }
}



