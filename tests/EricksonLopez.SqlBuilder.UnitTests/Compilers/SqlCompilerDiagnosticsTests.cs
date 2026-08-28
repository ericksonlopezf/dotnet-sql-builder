// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Compilers;

/// <summary>
/// Verifies OpenTelemetry diagnostics, ActivitySource lifecycle, tags, and exception event capture.
/// </summary>
[Collection("SqlBuilderDiagnosticsCollection")]
public class SqlCompilerDiagnosticsTests
{
    private class DefaultCompiler : SqlCompilerBase
    {
        protected override ISqlRenderer AotRenderer => null!;
        public override string EscapeIdentifier(string identifier) => $"\"{identifier}\"";
    }

    private class ThrowingQuery : IAstQuery
    {
        public string? Tag => null;
        public IReadOnlyList<ISqlNode> Nodes => throw new InvalidOperationException("Compiler diagnostic test error");
        public SqlResult Build(ISqlCompiler compiler) => throw new NotImplementedException();
    }

    [Fact]
    public void ActivitySource_WhenInitialized_ShouldExposeVersion100()
    {
        SqlBuilderDiagnostics.ActivitySource.Version.Should().Be("1.0.0.0");
    }

    [Fact]
    public void Compile_WhenExceptionThrown_ShouldLogExceptionToActivity()
    {
        using var scope = DiagnosticActivityScope.Start();

        Action act = () => new DefaultCompiler().Compile(new ThrowingQuery());
        act.Should().Throw<Exception>();

        var errorActivity = scope.Activities.FirstOrDefault(a => a.OperationName == "SqlCompiler.Compile" && a.Status == ActivityStatusCode.Error);
        errorActivity.Should().NotBeNull("Activity with error status should be captured");
        errorActivity!.StatusDescription.Should().Be("Compiler diagnostic test error");

        var exEvent = errorActivity.Events.FirstOrDefault(e => e.Name == "Exception");
        exEvent.Name.Should().Be("Exception");
        exEvent.Tags.Should().Contain(t => t.Key == "exception.message" && (string)t.Value! == "Compiler diagnostic test error");
        exEvent.Tags.Should().Contain(t => t.Key == "exception.stacktrace" && t.Value != null);
    }

    [Fact]
    public void Compile_Insert_SetsQueryTypeTagInsert()
    {
        using var scope = DiagnosticActivityScope.Start();

        var q = new InsertQuery<DummyEntity>().Into("t").Values(new { Id = 1 });
        new DefaultCompiler().Compile(q);

        scope.Activities.Should().Contain(a => (string?)a.GetTagItem("sqlbuilder.query_type") == "INSERT");
    }

    [Fact]
    public void Compile_Update_SetsQueryTypeTagUpdate()
    {
        using var scope = DiagnosticActivityScope.Start();

        var q = new UpdateQuery<DummyEntity>().Update("t").Set(x => x.Name, "x").Where(x => x.Id == 1);
        new DefaultCompiler().Compile(q);

        scope.Activities.Should().Contain(a => (string?)a.GetTagItem("sqlbuilder.query_type") == "UPDATE");
    }

    [Fact]
    public void Compile_Delete_SetsQueryTypeTagDelete()
    {
        using var scope = DiagnosticActivityScope.Start();

        var q = new DeleteQuery<DummyEntity>().Delete("t").WhereAll();
        new DefaultCompiler().Compile(q);

        scope.Activities.Should().Contain(a => (string?)a.GetTagItem("sqlbuilder.query_type") == "DELETE");
    }

    [Fact]
    public void Compile_Select_SetsQueryTypeTagSelect()
    {
        using var scope = DiagnosticActivityScope.Start();

        var q = new SelectQuery<DummyEntity>().From("t");
        new DefaultCompiler().Compile(q);

        scope.Activities.Should().Contain(a => (string?)a.GetTagItem("sqlbuilder.query_type") == "SELECT");
    }

    [Fact]
    public void Compile_RawQuery_SetsQueryTypeTagRaw()
    {
        using var scope = DiagnosticActivityScope.Start();

        var q = new RawQuery("SELECT 1", null);
        new DefaultCompiler().Compile(q);

        scope.Activities.Should().Contain(a => (string?)a.GetTagItem("sqlbuilder.query_type") == "RAW");
    }

    [Fact]
    public void Compile_Select_SetsParameterCountTag()
    {
        using var scope = DiagnosticActivityScope.Start();

        var q = new SelectQuery<DummyEntity>().From("t").Where(x => x.Id == 7);
        new DefaultCompiler().Compile(q);

        scope.Activities.Should().Contain(a => a.GetTagItem("sqlbuilder.parameter_count") != null && ((int)a.GetTagItem("sqlbuilder.parameter_count")!) == 1);
    }

    [Fact]
    public void Compile_Subquery_ParameterCountTagIsZero()
    {
        using var scope = DiagnosticActivityScope.Start();

        var compiler = new DefaultCompiler();
        var pm = new ParameterManager();
        var q = new SelectQuery<DummyEntity>().From("t").Where(x => x.Id == 7);
        compiler.Compile(q, pm);

        scope.Activities.Should().Contain(a => a.GetTagItem("sqlbuilder.parameter_count") != null && ((int)a.GetTagItem("sqlbuilder.parameter_count")!) == 0);
    }

    [Fact]
    public void Compile_RootCall_ReturnsPopulatedParameters()
    {
        var q = new SelectQuery<DummyEntity>().From("t").Where(x => x.Id == 5);
        var result = new DefaultCompiler().Compile(q);
        result.Parameters.Should().NotBeEmpty("Root compile should return all parameters");
    }

    [Fact]
    public void Compile_SubqueryCall_ReturnsEmptyParameters()
    {
        var compiler = new DefaultCompiler();
        var pm = new ParameterManager();
        pm.Add(99);
        var q = new SelectQuery<DummyEntity>().From("t").Where(x => x.Id == 5);
        var result = compiler.Compile(q, pm);
        result.Parameters.Should().BeEmpty("Subquery compile should not return a copy of parameters");
    }
}



