// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using Xunit;
using NSubstitute;

namespace EricksonLopez.SqlBuilder.MariaDb.Tests;

/// <summary>
/// Verifies MariaDB-specific SQL compilation, focusing on the differences from
/// the MySQL dialect (primarily <c>RETURNING</c> support) and ensuring all
/// inherited MySQL behaviors continue to work correctly.
/// </summary>
public class MariaDbCompilerTests
{
    private readonly MariaDbCompiler _compiler = new();

    // ─── RETURNING (key MariaDB differentiator) ───────────────────────────────

    [Fact]
    public void Compile_ReturningNode_WithColumns_GeneratesReturningClause()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", Array.Empty<string>()),
            new ValuesNode(new[] { new object[] { "Alice", 30 } }),
            new ReturningNode(new[] { "id" })
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().EndWith("RETURNING `id`");
        result.Sql.Should().NotContain("is not natively supported");
    }

    [Fact]
    public void Compile_ReturningNode_WithMultipleColumns_GeneratesCommaList()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("orders", Array.Empty<string>()),
            new ValuesNode(new[] { new object[] { 1, "item" } }),
            new ReturningNode(new[] { "id", "created_at", "total" })
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().EndWith("RETURNING `id`, `created_at`, `total`");
    }

    [Fact]
    public void Visit_ReturningNode_WithColumns_AppendsTrailingSpace()
    {
        var context = new CompilationContext(new ParameterManager());
        var visitor = _compiler.CreateVisitor(context);
        var node = new ReturningNode(new[] { "id" });

        visitor.Visit(node);

        context.Sql.ToString().Should().Be("RETURNING `id` ");
    }

    [Fact]
    public void Compile_ReturningNode_WithNoColumns_GeneratesReturningStar()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", Array.Empty<string>()),
            new ReturningNode(Array.Empty<string>())
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().EndWith("RETURNING *");
    }

    [Fact]
    public void Compile_ReturningNode_WithNullColumns_GeneratesReturningStar()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", Array.Empty<string>()),
            new ReturningNode(null!)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().EndWith("RETURNING *");
    }

    [Fact]
    public void Compile_ReturningNode_DoesNotThrowNotSupportedException()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", Array.Empty<string>()),
            new ReturningNode(new[] { "id" })
        }.ToImmutableList());

        // MariaDB supports RETURNING natively — must NOT throw
        var act = () => _compiler.Compile((ISqlQuery)query);
        act.Should().NotThrow();
    }

    // ─── Inherited MySQL behavior: identifier escaping ────────────────────────

    [Fact]
    public void Compile_SelectQuery_EscapesIdentifiersWithBackticks()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id", "name" }, false),
            new FromNode("users", "u")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT `id`, `name` FROM `users` AS `u`");
    }

    [Fact]
    public void Compile_SelectQuery_WithDistinct_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(new[] { "id", "name" }, true),
            new FromNode("users", "u"),
            new LimitOffsetNode(10, 20)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT DISTINCT `id`, `name` FROM `users` AS `u` LIMIT 10 OFFSET 20");
        result.Parameters.Should().BeEmpty();
    }

    [Fact]
    public void Compile_InsertQuery_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", Array.Empty<string>()),
            new ValuesNode(new[] { new object[] { 1, "test" } })
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("INSERT INTO `users` VALUES (@p0, @p1)");
        result.Parameters["p0"].Should().Be(1);
        result.Parameters["p1"].Should().Be("test");
    }

    [Fact]
    public void Compile_UpdateQuery_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("users")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("UPDATE `users`");
    }

    [Fact]
    public void Compile_DeleteQuery_BuildsCorrectSql()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("users")
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("DELETE FROM `users`");
    }

    // ─── Inherited MySQL behavior: LIMIT/OFFSET ───────────────────────────────

    [Fact]
    public void Compile_LimitOnly_EmitsLimit()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new FromNode("products"),
            new LimitOffsetNode(10, null)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT * FROM `products` LIMIT 10");
    }

    [Fact]
    public void Compile_OffsetOnly_EmitsMaxLimitWithOffset()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new FromNode("products"),
            new LimitOffsetNode(null, 5)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT * FROM `products` LIMIT 18446744073709551615 OFFSET 5");
    }

    [Fact]
    public void Compile_LimitAndOffset_EmitsLimitOffset()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new FromNode("products"),
            new LimitOffsetNode(10, 5)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT * FROM `products` LIMIT 10 OFFSET 5");
    }

    // ─── Inherited MySQL behavior: WHERE ──────────────────────────────────────

    [Fact]
    public void Compile_WithWhereClauses_BuildsCorrectSql()
    {
        Expression<Func<TestEntity, bool>> expr = x => x.Id == 1;

        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new FromNode("users", null),
            new RawWhereNode("status = 1", null, false),
            new ExpressionWhereNode(expr.Body, true)
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);

        result.Sql.Trim().Should().Be("SELECT * FROM `users` WHERE status = 1 OR (id = @p0)");
        result.Parameters["p0"].Should().Be(1);
    }

    // ─── Inherited MySQL behavior: ON DUPLICATE KEY UPDATE ────────────────────

    [Fact]
    public void Compile_OnConflictNode_DoNothing_EmitsInsertIgnoreTrick()
    {
        var query = new DummyMariaDbQuery();
        query.AddNode(new InsertNode("users", new[] { "id", "name" }));
        query.AddNode(new ValuesNode(new[] { new object[] { 1, "Test" } }));
        query.AddNode(new OnConflictNode(Array.Empty<string>(), null, null, null) { UpdateAction = "DO NOTHING" });

        var result = query.Build(_compiler);

        result.Sql.TrimEnd().Should().Be("INSERT INTO `users` (`id`, `name`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `id` = `id`");
    }

    // ─── Inherited MySQL behavior: DELETE with JOIN ───────────────────────────

    [Fact]
    public void Compile_DeleteWithJoin_EmitsMultiTableDelete()
    {
        var query = new DummyMariaDbQuery();
        query.AddNode(new DeleteNode("users"));
        query.AddNode(new JoinNode(JoinType.Inner, "roles", null, "roles.id = users.role_id"));

        var result = query.Build(_compiler);

        result.Sql.Trim().Should().Be("DELETE `users` FROM `users` INNER JOIN `roles` ON roles.id = users.role_id");
    }

    // ─── Inherited MySQL behavior: UPDATE with JOIN ───────────────────────────

    [Fact]
    public void Compile_UpdateWithJoin_EmitsJoinBeforeSet()
    {
        var query = new DummyMariaDbQuery();
        query.AddNode(new UpdateNode("users"));
        query.AddNode(new JoinNode(JoinType.Inner, "roles", "r", "r.id = users.role_id"));
        query.AddNode(new SetNode("name", "test"));

        var result = query.Build(_compiler);

        result.Sql.Trim().Should().Be("UPDATE `users` INNER JOIN `roles` AS `r` ON r.id = users.role_id SET `name` = @p0");
    }

    // ─── Inherited MySQL behavior: WINDOW FUNCTION FILTER not supported ────────

    [Fact]
    public void Compile_WindowFunction_WithFilter_ThrowsNotSupportedException()
    {
        var node = new WindowFunctionNode(
            "SUM", "Amount", null, null,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(),
            "sum_val", FilterRaw: "Status = 'Active'");

        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[] { new FromNode("users"), node }.ToImmutableList());

        Action act = () => _compiler.Compile((ISqlQuery)query);

        act.Should().Throw<NotSupportedException>()
           .WithMessage("*MySQL does not support the FILTER (WHERE ...) clause on window functions*");
    }
}

/// <summary>
/// Helper IAstQuery implementation for building multi-node queries in tests.
/// </summary>
public class DummyMariaDbQuery : IAstQuery
{
    public string? Tag => null;
    private readonly List<ISqlNode> _nodes = new();

    public IReadOnlyList<ISqlNode> Nodes => _nodes;

    public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);

    public void CompileTo(ISqlCompiler compiler, ISqlVisitor visitor)
    {
        foreach (var node in Nodes)
        {
            node.Accept(visitor);
        }
    }

    public DummyMariaDbQuery AddNode(ISqlNode node)
    {
        _nodes.Add(node);
        return this;
    }
}
