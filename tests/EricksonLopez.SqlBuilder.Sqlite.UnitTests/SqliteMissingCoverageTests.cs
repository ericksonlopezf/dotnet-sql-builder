// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.ColumnSelection;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Sqlite.Tests;

public class SqliteMissingCoverageTests
{
    private readonly SqliteCompiler _compiler = new();

    public class SqliteTestEntity : IStaticEntityMetadata<SqliteTestEntity>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }

        public static string TableName => "users";
        public static int ColumnCount => 3;

        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[]
        {
            new ColumnMetadata(0, "Id", ColumnFlags.PrimaryKey),
            new ColumnMetadata(1, "Name", ColumnFlags.None),
            new ColumnMetadata(2, "Age", ColumnFlags.None)
        };

        public static bool IsNull(SqliteTestEntity entity, int columnIndex) => false;
        public static bool IsDefault(SqliteTestEntity entity, int columnIndex) => false;
        public static bool AreEqual(SqliteTestEntity entity, SqliteTestEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => columnIndex switch
        {
            0 => "Id",
            1 => "Name",
            2 => "Age",
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };

        public static string BindParameter(SqliteTestEntity entity, int columnIndex, IParameterManager parameters) => columnIndex switch
        {
            0 => parameters.Add(entity.Id),
            1 => parameters.Add(entity.Name),
            2 => parameters.Add(entity.Age),
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };

        public static void ExtractColumnArrays(ReadOnlySpan<SqliteTestEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters)
        {
        }

        public static SqliteTestEntity FromReader(System.Data.IDataReader reader) => new SqliteTestEntity();
        public static Func<System.Data.IDataReader, SqliteTestEntity> GetReaderParser() => (r) => new SqliteTestEntity();
    }

    [Fact]
    public void SqliteVisitor_OrderByNode_NullsFirstAndLast()
    {
        Expression<Func<SqliteTestEntity, string?>> expr = e => e.Name;

        // Nulls First, Descending
        var nodeFirst = new OrderByNode(expr, true, NullsPosition.First);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), nodeFirst }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT * FROM \"users\" ORDER BY CASE WHEN \"name\" IS NULL THEN 0 ELSE 1 END, \"name\" DESC");

        // Nulls Last, Ascending
        var nodeLast = new OrderByNode(expr, false, NullsPosition.Last);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), nodeLast }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * FROM \"users\" ORDER BY CASE WHEN \"name\" IS NULL THEN 1 ELSE 0 END, \"name\"");
    }

    [Fact]
    public void SqliteVisitor_GroupByNode_NonStandard_ThrowsNotSupportedException()
    {
        var gbRollup = new GroupByNode(new[] { "dept" }, GroupByType.Rollup);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), gbRollup }.ToImmutableList());
        var act1 = () => _compiler.Compile((ISqlQuery)q1);
        act1.Should().Throw<NotSupportedException>().WithMessage("*SQLite does not support Rollup*");

        var gbCube = new GroupByNode(new[] { "dept" }, GroupByType.Cube);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), gbCube }.ToImmutableList());
        var act2 = () => _compiler.Compile((ISqlQuery)q2);
        act2.Should().Throw<NotSupportedException>().WithMessage("*SQLite does not support Cube*");
    }

    [Fact]
    public void SqliteVisitor_WindowFunctionNode_WithFilter_ThrowsNotSupportedException()
    {
        var win1 = new WindowFunctionNode("ROW_NUMBER", null, null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(), "rn", null, "x > 10", null);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), win1 }.ToImmutableList());
        var act1 = () => _compiler.Compile((ISqlQuery)q1);
        act1.Should().Throw<NotSupportedException>().WithMessage("*SQLite does not support the FILTER*");

        Expression<Func<SqliteTestEntity, bool>> filterExpr = e => e.Age > 18;
        var win2 = new WindowFunctionNode("ROW_NUMBER", null, null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(), "rn", filterExpr, null, null);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), win2 }.ToImmutableList());
        var act2 = () => _compiler.Compile((ISqlQuery)q2);
        act2.Should().Throw<NotSupportedException>().WithMessage("*SQLite does not support the FILTER*");
    }

    [Fact]
    public void SqliteVisitor_OnConflictNode_Variants()
    {
        // TargetColumns + Lambda new expression
        Expression<Func<SqliteTestEntity, object>> newExpr = e => new { e.Name, e.Age };
        var node1 = new OnConflictNode(new[] { "id" }, null, newExpr, null);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "id" }), node1 }.ToImmutableList());
        var res1 = _compiler.Compile((ISqlQuery)q1);
        res1.Sql.Trim().Should().EndWith("ON CONFLICT (\"id\") DO UPDATE SET \"name\" = EXCLUDED.\"name\", \"age\" = EXCLUDED.\"age\"");

        // Lambda single member expression
        Expression<Func<SqliteTestEntity, string?>> singleExpr = e => e.Name;
        var node2 = new OnConflictNode(Array.Empty<string>(), null, singleExpr, null);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "id" }), node2 }.ToImmutableList());
        var res2 = _compiler.Compile((ISqlQuery)q2);
        res2.Sql.Trim().Should().EndWith("ON CONFLICT DO UPDATE SET \"name\" = EXCLUDED.\"name\"");

        // UpdateAction DO NOTHING
        var node3 = new OnConflictNode(new[] { "id" }, "DO NOTHING", null, null);
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "id" }), node3 }.ToImmutableList());
        var res3 = _compiler.Compile((ISqlQuery)q3);
        res3.Sql.Trim().Should().EndWith("ON CONFLICT (\"id\") DO NOTHING");

        // UpdateAction raw without "DO " prefix with parameters
        var node4 = new OnConflictNode(new[] { "id" }, "\"name\" = @name_val", null, new object[] { "Alice" });
        var q4 = Substitute.For<IAstQuery>();
        q4.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "id" }), node4 }.ToImmutableList());
        var res4 = _compiler.Compile((ISqlQuery)q4);
        res4.Sql.Trim().Should().EndWith("ON CONFLICT (\"id\") DO UPDATE SET \"name\" = @name_val");
        res4.Parameters.Should().ContainKey("p0");
    }

    [Fact]
    public void SqliteCompiler_EscapeIdentifier_Overloads()
    {
        _compiler.EscapeIdentifier("users").Should().Be("\"users\"");

        var sb = new StringBuilder();
        _compiler.EscapeIdentifier(sb, "my_column".AsSpan());
        sb.ToString().Should().Be("\"my_column\"");
    }

    [Fact]
    public void SqliteCompiler_CompileLimitOffset_Variants()
    {
        // Limit only
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new LimitOffsetNode(10, null) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT * LIMIT 10");

        // Offset only (SQLite uses LIMIT -1 OFFSET {offset})
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new LimitOffsetNode(null, 5) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * LIMIT -1 OFFSET 5");

        // Limit and Offset
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new LimitOffsetNode(10, 20) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q3).Sql.Trim().Should().Be("SELECT * LIMIT 10 OFFSET 20");
    }

    [Fact]
    public void SqliteRenderer_UnsupportedBulkMethods_Throw()
    {
        var renderer = new SqliteRenderer(_compiler);
        var entities = new[] { new SqliteTestEntity() };
        var rules = new List<IColumnSelectionRule<SqliteTestEntity>>();

        var actInsert = () => renderer.RenderBulkInsert(entities, rules, 10);
        actInsert.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Insert is not supported natively for SQLite*");

        var actUpdate = () => renderer.RenderBulkUpdate(entities, rules, 10);
        actUpdate.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Update is not supported natively for SQLite*");

        var actMerge = () => renderer.RenderBulkMerge(entities, rules, 10);
        actMerge.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Merge is not supported for SQLite*");

        var actUpsert = () => renderer.RenderBulkUpsert(entities, rules, 10);
        actUpsert.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Upsert is not yet implemented for SQLite*");

        var actInsertIgnore = () => renderer.RenderBulkInsertIgnore(entities, rules, 10);
        actInsertIgnore.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Insert Ignore is not yet implemented for SQLite*");
    }

    [Fact]
    public void SqliteRenderer_AppendReturningClauses()
    {
        var renderer = new SqliteRenderer(_compiler);
        var ctx1 = new CompilationContext(new ParameterManager("@"));
        renderer.AppendInsertReturningClause(ctx1);
        ctx1.Sql.ToString().Should().Be(" RETURNING *");

        var ctx2 = new CompilationContext(new ParameterManager("@"));
        renderer.AppendUpdateReturningClause(ctx2);
        ctx2.Sql.ToString().Should().Be(" RETURNING *");
    }
}
