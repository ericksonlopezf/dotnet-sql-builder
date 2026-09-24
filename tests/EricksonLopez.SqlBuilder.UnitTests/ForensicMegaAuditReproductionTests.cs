// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Exceptions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class EntityWithReservedKeywords : ISqlEntity
{
    public int Order { get; set; }
    public string User { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public int Limit { get; set; }
    public string Offset { get; set; } = string.Empty;

    public string GetTableName() => "order_table";
    public string[] GetColumnNames() => new[] { "order", "user", "group", "limit", "offset" };
    public object?[] GetValues() => new object?[] { Order, User, Group, Limit, Offset };
    public string[] GetAllColumnNames() => GetColumnNames();
    public object?[] GetAllValues() => GetValues();
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
    {
        { "Order", "order" },
        { "User", "user" },
        { "Group", "group" },
        { "Limit", "limit" },
        { "Offset", "offset" }
    };
    public string[] GetIndexedColumns() => Array.Empty<string>();
}

public class ForensicMegaAuditReproductionTests
{
    private static readonly PostgreSqlCompiler PgCompiler = new();
    private static readonly SqlServerCompiler MsCompiler = new();

    [Fact]
    public void BUG_01_WindowFunction_WhenSelected_ShouldEmitSelectKeyword()
    {
        // Bug: If a query selects only a window function, the compiler omits "SELECT "
        var query = Sql.From<DummyEntity>()
            .Select(Window.RowNumber<DummyEntity>().OrderBy(e => e.Id).As("rn"));

        var result = PgCompiler.Compile(query);

        // FORENSIC EVIDENCE: Does it start with SELECT?
        // Observed behavior before fix: "ROW_NUMBER() OVER(ORDER BY \"id\" ASC) AS \"rn\" FROM \"dummy_entity\"" (Missing SELECT keyword!)
        result.Sql.TrimStart().Should().StartWith("SELECT ");
    }

    [Fact]
    public void BUG_02_WindowFunction_CombinedWithColumns_ShouldNotDropColumns()
    {
        // Bug: selectNodes[selectNodes.Count - 1] drops previous column projections
        var query = Sql.From<DummyEntity>()
            .Select("id", "name")
            .Select(Window.RowNumber<DummyEntity>().OrderBy(e => e.Id).As("rn"));

        var result = PgCompiler.Compile(query);

        // FORENSIC EVIDENCE: id and name must be present in the SELECT projection
        result.Sql.Should().Contain("\"id\"");
        result.Sql.Should().Contain("\"name\"");
        result.Sql.Should().Contain("ROW_NUMBER()");
    }

    [Fact]
    public void BUG_03_MultipleSelectCalls_ShouldNotSilentlyDropEarlierColumns()
    {
        // Bug: Chaining .Select("id").Select("name") drops "id"
        var query = Sql.From<DummyEntity>()
            .Select("id")
            .Select("name");

        var result = PgCompiler.Compile(query);

        // FORENSIC EVIDENCE: Both id and name should be selected
        result.Sql.Should().Contain("\"id\"");
        result.Sql.Should().Contain("\"name\"");
    }

    [Fact]
    public void BUG_04_ExpressionSelect_WithReservedKeywords_ShouldBeEscapedInPostgreSql()
    {
        // Bug: ExpressionSelectNode does not escape column names: string.Join(", ", cols) without Escape(col)
        var query = Sql.From<EntityWithReservedKeywords>()
            .Select(x => new { x.Order, x.User });

        var result = PgCompiler.Compile(query);

        // In PostgreSQL, "order" and "user" are keywords and must be quoted as "order", "user"
        result.Sql.Should().Contain("\"order\"");
        result.Sql.Should().Contain("\"user\"");
    }

    [Fact]
    public void BUG_05_ExpressionWhere_WithReservedKeywords_ShouldBeEscapedInPostgreSql()
    {
        // Bug: SqlCompilerVisitor passes null escapeFunc to SqlExpressionVisitor, emitting unquoted member names
        var query = Sql.From<EntityWithReservedKeywords>()
            .Where(x => x.Order > 10 && x.User == "admin");

        var result = PgCompiler.Compile(query);

        // In PostgreSQL, WHERE ("order" > @p0) AND ("user" = @p1)
        result.Sql.Should().Contain("\"order\"");
        result.Sql.Should().Contain("\"user\"");
    }

    [Fact]
    public void REDTEAM_01_PostgreSql_CopyNode_FormatInjection_ShouldBeSanitized()
    {
        // Bug: copyNode.Format is appended directly: WITH (FORMAT {copyNode.Format})
        var maliciousFormat = "CSV); DROP TABLE dummy; --";
        var copyNode = new CopyNode("dummy", new[] { "id" }, "STDIN", maliciousFormat);
        var astQuery = Sql.From<DummyEntity>().AddNode(copyNode);

        // FORENSIC VERIFICATION: Must reject malicious format with ArgumentException
        Action act = () => PgCompiler.Compile(astQuery);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AUDIT_DX_SqlEntityCache_ExceptionMessage_RefersToNonExistentMethod()
    {
        // SqlEntityCache<T> exception message tells users:
        // "To use unannotated POCOs, use Sql.From<T>(\"tableName\")."
        // But Sql.From<T>(string) doesn't exist on Sql!
        var exception = Assert.Throws<TypeInitializationException>(() =>
        {
            var _ = Sql.From<UnannotatedPocoClass>();
        });

        exception.InnerException.Should().NotBeNull();
        exception.InnerException!.Message.Should().Contain("Sql.From<T>");
    }

    [Fact]
    public void AUDIT_ROB_04_LimitOffsetNode_NegativeLimitOrOffset_ThrowsArgumentOutOfRangeException()
    {
        Action negativeLimit = () => new LimitOffsetNode(-1, 0);
        negativeLimit.Should().Throw<ArgumentOutOfRangeException>();

        Action negativeOffset = () => new LimitOffsetNode(10, -5);
        negativeOffset.Should().Throw<ArgumentOutOfRangeException>();

        var valid = new LimitOffsetNode(10, 20);
        valid.Limit.Should().Be(10);
        valid.Offset.Should().Be(20);

        var (l, o) = valid;
        l.Should().Be(10);
        o.Should().Be(20);
    }

    [Fact]
    public void AUDIT_ERR_01_DomainExceptionHierarchy_ShouldBePolymorphicallyCatchable()
    {
        var syntaxEx = new SqlSyntaxException("Invalid syntax token");
        var safetyEx = new SqlSafetyException("Unconstrained operation blocked");
        var valEx = new SqlValidationException("Validation error");

        syntaxEx.Should().BeAssignableTo<SqlBuilderException>();
        safetyEx.Should().BeAssignableTo<SqlBuilderException>();
        valEx.Should().BeAssignableTo<SqlBuilderException>();

        syntaxEx.Message.Should().Be("Invalid syntax token");
        safetyEx.Message.Should().Be("Unconstrained operation blocked");
        valEx.Message.Should().Be("Validation error");
    }

    [Fact]
    public void AUDIT_SEC_05_DeleteQuery_WhereAll_ExplicitIntent_GeneratesValidDelete()
    {
        var delete = Sql.Delete<DummyEntity>().WhereAll();
        var sql = PgCompiler.Compile((IAstQuery)delete).Sql;
        sql.Should().Be("DELETE FROM \"dummy_entity\"");
    }

    [Fact]
    public void AUDIT_SEC_05_UpdateQuery_WhereAll_ExplicitIntent_GeneratesValidUpdate()
    {
        var update = Sql.Update<DummyEntity>()
            .Set(x => x.Id, 42)
            .WhereAll();
        var sql = PgCompiler.Compile((IAstQuery)update).Sql;
        sql.Should().Be("UPDATE \"dummy_entity\" SET \"id\" = @p0");
    }

    private class UnannotatedPocoClass
    {
        public int Id { get; set; }
    }
}

