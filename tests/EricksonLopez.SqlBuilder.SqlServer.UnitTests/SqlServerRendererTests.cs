// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.SqlServer;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.SqlServer.UnitTests;

public class SqlServerRendererTests
{
    private sealed class DummyEntity : IStaticEntityMetadata<DummyEntity>
    {
        public int Id { get; set; }
        public static string TableName => "dummies";
        public static int ColumnCount => 1;
        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[] { new ColumnMetadata(0, "Id", ColumnFlags.PrimaryKey) };
        public static bool IsNull(DummyEntity entity, int columnIndex) => false;
        public static bool IsDefault(DummyEntity entity, int columnIndex) => false;
        public static bool AreEqual(DummyEntity entity, DummyEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => "Id";
        public static string BindParameter(DummyEntity entity, int columnIndex, IParameterManager parameters) => "@p0";
        public static void ExtractColumnArrays(ReadOnlySpan<DummyEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters) { }
        public static Func<IDataReader, DummyEntity> GetReaderParser() => _ => new DummyEntity();
        public static DummyEntity FromReader(IDataReader reader) => new DummyEntity();
    }

    [Fact]
    public void AppendInsertOutputClause_ShouldAppendOutputInserted()
    {
        // Arrange
        var compiler = Substitute.For<ISqlCompiler>();
        var renderer = new SqlServerRenderer(compiler);
        var context = new CompilationContext(new ParameterManager());

        // Act
        renderer.AppendInsertOutputClause(context);

        // Assert
        context.Sql.ToString().Should().Be(" OUTPUT INSERTED.*");
    }

    [Fact]
    public void AppendUpdateOutputClause_ShouldAppendOutputInserted()
    {
        // Arrange
        var compiler = Substitute.For<ISqlCompiler>();
        var renderer = new SqlServerRenderer(compiler);
        var context = new CompilationContext(new ParameterManager());

        // Act
        renderer.AppendUpdateOutputClause(context);

        // Assert
        context.Sql.ToString().Should().Be(" OUTPUT INSERTED.*");
    }

    [Fact]
    public void RenderBulkInsert_ThrowsNotSupportedException()
    {
        var compiler = Substitute.For<ISqlCompiler>();
        var renderer = new SqlServerRenderer(compiler);

        var act = () => renderer.RenderBulkInsert(new[] { new DummyEntity() }, new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("AOT Bulk Insert for SQL Server should use SqlBulkCopyStrategy via EricksonLopez.SqlBuilder.SqlServer.Bulk.");
    }

    [Fact]
    public void RenderBulkUpdate_ThrowsNotSupportedException()
    {
        var compiler = Substitute.For<ISqlCompiler>();
        var renderer = new SqlServerRenderer(compiler);

        var act = () => renderer.RenderBulkUpdate(new[] { new DummyEntity() }, new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("AOT Bulk Update is not natively supported for SQL Server via AotSqlRenderer.");
    }

    [Fact]
    public void RenderBulkMerge_ThrowsNotSupportedException()
    {
        var compiler = Substitute.For<ISqlCompiler>();
        var renderer = new SqlServerRenderer(compiler);

        var act = () => renderer.RenderBulkMerge(new[] { new DummyEntity() }, new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("AOT Bulk Merge is not supported for SQL Server (see ADR-025).");
    }

    [Fact]
    public void RenderBulkUpsert_ThrowsNotSupportedException()
    {
        var compiler = Substitute.For<ISqlCompiler>();
        var renderer = new SqlServerRenderer(compiler);

        var act = () => renderer.RenderBulkUpsert(new[] { new DummyEntity() }, new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("AOT Bulk Upsert is not supported for SQL Server.");
    }

    [Fact]
    public void RenderBulkInsertIgnore_ThrowsNotSupportedException()
    {
        var compiler = Substitute.For<ISqlCompiler>();
        var renderer = new SqlServerRenderer(compiler);

        var act = () => renderer.RenderBulkInsertIgnore(new[] { new DummyEntity() }, new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("AOT Bulk Insert Ignore is not supported for SQL Server.");
    }
}




