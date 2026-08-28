// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Testing.Domain;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.SqlServer.Tests;

public class SqlServerCompilerMissingCoverageTests
{
    private readonly SqlServerCompiler _compiler = new();

    [Theory]
    [InlineData(ProviderCapability.None, true)]
    [InlineData(ProviderCapability.Apply, true)]
    [InlineData(ProviderCapability.Cte, true)]
    [InlineData(ProviderCapability.WindowFunctions, true)]
    [InlineData(ProviderCapability.Merge, true)]
    [InlineData(ProviderCapability.Apply | ProviderCapability.Cte, true)]
    [InlineData(ProviderCapability.Returning, false)]
    public void SupportsCapability_ValidatesDialectCapabilities(ProviderCapability capability, bool expected)
    {
        _compiler.SupportsCapability(capability).Should().Be(expected);
    }

    [Fact]
    public void EscapeIdentifier_FormatsSquareBrackets()
    {
        _compiler.EscapeIdentifier("users").Should().Be("[users]");

        var sb = new StringBuilder();
        _compiler.EscapeIdentifier(sb, "orders".AsSpan());
        sb.ToString().Should().Be("[orders]");
    }

    [Fact]
    public void CreateParameterManager_ReturnsSqlServerDefaults()
    {
        var pm = _compiler.CreateParameterManager();
        pm.Add("test").Should().Be("@p0");
    }

