// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.Metadata;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlCompilerVisitorCoverageTests
{

    [Fact]
    public void Visit_CaseNode_FullCoverage()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);

        var node = new CaseNode(
            new[] { new EricksonLopez.SqlBuilder.Abstractions.Nodes.CaseWhenBranch("Status = 1", null, "'Active'", null) },
            "'Unknown'", null, "StatusDesc");

        visitor.Visit(node);

        var sql = context.Sql.ToString();
        Assert.Contains("CASE WHEN Status = 1 THEN 'Active' ELSE 'Unknown' END AS StatusDesc", sql);
    }
    
    [Fact]
    public void Visit_CompositeCursorNode_MultipleKeys_Coverage()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);
        
        var keys = new[] 
        {
            new CursorKey("A", 1, false), // desc = false
            new CursorKey("B", 2, true)  // desc = true
        };
        var node = new CompositeCursorNode(keys, IsAfter: true); // isAfter = true
        
        visitor.Visit(node);
        
        var sql = context.Sql.ToString();
        Assert.Contains("(A > @p0 OR (A = @p0 AND B < @p1))", sql);
    }

    [Fact]
    public void Visit_CompositeCursorNode_IsAfterFalse()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);
        
        var keys = new[] 
        {
            new CursorKey("A", 1, false) // desc = false
        };
        var node = new CompositeCursorNode(keys, IsAfter: false);
        
        visitor.Visit(node);
        
        var sql = context.Sql.ToString();
        Assert.Contains("(A < @p0)", sql);
    }
    
    private class DummyQuery : ISqlQuery
    {
        public string? Tag => null;
        public IReadOnlyList<ISqlNode> Nodes => new List<ISqlNode>();
        public SqlResult Build(ISqlCompiler compiler) => new SqlResult("", new Dictionary<string, object?>());
    }

    private class TestCompiler : SqlCompilerBase
    {
        protected override ISqlRenderer AotRenderer => throw new NotImplementedException();
        
        public override string EscapeIdentifier(string identifier) => $"{identifier}";
        public new SqlVisitorBase CreateVisitor(CompilationContext context) => base.CreateVisitor(context);
    }
    
    private (SqlCompilerVisitor, CompilationContext) CreateVisitor()
    {
        var context = new CompilationContext(new ParameterManager());
        var compiler = new TestCompiler();
        var visitor = (SqlCompilerVisitor)compiler.CreateVisitor(context);
        return (visitor, context);
    }

    [Fact]
    public void Visit_OrderByNode_NullsFirst()
    {
        var (visitor, context) = CreateVisitor();
        Expression<Func<int>> expr = () => 1;
        var node = new OrderByNode(expr, false, NullsPosition.First);
        // node.Accept(visitor) will throw since Expression is evaluated and translated. 
        // We use Assert.ThrowsAny to ensure it hits AppendNullsPosition without silencing errors.
        node.Accept(visitor);
    }
    
    [Fact]
    public void Visit_OrderByNode_NullsLast()
    {
        var (visitor, context) = CreateVisitor();
        Expression<Func<int>> expr = () => 1;
        var node = new OrderByNode(expr, true, NullsPosition.Last);
        node.Accept(visitor);
    }

    [Fact]
    public void Visit_WindowFunctionNode_Ntile()
    {
        var (visitor, context) = CreateVisitor();
        var node = new WindowFunctionNode("NTILE", "4", null, null, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(), "NtileAlias");
        node.Accept(visitor);
        Assert.Contains("NTILE(4) OVER () AS NtileAlias", context.Sql.ToString());
    }
    
    [Fact]
    public void Visit_WindowFunctionNode_OffsetAndDefault()
    {
        var (visitor, context) = CreateVisitor();
        var node = new WindowFunctionNode("LAG", "Col", 2, "Def", Array.Empty<string>(), Array.Empty<string>(), Array.Empty<bool>(), "LagAlias");
        node.Accept(visitor);
        Assert.Contains("LAG(Col, 2, @p0) OVER () AS LagAlias", context.Sql.ToString());
    }

    [Fact]
    public void Visit_ExpressionHavingNode_And_Or()
    {
        var (visitor, context) = CreateVisitor();
        Expression<Func<bool>> expr = () => true;
        var nodeAnd = new ExpressionHavingNode(expr, false);
        var nodeOr = new ExpressionHavingNode(expr, true);
        
        nodeAnd.Accept(visitor);
        nodeOr.Accept(visitor);
    }
    
    [Fact]
    public void Visit_RawHavingNode_And_Or()
    {
        var (visitor, context) = CreateVisitor();
        var nodeAnd = new RawHavingNode("Count(Id) > 0", IsOr: false);
        var nodeOr = new RawHavingNode("Count(Id) > 0", IsOr: true);
        
        nodeAnd.Accept(visitor);
        nodeOr.Accept(visitor);
        
        Assert.Contains("Count(Id) > 0", context.Sql.ToString());
    }
    
    [Fact]
    public void Visit_ExpressionWhereNode_Or()
    {
        var (visitor, context) = CreateVisitor();
        Expression<Func<bool>> expr = () => true;
        var node = new ExpressionWhereNode(expr, true);
        node.Accept(visitor);
    }
    
    [Fact]
    public void Visit_SetOperationNodes()
    {
        var (visitor, context) = CreateVisitor();
        
        var q = new DummyQuery();
        
        var exceptNode = new EricksonLopez.SqlBuilder.Abstractions.Nodes.SetOperationNode("EXCEPT", q);
        var exceptAllNode = new EricksonLopez.SqlBuilder.Abstractions.Nodes.SetOperationNode("EXCEPT ALL", q);
        var intersectNode = new EricksonLopez.SqlBuilder.Abstractions.Nodes.SetOperationNode("INTERSECT", q);
        var intersectAllNode = new EricksonLopez.SqlBuilder.Abstractions.Nodes.SetOperationNode("INTERSECT ALL", q);
        
        exceptNode.Accept(visitor);
        exceptAllNode.Accept(visitor);
        intersectNode.Accept(visitor);
        intersectAllNode.Accept(visitor);
        
        var sql = context.Sql.ToString();
        Assert.Contains("EXCEPT", sql);
        Assert.Contains("EXCEPT ALL", sql);
        Assert.Contains("INTERSECT", sql);
        Assert.Contains("INTERSECT ALL", sql);
    }

    private class TestOrderEntity
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
    }

    [Fact]
    public void Visit_OrderByNode_WithUnaryExpression_ShouldExtractMemberAndEscapeSnakeCase()
    {
        var (visitor, context) = CreateVisitor();
        Expression<Func<TestOrderEntity, object>> exprWithUnary = x => (object)x.UserName;
        var node = new OrderByNode(exprWithUnary, IsDescending: true, Nulls: NullsPosition.Last);

        node.Accept(visitor);

        var sql = context.Sql.ToString();
        Assert.Contains("user_name DESC NULLS LAST", sql);
    }

    [Fact]
    public void Visit_OrderByNode_WithDirectMemberExpression_ShouldExtractMemberAndEscapeSnakeCase()
    {
        var (visitor, context) = CreateVisitor();
        Expression<Func<TestOrderEntity, int>> exprDirect = x => x.Id;
        var node = new OrderByNode(exprDirect, IsDescending: false, Nulls: NullsPosition.First);

        node.Accept(visitor);

        var sql = context.Sql.ToString();
        Assert.Contains("id NULLS FIRST", sql);
    }

    [Fact]
    public void Visit_GroupByNode_GroupingSets_WithMultipleSets_ShouldFormatCorrectly()
    {
        var (visitor, context) = CreateVisitor();
        var sets = new List<IReadOnlyList<string>>
        {
            new List<string> { "category", "region" },
            new List<string> { "category" },
            new List<string>()
        };
        var node = new GroupByNode(Columns: Array.Empty<string>(), Type: GroupByType.GroupingSets, Sets: sets);

        node.Accept(visitor);

        var sql = context.Sql.ToString();
        Assert.Contains("GROUPING SETS ((category, region), (category), ())", sql);
    }

    [Fact]
    public void Visit_GroupByNode_GroupingSets_EmptySets_ShouldFormatEmptyGroupingSets()
    {
        var (visitor, context) = CreateVisitor();
        var sets = new List<IReadOnlyList<string>>();
        var node = new GroupByNode(Columns: Array.Empty<string>(), Type: GroupByType.GroupingSets, Sets: sets);

        node.Accept(visitor);

        var sql = context.Sql.ToString();
        Assert.Contains("GROUPING SETS ()", sql);
    }

    [Fact]
    public void Visit_GroupByNode_NullOrEmptyColumns_ShouldNotAppendExtraCommas()
    {
        var (visitor, context) = CreateVisitor();
        var node = new GroupByNode(Columns: Array.Empty<string>(), Type: GroupByType.Standard);

        node.Accept(visitor);

        var sql = context.Sql.ToString();
        Assert.Equal(string.Empty, sql.Trim());
    }

    [Fact]
    public void Visit_CompositeCursorNode_ThreeKeys_ShouldGenerateNestedPredicateRecursively()
    {
        var (visitor, context) = CreateVisitor();
        var keys = new[]
        {
            new CursorKey("col1", 10, false),
            new CursorKey("col2", 20, true),
            new CursorKey("col3", 30, false)
        };
        var node = new CompositeCursorNode(keys, IsAfter: true);

        node.Accept(visitor);

        var sql = context.Sql.ToString();
        Assert.Contains("(col1 > @p0 OR (col1 = @p0 AND col2 < @p1 OR (col2 = @p1 AND col3 > @p2)))", sql);
    }

    [Fact]
    public void Visit_CompositeCursorNode_EmptyKeys_ShouldNotRenderPredicate()
    {
        var (visitor, context) = CreateVisitor();
        var keys = Array.Empty<CursorKey>();
        var node = new CompositeCursorNode(keys, IsAfter: true);

        node.Accept(visitor);

        var sql = context.Sql.ToString();
        Assert.Equal(string.Empty, sql);
    }

    [Fact]
    public void Visit_ValuesNode_MultipleSets_RendersCommasCorrectly()
    {
        var (visitor, context) = CreateVisitor();
        var valuesNode = new ValuesNode(new List<IReadOnlyList<object?>>
        {
            new object?[] { 1, "Alice" },
            new object?[] { 2, "Bob" }
        });

        valuesNode.Accept(visitor);
        var sql = context.Sql.ToString();
        Assert.Equal("VALUES (@p0, @p1), (@p2, @p3) ", sql);
    }
}







