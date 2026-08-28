// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.ColumnSelection;
using EricksonLopez.SqlBuilder.SqlServer;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class AotSqlRendererBaseTests
{
    private class ConcreteRenderer : AotSqlRendererBase
    {
        public ConcreteRenderer(ISqlCompiler compiler) : base(compiler) { }

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkInsert<T>(
            IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkUpdate<T>(
            IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkMerge<T>(
            IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkUpsert<T>(
            IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkInsertIgnore<T>(
            IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();
    }

    private class CustomClauseRenderer : ConcreteRenderer
    {
        public CustomClauseRenderer(ISqlCompiler compiler) : base(compiler) { }

        internal override void AppendInsertOutputClause(CompilationContext context) => context.Sql.Append(" OUTPUT INSERTED.*");
        internal override void AppendInsertReturningClause(CompilationContext context) => context.Sql.Append(" RETURNING *");
        internal override void AppendUpdateOutputClause(CompilationContext context) => context.Sql.Append(" OUTPUT INSERTED.Id");
        internal override void AppendUpdateReturningClause(CompilationContext context) => context.Sql.Append(" RETURNING id");
    }

    [Fact]
    public void Constructor_NullCompiler_ThrowsArgumentNullException()
    {
        var act = () => new ConcreteRenderer(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("compiler");
    }

    [Fact]
    public void RenderInsert_MultipleColumns_GeneratesCorrectSqlAndParameters()
    {
        var compiler = new SqlServerCompiler();
        var renderer = new ConcreteRenderer(compiler);
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };

        Span<bool> mask = stackalloc bool[3] { true, true, true };
        var result = renderer.RenderInsert(entity, mask);

        result.Sql.Should().Be("INSERT INTO [test_entity] ([Id], [Name], [Age]) VALUES (@p0, @p1, @p2)");
        result.Parameters.Should().HaveCount(3);
        result.Parameters["p0"].Should().Be(1);
        result.Parameters["p1"].Should().Be("Test");
        result.Parameters["p2"].Should().Be(30);
    }

    [Fact]
    public void RenderInsert_WithOutputAndReturningClauses_GeneratesSql()
    {
        var compiler = new SqlServerCompiler();
        var renderer = new CustomClauseRenderer(compiler);
        var entity = new TestEntity { Id = 2, Name = "Alice", Age = null };

        Span<bool> mask = stackalloc bool[3] { true, true, false };
        var result = renderer.RenderInsert(entity, mask);

        result.Sql.Should().Be("INSERT INTO [test_entity] ([Id], [Name]) OUTPUT INSERTED.* VALUES (@p0, @p1) RETURNING *");
        result.Parameters.Should().HaveCount(2);
        result.Parameters["p0"].Should().Be(2);
        result.Parameters["p1"].Should().Be("Alice");
    }

    [Fact]
    public void RenderUpdate_MultipleSetAndWhereColumns_GeneratesCorrectSqlAndParameters()
    {
        var compiler = new SqlServerCompiler();
        var renderer = new ConcreteRenderer(compiler);
        var entity = new TestEntity { Id = 10, Name = "UpdatedName", Age = 45 };

        Span<bool> setMask = stackalloc bool[3] { false, true, true };
        Span<bool> whereMask = stackalloc bool[3] { true, false, false };

        var result = renderer.RenderUpdate(entity, setMask, whereMask);

        result.Sql.Should().Be("UPDATE [test_entity] SET [Name] = @p0, [Age] = @p1 WHERE [Id] = @p2");
        result.Parameters.Should().HaveCount(3);
        result.Parameters["p0"].Should().Be("UpdatedName");
        result.Parameters["p1"].Should().Be(45);
        result.Parameters["p2"].Should().Be(10);
    }

    [Fact]
    public void RenderUpdate_WithOutputAndReturningClauses_GeneratesSql()
    {
        var compiler = new SqlServerCompiler();
        var renderer = new CustomClauseRenderer(compiler);
        var entity = new TestEntity { Id = 20, Name = "ClauseTest", Age = 25 };

        Span<bool> setMask = stackalloc bool[3] { false, true, false };
        Span<bool> whereMask = stackalloc bool[3] { true, false, true };

        var result = renderer.RenderUpdate(entity, setMask, whereMask);

        result.Sql.Should().Be("UPDATE [test_entity] SET [Name] = @p0 OUTPUT INSERTED.Id WHERE [Id] = @p1 AND [Age] = @p2 RETURNING id");
        result.Parameters.Should().HaveCount(3);
        result.Parameters["p0"].Should().Be("ClauseTest");
        result.Parameters["p1"].Should().Be(20);
        result.Parameters["p2"].Should().Be(25);
    }

    [Fact]
    public void RenderInsert_EmitsDiagnosticActivityAndTags()
    {
        var compiler = new SqlServerCompiler();
        var renderer = new ConcreteRenderer(compiler);
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };

        System.Diagnostics.Activity? capturedActivity = null;
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = a => { if (a.OperationName == "SqlRenderer.RenderInsert") capturedActivity = a; }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        Span<bool> mask = stackalloc bool[3] { true, true, true };
        var result = renderer.RenderInsert(entity, mask);

        capturedActivity.Should().NotBeNull();
        capturedActivity!.OperationName.Should().Be("SqlRenderer.RenderInsert");
        capturedActivity.GetTagItem("db.statement").Should().Be(result.Sql);
        capturedActivity.GetTagItem("sqlbuilder.query_type").Should().Be("INSERT_AOT");
    }

    [Fact]
    public void RenderUpdate_EmitsDiagnosticActivityAndTags()
    {
        var compiler = new SqlServerCompiler();
        var renderer = new ConcreteRenderer(compiler);
        var entity = new TestEntity { Id = 10, Name = "UpdatedName", Age = 45 };

        System.Diagnostics.Activity? capturedActivity = null;
        using var listener = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = s => s.Name == SqlBuilderDiagnostics.SourceName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = a => { if (a.OperationName == "SqlRenderer.RenderUpdate") capturedActivity = a; }
        };
        System.Diagnostics.ActivitySource.AddActivityListener(listener);

        Span<bool> setMask = stackalloc bool[3] { false, true, true };
        Span<bool> whereMask = stackalloc bool[3] { true, false, false };
        var result = renderer.RenderUpdate(entity, setMask, whereMask);

        capturedActivity.Should().NotBeNull();
        capturedActivity!.OperationName.Should().Be("SqlRenderer.RenderUpdate");
        capturedActivity.GetTagItem("db.statement").Should().Be(result.Sql);
        capturedActivity.GetTagItem("sqlbuilder.query_type").Should().Be("UPDATE_AOT");
    }
}



