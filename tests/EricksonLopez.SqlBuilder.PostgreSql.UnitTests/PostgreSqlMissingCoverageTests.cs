// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq.Expressions;
using System.Text;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Builders.Bulk;
using EricksonLopez.SqlBuilder.PostgreSql;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class PostgreSqlMissingCoverageTests
{
    private readonly PostgreSqlCompiler _compiler = new();

    private sealed class PgTestEntity : IStaticEntityMetadata<PgTestEntity>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public int RowVersion { get; set; }

        public static string TableName => "users";
        public static int ColumnCount => 4;

        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[]
        {
            new ColumnMetadata(0, "Id", ColumnFlags.PrimaryKey),
            new ColumnMetadata(1, "Name", ColumnFlags.None),
            new ColumnMetadata(2, "Age", ColumnFlags.None),
            new ColumnMetadata(3, "RowVersion", ColumnFlags.Identity)
        };

        public static bool IsNull(PgTestEntity entity, int columnIndex) => false;
        public static bool IsDefault(PgTestEntity entity, int columnIndex) => false;
        public static bool AreEqual(PgTestEntity entity, PgTestEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => columnIndex switch
        {
            0 => "Id",
            1 => "Name",
            2 => "Age",
            3 => "RowVersion",
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };
        public static string BindParameter(PgTestEntity entity, int columnIndex, IParameterManager parameters) => parameters.Add(entity.Id);
        public static void ExtractColumnArrays(ReadOnlySpan<PgTestEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters) { }
        public static Func<IDataReader, PgTestEntity> GetReaderParser() => _ => new PgTestEntity();
        public static PgTestEntity FromReader(IDataReader reader) => new PgTestEntity();
    }

    private sealed class CompositePgEntity : IStaticEntityMetadata<CompositePgEntity>
    {
        public static string TableName => "order_items";
        public static int ColumnCount => 3;

        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[]
        {
            new ColumnMetadata(0, "OrderId", ColumnFlags.PrimaryKey),
            new ColumnMetadata(1, "ItemId", ColumnFlags.PrimaryKey),
            new ColumnMetadata(2, "Quantity", ColumnFlags.None)
        };

        public static bool IsNull(CompositePgEntity entity, int columnIndex) => false;
        public static bool IsDefault(CompositePgEntity entity, int columnIndex) => false;
        public static bool AreEqual(CompositePgEntity entity, CompositePgEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => columnIndex switch
        {
            0 => "OrderId",
            1 => "ItemId",
            2 => "Quantity",
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };
        public static string BindParameter(CompositePgEntity entity, int columnIndex, IParameterManager parameters) => parameters.Add(1);
        public static void ExtractColumnArrays(ReadOnlySpan<CompositePgEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters) { }
        public static Func<IDataReader, CompositePgEntity> GetReaderParser() => _ => new CompositePgEntity();
        public static CompositePgEntity FromReader(IDataReader reader) => new CompositePgEntity();
    }

    [Fact]
    public void NpgsqlBulkMergeStrategy_BuildUpsertSql_SingleAndCompositeKeys()
    {
        var singleSql = NpgsqlBulkMergeStrategy.BuildUpsertSql<PgTestEntity>("users", "_staging_users_1", PgTestEntity.GetColumns());
        var normSingle = singleSql.Replace("\r\n", "\n");
        var expectedSingle =
            "INSERT INTO \"users\"\n" +
            "SELECT * FROM \"_staging_users_1\"\n" +
            "ON CONFLICT (\n" +
            "\"Id\") DO UPDATE SET\n" +
            "    \"Name\" = EXCLUDED.\"Name\"\n" +
            ",    \"Age\" = EXCLUDED.\"Age\"\n";
        normSingle.Should().Be(expectedSingle);

        var compSql = NpgsqlBulkMergeStrategy.BuildUpsertSql<CompositePgEntity>("order_items", "_staging_items_1", CompositePgEntity.GetColumns());
        var normComp = compSql.Replace("\r\n", "\n");
        var expectedComp =
            "INSERT INTO \"order_items\"\n" +
            "SELECT * FROM \"_staging_items_1\"\n" +
            "ON CONFLICT (\n" +
            "\"OrderId\", \"ItemId\") DO UPDATE SET\n" +
            "    \"Quantity\" = EXCLUDED.\"Quantity\"\n";
        normComp.Should().Be(expectedComp);
    }

    [Fact]
    public void NpgsqlBulkMergeStrategy_NonNpgsqlConnection_ThrowsInvalidOperationException()
    {
        var mockConn = Substitute.For<IDbConnection>();
        var act = () => NpgsqlBulkMergeStrategy.BulkMergeAsync<PgTestEntity>(mockConn, new[] { new PgTestEntity() });
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*requires an NpgsqlConnection*");
    }

    [Fact]
    public void NpgsqlCopyStrategy_BuildActiveColumnNames_And_BuildCopyCommand()
    {
        var cols = PgTestEntity.GetColumns();
        var optionsNoIdentities = new BulkOptions { ReturnIdentities = false };
        var names1 = NpgsqlCopyStrategy.BuildActiveColumnNames(cols, optionsNoIdentities);
        names1.Should().Equal("Id", "Name", "Age");

        var optionsWithIdentities = new BulkOptions { ReturnIdentities = true };
        var names2 = NpgsqlCopyStrategy.BuildActiveColumnNames(cols, optionsWithIdentities);
        names2.Should().Equal("Id", "Name", "Age", "RowVersion");

        var cmd = NpgsqlCopyStrategy.BuildCopyCommand("users", names1);
        cmd.Should().Be("COPY \"users\" (\"Id\", \"Name\", \"Age\") FROM STDIN (FORMAT BINARY)");
    }

    [Fact]
    public void NpgsqlCopyStrategy_NonNpgsqlConnection_ThrowsInvalidOperationException()
    {
        var mockConn = Substitute.For<IDbConnection>();
        var act = () => NpgsqlCopyStrategy.BulkInsertAsync<PgTestEntity>(mockConn, new[] { new PgTestEntity() });
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*requires an NpgsqlConnection*");
    }

    [Fact]
    public void PostgreSqlVisitor_DistinctOnNode()
    {
        var node = new DistinctOnNode(new[] { "department", "role" });
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id", "name" }, false),
            node,
            new FromNode("employees", null)
        }.ToImmutableList());

        var res = _compiler.Compile((ISqlQuery)query);
        res.Sql.Trim().Should().Be("SELECT DISTINCT ON (\"department\", \"role\") \"id\", \"name\" FROM \"employees\"");
    }

    [Fact]
    public void PostgreSqlVisitor_SubqueryJoinNode_ApplyVariants()
    {
        var subQuery = Substitute.For<IAstQuery>();
        subQuery.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());

        // CrossApply -> CROSS JOIN LATERAL
        var crossApply = new SubqueryJoinNode(JoinType.CrossApply, subQuery, "sub", "1 = 1", false, null);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), crossApply }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT * FROM \"users\" CROSS JOIN LATERAL (SELECT 1) AS \"sub\" ON 1 = 1");

        // OuterApply -> LEFT JOIN LATERAL
        var outerApply = new SubqueryJoinNode(JoinType.OuterApply, subQuery, "sub", "1 = 1", false, null);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), outerApply }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * FROM \"users\" LEFT JOIN LATERAL (SELECT 1) AS \"sub\" ON 1 = 1");
    }

    [Fact]
    public void PostgreSqlVisitor_SubqueryJoinNode_LateralVariants()
    {
        var subQuery = Substitute.For<IAstQuery>();
        subQuery.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());

        // Inner Lateral
        var innerLat = new SubqueryJoinNode(JoinType.Inner, subQuery, "sub", "sub.id = users.id", true, null);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), innerLat }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT * FROM \"users\" INNER JOIN LATERAL (SELECT 1) AS \"sub\" ON sub.id = users.id");

        // Left Lateral with Expression condition
        Expression<Func<PgTestEntity, bool>> exprCondition = e => e.Id > 0;
        var leftLat = new SubqueryJoinNode(JoinType.Left, subQuery, "sub", null, true, exprCondition);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), leftLat }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * FROM \"users\" LEFT JOIN LATERAL (SELECT 1) AS \"sub\" ON (id > @p0)");

        // Right Lateral without ON condition
        var rightLat = new SubqueryJoinNode(JoinType.Right, subQuery, "sub", null, true, null);
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), rightLat }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q3).Sql.Trim().Should().Be("SELECT * FROM \"users\" RIGHT JOIN LATERAL (SELECT 1) AS \"sub\"");

        // Full Lateral
        var fullLat = new SubqueryJoinNode(JoinType.Full, subQuery, "sub", null, true, null);
        var q4 = Substitute.For<IAstQuery>();
        q4.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), fullLat }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q4).Sql.Trim().Should().Be("SELECT * FROM \"users\" FULL JOIN LATERAL (SELECT 1) AS \"sub\"");

        // Cross Lateral
        var crossLat = new SubqueryJoinNode(JoinType.Cross, subQuery, "sub", null, true, null);
        var q5 = Substitute.For<IAstQuery>();
        q5.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), crossLat }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q5).Sql.Trim().Should().Be("SELECT * FROM \"users\" CROSS JOIN LATERAL (SELECT 1) AS \"sub\"");

        // Non-lateral regular subquery join falls back to base
        var regSub = new SubqueryJoinNode(JoinType.Inner, subQuery, "sub", "sub.id = users.id", false, null);
        var q6 = Substitute.For<IAstQuery>();
        q6.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), regSub }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q6).Sql.Trim().Should().Be("SELECT * FROM \"users\" INNER JOIN (SELECT 1) AS \"sub\" ON sub.id = users.id");
    }

    [Fact]
    public void PostgreSqlVisitor_CteNode_MaterializationVariants()
    {
        var cteQuery = Substitute.For<IAstQuery>();
        cteQuery.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());

        // Materialized
        var cteMat = new CteNode("cte_mat", cteQuery, false, MaterializationHint.Materialized);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { cteMat, new SelectNode(Array.Empty<string>(), false), new FromNode("cte_mat", null) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("WITH \"cte_mat\" AS MATERIALIZED (SELECT 1) SELECT * FROM \"cte_mat\"");

        // Not Materialized
        var cteNotMat = new CteNode("cte_not_mat", cteQuery, false, MaterializationHint.NotMaterialized);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { cteNotMat, new SelectNode(Array.Empty<string>(), false), new FromNode("cte_not_mat", null) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("WITH \"cte_not_mat\" AS NOT MATERIALIZED (SELECT 1) SELECT * FROM \"cte_not_mat\"");

        // Default
        var cteDefault = new CteNode("cte_def", cteQuery, false, MaterializationHint.Default);
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[] { cteDefault, new SelectNode(Array.Empty<string>(), false), new FromNode("cte_def", null) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q3).Sql.Trim().Should().Be("WITH \"cte_def\" AS (SELECT 1) SELECT * FROM \"cte_def\"");
    }

    private sealed record CustomUnknownNode : ISqlNode
    {
        public void Accept(ISqlVisitor visitor) => visitor.VisitUnknown(this);
    }

    [Fact]
    public void PostgreSqlVisitor_VisitUnknown()
    {
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = _compiler.CreateVisitor(ctx);
        var node = new CustomUnknownNode();
        var act = () => node.Accept(visitor);
        act.Should().Throw<NotSupportedException>().WithMessage("*CustomUnknownNode*");
    }

    [Fact]
    public void PostgreSqlCompiler_CompileBeforeSelect_Variants()
    {
        // CopyNode with format
        var copyWithFormat = new CopyNode("users", new[] { "id", "name" }, "STDIN", "BINARY");
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { copyWithFormat }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("COPY \"users\" (\"id\", \"name\") FROM STDIN WITH (FORMAT BINARY)");

        // CopyNode without format
        var copyNoFormat = new CopyNode("users", new[] { "id", "name" }, "STDIN", null!);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { copyNoFormat }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("COPY \"users\" (\"id\", \"name\") FROM STDIN");

        // Non-Copy extension node returns false in CompileBeforeSelect and throws on unknown node during visitor pass
        var nonCopy = new CustomUnknownNode();
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[] { nonCopy, new SelectNode(Array.Empty<string>(), false) }.ToImmutableList());
        var act = () => _compiler.Compile((ISqlQuery)q3);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void PostgreSqlCompiler_CompileFrom_UnnestVariants()
    {
        // FromNode + UnnestNodes
        var unnest1 = new UnnestNode(new[] { new object[] { 1, 2 } }, "u1");
        var unnest2 = new UnnestNode(new[] { new object[] { "a", "b" } }, "u2");
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", "u"),
            unnest1,
            unnest2
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT * FROM \"users\" AS \"u\" , UNNEST(@p0) AS \"u1\" , UNNEST(@p1) AS \"u2\"");

        // Only UnnestNodes (fromNode is null or UnnestNode)
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            unnest1,
            unnest2
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * FROM UNNEST(@p0) AS \"u1\" , UNNEST(@p1) AS \"u2\"");
    }

    [Fact]
    public void PostgreSqlCompiler_CompileDelete_UsingAndReturning()
    {
        // Delete with FromNode (with alias) + Join + Where + Returning
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("orders"),
            new FromNode("customers", "c"),
            new JoinNode(JoinType.Inner, "regions", "r", "r.id = c.region_id", null),
            new RawWhereNode("c.is_active = 0", null, false),
            new ReturningNode(new[] { "id", "order_date" })
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("DELETE FROM \"orders\" USING \"customers\" AS \"c\" INNER JOIN \"regions\" AS \"r\" ON r.id = c.region_id WHERE c.is_active = 0 RETURNING \"id\", \"order_date\"");

        // Delete with FromNode (without alias)
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("orders"),
            new FromNode("customers", null)
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("DELETE FROM \"orders\" USING \"customers\"");
    }

    [Fact]
    public void PostgreSqlCompiler_EscapeIdentifier_Overloads()
    {
        _compiler.EscapeIdentifier("column_name").Should().Be("\"column_name\"");

        var sb = new StringBuilder();
        _compiler.EscapeIdentifier(sb, "col".AsSpan());
        sb.ToString().Should().Be("\"col\"");
    }

    [Fact]
    public void PostgreSqlRenderer_RenderInsert_And_RenderUpdate()
    {
        var renderer = new PostgreSqlRenderer(_compiler);
        var entity = new PgTestEntity { Id = 42, Name = "Alice", Age = 30 };

        // RenderInsert
        Span<bool> insertMask = stackalloc bool[4] { true, true, false, false };
        var insertResult = renderer.RenderInsert(entity, insertMask);
        insertResult.Sql.Should().Be("INSERT INTO \"users\" (\"Id\", \"Name\") VALUES (@p0, @p1) RETURNING *");

        // RenderUpdate with multiple where columns
        Span<bool> setMask2 = stackalloc bool[4] { false, true, false, false };
        Span<bool> whereMask2 = stackalloc bool[4] { true, false, true, false };
        var updateResult2 = renderer.RenderUpdate(entity, setMask2, whereMask2);
        updateResult2.Sql.Should().Be("UPDATE \"users\" SET \"Name\" = @p0 WHERE \"Id\" = @p1 AND \"Age\" = @p2 RETURNING *");
    }

    [Fact]
    public void PostgreSqlRenderer_RenderBulkInsert_ThrowsOnEmpty()
    {
        var renderer = new PostgreSqlRenderer(_compiler);
        var act = () => renderer.RenderBulkInsert(Array.Empty<PgTestEntity>(), new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<PgTestEntity>>(), 10);
        act.Should().Throw<InvalidOperationException>().WithMessage("Collection is empty.");
    }

    [Fact]
    public void PostgreSqlRenderer_UnsupportedBulkOperations_ThrowNotSupportedException()
    {
        var renderer = new PostgreSqlRenderer(_compiler);
        var entities = new[] { new PgTestEntity() };
        var rules = new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<PgTestEntity>>();

        var actUpdate = () => renderer.RenderBulkUpdate(entities, rules, 10);
        actUpdate.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Update is not natively implemented*");

        var actMerge = () => renderer.RenderBulkMerge(entities, rules, 10);
        actMerge.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Merge is not supported*");

        var actUpsert = () => renderer.RenderBulkUpsert(entities, rules, 10);
        actUpsert.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Upsert is not yet implemented*");

        var actInsertIgnore = () => renderer.RenderBulkInsertIgnore(entities, rules, 10);
        actInsertIgnore.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Insert Ignore is not yet implemented*");
    }

    [Fact]
    public void CopyQuery_And_CopyNode_Tests()
    {
        var copyQuery1 = new CopyQuery<EricksonLopez.SqlBuilder.Testing.DataBuilders.TestEntity>();
        copyQuery1.Nodes.Should().NotBeEmpty();
        copyQuery1.Tag.Should().BeNull();
        var res1 = copyQuery1.Build(_compiler);
        res1.Sql.Should().Contain("COPY");

        var copyQuery2 = new CopyQuery<EricksonLopez.SqlBuilder.Testing.DataBuilders.TestEntity>(new[] { "id", "name" });
        copyQuery2.Nodes.Should().NotBeEmpty();
        var res2 = copyQuery2.Build(_compiler);
        res2.Sql.Should().Contain("COPY");

        var node = new CopyNode("my_table", new[] { "col1" }, "STDIN", "BINARY");
        node.TableName.Should().Be("my_table");
        node.Columns.Should().Equal("col1");
        node.FromSource.Should().Be("STDIN");
        node.Format.Should().Be("BINARY");

        var ctx = new CompilationContext(new ParameterManager());
        var visitor = _compiler.CreateVisitor(ctx);
        var actVisitor = () => node.Accept(visitor);
        actVisitor.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void PostgreSqlVisitor_SubqueryJoinNode_CustomJoinType_Lateral()
    {
        var subQuery = Substitute.For<IAstQuery>();
        subQuery.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());

        var customJoin = new SubqueryJoinNode((JoinType)999, subQuery, "sub", null, true, null);
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), customJoin }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q).Sql.Trim().Should().Be("SELECT * FROM \"users\" 999 JOIN LATERAL (SELECT 1) AS \"sub\"");
    }

    private sealed class MismatchedMetadataEntity : ISqlEntity
    {
        public string GetTableName() => "mismatched";
        public string[] GetColumnNames() => new[] { "col1", "col2" };
        public object?[] GetValues() => new object?[] { 1 };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public System.Collections.Generic.IReadOnlyDictionary<string, string> GetPropertyMap() => new System.Collections.Generic.Dictionary<string, string>();
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }

    [Fact]
    public void PostgreSqlDapperExtensions_BulkInsertUnnestAsync_MetadataMismatch_Throws()
    {
        var mockConn = Substitute.For<IDbConnection>();
        var entities = new[] { new MismatchedMetadataEntity() };
        var act = () => mockConn.BulkInsertUnnestAsync(entities);
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Entity metadata mismatch*");
    }

    [Fact]
    public void BulkParameters_NullArguments_ThrowArgumentNullException()
    {
        var act1 = () => BulkParameters.From<PgTestEntity>(null!);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("items");

        var bp = BulkParameters.From(new[] { new PgTestEntity() });
        var act2 = () => bp.Add<int>("param", null!, NpgsqlTypes.NpgsqlDbType.Integer);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("selector");
    }

    [Fact]
    public void PostgreSqlRenderer_RenderUpdate_MultipleColumns_RendersCommasCorrectly()
    {
        var renderer = new PostgreSqlRenderer(_compiler);
        var entity = new PgTestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> setMask = stackalloc bool[] { false, true, true, false };
        Span<bool> whereMask = stackalloc bool[] { true, false, false, false };

        var result = renderer.RenderUpdate(entity, setMask, whereMask);
        result.Sql.Should().Be("UPDATE \"users\" SET \"Name\" = @p0, \"Age\" = @p1 WHERE \"Id\" = @p2 RETURNING *");
        result.Parameters.Should().HaveCount(3);
    }

    [Fact]
    public void PostgreSqlRenderer_RenderBulkInsert_NonArrayEnumerable_Succeeds()
    {
        var renderer = new PostgreSqlRenderer(_compiler);
        static IEnumerable<PgTestEntity> GetEntities()
        {
            yield return new PgTestEntity { Id = 1, Name = "Alice", Age = 20 };
            yield return new PgTestEntity { Id = 2, Name = "Bob", Age = 25 };
        }

        var result = renderer.RenderBulkInsert(GetEntities(), new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<PgTestEntity>>(), 10);
        result.Batches.Should().HaveCount(1);
        result.Batches[0].Sql.Should().Contain("INSERT INTO \"users\"");
        result.Batches[0].Sql.Should().Contain("SELECT * FROM UNNEST(");
    }

    [Fact]
    public void PostgreSqlVisitor_SubqueryJoinNode_WithOnConditionString()
    {
        var subQuery = Substitute.For<IAstQuery>();
        subQuery.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1", null, false) }.ToImmutableList());

        var joinWithOn = new SubqueryJoinNode(JoinType.Inner, subQuery, "sub", "sub.id = users.id", false, null);
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), joinWithOn }.ToImmutableList());
        var res = _compiler.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Be("SELECT * FROM \"users\" INNER JOIN (SELECT 1) AS \"sub\" ON sub.id = users.id");
    }

    [Fact]
    public void PostgreSqlCompiler_CompileBeforeSelect_Branches()
    {
        var partitionNullExt = new SqlNodePartition(Array.Empty<ISqlNode>());
        var ctx = new CompilationContext(new ParameterManager());
        var visitor = _compiler.CreateVisitor(ctx);
        var resNull = _compiler.CompileBeforeSelect(partitionNullExt, visitor, ctx);
        resNull.Should().BeFalse();

        var partitionNonCopy = new SqlNodePartition(new ISqlNode[] { new DistinctOnNode(new[] { "col" }) });
        var resNonCopy = _compiler.CompileBeforeSelect(partitionNonCopy, visitor, ctx);
        resNonCopy.Should().BeFalse();
    }
}


