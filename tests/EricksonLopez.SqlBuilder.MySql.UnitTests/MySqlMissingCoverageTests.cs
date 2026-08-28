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
using EricksonLopez.SqlBuilder.Builders.Bulk;
using EricksonLopez.SqlBuilder.ColumnSelection;
using EricksonLopez.SqlBuilder.MySql;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.MySql.Tests;

public class MySqlMissingCoverageTests
{
    private readonly MySqlCompiler _compiler = new();

    public class MySqlTestEntity : IStaticEntityMetadata<MySqlTestEntity>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Age { get; set; }
        public long RowVersion { get; set; }

        public static string TableName => "users";
        public static int ColumnCount => 4;

        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[]
        {
            new ColumnMetadata(0, "Id", ColumnFlags.PrimaryKey),
            new ColumnMetadata(1, "Name", ColumnFlags.None),
            new ColumnMetadata(2, "Age", ColumnFlags.None),
            new ColumnMetadata(3, "RowVersion", ColumnFlags.Identity)
        };

        public static bool IsNull(MySqlTestEntity entity, int columnIndex) => columnIndex switch
        {
            1 => entity.Name is null,
            _ => false
        };

        public static bool IsDefault(MySqlTestEntity entity, int columnIndex) => false;
        public static bool AreEqual(MySqlTestEntity entity, MySqlTestEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => columnIndex switch
        {
            0 => "Id",
            1 => "Name",
            2 => "Age",
            3 => "RowVersion",
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };

        public static string BindParameter(MySqlTestEntity entity, int columnIndex, IParameterManager parameters) => columnIndex switch
        {
            0 => parameters.Add(entity.Id),
            1 => parameters.Add(entity.Name),
            2 => parameters.Add(entity.Age),
            3 => parameters.Add(entity.RowVersion),
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };

        public static void ExtractColumnArrays(ReadOnlySpan<MySqlTestEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters)
        {
        }

        public static MySqlTestEntity FromReader(System.Data.IDataReader reader) => new MySqlTestEntity();
        public static Func<System.Data.IDataReader, MySqlTestEntity> GetReaderParser() => (r) => new MySqlTestEntity();
    }

    [Fact]
    public void MySqlBatchStrategy_GetActiveColumnIndices_Options()
    {
        var cols = MySqlTestEntity.GetColumns();

        var indicesNoIdentity = MySqlBatchStrategy.GetActiveColumnIndices(cols, new BulkOptions { ReturnIdentities = false });
        indicesNoIdentity.Should().Equal(0, 1, 2);

        var indicesWithIdentity = MySqlBatchStrategy.GetActiveColumnIndices(cols, new BulkOptions { ReturnIdentities = true });
        indicesWithIdentity.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void MySqlBatchStrategy_BuildDataTable_PopulatesRowsAndNulls()
    {
        var cols = MySqlTestEntity.GetColumns();
        var activeIndices = new[] { 0, 1, 2 };
        var entities = new[]
        {
            new MySqlTestEntity { Id = 1, Name = "Alice", Age = 30 },
            new MySqlTestEntity { Id = 2, Name = null, Age = 25 }
        };

        var dt = MySqlBatchStrategy.BuildDataTable(entities, cols, activeIndices);
        dt.Columns.Count.Should().Be(3);
        dt.Rows.Count.Should().Be(2);
        dt.Rows[0]["Name"].Should().Be("Alice");
        dt.Rows[1]["Name"].Should().Be(DBNull.Value);
    }

    [Fact]
    public void MySqlBatchStrategy_NonMySqlConnection_Throws()
    {
        var mockConn = Substitute.For<IDbConnection>();
        var act = () => MySqlBatchStrategy.BulkInsertAsync(mockConn, new[] { new MySqlTestEntity() });
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*requires a MySqlConnection*");
    }

    [Fact]
    public void MySqlBulkMergeStrategy_BuildUpsertStatement_And_Slice()
    {
        var cols = MySqlTestEntity.GetColumns();
        var batch = new[]
        {
            new MySqlTestEntity { Id = 1, Name = "Alice", Age = 30 },
            new MySqlTestEntity { Id = 2, Name = null, Age = 40 }
        };

        var (sql, parameters) = MySqlBulkMergeStrategy.BuildUpsertStatement(batch, cols);
        var normSql = sql.Replace("\r\n", "\n");
        normSql.Should().Contain("INSERT INTO `users`");
        normSql.Should().Contain("`Id`, `Name`, `Age`) VALUES");
        normSql.Should().Contain("ON DUPLICATE KEY UPDATE");
        normSql.Should().Contain("`Name` = VALUES(`Name`)");
        normSql.Should().Contain("`Age` = VALUES(`Age`)");
        parameters.Should().HaveCount(6);

        var list = new List<int> { 10, 20, 30, 40, 50 };
        var sliced = MySqlBulkMergeStrategy.Slice(list, 1, 4);
        sliced.Should().Equal(20, 30, 40);
    }

    [Fact]
    public void MySqlBulkMergeStrategy_NonMySqlConnection_Throws()
    {
        var mockConn = Substitute.For<IDbConnection>();
        var act = () => MySqlBulkMergeStrategy.BulkMergeAsync(mockConn, new[] { new MySqlTestEntity() });
        act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*requires a MySqlConnection*");
    }

    [Fact]
    public void MySqlVisitor_OrderByNode_NullsPosition()
    {
        Expression<Func<MySqlTestEntity, string?>> expr = e => e.Name;

        // Nulls First, Descending
        var nodeFirst = new OrderByNode(expr, true, NullsPosition.First);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), nodeFirst }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT * FROM `users` ORDER BY CASE WHEN `name` IS NULL THEN 0 ELSE 1 END, `name` DESC");