    [Fact]
    public void Visit_OnConflictNode_ThrowsNotSupportedException()
    {
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", new[] { "id", "name" }),
            new ValuesNode(new List<IReadOnlyList<object?>> { new object?[] { 1, "Alice" } }),
            new OnConflictNode(new[] { "id" }, "DO NOTHING", null, null)
        }.ToImmutableList());

        var act = () => _compiler.Compile((ISqlQuery)query);
        act.Should().Throw<NotSupportedException>().WithMessage("SQL Server does not support ON CONFLICT syntax. Use Sql.Raw() with a MERGE statement instead.");
    }

    [Fact]
    public void Visit_WindowFunctionNode_WithFilter_ThrowsNotSupportedException()
    {
        var winFuncExpr = new WindowFunctionNode("SUM", "salary", null, null, new[] { "dept" }, new[] { "salary" }, new[] { true }, "total", Expression.Constant(true), null, null);

        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            winFuncExpr
        }.ToImmutableList());

        var act = () => _compiler.Compile((ISqlQuery)query);
        act.Should().Throw<NotSupportedException>().WithMessage("*FILTER (WHERE ...)*");

        var winFuncRaw = new WindowFunctionNode("SUM", "salary", null, null, new[] { "dept" }, new[] { "salary" }, new[] { true }, "total", null, "salary > 0", null);
        var queryRaw = Substitute.For<IAstQuery>();
        queryRaw.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            winFuncRaw
        }.ToImmutableList());

        var actRaw = () => _compiler.Compile((ISqlQuery)queryRaw);
        actRaw.Should().Throw<NotSupportedException>().WithMessage("*FILTER (WHERE ...)*");
    }

    [Fact]
    public void Visit_WindowFunctionNode_WithoutFilter_CompilesSuccessfully()
    {
        var winFunc = new WindowFunctionNode("SUM", "salary", null, null, new[] { "dept" }, new[] { "salary" }, new[] { true }, "total", null, null, null);
        var query = Substitute.For<IAstQuery>();
        query.Nodes.Returns(new ISqlNode[]
        {
            winFunc
        }.ToImmutableList());

        var result = _compiler.Compile((ISqlQuery)query);
        result.Sql.Trim().Should().Be("SUM([salary]) OVER (PARTITION BY [dept] ORDER BY [salary] DESC) AS [total]");
    }

    [Fact]
    public void Visit_OrderByNode_DirectMemberAndNonMember_CompilesCorrectly()
    {
        // Direct member expression (string -> no boxing UnaryExpression)
        Expression<Func<User, string>> directExpr = u => u.FirstName;
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new OrderByNode(directExpr, false, NullsPosition.None)
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT * ORDER BY [first_name]");

        // Non-member expression (e.g. Constant)
        var constantLambda = Expression.Lambda(Expression.Constant(1));
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new OrderByNode(constantLambda, false, NullsPosition.None)
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * ORDER BY");

        // Non-lambda selector
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new OrderByNode(Expression.Constant(1), true, NullsPosition.None)
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q3).Sql.Trim().Should().Be("SELECT * ORDER BY  DESC");

        // Unary expression whose operand is not MemberExpression (e.g. unary minus on binary expression)
        Expression<Func<User, int>> negExpr = u => -(u.Id + 1);
        var q4 = Substitute.For<IAstQuery>();
        q4.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new OrderByNode(negExpr, false, NullsPosition.None)
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q4).Sql.Trim().Should().Be("SELECT * ORDER BY");
    }

    [Fact]
    public void CompileLimitOffset_Variants()
    {
        // Limit only -> OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new LimitOffsetNode(5, null)
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("SELECT * OFFSET 0 ROWS FETCH NEXT 5 ROWS ONLY");

        // Offset only -> OFFSET 10 ROWS
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new LimitOffsetNode(null, 10)
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("SELECT * OFFSET 10 ROWS");

        // Both -> OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[]
        {
            new SelectNode(Array.Empty<string>(), false),
            new LimitOffsetNode(5, 10)
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q3).Sql.Trim().Should().Be("SELECT * OFFSET 10 ROWS FETCH NEXT 5 ROWS ONLY");
    }

    [Fact]
    public void CompileInsert_Variants()
    {
        // InsertSelect priority over any trailing values nodes (kills Mutant 312)
        var subQuery = Substitute.For<IAstQuery>();
        subQuery.Nodes.Returns(new ISqlNode[] { new RawSelectNode("1, 'Admin'", null, false) }.ToImmutableList());

        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new InsertSelectNode("users", new[] { "id", "role" }, subQuery),
            new ValuesNode(new List<IReadOnlyList<object?>> { new object?[] { 99, "Ignored" } })
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("INSERT INTO [users] ([id], [role]) SELECT 1, 'Admin'");

        // Insert + Returning empty columns (INSERTED.*) + DefaultValues
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", Array.Empty<string>()),
            new ReturningNode(Array.Empty<string>()),
            new DefaultValuesNode()
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("INSERT INTO [users] OUTPUT INSERTED.* DEFAULT VALUES");

        // Insert + Returning specific columns + Values
        var q3 = Substitute.For<IAstQuery>();
        q3.Nodes.Returns(new ISqlNode[]
        {
            new InsertNode("users", new[] { "id", "name" }),
            new ReturningNode(new[] { "id", "name" }),
            new ValuesNode(new List<IReadOnlyList<object?>> { new object?[] { 1, "Alice" } })
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q3).Sql.Trim().Should().Be("INSERT INTO [users] ([id], [name]) OUTPUT INSERTED.[id], INSERTED.[name] VALUES (@p0, @p1)");

        // Insert with no trailing space before ReturningNode (kills Mutant 321 on non-empty buffer)
        var ctx = new CompilationContext(new ParameterManager());
        ctx.Sql.Append("INSERT INTO [users]");
        var visitor = _compiler.CreateVisitor(ctx);
        var insertNodes = new ISqlNode[]
        {
            new ReturningNode(Array.Empty<string>()),
            new DefaultValuesNode()
        };
        _compiler.CompileInsert(insertNodes, visitor, ctx);
        ctx.Sql.ToString().Trim().Should().Be("INSERT INTO [users] OUTPUT INSERTED.* DEFAULT VALUES");

        // Insert with empty buffer before ReturningNode (kills Mutant 321 IndexOutOfRange check on empty buffer)
        var emptyCtx = new CompilationContext(new ParameterManager());
        var emptyVisitor = _compiler.CreateVisitor(emptyCtx);
        _compiler.CompileInsert(insertNodes, emptyVisitor, emptyCtx);
        emptyCtx.Sql.ToString().Trim().Should().Be("OUTPUT INSERTED.* DEFAULT VALUES");
    }

    [Fact]
    public void CompileUpdate_Variants()
    {
        // Update with Set + ConcurrencyToken (AutoIncrement with null NewValue) + Returning (empty columns)
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("products"),
            new SetNode("price", 99.9),
            new ConcurrencyTokenNode("version", ExpectedValue: 1, NewValue: null, AutoIncrement: true),
            new ReturningNode(Array.Empty<string>())
        }.ToImmutableList());
        var res1 = _compiler.Compile((ISqlQuery)q1);
        res1.Sql.Trim().Should().Be("UPDATE [products] SET [price] = @p0, [version] = [version] + 1 OUTPUT INSERTED.* WHERE [version] = @p1");

        // Update with ConcurrencyToken (AutoIncrement = true BUT explicit NewValue provided)
        var qAutoExplicit = Substitute.For<IAstQuery>();
        qAutoExplicit.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("products"),
            new SetNode("price", 99.9),
            new ConcurrencyTokenNode("version", ExpectedValue: 1, NewValue: 5, AutoIncrement: true)
        }.ToImmutableList());
        var resAutoExplicit = _compiler.Compile((ISqlQuery)qAutoExplicit);
        resAutoExplicit.Sql.Trim().Should().Be("UPDATE [products] SET [price] = @p0, [version] = @p1 WHERE [version] = @p2");

        // Update with ConcurrencyToken (Explicit NewValue) + Returning (specific columns) + Existing WHERE + JOIN + FROM
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("products"),
            new SetNode("price", 99.9),
            new ConcurrencyTokenNode("row_guid", ExpectedValue: "old-guid", NewValue: "new-guid", AutoIncrement: false),
            new ReturningNode(new[] { "id", "price" }),
            new FromNode("products", "p"),
            new JoinNode(JoinType.Inner, "categories", "c", "c.id = p.category_id", null),
            new RawWhereNode("p.is_active = 1", null, false)
        }.ToImmutableList());
        var res2 = _compiler.Compile((ISqlQuery)q2);
        res2.Sql.Trim().Should().Be("UPDATE [products] SET [price] = @p0, [row_guid] = @p1 OUTPUT INSERTED.[id], INSERTED.[price] FROM [products] AS [p] INNER JOIN [categories] AS [c] ON c.id = p.category_id WHERE p.is_active = 1 AND [row_guid] = @p2");

        // Update with multiple concurrency tokens and NO existing WHERE
        var qTokensOnly = Substitute.For<IAstQuery>();
        qTokensOnly.Nodes.Returns(new ISqlNode[]
        {
            new UpdateNode("products"),
            new SetNode("price", 50.0),
            new ConcurrencyTokenNode("t1", ExpectedValue: 1, NewValue: 2, AutoIncrement: false),
            new ConcurrencyTokenNode("t2", ExpectedValue: 10, NewValue: 20, AutoIncrement: false)
        }.ToImmutableList());
        var resTokensOnly = _compiler.Compile((ISqlQuery)qTokensOnly);
        resTokensOnly.Sql.Trim().Should().Be("UPDATE [products] SET [price] = @p0, [t1] = @p1, [t2] = @p2 WHERE [t1] = @p3 AND [t2] = @p4");
    }

    [Fact]
    public void CompileDelete_Variants()
    {
        // Delete with Returning (empty columns DELETED.*)
        var q1 = Substitute.For<IAstQuery>();
        q1.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("orders"),
            new ReturningNode(Array.Empty<string>())
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q1).Sql.Trim().Should().Be("DELETE FROM [orders] OUTPUT DELETED.*");

        // Delete with Returning (specific columns) + FROM + JOIN + WHERE
        var q2 = Substitute.For<IAstQuery>();
        q2.Nodes.Returns(new ISqlNode[]
        {
            new DeleteNode("orders"),
            new ReturningNode(new[] { "id", "status" }),
            new FromNode("orders", "o"),
            new JoinNode(JoinType.Inner, "customers", "c", "c.id = o.customer_id", null),
            new RawWhereNode("c.is_deleted = 1", null, false)
        }.ToImmutableList());
        _compiler.Compile((ISqlQuery)q2).Sql.Trim().Should().Be("DELETE FROM [orders] OUTPUT DELETED.[id], DELETED.[status] FROM [orders] AS [o] INNER JOIN [customers] AS [c] ON c.id = o.customer_id WHERE c.is_deleted = 1");
    }
}
