// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using Xunit;

namespace EricksonLopez.SqlBuilder.MySql.UnitTests;

public class MySqlCompilerTestsExtended
{
    private class FakeQuery : EricksonLopez.SqlBuilder.Abstractions.IAstQuery
    {
        public string? Tag => null;
        private readonly EricksonLopez.SqlBuilder.Abstractions.ISqlNode[] _nodes;
        public FakeQuery(params EricksonLopez.SqlBuilder.Abstractions.ISqlNode[] nodes) => _nodes = nodes;
        public IReadOnlyList<EricksonLopez.SqlBuilder.Abstractions.ISqlNode> Nodes => _nodes;
        public EricksonLopez.SqlBuilder.Abstractions.SqlResult Build(EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler compiler) => compiler.Compile(this);
        public void CompileTo(EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler compiler, EricksonLopez.SqlBuilder.Abstractions.ISqlVisitor visitor)
        {
            foreach (var node in Nodes)
            {
                node.Accept(visitor);
            }
        }
    }

    private MySqlCompiler _compiler;

    public MySqlCompilerTestsExtended()
    {
        _compiler = new MySqlCompiler();
    }

    [Fact]
    public void Visit_ReturningNode_ThrowsNotSupportedException()
    {
        var node = new ReturningNode(new string[] { "id" });
        var insertNode = new InsertNode("users", new[] { "id" });
        Action act = () => _compiler.Compile(new FakeQuery(insertNode, node));
        act.Should().Throw<NotSupportedException>().WithMessage("RETURNING clause is not natively supported in MySQL 8.x. Use LAST_INSERT_ID() for INSERT, or execute a SELECT after your DML statement. If you are using MariaDB 10.5+, use Sql.Raw() with RETURNING.");
    }

    [Fact]
    public void Visit_OnConflictNode_DoNothing_EmitsInsertIgnoreTrick()
    {
        var query = new DummyQuery();
        query.AddNode(new InsertNode("users", new[] { "id", "name" }));
        query.AddNode(new ValuesNode(new [] { new object[] { 1, "Test" } }));
        query.AddNode(new OnConflictNode(new string[0], null, null, null) { UpdateAction = "DO NOTHING" });
        var result = query.Build(_compiler);
        result.Sql.TrimEnd().Should().Be("INSERT INTO `users` (`id`, `name`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `id` = `id`");
    }

    [Fact]
    public void Visit_OnConflictNode_WithRawAction_EmitsRawAction()
    {
        var query = new DummyQuery();
        query.AddNode(new InsertNode("users", new[] { "id", "name" }));
        query.AddNode(new ValuesNode(new [] { new object[] { 1, "Test" } }));
        query.AddNode(new OnConflictNode(new string[0], null, null, null) { UpdateAction = "`active` = 1" });
        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("INSERT INTO `users` (`id`, `name`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `active` = 1");
    }

    [Fact]
    public void Visit_OnConflictNode_WithLambdaMember_EmitsUpdate()
    {
        // This simulates a lambda expression
        Expression<Func<TestEntity, object>> expr = c => c.Name;
        var node = new OnConflictNode(new string[0], null, expr, null);
        
        var query = new DummyQuery();
        query.AddNode(new InsertNode("users", new[] { "id", "name" }));
        query.AddNode(new ValuesNode(new [] { new object[] { 1, "Test" } }));
        query.AddNode(node);
        
        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("INSERT INTO `users` (`id`, `name`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `name` = VALUES(`name`)");
    }

    [Fact]
    public void Visit_OnConflictNode_WithLambdaNew_EmitsMultipleUpdates()
    {
        Expression<Func<TestEntity, object>> expr = c => new { c.Id, c.Name };
        var node = new OnConflictNode(new string[0], null, expr, null);
        
        var query = new DummyQuery();
        query.AddNode(new InsertNode("users", new[] { "id", "name" }));
        query.AddNode(new ValuesNode(new [] { new object[] { 1, "Test" } }));
        query.AddNode(node);
        
        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("INSERT INTO `users` (`id`, `name`) VALUES (@p0, @p1) ON DUPLICATE KEY UPDATE `id` = VALUES(`id`), `name` = VALUES(`name`)");
    }

    [Fact]
    public void Visit_UpdateNode_WithJoin_EmitsJoinBeforeSet()
    {
        var query = new DummyQuery();
        query.AddNode(new UpdateNode("users"));
        query.AddNode(new JoinNode(JoinType.Inner, "roles", "r", "r.id = users.role_id"));
        query.AddNode(new SetNode("name", "test"));

        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("UPDATE `users` INNER JOIN `roles` AS `r` ON r.id = users.role_id SET `name` = @p0");
        result.Parameters["p0"].Should().Be("test");
    }

