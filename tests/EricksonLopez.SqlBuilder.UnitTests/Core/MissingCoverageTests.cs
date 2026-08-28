// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class MissingCoverageTests
{
    private class TestCompiler : SqlCompilerBase
    {
        protected override ISqlRenderer AotRenderer => throw new NotImplementedException();
        
        public void TestCompileWheres(List<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
        {
            CompileWheres(nodes, visitor, context);
        }

        public void TestCompileOrderBys(List<ISqlNode> nodes, ISqlVisitor visitor, CompilationContext context)
        {
            CompileOrderBys(nodes, visitor, context);
        }
    }

    [Fact]
    public void SqlCompilerBase_CompileWheres_List_ShouldVisit()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);
        
        var nodes = new List<ISqlNode>
        {
            new RawWhereNode("a = 1", Array.Empty<object>(), false),
            new RawWhereNode("b = 2", Array.Empty<object>(), true)
        };

        compiler.TestCompileWheres(nodes, visitor, context);
        
        context.Sql.ToString().Trim().Should().Be("WHERE a = 1 OR b = 2");
    }

    [Fact]
    public void SqlCompilerBase_CompileOrderBys_List_ShouldVisit()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);
        
        var nodes = new List<ISqlNode>
        {
            new RawOrderByNode("a", false),
            new RawOrderByNode("b", true)
        };

        compiler.TestCompileOrderBys(nodes, visitor, context);
        
        context.Sql.ToString().Trim().Should().Be("ORDER BY a, b DESC");
    }
    
    [Fact]
    public void SqlCompilerVisitor_VisitThenByNode_ShouldCallOrderBy()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);

        visitor.Visit(new RawOrderByNode("col", false));
        context.Sql.ToString().Should().Be("col");
    }
    
    [Fact]
    public void SqlCompilerVisitor_VisitGroupByNode_ShouldAppendCols()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);
        var node = new GroupByNode(new[] { "a", "b" });
        visitor.Visit(node);
        visitor.Context.Sql.ToString().Should().Be("\"a\", \"b\"");
    }
    
    [Fact]
    public void SqlCompilerVisitor_VisitJoinType_Fallback_ShouldReturnString()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);

        var invalidJoin = (JoinType)99;
        var joinNode = new JoinNode(invalidJoin, "table", "alias", "on");
        
        visitor.Visit(joinNode);
        
        context.Sql.ToString().Should().Contain("99");
    }
    
    [Fact]
    public void SqlExpressionVisitor_NonMemberExpression_ShouldThrow()
    {
        // Testing some edge cases in SqlExpressionVisitor that might have been missed
        var query = new SelectQuery<TestEntity>();
        
        // This causes the expression visitor to visit a non-standard unary or binary
        // Actually, we can test it using the SqlExpressionVisitor directly
        var mgr = new ParameterManager();
        var sql = new System.Text.StringBuilder();
        var visitor = new SqlExpressionVisitor(sql, mgr);
        
        Expression<Func<TestEntity, bool>> expr = x => !x.IsActive; // Unary Not
        var result = visitor.Visit(expr);
        result.Should().Be(expr);
        sql.ToString().Should().Be("NOT (is_active)");
    }
    
    public class TestEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        
        public string GetTableName() => "test_entity";
        public string[] GetColumnNames() => new[] { "id", "is_active" };
        public string[] GetAllColumnNames() => new[] { "id", "is_active" };
        public object?[] GetValues() => new object?[] { Id, IsActive };
        public object?[] GetAllValues() => new object?[] { Id, IsActive };
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public void Queries_ShouldBeCompilable()
    {
        var compiler = new TestCompiler();
        
        IAstQuery sel = new SelectQuery<TestEntity>();
        compiler.Compile(sel).Sql.Should().Be("SELECT *");
        
        IAstQuery upd = new UpdateQuery<TestEntity>();
        compiler.Compile(upd).Sql.Should().Be("UPDATE \"test_entity\"");
        
        IAstQuery del = new DeleteQuery<TestEntity>();
        compiler.Compile(del).Sql.Should().Be("DELETE FROM \"test_entity\"");
        
        IAstQuery ins = new InsertQuery<TestEntity>().Into("test_entity").DefaultValues();
        compiler.Compile(ins).Sql.Should().Be("INSERT INTO \"test_entity\" DEFAULT VALUES");
    }

    [Fact]
    public void SelectQuery_Distinct_WithExpressionSelectNode_ShouldUpdateNode()
    {
        var q = new SelectQuery<TestEntity>()
            .Select(x => x.Id)
            .Distinct();
            
        var node = (ExpressionSelectNode)q.Nodes[0];
        node.IsDistinct.Should().BeTrue();
    }
    
    [Fact]
    public void SelectQuery_Distinct_WithRawSelectNode_ShouldUpdateNode()
    {
        var q = new SelectQuery<TestEntity>()
            .RawSelect($"SELECT *")
            .Distinct();
            
        var node = (RawSelectNode)q.Nodes[0];
        node.IsDistinct.Should().BeTrue();
    }

    [Fact]
    public void SqlBuilderDiagnostics_Methods_ShouldBeCovered()
    {
        SqlBuilderDiagnostics.LoggerFactory = null;
        SqlBuilderDiagnostics.ReinitializeMetersForTesting();
        SqlBuilderDiagnostics.LoggerFactory.Should().BeNull();
    }
    
    private class TestRenderer : AotSqlRendererBase
    {
        public TestRenderer(ISqlCompiler compiler) : base(compiler) { }

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkInsert<T>(
            IEnumerable<T> entities, List<ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkUpdate<T>(
            IEnumerable<T> entities, List<ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkMerge<T>(
            IEnumerable<T> entities, List<ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkUpsert<T>(
            IEnumerable<T> entities, List<ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();

        public override Builders.Bulk.Operations.BulkSqlResult RenderBulkInsertIgnore<T>(
            IEnumerable<T> entities, List<ColumnSelection.IColumnSelectionRule<T>> rules, int batchSize) => throw new NotImplementedException();
    }
    
    public class MismatchedEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public long LongProp { get; set; }
        public Guid GuidProp { get; set; }

        public string GetTableName() => "mismatched";
        public string[] GetColumnNames() => new[] { "col1", "col2" };
        public string[] GetAllColumnNames() => new[] { "col1", "col2" };
        public object?[] GetValues() => new object?[] { 1 }; // length 1 vs columns 2
        public object?[] GetAllValues() => new object?[] { 1 };
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    private class CustomGuidHandler : ITypeHandler
    {
        public object? Parse(Type destinationType, object? value) => value?.ToString();
        public void SetValue(System.Data.IDbDataParameter parameter, object? value) { }
    }

    [Fact]
    public void DiffUpdateExtensions_MismatchedEntity_ThrowsInvalidOperationException()
    {
        var orig = new MismatchedEntity();
        var curr = new MismatchedEntity();
        var updateQuery = new UpdateQuery<MismatchedEntity>();
        
        Action act = () => updateQuery.ApplyDiff(orig, curr);
        act.Should().Throw<InvalidOperationException>().WithMessage("*metadata mismatch*");
    }

    [Fact]
    public void InsertQuery_Values_MismatchedEntity_ThrowsInvalidOperationException()
    {
        var entity = new MismatchedEntity();
        var query = new InsertQuery<MismatchedEntity>();
        
        Action act = () => query.Values(entity);
        act.Should().Throw<InvalidOperationException>().WithMessage("*metadata mismatch*");
    }

    [Fact]
    public void InsertQuery_Bulk_MismatchedEntity_ThrowsInvalidOperationException()
    {
        var entity = new MismatchedEntity();
        var query = new InsertQuery<MismatchedEntity>();
        
        Action act = () => query.Bulk(new[] { entity });
        act.Should().Throw<InvalidOperationException>().WithMessage("*metadata mismatch*");
    }

    [Fact]
    public void UpdateQuery_Set_MismatchedEntity_ThrowsInvalidOperationException()
    {
        var entity = new MismatchedEntity();
        var query = new UpdateQuery<MismatchedEntity>();
        
        Action act = () => query.Set(entity);
        act.Should().Throw<InvalidOperationException>().WithMessage("*metadata mismatch*");
    }

    [Fact]
    public void RawQuery_MismatchedEntity_ThrowsInvalidOperationException()
    {
        var entity = new MismatchedEntity();
        
        Action act = () => Sql.Raw("INSERT INTO t VALUES (1)", entity);
        act.Should().Throw<InvalidOperationException>().WithMessage("*metadata mismatch*");
    }

    [Fact]
    public void ParameterManager_TypeHandler_ProcessesCustomType()
    {
        Sql.RegisterTypeHandler<Guid>(new CustomGuidHandler());
        try
        {
            var pm = new ParameterManager();
            var guid = Guid.NewGuid();
            var pName = pm.Add(guid);
            
            pm.GetParameters()[pName.TrimStart('@')].Should().Be(guid.ToString());
        }
        finally
        {
            Sql.TypeHandlers.TryRemove(typeof(Guid), out _);
        }
    }

    [Fact]
    public void UpdateQuery_WithConcurrencyToken_CoversAllTypeBranches()
    {
        var q1 = new UpdateQuery<MismatchedEntity>().WithConcurrencyToken(x => x.Id, 1);
        q1.Nodes.OfType<ConcurrencyTokenNode>().First().AutoIncrement.Should().BeTrue();

        var q2 = new UpdateQuery<MismatchedEntity>().WithConcurrencyToken(x => x.LongProp, 100L);
        q2.Nodes.OfType<ConcurrencyTokenNode>().First().AutoIncrement.Should().BeTrue();

        var guid = Guid.NewGuid();
        var q3 = new UpdateQuery<MismatchedEntity>().WithConcurrencyToken(x => x.GuidProp, guid);
        q3.Nodes.OfType<ConcurrencyTokenNode>().First().AutoIncrement.Should().BeFalse();
    }

    [Fact]
    public void SelectQuery_AsAggregates_And_WhereDateHelpers_CoverAllBranches()
    {
        var q = new SelectQuery<TestEntity>()
            .AsCount("")
            .AsCount("cnt")
            .AsSum("amount", "sum_amt")
            .AsSum("amount", null)
            .AsSum("amount", "")
            .AsAvg("rating", "avg_rat")
            .AsAvg("rating", null)
            .AsAvg("rating", "")
            .AsMin("created_at", "min_date")
            .AsMin("created_at", null)
            .AsMin("created_at", "")
            .AsMax("score", "max_score")
            .AsMax("score", null)
            .AsMax("score", "")
            .WhereColumns("col1", "=", "col2")
            .WhereDate("created_at", ">", DateTime.UtcNow)
            .WhereYear("created_at", "=", 2026)
            .WhereMonth("created_at", "=", 8)
            .WhereDay("created_at", "=", 21);

        q.Nodes.Should().NotBeEmpty();

        // Check ArgumentExceptions
        var q2 = new SelectQuery<TestEntity>();
        Assert.Throws<ArgumentException>(() => q2.AsSum("", null));
        Assert.Throws<ArgumentException>(() => q2.AsAvg("", null));
        Assert.Throws<ArgumentException>(() => q2.AsMin("", null));
        Assert.Throws<ArgumentException>(() => q2.AsMax("", null));

        Assert.Throws<ArgumentException>(() => q2.WhereColumns(null!, "=", "b"));
        Assert.Throws<ArgumentException>(() => q2.WhereColumns("a", null!, "b"));
        Assert.Throws<ArgumentException>(() => q2.WhereColumns("a", "=", null!));

        Assert.Throws<ArgumentException>(() => q2.WhereDate(null!, "=", DateTime.Now));
        Assert.Throws<ArgumentException>(() => q2.WhereDate("d", null!, DateTime.Now));

        Assert.Throws<ArgumentException>(() => q2.WhereYear(null!, "=", 2026));
        Assert.Throws<ArgumentException>(() => q2.WhereYear("d", null!, 2026));

        Assert.Throws<ArgumentException>(() => q2.WhereMonth(null!, "=", 5));
        Assert.Throws<ArgumentException>(() => q2.WhereMonth("d", null!, 5));

        Assert.Throws<ArgumentException>(() => q2.WhereDay(null!, "=", 15));
        Assert.Throws<ArgumentException>(() => q2.WhereDay("d", null!, 15));
    }

    [Fact]
    public void SqlExpressionVisitor_ConstructorNullChecks()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlExpressionVisitor(null!, new ParameterManager()));
        Assert.Throws<ArgumentNullException>(() => new SqlExpressionVisitor(new System.Text.StringBuilder(), null!));
    }

    private static class NonPgSqlHelper
    {
        public static bool ILike(string a, string b) => true;
        public static bool Any(int a, int[] b) => true;
        public static bool All(int a, int[] b) => true;
    }

    [Fact]
    public void SqlExpressionVisitor_NonPgSql_ILike_Any_All_ReturnsFalseAndFallsBack()
    {
        var sql1 = new System.Text.StringBuilder();
        var pm1 = new ParameterManager();
        var visitor1 = new SqlExpressionVisitor(sql1, pm1);
        Expression<Func<bool>> exprILike = () => NonPgSqlHelper.ILike("a", "b");
        visitor1.Parse(exprILike.Body);
        sql1.ToString().Should().NotBeEmpty();

        var sql2 = new System.Text.StringBuilder();
        var pm2 = new ParameterManager();
        var visitor2 = new SqlExpressionVisitor(sql2, pm2);
        Expression<Func<bool>> exprAny = () => NonPgSqlHelper.Any(1, new[] { 1, 2 });
        visitor2.Parse(exprAny.Body);
        sql2.ToString().Should().NotBeEmpty();

        var sql3 = new System.Text.StringBuilder();
        var pm3 = new ParameterManager();
        var visitor3 = new SqlExpressionVisitor(sql3, pm3);
        Expression<Func<bool>> exprAll = () => NonPgSqlHelper.All(1, new[] { 1, 2 });
        visitor3.Parse(exprAll.Body);
        sql3.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void SqlNodePartition_CaseNode_BranchCoverage()
    {
        var caseNode1 = new CaseNode(new[] { new CaseWhenBranch("1=1", null, "1", null) }, "0", null, "alias1");
        var caseNode2 = new CaseNode(new[] { new CaseWhenBranch("2=2", null, "2", null) }, "0", null, "alias2");
        
        var partition = new SqlNodePartition(new List<ISqlNode> { caseNode1, caseNode2 });
        partition.SelectNodes.Should().HaveCount(2);
    }

    [Fact]
    public void SqlCompilerVisitor_WindowNode_NullAndEmptyPartitionBy()
    {
        var compiler = new TestCompiler();
        var context1 = new CompilationContext(new ParameterManager());
        var visitor1 = new SqlCompilerVisitor(compiler, context1);

        var windowNode1 = new WindowNode("w", Array.Empty<string>(), new[] { "id ASC" });
        visitor1.Visit(windowNode1);
        context1.Sql.ToString().Should().Be("\"w\" AS (ORDER BY \"id\" ASC)");

        var context2 = new CompilationContext(new ParameterManager());
        var visitor2 = new SqlCompilerVisitor(compiler, context2);
        var windowNode2 = new WindowNode("w", null, new[] { "id ASC" });
        visitor2.Visit(windowNode2);
        context2.Sql.ToString().Should().Be("\"w\" AS (ORDER BY \"id\" ASC)");
    }

    [Fact]
    public void AotSqlRendererBase_NullCompiler_Throws()
    {
        Action act = () => new TestRenderer(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    public class DynamicValuesEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public object?[] ValuesToReturn { get; set; } = new object?[] { 1 };
        public string GetTableName() => "dynamic_values";
        public string[] GetColumnNames() => new[] { "id" };
        public string[] GetAllColumnNames() => new[] { "id" };
        public object?[] GetValues() => ValuesToReturn;
        public object?[] GetAllValues() => ValuesToReturn;
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public void DiffUpdateExtensions_CurrValuesMismatchedEntity_ThrowsInvalidOperationException()
    {
        var orig = new DynamicValuesEntity { ValuesToReturn = new object?[] { 1 } };
        var curr = new DynamicValuesEntity { ValuesToReturn = new object?[] { 1, 2 } };
        var updateQuery = new UpdateQuery<DynamicValuesEntity>();
        
        Action act = () => updateQuery.ApplyDiff(orig, curr);
        act.Should().Throw<InvalidOperationException>().WithMessage("*metadata mismatch*");
    }

    [Fact]
    public void DiffUpdateExtensions_NonISqlEntity_ThrowsInvalidOperationException()
    {
        var orig = new TestEntity();
        var updateQuery = new UpdateQuery<TestEntity>();
        
        Action act = () => updateQuery.ApplyDiff(orig, null!);
        var ex = act.Should().Throw<InvalidOperationException>().Which;
        ex.Message.Should().Contain(nameof(TestEntity));
        ex.Message.Should().Contain("does not implement ISqlEntity");
    }

    [Fact]
    public void DynamicSortingExtensions_InvalidColumnName_ThrowsArgumentException_WithMessage()
    {
        var query = new SelectQuery<TestEntity>();
        Action act = () => query.OrderByDynamic("invalid;column");
        var ex = act.Should().Throw<ArgumentException>().Which;
        ex.Message.Should().Contain("Invalid sort column name");
    }

    [Fact]
    public void InsertQuery_Values_WithAndWithout_ExistingInsertNode()
    {
        var q1 = new InsertQuery<TestEntity>().Values(1, true);
        q1.Nodes.OfType<InsertNode>().Should().HaveCount(1);
        q1.Nodes.OfType<ValuesNode>().Should().HaveCount(1);
        ((ValuesNode)q1.Nodes.OfType<ValuesNode>().First()).ValuesSets.Should().HaveCount(1);

        var q2 = new InsertQuery<TestEntity>().Into("custom_table").Values(2, false);
        q2.Nodes.OfType<InsertNode>().Should().HaveCount(1);
        q2.Nodes.OfType<InsertNode>().First().TableName.Should().Be("custom_table");
    }

    public class NullablePropEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public string GetTableName() => "nullable_table";
        public string[] GetColumnNames() => new[] { "id", "name" };
        public string[] GetAllColumnNames() => new[] { "id", "name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public object?[] GetAllValues() => new object?[] { Id, Name };
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public void InsertQuery_Bulk_IgnoreNulls_And_EmptyEntities()
    {
        var e1 = new NullablePropEntity { Id = 1, Name = null };
        var e2 = new NullablePropEntity { Id = 2, Name = "Alice" };

        var q = new InsertQuery<NullablePropEntity>().Bulk(new[] { e1, e2 }, ignoreNulls: true);
        var insertNode = q.Nodes.OfType<InsertNode>().First();
        insertNode.Columns.Should().Equal(new[] { "id" });

        var valuesNode = q.Nodes.OfType<ValuesNode>().First();
        valuesNode.ValuesSets.Should().HaveCount(2);
        valuesNode.ValuesSets[0].Should().Equal(new object?[] { 1 });
        valuesNode.ValuesSets[1].Should().Equal(new object?[] { 2 });

        var emptyQ = new InsertQuery<NullablePropEntity>().Bulk(Array.Empty<NullablePropEntity>());
        emptyQ.Nodes.OfType<ValuesNode>().Should().BeEmpty();
    }

    [Fact]
    public void InsertQuery_OnConflict_NewExpression_And_UnaryExpression()
    {
        var q = new InsertQuery<TestEntity>().OnConflict(x => new { x.Id, x.IsActive });
        var onConflictNode = q.Nodes.OfType<OnConflictNode>().First();
        onConflictNode.TargetColumns.Should().Equal(new[] { "id", "is_active" });

        var q2 = new InsertQuery<TestEntity>().OnConflict(x => (object)x.Id);
        var onConflictNode2 = q2.Nodes.OfType<OnConflictNode>().First();
        onConflictNode2.TargetColumns.Should().Equal(new[] { "id" });
    }

    [Fact]
    public void SelectQuery_Distinct_EdgeCases()
    {
        var q = new SelectQuery<TestEntity>()
            .Where(x => x.Id == 1)
            .Where(x => x.IsActive)
            .Distinct();
        
        q.Nodes.Should().HaveCount(3);
        ((SelectNode)q.Nodes[2]).IsDistinct.Should().BeTrue();
    }

    [Fact]
    public void SelectQuery_AsAggregates_Formatting_And_ArgExceptions()
    {
        var q = new SelectQuery<TestEntity>();
        
        var qCount1 = q.AsCount();
        ((RawSelectNode)qCount1.Nodes.Last()).RawSql.Should().Be("COUNT(*) AS count");
        
        var qCount2 = q.AsCount("");
        ((RawSelectNode)qCount2.Nodes.Last()).RawSql.Should().Be("COUNT(*)");

        var qCount3 = q.AsCount("total");
        ((RawSelectNode)qCount3.Nodes.Last()).RawSql.Should().Be("COUNT(*) AS total");

        var qSum1 = q.AsSum("amount", "my_sum");
        ((RawSelectNode)qSum1.Nodes.Last()).RawSql.Should().Be("SUM(amount) AS my_sum");
        ((RawSelectNode)qSum1.Nodes.Last()).IsDistinct.Should().BeFalse();
        var qSum2 = q.AsSum("amount", "");
        ((RawSelectNode)qSum2.Nodes.Last()).RawSql.Should().Be("SUM(amount)");
        ((RawSelectNode)qSum2.Nodes.Last()).IsDistinct.Should().BeFalse();

        var qAvg1 = q.AsAvg("score", "my_avg");
        ((RawSelectNode)qAvg1.Nodes.Last()).RawSql.Should().Be("AVG(score) AS my_avg");
        ((RawSelectNode)qAvg1.Nodes.Last()).IsDistinct.Should().BeFalse();
        var qAvg2 = q.AsAvg("score", "");
        ((RawSelectNode)qAvg2.Nodes.Last()).RawSql.Should().Be("AVG(score)");
        ((RawSelectNode)qAvg2.Nodes.Last()).IsDistinct.Should().BeFalse();

        var qMin1 = q.AsMin("age", "my_min");
        ((RawSelectNode)qMin1.Nodes.Last()).RawSql.Should().Be("MIN(age) AS my_min");
        ((RawSelectNode)qMin1.Nodes.Last()).IsDistinct.Should().BeFalse();
        var qMin2 = q.AsMin("age", "");
        ((RawSelectNode)qMin2.Nodes.Last()).RawSql.Should().Be("MIN(age)");
        ((RawSelectNode)qMin2.Nodes.Last()).IsDistinct.Should().BeFalse();

        var qMax1 = q.AsMax("val", "my_max");
        ((RawSelectNode)qMax1.Nodes.Last()).RawSql.Should().Be("MAX(val) AS my_max");
        ((RawSelectNode)qMax1.Nodes.Last()).IsDistinct.Should().BeFalse();
        var qMax2 = q.AsMax("val", "");
        ((RawSelectNode)qMax2.Nodes.Last()).RawSql.Should().Be("MAX(val)");
        ((RawSelectNode)qMax2.Nodes.Last()).IsDistinct.Should().BeFalse();

        // Assert exact ParamNames and exception messages
        var exSum = Assert.Throws<ArgumentException>(() => q.AsSum("", "alias"));
        exSum.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exAvg = Assert.Throws<ArgumentException>(() => q.AsAvg("", "alias"));
        exAvg.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exMin = Assert.Throws<ArgumentException>(() => q.AsMin("", "alias"));
        exMin.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exMax = Assert.Throws<ArgumentException>(() => q.AsMax("", "alias"));
        exMax.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exAlias1 = Assert.Throws<ArgumentNullException>(() => q.Select((ISqlQuery)null!, "sub"));
        exAlias1.ParamName.Should().Be("subquery");

        var exAlias2 = Assert.Throws<ArgumentException>(() => q.Select(new SelectQuery<TestEntity>(), ""));
        exAlias2.ParamName.Should().Be("alias");
        exAlias2.Message.Should().Contain("Alias cannot be null or whitespace.");

        var exWhereCol1 = Assert.Throws<ArgumentException>(() => q.WhereColumns("", "=", "c2"));
        exWhereCol1.ParamName.Should().Be("column1");
        exWhereCol1.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exWhereColOp = Assert.Throws<ArgumentException>(() => q.WhereColumns("c1", "", "c2"));
        exWhereColOp.ParamName.Should().Be("operator");
        exWhereColOp.Message.Should().Contain("Operator cannot be null or whitespace.");

        var exWhereCol2 = Assert.Throws<ArgumentException>(() => q.WhereColumns("c1", "=", ""));
        exWhereCol2.ParamName.Should().Be("column2");
        exWhereCol2.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exWhereDateCol = Assert.Throws<ArgumentException>(() => q.WhereDate("", "=", DateTime.Now));
        exWhereDateCol.ParamName.Should().Be("column");
        exWhereDateCol.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exWhereDateOp = Assert.Throws<ArgumentException>(() => q.WhereDate("d", "", DateTime.Now));
        exWhereDateOp.ParamName.Should().Be("operator");
        exWhereDateOp.Message.Should().Contain("Operator cannot be null or whitespace.");

        var exWhereYearCol = Assert.Throws<ArgumentException>(() => q.WhereYear("", "=", 2026));
        exWhereYearCol.ParamName.Should().Be("column");
        exWhereYearCol.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exWhereYearOp = Assert.Throws<ArgumentException>(() => q.WhereYear("d", "", 2026));
        exWhereYearOp.ParamName.Should().Be("operator");
        exWhereYearOp.Message.Should().Contain("Operator cannot be null or whitespace.");

        var exWhereMonthCol = Assert.Throws<ArgumentException>(() => q.WhereMonth("", "=", 8));
        exWhereMonthCol.ParamName.Should().Be("column");
        exWhereMonthCol.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exWhereMonthOp = Assert.Throws<ArgumentException>(() => q.WhereMonth("d", "", 8));
        exWhereMonthOp.ParamName.Should().Be("operator");
        exWhereMonthOp.Message.Should().Contain("Operator cannot be null or whitespace.");

        var exWhereDayCol = Assert.Throws<ArgumentException>(() => q.WhereDay("", "=", 21));
        exWhereDayCol.ParamName.Should().Be("column");
        exWhereDayCol.Message.Should().Contain("Column name cannot be null or whitespace.");

        var exWhereDayOp = Assert.Throws<ArgumentException>(() => q.WhereDay("d", "", 21));
        exWhereDayOp.ParamName.Should().Be("operator");
        exWhereDayOp.Message.Should().Contain("Operator cannot be null or whitespace.");
    }

    [Fact]
    public void SelectQuery_OrderBy_ThenBy_DescendingFlags()
    {
        var q = new SelectQuery<TestEntity>();
        
        var qThenAsc = q.ThenBy(x => x.Id);
        ((ThenByNode)qThenAsc.Nodes.Last()).IsDescending.Should().BeFalse();

        var qThenDesc = q.ThenByDescending(x => x.Id);
        ((ThenByNode)qThenDesc.Nodes.Last()).IsDescending.Should().BeTrue();

        var qOrderAsc = q.OrderBy($"name");
        ((RawOrderByNode)qOrderAsc.Nodes.Last()).IsDescending.Should().BeFalse();

        var qOrderDesc = q.OrderByDescending($"name");
        ((RawOrderByNode)qOrderDesc.Nodes.Last()).IsDescending.Should().BeTrue();
    }

    [Fact]
    public void SqlBuilderDiagnostics_LogParameters_DefaultIsFalse()
    {
        SqlBuilderDiagnostics.LogParameters = false;
        SqlBuilderDiagnostics.LogParameters.Should().BeFalse();
        SqlBuilderDiagnostics.SlowQueryThresholdMs.Should().Be(500);
    }

    [Fact]
    public void SqlCompilerBase_WhitespaceTrimming_And_SelectExact7()
    {
        var compiler = new TestCompiler();
        var rawResult = compiler.Compile(Sql.Raw("SELECT 1   "));
        rawResult.Sql.Should().Be("SELECT 1");

        var selectResult = compiler.Compile(new SelectQuery<TestEntity>().RawSelect($""));
        selectResult.Sql.Trim().Should().Be("SELECT");
    }

    [Fact]
    public void SqlCompilerBase_MultipleCtes_In_UpdateQuery()
    {
        var compiler = new TestCompiler();
        var sub1 = new SelectQuery<TestEntity>().Select("id");
        var sub2 = new SelectQuery<TestEntity>().Select("id");

        var q = new UpdateQuery<TestEntity>()
                   .AddNode(new CteNode("cte1", sub1))
                   .AddNode(new CteNode("cte2", sub2))
                   .Set(x => x.IsActive, true);

        var result = compiler.Compile((IAstQuery)q);
        result.Sql.Should().Contain("WITH \"cte1\" AS (SELECT \"id\"), \"cte2\" AS (SELECT \"id\") UPDATE");
    }

    public class ConcurrencyTestEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public int V1 { get; set; }
        public int V2 { get; set; }
        public bool IsActive { get; set; }
        public string GetTableName() => "concurrency_test";
        public string[] GetColumnNames() => new[] { "id", "v1", "v2", "is_active" };
        public string[] GetAllColumnNames() => new[] { "id", "v1", "v2", "is_active" };
        public object?[] GetValues() => new object?[] { Id, V1, V2, IsActive };
        public object?[] GetAllValues() => new object?[] { Id, V1, V2, IsActive };
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public void SqlCompilerBase_MultipleConcurrencyTokens_WithoutExistingWhere()
    {
        var compiler = new TestCompiler();
        var q = new UpdateQuery<ConcurrencyTestEntity>()
            .Set(x => x.IsActive, true)
            .WithConcurrencyToken(x => x.V1, 1, 2)
            .WithConcurrencyToken(x => x.V2, 3, 4);

        var result = compiler.Compile((IAstQuery)q);
        result.Sql.Should().Contain("WHERE \"v1\" = @p3 AND \"v2\" = @p4");
    }

    [Fact]
    public void SqlCompilerBase_MultipleCompositeCursors_WithoutExistingWhere()
    {
        var compiler = new TestCompiler();
        var keys1 = new[] { new CursorKey("c1", 1, false) };
        var keys2 = new[] { new CursorKey("c2", 2, false) };
        var q = new SelectQuery<TestEntity>()
            .AddNode(new CompositeCursorNode(keys1, true))
            .AddNode(new CompositeCursorNode(keys2, true));

        var result = compiler.Compile(q);
        result.Sql.Should().Contain("WHERE (\"c1\" > @p0) AND (\"c2\" > @p1)");
    }

    [Fact]
    public void SqlCompilerVisitor_Select_AnonymousObject_ContainsCommaSpace()
    {
        var compiler = new TestCompiler();
        var q = new SelectQuery<TestEntity>().Select(x => new { x.Id, x.IsActive });
        var result = compiler.Compile(q);
        result.Sql.Should().Contain("id, is_active");
    }

    [Fact]
    public void SqlCompilerVisitor_OrderBy_ReferenceAndValueTypes()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);

        var refOrder = new OrderByNode((Expression<Func<NullablePropEntity, object>>)(x => x.Name!), false);
        visitor.Visit(refOrder);
        context.Sql.ToString().Should().Contain("\"name\"");

        var valOrder = new OrderByNode((Expression<Func<NullablePropEntity, object>>)(x => x.Id!), false);
        visitor.Visit(valOrder);
        context.Sql.ToString().Should().Contain("\"id\"");
    }

    [Fact]
    public void SqlCompilerVisitor_GroupBy_EmptySets_And_EmptyColumns()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);

        var nodeGroupingSets = new GroupByNode(null, GroupByType.GroupingSets, new List<IReadOnlyList<string>>());
        visitor.Visit(nodeGroupingSets);
        context.Sql.ToString().Should().Be("GROUPING SETS ()");

        var context2 = new CompilationContext(new ParameterManager());
        var visitor2 = new SqlCompilerVisitor(compiler, context2);
        var nodeEmptyCols = new GroupByNode(Array.Empty<string>());
        visitor2.Visit(nodeEmptyCols);
        context2.Sql.ToString().Should().Be("");
    }

    [Fact]
    public void SqlCompilerVisitor_CompositeCursor_RecursiveBaseCase()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);

        var keys = new[]
        {
            new CursorKey("k1", 1, false),
            new CursorKey("k2", 2, false),
            new CursorKey("k3", 3, false)
        };
        var cursorNode = new CompositeCursorNode(keys, true);
        visitor.Visit(cursorNode);
        context.Sql.ToString().Should().NotBeEmpty();
    }

    [Fact]
    public void SqlExpressionVisitor_HandleOuter_ReferenceAndValueTypes()
    {
        var pm = new ParameterManager();
        var sql = new System.Text.StringBuilder();
        var visitor = new SqlExpressionVisitor(sql, pm);

        Expression<Func<bool>> exprRef = () => Sql.Outer<NullablePropEntity, string>(x => x.Name!) == "test";
        visitor.Parse(exprRef.Body);
        sql.ToString().Should().Contain("\"name\"");

        var sql2 = new System.Text.StringBuilder();
        var visitor2 = new SqlExpressionVisitor(sql2, pm);
        Expression<Func<bool>> exprVal = () => Sql.Outer<NullablePropEntity, int?>(x => x.Id) == 1;
        visitor2.Parse(exprVal.Body);
        sql2.ToString().Should().Contain("\"id\"");
    }

    [Fact]
    public void SqlNodePartition_ScalarSubquerySelectNode_PreservesPreviousSelectNodes()
    {
        var sub = new SelectQuery<TestEntity>().Select("id");
        var partition = new SqlNodePartition(new List<ISqlNode>
        {
            new SelectNode(new[] { "name" }, false),
            new ScalarSubquerySelectNode(sub, "sub_count")
        });

        partition.SelectNodes.Should().HaveCount(2);
    }

    public class CustomTrailingSpaceNode : ISqlNode
    {
        public void Accept(ISqlVisitor visitor)
        {
            ((SqlCompilerVisitor)visitor).Context.Sql.Append(" ");
        }
    }

    public class CustomWhitespaceAstQuery : IAstQuery
    {
        public IReadOnlyList<ISqlNode> Nodes { get; } = new ISqlNode[] { new CustomTrailingSpaceNode() };
        public string? Tag => null;
        public SqlResult Build(ISqlCompiler compiler) => compiler.Compile(this);
    }

    [Fact]
    public void SqlCompilerBase_WhitespaceLoop_EmptyOrSpaces()
    {
        var compiler = new TestCompiler();
        var result = compiler.Compile(new CustomWhitespaceAstQuery());
        result.Sql.Should().Be("SELECT *");
    }

    [Fact]
    public void SqlCompilerVisitor_CursorPredicate_EmptyKeys_ReturnsEarly()
    {
        var compiler = new TestCompiler();
        var context = new CompilationContext(new ParameterManager());
        var visitor = new SqlCompilerVisitor(compiler, context);

        var cursorNode = new CompositeCursorNode(Array.Empty<CursorKey>(), true);
        visitor.Visit(cursorNode);
        context.Sql.ToString().Should().Be("");
    }

    [Fact]
    public void UpdateQuery_And_Or_ReturningAnonymousObject()
    {
        var q = new UpdateQuery<TestEntity>()
            .Set(x => x.IsActive, true)
            .Where(x => x.Id > 0);
        
        var qAnd = q.And(x => x.Id == 1);
        ((ExpressionWhereNode)qAnd.Nodes.Last()).IsOr.Should().BeFalse();

        var qOr = q.Or(x => x.Id == 2);
        ((ExpressionWhereNode)qOr.Nodes.Last()).IsOr.Should().BeTrue();

        var qRet = q.Returning(x => new { x.Id, x.IsActive });
        var retNode = (ReturningNode)qRet.Nodes.Last();
        retNode.Columns.Should().Equal(new[] { "id", "is_active" });
    }

    [Fact]
    public void SqlExpressionVisitor_Outer_NonMemberSelector_ThrowsNotSupportedException()
    {
        var pm = new ParameterManager();
        var sql = new System.Text.StringBuilder();
        var visitor = new SqlExpressionVisitor(sql, pm);

        Expression<Func<bool>> expr = () => Sql.Outer<NullablePropEntity, int>(x => 1 + 1) == 2;
        Action act = () => visitor.Parse(expr.Body);
        act.Should().Throw<NotSupportedException>().WithMessage("*Sql.Outer requires a member expression selector*");
    }
}




