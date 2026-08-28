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

public class SqlCompilerBaseCoverageTests
{
    
    [Fact]
    public void Escape_And_EscapeIdentifier_AreTested()
    {
        var compiler = new TestCompiler();
        
        Assert.Equal("*", compiler.Escape("*"));
        Assert.Equal("", compiler.Escape(""));
        Assert.Equal("\"foo\"", compiler.Escape("foo"));
        Assert.Equal("\"foo\".\"bar\"", compiler.Escape("foo.bar"));
    }
    [Fact]
    public void RenderBulk_Methods_CallAotRenderer()
    {
        var compiler = new TestCompiler();
        var entities = new List<TestEntity> { new TestEntity() };
        var rules = new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<TestEntity>>();
        
        var insertResult = compiler.RenderBulkInsert(entities, rules, 100);
        Assert.NotNull(insertResult);
        
        var updateResult = compiler.RenderBulkUpdate(entities, rules, 100);
        Assert.NotNull(updateResult);
        
        var mergeResult = compiler.RenderBulkMerge(entities, rules, 100);
        Assert.NotNull(mergeResult);
        
        var upsertResult = compiler.RenderBulkUpsert(entities, rules, 100);
        Assert.NotNull(upsertResult);
        
        var insertIgnoreResult = compiler.RenderBulkInsertIgnore(entities, rules, 100);
        Assert.NotNull(insertIgnoreResult);
    }
    [Fact]
    public void CompileCompositeCursors_MultipleAndHasExistingWhere()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var cursors = new List<EricksonLopez.SqlBuilder.Abstractions.Nodes.CompositeCursorNode>
        {
            new EricksonLopez.SqlBuilder.Abstractions.Nodes.CompositeCursorNode(new[] { new CursorKey("A", 1) }),
            new EricksonLopez.SqlBuilder.Abstractions.Nodes.CompositeCursorNode(new[] { new CursorKey("B", 2) })
        };
        