    [Fact]
    public void Visit_UpdateNode_WithMultipleSets_EmitsCommaSeparatedSets()
    {
        var query = new DummyQuery();
        query.AddNode(new UpdateNode("users"));
        query.AddNode(new SetNode("name", "test"));
        query.AddNode(new SetNode("age", 25));
        
        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("UPDATE `users` SET `name` = @p0, `age` = @p1");
    }
    
    [Fact]
    public void Visit_UpdateNode_WithoutUpdateNodeButWithSet_DoesNotEmitUpdate()
    {
        var query = new DummyQuery();
        query.AddNode(new FromNode("users"));
        query.AddNode(new SetNode("name", "test"));
        
        var result = query.Build(_compiler);
        // Uses base CompileUpdate, if there is no UpdateNode it throws or returns a SELECT? Wait. 
        // If it compiles a DummyQuery with FromNode and SetNode, but no select node, standard compiler adds SELECT *
        result.Sql.Trim().Should().Be("SELECT * FROM `users`");
    }

    [Fact]
    public void Visit_DeleteNode_WithJoin_EmitsMultiTableDelete()
    {
        var query = new DummyQuery();
        query.AddNode(new DeleteNode("users"));
        query.AddNode(new JoinNode(JoinType.Inner, "roles", null, "roles.id = users.role_id"));

        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("DELETE `users` FROM `users` INNER JOIN `roles` ON roles.id = users.role_id");
    }

    [Fact]
    public void Visit_DeleteNode_SimpleDelete_EmitsSimpleDelete()
    {
        var query = new DummyQuery();
        query.AddNode(new DeleteNode("users"));
        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("DELETE FROM `users`");
    }

    [Fact]
    public void Visit_LimitOffsetNode_LimitOnly_EmitsLimit()
    {
        var query = new DummyQuery();
        query.AddNode(new FromNode("users"));
        query.AddNode(new LimitOffsetNode(10, null));
        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("SELECT * FROM `users` LIMIT 10");
    }

    [Fact]
    public void Visit_LimitOffsetNode_LimitAndOffset_EmitsLimitOffset()
    {
        var query = new DummyQuery();
        query.AddNode(new FromNode("users"));
        query.AddNode(new LimitOffsetNode(10, 5));
        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("SELECT * FROM `users` LIMIT 10 OFFSET 5");
    }

    [Fact]
    public void Visit_LimitOffsetNode_OffsetOnly_EmitsMaxLimitWithOffset()
    {
        var query = new DummyQuery();
        query.AddNode(new FromNode("users"));
        query.AddNode(new LimitOffsetNode(null, 5));
        var result = query.Build(_compiler);
        result.Sql.Trim().Should().Be("SELECT * FROM `users` LIMIT 18446744073709551615 OFFSET 5");
    }
    [Fact]
    public void Visit_OnConflictNode_WithUnsupportedLambda_ThrowsNotSupportedException()
    {
        Expression<Func<TestEntity, object>> expr = c => c.ToString();
        var node = new OnConflictNode(new string[0], null, expr, null);
        var query = new DummyQuery();
        query.AddNode(new InsertNode("users", new[] { "id", "name" }));
        query.AddNode(new ValuesNode(new [] { new object[] { 1, "Test" } }));
        query.AddNode(node);
        
        Action act = () => query.Build(_compiler);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("Unsupported lambda expression in ON DUPLICATE KEY UPDATE.");
    }

    [Fact]
    public void Visit_OnConflictNode_WithNullActionAndExpression_DoesNotThrowButEmitsIncompleteSql()
    {
        var node = new OnConflictNode(new string[0], null, null, null);
        var query = new DummyQuery();
        query.AddNode(new InsertNode("users", new[] { "id" }));
        query.AddNode(node);
        
        var result = query.Build(_compiler);
        result.Sql.TrimEnd().Should().Be("INSERT INTO `users` (`id`) ON DUPLICATE KEY UPDATE");
    }
    [Fact]
    public void Visit_OnConflictNode_WithNonLambdaExpression_DoesNotThrowButEmitsNothing()
    {
        var expr = System.Linq.Expressions.Expression.Constant(1);
        var node = new OnConflictNode(new string[0], null, expr, null);
        var query = new DummyQuery();
        query.AddNode(new InsertNode("users", new[] { "id" }));
        query.AddNode(node);
        
        var result = query.Build(_compiler);
        result.Sql.TrimEnd().Should().Be("INSERT INTO `users` (`id`) ON DUPLICATE KEY UPDATE");
    }

