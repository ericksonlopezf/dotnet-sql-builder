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
using EricksonLopez.SqlBuilder.Oracle;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Oracle.Tests;

public class OracleMissingCoverageTests
{
    private readonly OracleCompiler _compiler12c = new(OracleDialectVersion.Oracle12cPlus);
    private readonly OracleCompiler _compiler11g = new(OracleDialectVersion.Oracle11g);

    public class OracleTestEntity : IStaticEntityMetadata<OracleTestEntity>
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

        public static bool IsNull(OracleTestEntity entity, int columnIndex) => false;
        public static bool IsDefault(OracleTestEntity entity, int columnIndex) => false;
        public static bool AreEqual(OracleTestEntity entity, OracleTestEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => columnIndex switch
        {
            0 => "Id",
            1 => "Name",
            2 => "Age",
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };

        public static string BindParameter(OracleTestEntity entity, int columnIndex, IParameterManager parameters) => columnIndex switch
        {
            0 => parameters.Add(entity.Id),
            1 => parameters.Add(entity.Name),
            2 => parameters.Add(entity.Age),
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };

        public static void ExtractColumnArrays(ReadOnlySpan<OracleTestEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters)
        {
        }

        public static OracleTestEntity FromReader(System.Data.IDataReader reader) => new OracleTestEntity();
        public static Func<System.Data.IDataReader, OracleTestEntity> GetReaderParser() => (r) => new OracleTestEntity();
    }