        // Nulls Last, Ascending
        var nodeLast = new OrderByNode(expr, false, NullsPosition.Last);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new FromNode("users", null), nodeLast }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * FROM `users` ORDER BY CASE WHEN `name` IS NULL THEN 1 ELSE 0 END, `name`");
    }

    [Fact]
    public void MySqlVisitor_ReturningNode_ThrowsNotSupportedException()
    {
        var returningNode = new ReturningNode(new[] { "id" });
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "name" }), returningNode }.ToImmutableList());
        var act = () => _compiler.Compile((ISqlQuery)q);
        act.Should().Throw<NotSupportedException>().WithMessage("*RETURNING clause is not natively supported in MySQL*");
    }

    [Fact]
    public void MySqlVisitor_WindowFunctionNode_FilterClause_ThrowsNotSupportedException()
    {
        var winNode1 = new WindowFunctionNode("ROW_NUMBER", null, null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(), "rn", null, "x > 10", null);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), winNode1 }.ToImmutableList());
        var act1 = () => _compiler.Compile((ISqlQuery)q1);
        act1.Should().Throw<NotSupportedException>().WithMessage("*MySQL does not support the FILTER*");

        Expression<Func<MySqlTestEntity, bool>> filterExpr = e => e.Age > 18;
        var winNode2 = new WindowFunctionNode("ROW_NUMBER", null, null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(), "rn", filterExpr, null, null);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), winNode2 }.ToImmutableList());
        var act2 = () => _compiler.Compile((ISqlQuery)q2);
        act2.Should().Throw<NotSupportedException>().WithMessage("*MySQL does not support the FILTER*");
    }

    [Fact]
    public void MySqlVisitor_OnConflictNode_Variants()
    {
        // DO NOTHING -> ON DUPLICATE KEY UPDATE `id` = `id`
        var nodeNothing = new OnConflictNode(Array.Empty<string>(), "DO NOTHING", null, null);
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "id" }), nodeNothing }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().EndWith("ON DUPLICATE KEY UPDATE `id` = `id`");

        // MemberExpression single column
        Expression<Func<MySqlTestEntity, string?>> singleExpr = e => e.Name;
        var nodeSingle = new OnConflictNode(Array.Empty<string>(), null, singleExpr, null);
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "id" }), nodeSingle }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().EndWith("ON DUPLICATE KEY UPDATE `name` = VALUES(`name`)");

        // Unsupported lambda expression
        Expression<Func<MySqlTestEntity, int>> unsupportedExpr = e => e.Age + 1;
        var nodeUnsupported = new OnConflictNode(Array.Empty<string>(), null, unsupportedExpr, null);
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "id" }), nodeUnsupported }.ToImmutableList());
        var act3 = () => _compiler.Compile((ISqlQuery)q3);
        act3.Should().Throw<NotSupportedException>().WithMessage("*Unsupported lambda expression*");

        // Raw UpdateAction with parameters
        var nodeRaw = new OnConflictNode(Array.Empty<string>(), "`name` = @name_val", null, new object[] { "Bob" });
        var q4 = Substitute.For<IAstQuery>();
        q4.Nodes.Returns(new ISqlNode[] { new InsertNode("users", new[] { "id" }), nodeRaw }.ToImmutableList());
        var res4 = _compiler.Compile((ISqlQuery)q4);
        res4.Sql.Trim().Should().EndWith("ON DUPLICATE KEY UPDATE `name` = @name_val");
        res4.Parameters.Should().ContainKey("p0");
    }

    [Fact]
    public void MySqlCompiler_EscapeIdentifier_Overloads()
    {
        _compiler.EscapeIdentifier("column_name").Should().Be("`column_name`");

        var sb = new StringBuilder();
        _compiler.EscapeIdentifier(sb, "my_col".AsSpan());
        sb.ToString().Should().Be("`my_col`");
    }

    [Fact]
    public void MySqlCompiler_CompileUpdate_WithJoins_And_ConcurrencyTokens()
    {
        // Auto-increment concurrency token with NO existing where clause
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new JoinNode(JoinType.Inner, "profiles", "p", "p.user_id = users.id", null),
            new SetNode("name", "Alice"),
            new ConcurrencyTokenNode("version", 1, null, true)
        }.ToImmutableList());
        var res1 = _compiler.Compile((ISqlQuery)q1);
        res1.Sql.Trim().Should().Be("UPDATE `users` INNER JOIN `profiles` AS `p` ON p.user_id = users.id SET `name` = @p0, `version` = `version` + 1 WHERE `version` = @p1");

        // Explicit new value concurrency token WITH existing where clause
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new SetNode("name", "Alice"),
            new RawWhereNode("is_active = 1", null, false),
            new ConcurrencyTokenNode("row_guid", "old-guid", "new-guid", false)
        }.ToImmutableList());
        var res2 = _compiler.Compile((ISqlQuery)q2);
        res2.Sql.Trim().Should().Be("UPDATE `users` SET `name` = @p0, `row_guid` = @p1 WHERE is_active = 1 AND `row_guid` = @p2");
    }

    [Fact]
    public void MySqlCompiler_CompileDelete_WithAndWithoutJoins()
    {
        // Delete with join
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("users"),
            new JoinNode(JoinType.Inner, "deleted_users", "d", "d.id = users.id", null),
            new RawWhereNode("d.deleted = 1", null, false)
        }.ToImmutableList());
        var res1 = _compiler.Compile((ISqlQuery)q1);
        res1.Sql.Trim().Should().Be("DELETE `users` FROM `users` INNER JOIN `deleted_users` AS `d` ON d.id = users.id WHERE d.deleted = 1");

        // Delete without join
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("users"),
            new RawWhereNode("id = 5", null, false)
        }.ToImmutableList());
        var res2 = _compiler.Compile((ISqlQuery)q2);
        res2.Sql.Trim().Should().Be("DELETE FROM `users` WHERE id = 5");
    }

    [Fact]
    public void MySqlCompiler_CompileLimitOffset_Variants()
    {
        // Limit node null
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT *");

        // Limit only
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new LimitOffsetNode(10, null) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * LIMIT 10");

        // Offset only (emulates MySQL large limit requirement)
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[] { new SelectNode(Array.Empty<string>(), false), new LimitOffsetNode(null, 25) }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q3).Sql.Trim().Should().Be("SELECT * LIMIT 18446744073709551615 OFFSET 25");
    }

    [Fact]
    public void MySqlRenderer_UnsupportedBulkMethods_Throw()
    {
        var renderer = new MySqlRenderer(_compiler);
        var entities = new[] { new MySqlTestEntity() };
        var rules = new List<IColumnSelectionRule<MySqlTestEntity>>();

        var actInsert = () => renderer.RenderBulkInsert(entities, rules, 10);
        actInsert.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Insert for MySQL should use MySqlBatchStrategy*");

        var actUpdate = () => renderer.RenderBulkUpdate(entities, rules, 10);
        actUpdate.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Update is not natively implemented for MySQL*");

        var actMerge = () => renderer.RenderBulkMerge(entities, rules, 10);
        actMerge.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Merge is not supported for MySQL*");

        var actUpsert = () => renderer.RenderBulkUpsert(entities, rules, 10);
        actUpsert.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Upsert is not yet implemented for MySQL*");

        var actInsertIgnore = () => renderer.RenderBulkInsertIgnore(entities, rules, 10);
        actInsertIgnore.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Insert Ignore is not yet implemented for MySQL*");
    }

    [Fact]
    public void MySqlBulkMergeStrategy_BuildUpsertStatement_MultipleRowsAndColumns()
    {
        var cols = MySqlTestEntity.GetColumns();
        var batch = new List<MySqlTestEntity>
        {
            new MySqlTestEntity { Id = 1, Name = "Alice", Age = 30 },
            new MySqlTestEntity { Id = 2, Name = null, Age = 25 }
        };

        var (sql, parameters) = MySqlBulkMergeStrategy.BuildUpsertStatement(batch, cols);
        var normSql = sql.Replace("\r\n", "\n");
        normSql.Should().Contain("INSERT INTO `users` (\n`Id`, `Name`, `Age`) VALUES\n(@p_0_0, @p_0_1, @p_0_2)\n,(@p_1_0, @p_1_1, @p_1_2)\nON DUPLICATE KEY UPDATE\n`Name` = VALUES(`Name`)\n,`Age` = VALUES(`Age`)");
        parameters.Should().HaveCount(6);
        parameters[0].Should().Be(("@p_0_0", 1));
        parameters[1].Should().Be(("@p_0_1", "Alice"));
        parameters[2].Should().Be(("@p_0_2", 30));
        parameters[3].Should().Be(("@p_1_0", 2));
        parameters[4].Should().Be(("@p_1_1", null));
        parameters[5].Should().Be(("@p_1_2", 25));
    }

    [Fact]
    public void MySqlBulkMergeStrategy_Slice_ReturnsSublist()
    {
        var source = new List<int> { 10, 20, 30, 40, 50 };
        var sliced = MySqlBulkMergeStrategy.Slice(source, 1, 4);
        sliced.Capacity.Should().Be(3);
        sliced.Should().Equal(20, 30, 40);
    }

    [Fact]
    public void MySqlCompiler_CompileUpdate_MultipleConcurrencyTokens_And_AutoIncrement()
    {
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new SetNode("name", "Bob"),
            new ConcurrencyTokenNode("version", 1, null, true),
            new ConcurrencyTokenNode("token2", "old", "new", false)
        }.ToImmutableList());

        var res = _compiler.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Be("UPDATE `users` SET `name` = @p0, `version` = `version` + 1, `token2` = @p1 WHERE `version` = @p2 AND `token2` = @p3");
    }

    [Fact]
    public void MySqlCompiler_CompileUpdate_ConcurrencyToken_NotAutoIncrement_NullNewValue()
    {
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users"),
            new ConcurrencyTokenNode("version", 1, null, false)
        }.ToImmutableList());

        var res = _compiler.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Be("UPDATE `users` SET `version` = @p0 WHERE `version` = @p1");
    }

    [Fact]
    public void MySqlCompiler_RenderInsert_InvokesAotRenderer()
    {
        var entity = new MySqlTestEntity { Id = 1, Name = "Alice", Age = 30 };
        var res = _compiler.RenderInsert(entity, stackalloc bool[] { true, true, true, false });
        res.Sql.Should().Contain("INSERT INTO `users`");
    }

    [Fact]
    public void MySqlVisitor_VisitOrderByNode_UnaryExpressionMember()
    {
        Expression<Func<MySqlTestEntity, object>> orderExpr = x => x.Age;
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", null),
            new OrderByNode(orderExpr, true)
        }.ToImmutableList());

        var res = _compiler.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Be("SELECT * FROM `users` ORDER BY `age` DESC");
    }

    [Fact]
    public void MySqlVisitor_VisitOrderByNode_NonMemberExpression()
    {
        Expression<Func<MySqlTestEntity, object>> orderExpr = x => 1;
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", null),
            new OrderByNode(orderExpr, false)
        }.ToImmutableList());

        var res = _compiler.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Be("SELECT * FROM `users` ORDER BY");
    }

    [Fact]
    public void MySqlVisitor_VisitOrderByNode_NullKeySelector()
    {
        var q = Substitute.For<IAstQuery>();
        q.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", null),
            new OrderByNode(null!, true)
        }.ToImmutableList());

        var res = _compiler.Compile((ISqlQuery)q);
        res.Sql.Trim().Should().Be("SELECT * FROM `users` ORDER BY  DESC");
    }
}