    [Fact]
    public void Compile_UpdateQuery_WithWhere_BuildsCorrectSql()
    {
        var query = new DummyQuery();
        query.AddNode(new UpdateNode("users"));
        query.AddNode(new SetNode("name", "New Name"));
        query.AddNode(new RawWhereNode("`id` = 1"));
        
        var result = query.Build(_compiler);
        result.Sql.TrimEnd().Should().Be("UPDATE `users` SET `name` = @p0 WHERE `id` = 1");
    }

    [Fact]
    public void Compile_UpdateQuery_WithoutWhere_BuildsCorrectSqlEndingWithSpace()
    {
        var query = new DummyQuery();
        query.AddNode(new UpdateNode("users"));
        query.AddNode(new SetNode("name", "New Name"));
        
        var result = query.Build(_compiler);
        result.Sql.TrimEnd().Should().Be("UPDATE `users` SET `name` = @p0");
    }

    [Fact]
    public void Compile_DeleteQuery_WithWhere_BuildsCorrectSql()
    {
        var query = new DummyQuery();
        query.AddNode(new DeleteNode("users"));
        query.AddNode(new RawWhereNode("`id` = 1"));
        
        var result = query.Build(_compiler);
        result.Sql.TrimEnd().Should().Be("DELETE FROM `users` WHERE `id` = 1");
    }

    [Fact]
    public void Visit_WindowFunctionNode_WithoutFilter_BuildsCorrectSql()
    {
        var node = new WindowFunctionNode("SUM", "amount", null, null, new[] { "dept" }, new[] { "salary" }, new[] { true }, "total");
        var query = new DummyQuery();
        query.AddNode(new SelectNode(new[] { "id" }, false));
        query.AddNode(node);

        var result = query.Build(_compiler);
        result.Sql.TrimEnd().Should().Contain("SUM(`amount`) OVER (PARTITION BY `dept` ORDER BY `salary` DESC) AS `total`");
    }

    [Fact]
    public void Visit_WindowFunctionNode_WithFilterExpression_ThrowsNotSupportedException()
    {
        var expr = System.Linq.Expressions.Expression.Lambda<Func<User, bool>>(
            System.Linq.Expressions.Expression.Constant(true),
            System.Linq.Expressions.Expression.Parameter(typeof(User), "x"));
        var node = new WindowFunctionNode("SUM", "amount", null, null, null, null, null, "total", FilterExpression: expr);
        var query = new DummyQuery();
        query.AddNode(node);

        var act = () => query.Build(_compiler);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("MySQL does not support the FILTER (WHERE ...) clause on window functions.*");
    }

    [Fact]
    public void Visit_WindowFunctionNode_WithFilterRaw_ThrowsNotSupportedException()
    {
        var node = new WindowFunctionNode("SUM", "amount", null, null, null, null, null, "total", FilterRaw: "amount > 0");
        var query = new DummyQuery();
        query.AddNode(node);

        var act = () => query.Build(_compiler);
        act.Should().Throw<NotSupportedException>()
            .WithMessage("MySQL does not support the FILTER (WHERE ...) clause on window functions.*");
    }
}

public class DummyQuery : EricksonLopez.SqlBuilder.Abstractions.IAstQuery
{
        public string? Tag => null;
    private readonly List<EricksonLopez.SqlBuilder.Abstractions.ISqlNode> _nodes = new List<EricksonLopez.SqlBuilder.Abstractions.ISqlNode>();
    
    public IReadOnlyList<EricksonLopez.SqlBuilder.Abstractions.ISqlNode> Nodes => _nodes;
    
    public EricksonLopez.SqlBuilder.Abstractions.SqlResult Build(EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler compiler) => compiler.Compile(this);
    public void CompileTo(EricksonLopez.SqlBuilder.Abstractions.ISqlCompiler compiler, EricksonLopez.SqlBuilder.Abstractions.ISqlVisitor visitor)
    {
        foreach (var node in Nodes)
        {
            node.Accept(visitor);
        }
    }
    
    public DummyQuery AddNode(EricksonLopez.SqlBuilder.Abstractions.ISqlNode node)
    {
        _nodes.Add(node);
        return this;
    }
}