        compiler.CompileCompositeCursors(cursors, visitor, context, true);
        var sql = context.Sql.ToString();
        Assert.DoesNotContain("WHERE", sql);
        Assert.Contains("AND", sql);
    }
    [Fact]
    public void CompileWheres_AllWhereNodes_IsOr()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new RawWhereNode("A", null, false), // WHERE
            new RawWhereNode("B", null, true),  // OR
            new ExpressionWhereNode(System.Linq.Expressions.Expression.Constant(true), true), // OR
            new EricksonLopez.SqlBuilder.Abstractions.Nodes.ExistsWhereNode(new DummyQuery(), true, false), // OR
            new RawWhereNode("C", null, false) // AND
        };
        
        compiler.CompileWheres(nodes, visitor, context);
        var sql = context.Sql.ToString();
        Assert.Contains("WHERE ", sql);
        Assert.Contains("OR ", sql);
        Assert.Contains("AND ", sql);
    }
    private record DummyExtensionNode : SqlExtensionNode
    {
        public override void Accept(ISqlVisitor visitor)
        {
            visitor.VisitExtension(this);
        }
    }
    [Fact]
    public void CompileSelect_SubContextNoSpace_DoesNotTrim()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new SelectNode(new[] { "A" }, false)
        };
        
        compiler.CompileSelect(nodes, visitor, context);
        Assert.Equal("SELECT \"A\" ", context.Sql.ToString());
    }

    private class TestShortSelectVisitor : SqlCompilerVisitor
    {
        public TestShortSelectVisitor(ISqlCompiler compiler, CompilationContext context) : base(compiler, context) { }
        public override void Visit(SelectNode node)
        {
            Context.Sql.Append("A");
        }
    }

    private class TestShortSelectCompiler : Compilers.TestDefaultCompiler
    {
        internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new TestShortSelectVisitor(this, context);
    }

    [Fact]
    public void CompileSelect_SubContextShortSql_DoesNotThrowOrPrefix()
    {
        var compiler = new TestShortSelectCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new SelectNode(new[] { "X" }, false)
        };
        
        compiler.CompileSelect(nodes, visitor, context);
        Assert.Equal("A ", context.Sql.ToString());
    }

    [Fact]
    public void CompileDelete_ReturningNode_AppendsSpaceIfMissing()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new DeleteNode("Users"),
            new EricksonLopez.SqlBuilder.Abstractions.Nodes.ReturningNode(new[] { "Id" })
        };
        
        context.Sql.Append("SOMETHING");
        
        compiler.CompileDelete(nodes, visitor, context);
        Assert.Contains("Users\" RETURNING", context.Sql.ToString());
    }

    [Fact]
    public void CompileDelete_ExtensionNodes()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new DeleteNode("Users"),
            new DummyExtensionNode()
        };
        
        Assert.Throws<NotSupportedException>(() => compiler.CompileDelete(nodes, visitor, context));
    }
    [Fact]
    public void CompileSelect_CteNodes_WithRecursive()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new CteNode("CteAlias", new DummyQuery(), true),
            new SelectNode(new[] { "A" }, false)
        };
        
        compiler.CompileSelect(nodes, visitor, context);
        Assert.Contains("WITH RECURSIVE ", context.Sql.ToString());
    }

    [Fact]
    public void CompileSelect_CteNodes_WithWindowPageNode_AppendsComma()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new CteNode("CteAlias", new DummyQuery(), false),
            new SelectNode(new[] { "A" }, false),
            new WindowPageNode(1, 10, "Id", false)
        };
        
        compiler.CompileSelect(nodes, visitor, context);
        var sql = context.Sql.ToString();
        Assert.Contains("WITH ", sql);
        Assert.Contains(", __wp AS (", sql);
    }
    [Fact]
    public void Compile_RawQuery_Whitespace_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new EricksonLopez.SqlBuilder.RawQuery("   ", null));
    }
    [Fact]
    public void Compile_Subquery_ReturnsEmptyParameters()
    {
        var compiler = new TestCompiler();
        var existingParams = new ParameterManager();
        existingParams.Add("test");
        var query = new DummyQuery();
        
        var result = compiler.Compile(query, existingParams);
        
        Assert.Empty(result.Parameters);
    }
    
    [Fact]
    public void Compile_RawQuery_AppendsRawSql()
    {
        var compiler = new TestCompiler();
        var dict = new Dictionary<string, object?> { { "@p0", 123 } };
        var query = new EricksonLopez.SqlBuilder.RawQuery("SELECT 1", dict);
        
        var result = compiler.Compile(query);
        
        Assert.Equal("SELECT 1", result.Sql);
        Assert.Contains("p0", result.Parameters.Keys);
    }
    [Fact]
    public void CompileInsert_ReturningNode_AppendsSpaceIfMissing()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new EricksonLopez.SqlBuilder.Abstractions.Nodes.ReturningNode(new[] { "Id" })
        };
        
        context.Sql.Append("SOMETHING");
        
        compiler.CompileInsert(nodes, visitor, context);
        Assert.Contains("SOMETHING ", context.Sql.ToString());
    }
    [Fact]
    public void CompileInsert_InsertSelect_ReturnsEarly()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new EricksonLopez.SqlBuilder.Abstractions.Nodes.InsertSelectNode("Users", new[] { "Id" }, new DummyQuery())
        };
        
        compiler.CompileInsert(nodes, visitor, context);
        Assert.Contains("INSERT INTO \"Users\" (\"Id\")", context.Sql.ToString());
    }
    private class DummyQuery : ISqlQuery
    {
        public string? Tag => null;
        public IReadOnlyList<ISqlNode> Nodes => new List<ISqlNode>();
        public SqlResult Build(ISqlCompiler compiler) => new SqlResult("", new Dictionary<string, object?>());
    }
    [Fact]
    public void CompileUpdate_ConcurrencyTokens_ExplicitNewValue()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new UpdateNode("Users"),
            new ConcurrencyTokenNode("Version", ExpectedValue: 1, NewValue: 2, AutoIncrement: false)
        };
        
        compiler.CompileUpdate(nodes, visitor, context);
        Assert.Contains("SET \"Version\" = @p0", context.Sql.ToString());
        Assert.Contains("WHERE \"Version\" = @p1", context.Sql.ToString());
    }
    [Fact]
    public void CompileSelect_WithWindowPageNode()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new SelectNode(new[] { "Id", "Name" }, false),
            new WindowPageNode(1, 10, "Id", false)
        };
        
        compiler.CompileSelect(nodes, visitor, context);
        
        var sql = context.Sql.ToString();
        Assert.Contains("SELECT ", sql);
        Assert.Contains("ROW_NUMBER() OVER(ORDER BY \"Id\" ASC) AS __row_num", sql);
    }
    private class TestCompiler : SqlCompilerBase
    {
        public bool ReturnTrueForCompileBeforeSelect { get; set; }

        protected override ISqlRenderer AotRenderer => new MockSqlRenderer();
        
        public override string EscapeIdentifier(string identifier) => $"\"{identifier}\"";

        internal override bool CompileBeforeSelect(SqlNodePartition partition, ISqlVisitor visitor, CompilationContext context)
        {
            if (ReturnTrueForCompileBeforeSelect) return true;
            return base.CompileBeforeSelect(partition, visitor, context);
        }

        public new void CompileSelect(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
            => base.CompileSelect(nodes, visitor, context);
        
        public new void CompileUpdate(IReadOnlyList<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
            => base.CompileUpdate(nodes, visitor, context);
            
        public new void CompileCompositeCursors(IReadOnlyList<CompositeCursorNode> cursors, ISqlVisitor visitor, CompilationContext context, bool hasExistingWhere)
            => base.CompileCompositeCursors(cursors, visitor, context, hasExistingWhere);
    }

    [Fact]
    public void CompileBeforeSelect_ReturnsTrue_ExitsEarly()
    {
        var compiler = new TestCompiler { ReturnTrueForCompileBeforeSelect = true };
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        compiler.CompileSelect(new List<ISqlNode> { new SelectNode(new[] { "A" }, false) }, visitor, context);
        
        Assert.Equal("", context.Sql.ToString());
    }
    
    [Fact]
    public void CompileUpdate_ConcurrencyTokens_AutoIncrement()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new UpdateNode("Users"),
            new ConcurrencyTokenNode("Version", ExpectedValue: 1, AutoIncrement: true)
        };
        
        compiler.CompileUpdate(nodes, visitor, context);
        Assert.Contains("SET \"Version\" = \"Version\" + 1", context.Sql.ToString());
        Assert.Contains("WHERE \"Version\" = @p0", context.Sql.ToString());
    }
    
    [Fact]
    public void CompileUpdate_ConcurrencyTokens_WithExistingWhere()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var nodes = new List<ISqlNode>
        {
            new UpdateNode("Users"),
            new RawWhereNode("Id = 1", IsOr: false),
            new ConcurrencyTokenNode("Version", ExpectedValue: 1, AutoIncrement: true)
        };
        
        compiler.CompileUpdate(nodes, visitor, context);
        var sql = context.Sql.ToString();
        Assert.Contains("SET \"Version\" = \"Version\" + 1", sql);
        Assert.Contains("AND \"Version\" = @p0", sql);
    }
    
    [Fact]
    public void CompileCompositeCursors_NoExistingWhere()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var cursors = new List<CompositeCursorNode>
        {
            new CompositeCursorNode(new[] { new CursorKey("A", 1, false) }, false)
        };
        
        compiler.CompileCompositeCursors(cursors, visitor, context, false);
        var sql = context.Sql.ToString();
        Assert.Contains("WHERE ", sql);
        Assert.EndsWith(" ", sql);
    }
    
    [Fact]
    public void CompileCompositeCursors_ExistingWhere()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var cursors = new List<CompositeCursorNode>
        {
            new CompositeCursorNode(new[] { new CursorKey("A", 1, false) }, false)
        };
        
        compiler.CompileCompositeCursors(cursors, visitor, context, true);
        var sql = context.Sql.ToString();
        Assert.Contains("AND ", sql);
        Assert.EndsWith(" ", sql);
    }
    
    [Fact]
    public void CompileCompositeCursors_MultipleCursors()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);
        
        var cursors = new List<CompositeCursorNode>
        {
            new CompositeCursorNode(new[] { new CursorKey("A", 1, false) }, false),
            new CompositeCursorNode(new[] { new CursorKey("B", 2, false) }, false)
        };
        
        compiler.CompileCompositeCursors(cursors, visitor, context, false);
        var sql = context.Sql.ToString();
        Assert.Contains("WHERE ", sql);
        Assert.Contains("AND ", sql);
    }

    [Fact]
    public void CompileUpdate_With_Recursive_And_Multiple_Ctes()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);

        var cte1 = new CteNode("cte1", Sql.From<TestEntity>(), IsRecursive: true);
        var cte2 = new CteNode("cte2", Sql.From<TestEntity>(), IsRecursive: false);
        var updateNode = new UpdateNode("TestEntity");
        var nodes = new List<ISqlNode> { cte1, cte2, updateNode };

        compiler.CompileUpdate(nodes, visitor, context);
        var sql = context.Sql.ToString();
        Assert.StartsWith("WITH RECURSIVE ", sql);
        Assert.Contains("\"cte1\" AS", sql);
        Assert.Contains(", \"cte2\" AS", sql);
    }

    private class TestWhitespaceVisitor : SqlCompilerVisitor
    {
        public TestWhitespaceVisitor(ISqlCompiler compiler, CompilationContext context) : base(compiler, context) { }
        public override void Visit(SelectNode node) => Context.Sql.Append("   ");
        public override void Visit(FromNode node) => Context.Sql.Append("   ");
    }

    private class TestWhitespaceCompiler : Compilers.TestDefaultCompiler
    {
        internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new TestWhitespaceVisitor(this, context);
    }

    [Fact]
    public void Compile_AllWhitespace_TrimsToEmptyString()
    {
        var compiler = new TestWhitespaceCompiler();
        var result = compiler.Compile(Sql.From<TestEntity>().Select("Name"));
        Assert.Equal("", result.Sql);
    }

    private class TestExactSevenSelectVisitor : SqlCompilerVisitor
    {
        public TestExactSevenSelectVisitor(ISqlCompiler compiler, CompilationContext context) : base(compiler, context) { }
        public override void Visit(SelectNode node)
        {
            Context.Sql.Append("SELECT ");
        }
    }

    private class TestExactSevenSelectCompiler : Compilers.TestDefaultCompiler
    {
        internal override SqlVisitorBase CreateVisitor(CompilationContext context) => new TestExactSevenSelectVisitor(this, context);
    }

    [Fact]
    public void CompileSelect_SubContextExactSevenSelect_PrefixesCorrectly()
    {
        var compiler = new TestExactSevenSelectCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = compiler.CreateVisitor(context);

        var nodes = new List<ISqlNode>
        {
            new SelectNode(new[] { "X" }, false)
        };

        compiler.CompileSelect(nodes, visitor, context);
        Assert.Equal("SELECT  ", context.Sql.ToString());
    }

    [Fact]
    public void SelectQuery_AsCount_WithCustomAlias_EmitsAlias()
    {
        var query = Sql.From<TestEntity>().AsCount("total_rows");
        var compiler = new TestCompiler();
        var result = compiler.Compile(query);
        Assert.Contains("COUNT(*) AS total_rows", result.Sql);
    }
}




