    [Fact]
    public void OracleVisitor_ReturningNode_EmptyColumns_ThrowsNotSupportedException()
    {
        var returningNode = new ReturningNode(Array.Empty<string>());
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "name" }), returningNode }.ToImmutableList());
        var act = () => _compiler12c.Compile((ISqlQuery)q);
        act.Should().Throw<NotSupportedException>().WithMessage("*requires explicit column names*");
    }

    [Fact]
    public void OracleVisitor_OnConflictNode_ThrowsNotSupportedException()
    {
        var conflictNode = new OnConflictNode(new[] { "id" }, "DO NOTHING", null, null);
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "name" }), conflictNode }.ToImmutableList());
        var act = () => _compiler12c.Compile((ISqlQuery)q);
        act.Should().Throw<NotSupportedException>().WithMessage("*Oracle does not support ON CONFLICT*");
    }

    [Fact]
    public void OracleVisitor_WindowFunctionNode_WithFilter_ThrowsNotSupportedException()
    {
        var win1 = new WindowFunctionNode("ROW_NUMBER", null, null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(), "rn", null, "x > 10", null);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), win1 }.ToImmutableList());
        var act1 = () => _compiler12c.Compile((ISqlQuery)q1);
        act1.Should().Throw<NotSupportedException>().WithMessage("*Oracle does not support the FILTER*");

        Expression<Func<OracleTestEntity, bool>> filterExpr = e => e.Age > 18;
        var win2 = new WindowFunctionNode("ROW_NUMBER", null, null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(), "rn", filterExpr, null, null);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), win2 }.ToImmutableList());
        var act2 = () => _compiler12c.Compile((ISqlQuery)q2);
        act2.Should().Throw<NotSupportedException>().WithMessage("*Oracle does not support the FILTER*");
    }

    [Fact]
    public void OracleCompiler_EscapeIdentifier_Overloads()
    {
        _compiler12c.EscapeIdentifier("users").Should().Be("\"USERS\"");

        var sb = new StringBuilder();
        _compiler12c.EscapeIdentifier(sb, "my_column".AsSpan());
        sb.ToString().Should().Be("\"MY_COLUMN\"");
    }

    [Fact]
    public void OracleCompiler_Oracle11g_SelectWithPagination_Variants()
    {
        // 11g with Limit and Offset (ROWNUM <= maxRow and rnum_ > offset)
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id", "name" }, false),
            new FromNode("users", null),
            new LimitOffsetNode(10, 20)
        }.ToImmutableList());
        var res1 = _compiler11g.Compile((ISqlQuery)q1);
        res1.Sql.Trim().Should().Be("SELECT * FROM (SELECT a_.*, ROWNUM rnum_ FROM (SELECT \"ID\", \"NAME\" FROM \"USERS\") a_ WHERE ROWNUM <= 30) WHERE rnum_ > 20");

        // 11g with Limit only (ROWNUM <= limit)
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", null),
            new LimitOffsetNode(5, null)
        }.ToImmutableList());
        var res2 = _compiler11g.Compile((ISqlQuery)q2);
        res2.Sql.Trim().Should().Be("SELECT * FROM (SELECT \"ID\" FROM \"USERS\") WHERE ROWNUM <= 5");

        // 11g with Offset only (rnum_ > offset)
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", null),
            new LimitOffsetNode(null, 15)
        }.ToImmutableList());
        var res3 = _compiler11g.Compile((ISqlQuery)q3);
        res3.Sql.Trim().Should().Be("SELECT * FROM (SELECT a_.*, ROWNUM rnum_ FROM (SELECT \"ID\" FROM \"USERS\") a_) WHERE rnum_ > 15");

        // 11g without LimitOffset
        var q4 = Substitute.For<IAstQuery>();
        q4.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id" }, false),
            new FromNode("users", null)
        }.ToImmutableList());
        var res4 = _compiler11g.Compile((ISqlQuery)q4);
        res4.Sql.Trim().Should().Be("SELECT \"ID\" FROM \"USERS\"");
    }

    [Fact]
    public void OracleCompiler_CompileInsert_Variants()
    {
        // Multi-row INSERT ALL
        var multiValues = new ValuesNode(new[]
        {
            new object[] { 1, "Alice" },
            new object[] { 2, "Bob" }
        });
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", new[] { "id", "name" }),
            multiValues
        }.ToImmutableList());
        var res1 = _compiler12c.Compile((ISqlQuery)q1);
        res1.Sql.Trim().Should().Be("BEGIN INSERT INTO \"USERS\" (\"ID\", \"NAME\") VALUES (:p0, :p1); INSERT INTO \"USERS\" (\"ID\", \"NAME\") VALUES (:p2, :p3); END;");

        // Multi-row INSERT ALL with RETURNING throws
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", new[] { "id", "name" }),
            multiValues,
            new ReturningNode(new[] { "id" })
        }.ToImmutableList());
        var act2 = () => _compiler12c.Compile((ISqlQuery)q2);
        act2.Should().Throw<NotSupportedException>().WithMessage("*does not support RETURNING with multi-row INSERT ALL*");

        // Insert with DefaultValuesNode
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", Array.Empty<string>()),
            new DefaultValuesNode()
        }.ToImmutableList());
        var res3 = _compiler12c.Compile((ISqlQuery)q3);
        res3.Sql.Trim().Should().Be("INSERT INTO \"USERS\" /* Oracle: specify explicit DEFAULT values per column via VALUES () */");
    }

    [Fact]
    public void OracleCompiler_CompileUpdate_WithConcurrencyTokens_And_Returning()
    {
        // Auto-increment concurrency token with NO existing where clause
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new SetNode("name", "Alice"),
            new ConcurrencyTokenNode("version", 1, null, true),
            new ReturningNode(new[] { "id" })
        }.ToImmutableList());
        var res1 = _compiler12c.Compile((ISqlQuery)q1);
        res1.Sql.Trim().Should().Be("UPDATE \"USERS\" SET \"NAME\" = :p0, \"VERSION\" = \"VERSION\" + 1 WHERE \"VERSION\" = :p1 RETURNING \"ID\" INTO :out_id");

        // Explicit new value concurrency token WITH existing where clause
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new SetNode("name", "Alice"),
            new RawWhereNode("is_active = 1", null, false),
            new ConcurrencyTokenNode("row_guid", "old-guid", "new-guid", false)
        }.ToImmutableList());
        var res2 = _compiler12c.Compile((ISqlQuery)q2);
        res2.Sql.Trim().Should().Be("UPDATE \"USERS\" SET \"NAME\" = :p0, \"ROW_GUID\" = :p1 WHERE is_active = 1 AND \"ROW_GUID\" = :p2");
    }

    [Fact]
    public void OracleCompiler_CompileDelete_WithReturning()
    {
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("users"),
            new RawWhereNode("id = 1", null, false),
            new ReturningNode(new[] { "name" })
        }.ToImmutableList());
        var res = _compiler12c.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Be("DELETE FROM \"USERS\" WHERE id = 1 RETURNING \"NAME\" INTO :out_name");
    }

    [Fact]
    public void OracleCompiler_CompileLimitOffset_Variants()
    {
        // Limit node null
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false) }.ToImmutableList());
        _compiler12c.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT *");

        // Offset only
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new LimitOffsetNode(null, 10) }.ToImmutableList());
        _compiler12c.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * OFFSET 10 ROWS");

        // Limit only
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new LimitOffsetNode(25, null) }.ToImmutableList());
        _compiler12c.Compile((ISqlQuery)q3).Sql.Trim().Should().Be("SELECT * FETCH NEXT 25 ROWS ONLY");
    }

    [Fact]
    public void OracleParameterManager_Process_BooleansAndObjects()
    {
        var pm = new OracleParameterManager();
        var pBoolTrue = pm.Add(true);
        var pBoolFalse = pm.Add(false);
        var pInt = pm.Add(42);
        var pNamed = pm.AddNamed("custom", "val");

        var pms = pm.GetParameters();
        pms[pBoolTrue.TrimStart(':')].Should().Be(1);
        pms[pBoolFalse.TrimStart(':')].Should().Be(0);
        pms[pInt.TrimStart(':')].Should().Be(42);
        pms["custom"].Should().Be("val");
    }

    [Fact]
    public void OracleRenderer_UnsupportedBulkMethods_Throw()
    {
        var renderer = new OracleRenderer(_compiler12c);
        var entities = new[] { new OracleTestEntity() };
        var rules = new List<IColumnSelectionRule<OracleTestEntity>>();

        var actInsert = () => renderer.RenderBulkInsert(entities, rules, 10);
        actInsert.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Insert is not yet implemented for Oracle*");

        var actUpdate = () => renderer.RenderBulkUpdate(entities, rules, 10);
        actUpdate.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Update is not natively implemented for Oracle*");

        var actMerge = () => renderer.RenderBulkMerge(entities, rules, 10);
        actMerge.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Merge is not supported for Oracle*");

        var actUpsert = () => renderer.RenderBulkUpsert(entities, rules, 10);
        actUpsert.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Upsert is not yet implemented for Oracle*");

        var actInsertIgnore = () => renderer.RenderBulkInsertIgnore(entities, rules, 10);
        actInsertIgnore.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Insert Ignore is not yet implemented for Oracle*");
    }

    [Fact]
    public void OracleVisitor_WindowFunctionNode_WithoutFilter_Compiles()
    {
        var node = new WindowFunctionNode("SUM", "Amount", null, null, new[] { "Dept" }, new[] { "Salary" }, new[] { true }, "sum_val");
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[] { new SelectNode(new[] { "id" }, false), new FromNode("users"), node }.ToImmutableList());

        var res = _compiler12c.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Contain("SUM(\"AMOUNT\") OVER (PARTITION BY \"DEPT\" ORDER BY \"SALARY\" DESC) AS \"SUM_VAL\"");
    }

    [Fact]
    public void OracleVisitor_WindowFunctionNode_WithFilterExpression_Throws()
    {
        var expr = Expression.Lambda<Func<OracleTestEntity, bool>>(
            Expression.Constant(true),
            Expression.Parameter(typeof(OracleTestEntity), "x"));
        var node = new WindowFunctionNode("SUM", "Amount", null, null, null, null, null, "sum_val", FilterExpression: expr);
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[] { node }.ToImmutableList());

        Action act = () => _compiler12c.Compile((ISqlQuery)q);
        act.Should().Throw<NotSupportedException>().WithMessage("*Oracle does not support the FILTER (WHERE ...) clause on window functions*");
    }

    [Fact]
    public void OracleCompiler_CompileInsert_WithInsertSelectNode_Compiles()
    {
        var subQuery = Substitute.For<IAstQuery>();
        subQuery.Nodes.Returns(new ISqlNode[] { new SelectNode(new[] { "id", "name" }, false), new FromNode("source_users") }.ToImmutableList());
        
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[] { new InsertSelectNode("dest_users", new[] { "id", "name" }, subQuery) }.ToImmutableList());

        var res = _compiler12c.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Contain("INSERT INTO \"DEST_USERS\" (\"ID\", \"NAME\") SELECT \"ID\", \"NAME\" FROM \"SOURCE_USERS\"");
    }

    [Fact]
    public void OracleCompiler_RenderInsert_InvokesAotRenderer()
    {
        var entity = new OracleTestEntity { Id = 1, Name = "Alice" };
        var res = _compiler12c.RenderInsert(entity, stackalloc bool[] { true, true, false, false });
        res.Sql.Should().Contain("INSERT INTO \"USERS\"");
    }

    [Fact]
    public void OracleCompiler_CompileUpdate_ConcurrencyToken_NotAutoIncrement_NullNewValue()
    {
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new ConcurrencyTokenNode("version", 1, null, false)
        }.ToImmutableList());

        var res = _compiler12c.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Be("UPDATE \"USERS\" SET \"VERSION\" = :p0 WHERE \"VERSION\" = :p1");
    }

    [Fact]
    public void OracleCompiler_CompileUpdate_MultipleConcurrencyTokens_WithoutWhere_BuildsCorrectSql()
    {
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new ConcurrencyTokenNode("version", 1, null, true),
            new ConcurrencyTokenNode("token2", "old", "new", false)
        }.ToImmutableList());

        var res = _compiler12c.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Be("UPDATE \"USERS\" SET \"VERSION\" = \"VERSION\" + 1, \"TOKEN2\" = :p0 WHERE \"VERSION\" = :p1 AND \"TOKEN2\" = :p2");
    }
}
