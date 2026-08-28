// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests.Nodes;

public class NodeCoverageTests
{
    private readonly IQueryFingerprinter _fingerprinter = Substitute.For<IQueryFingerprinter>();
    private readonly ISqlVisitor _visitor = Substitute.For<ISqlVisitor>();
    private readonly ISqlQuery _mockQuery = Substitute.For<ISqlQuery>();

    [Fact]
    public void CaseNodes_PropertiesAndAccept_Covered()
    {
        var branch = new CaseWhenBranch("x > 0", new object?[] { 0 }, "'positive'", null);
        branch.WhenSql.Should().Be("x > 0");
        branch.WhenParameters.Should().Equal(new object?[] { 0 });
        branch.ThenSql.Should().Be("'positive'");
        branch.ThenParameters.Should().BeNull();

        var caseNode = new CaseNode(new[] { branch }, "DEFAULT", new object?[] { "def" }, "status_text");
        caseNode.Branches.Should().ContainSingle().Which.Should().BeSameAs(branch);
        caseNode.ElseSql.Should().Be("DEFAULT");
        caseNode.ElseParameters.Should().Equal(new object?[] { "def" });
        caseNode.Alias.Should().Be("status_text");

        caseNode.Accept(_visitor);
        _visitor.Received(1).Visit(caseNode);
    }

    [Fact]
    public void CompositeCursorNodes_PropertiesAndAccept_Covered()
    {
        var key1 = new CursorKey("created_at", DateTime.UtcNow, true);
        key1.ColumnName.Should().Be("created_at");
        key1.Value.Should().NotBeNull();
        key1.IsDescending.Should().BeTrue();

        var key2 = new CursorKey("id", 123);
        key2.IsDescending.Should().BeFalse();

        var cursorNode = new CompositeCursorNode(new[] { key1, key2 }, IsAfter: false);
        cursorNode.Keys.Should().Equal(key1, key2);
        cursorNode.IsAfter.Should().BeFalse();

        cursorNode.Accept(_visitor);
        _visitor.Received(1).Visit(cursorNode);
    }

    [Fact]
    public void ConcurrencyTokenNode_PropertiesAndAccept_Covered()
    {
        var tokenNode = new ConcurrencyTokenNode("version", 1, 2, AutoIncrement: false);
        tokenNode.ColumnName.Should().Be("version");
        tokenNode.ExpectedValue.Should().Be(1);
        tokenNode.NewValue.Should().Be(2);
        tokenNode.AutoIncrement.Should().BeFalse();

        tokenNode.Accept(_visitor);
        _visitor.Received(1).Visit(tokenNode);
    }

    [Fact]
    public void GroupByNode_FingerprintBranches_Covered()
    {
        var fp = Substitute.For<IQueryFingerprinter>();
        var cols = new[] { "dept_id", "job_id" };
        var sets = new[] { new[] { "dept_id" }, new[] { "job_id" } };

        var nodeSets = new GroupByNode(cols, GroupByType.GroupingSets, sets);
        nodeSets.Columns.Should().Equal(cols);
        nodeSets.Type.Should().Be(GroupByType.GroupingSets);
        nodeSets.Sets.Should().BeSameAs(sets);

        nodeSets.ContributeToFingerprint(fp);
        fp.Received(1).Contribute("GroupByNode");
        fp.Received(1).Contribute("GroupingSets");
        fp.Received(2).Contribute("dept_id"); // once in columns, once in sets
        fp.Received(2).Contribute("job_id");  // once in columns, once in sets

        var fp2 = Substitute.For<IQueryFingerprinter>();
        nodeSets.ContributeToFingerprinter(fp2);
        fp2.Received(1).Contribute("GroupByNode");
        fp2.Received(1).Contribute("GroupingSets");
        fp2.Received(2).Contribute("dept_id");
        fp2.Received(2).Contribute("job_id");

        var fp3 = Substitute.For<IQueryFingerprinter>();
        var nodeSimple = new GroupByNode(cols, GroupByType.Rollup);
        nodeSimple.Sets.Should().BeNull();
        nodeSimple.ContributeToFingerprint(fp3);
        fp3.Received(1).Contribute("GroupByNode");
        fp3.Received(1).Contribute("Rollup");
        fp3.Received(1).Contribute("dept_id");
        fp3.Received(1).Contribute("job_id");

        nodeSets.Accept(_visitor);
        _visitor.Received(1).Visit(nodeSets);
    }

    [Fact]
    public void HavingNodes_PropertiesAndFingerprint_Covered()
    {
        Expression<Func<int, bool>> expr = x => x > 10;
        var exprHaving = new ExpressionHavingNode(expr, IsOr: true);
        exprHaving.Expression.Should().BeSameAs(expr);
        exprHaving.IsOr.Should().BeTrue();
        exprHaving.ContributeToFingerprint(_fingerprinter);
        exprHaving.Accept(_visitor);
        _visitor.Received(1).Visit(exprHaving);

        var rawHaving = new RawHavingNode("COUNT(*) > {0}", new object?[] { 5 }, IsOr: false);
        rawHaving.Condition.Should().Be("COUNT(*) > {0}");
        rawHaving.Parameters.Should().Equal(new object?[] { 5 });
        rawHaving.IsOr.Should().BeFalse();
        rawHaving.ContributeToFingerprint(_fingerprinter);
        rawHaving.Accept(_visitor);
        _visitor.Received(1).Visit(rawHaving);
    }

    [Fact]
    public void CteNode_PropertiesAndAccept_Covered()
    {
        var cte = new CteNode("cte1", _mockQuery, true);
        cte.Name.Should().Be("cte1");
        cte.Query.Should().BeSameAs(_mockQuery);
        cte.IsRecursive.Should().BeTrue();

        cte.Accept(_visitor);
        _visitor.Received(1).Visit(cte);
    }

    [Fact]
    public void DeleteNode_PropertiesFingerprintAndAccept_Covered()
    {
        var del = new DeleteNode("users");
        del.TableName.Should().Be("users");
        del.ContributeToFingerprint(_fingerprinter);
        del.Accept(_visitor);
        _visitor.Received(1).Visit(del);
    }

    [Fact]
    public void ExistsWhereNode_PropertiesAndAccept_Covered()
    {
        var exists = new ExistsWhereNode(_mockQuery, false);
        exists.Subquery.Should().BeSameAs(_mockQuery);
        exists.IsNot.Should().BeFalse();
        exists.Accept(_visitor);
        _visitor.Received(1).Visit(exists);
    }

    [Fact]
    public void FromNodes_PropertiesFingerprintAndAccept_Covered()
    {
        var from = new FromNode("customers", "c");
        from.TableName.Should().Be("customers");
        from.Alias.Should().Be("c");
        from.ContributeToFingerprint(_fingerprinter);
        from.Accept(_visitor);
        _visitor.Received(1).Visit(from);

        var subqueryFrom = new SubqueryFromNode(_mockQuery, "sq");
        subqueryFrom.Query.Should().BeSameAs(_mockQuery);
        subqueryFrom.Alias.Should().Be("sq");
        subqueryFrom.ContributeToFingerprint(_fingerprinter);
        subqueryFrom.Accept(_visitor);
        _visitor.Received(1).Visit(subqueryFrom);

        var unnest = new UnnestNode(new object?[] { 1, 2, 3 }, "u");
        unnest.Arrays.Should().Equal(1, 2, 3);
        unnest.Alias.Should().Be("u");
        unnest.ContributeToFingerprint(_fingerprinter);
        unnest.Accept(_visitor);
        _visitor.Received(1).Visit(unnest);
    }

    [Fact]
    public void InsertNodes_PropertiesFingerprintAndAccept_Covered()
    {
        var insert = new InsertNode("orders", new[] { "id", "total" });
        insert.TableName.Should().Be("orders");
        insert.Columns.Should().Equal("id", "total");
        insert.ContributeToFingerprint(_fingerprinter);
        insert.Accept(_visitor);
        _visitor.Received(1).Visit(insert);

        var valSets = new List<IReadOnlyList<object?>> { new object?[] { 1, 100 } };
        var values = new ValuesNode(valSets);
        values.ValuesSets.Should().BeSameAs(valSets);
        ((ISqlNode)values).ContributeToFingerprint(_fingerprinter);
        values.Accept(_visitor);
        _visitor.Received(1).Visit(values);

        var returning = new ReturningNode(new[] { "id", "created_at" });
        returning.Columns.Should().Equal("id", "created_at");
        returning.ContributeToFingerprint(_fingerprinter);
        returning.Accept(_visitor);
        _visitor.Received(1).Visit(returning);

        Expression<Func<int, int>> updateExpr = x => x + 1;
        var onConflict = new OnConflictNode(new[] { "id" }, "DO UPDATE SET total = excluded.total", updateExpr, new object?[] { 1 });
        onConflict.TargetColumns.Should().Equal("id");
        onConflict.UpdateAction.Should().Be("DO UPDATE SET total = excluded.total");
        onConflict.UpdateExpression.Should().BeSameAs(updateExpr);
        onConflict.Parameters.Should().Equal(new object?[] { 1 });
        ((ISqlNode)onConflict).ContributeToFingerprint(_fingerprinter);
        onConflict.Accept(_visitor);
        _visitor.Received(1).Visit(onConflict);

        var defaultVals = new DefaultValuesNode();
        ((ISqlNode)defaultVals).ContributeToFingerprint(_fingerprinter);
        defaultVals.Accept(_visitor);
        _visitor.Received(1).Visit(defaultVals);

        var insertSelect = new InsertSelectNode("archive_orders", new[] { "c1", "c2" }, _mockQuery);
        insertSelect.TableName.Should().Be("archive_orders");
        insertSelect.Columns.Should().Equal("c1", "c2");
        insertSelect.SelectQuery.Should().BeSameAs(_mockQuery);
        ((ISqlNode)insertSelect).ContributeToFingerprint(_fingerprinter);
        insertSelect.Accept(_visitor);
        _visitor.Received(1).Visit(insertSelect);
    }

    [Fact]
    public void JoinNodes_PropertiesFingerprintAndAccept_Covered()
    {
        Expression<Func<int, bool>> onExpr = x => x == 1;
        var join = new JoinNode(JoinType.Inner, "orders", "o", "o.user_id = users.id", onExpr);
        join.Type.Should().Be(JoinType.Inner);
        join.TableName.Should().Be("orders");
        join.Alias.Should().Be("o");
        join.RawCondition.Should().Be("o.user_id = users.id");
        join.ExpressionCondition.Should().BeSameAs(onExpr);
        join.ContributeToFingerprint(_fingerprinter);
        join.Accept(_visitor);
        _visitor.Received(1).Visit(join);

        var rawJoin = new RawJoinNode("LEFT JOIN audit ON audit.user_id = users.id", new object?[] { 1 });
        rawJoin.JoinSql.Should().Be("LEFT JOIN audit ON audit.user_id = users.id");
        rawJoin.Parameters.Should().Equal(new object?[] { 1 });
        rawJoin.ContributeToFingerprint(_fingerprinter);
        rawJoin.Accept(_visitor);
        _visitor.Received(1).Visit(rawJoin);

        var astQuery = Substitute.For<IAstQuery>();
        var subqueryJoin = new SubqueryJoinNode(JoinType.Left, astQuery, "sq_alias", "sq_alias.id = users.id", true, onExpr);
        subqueryJoin.Type.Should().Be(JoinType.Left);
        subqueryJoin.Subquery.Should().BeSameAs(astQuery);
        subqueryJoin.Alias.Should().Be("sq_alias");
        subqueryJoin.OnCondition.Should().Be("sq_alias.id = users.id");
        subqueryJoin.IsLateral.Should().BeTrue();
        subqueryJoin.ExpressionCondition.Should().BeSameAs(onExpr);
        subqueryJoin.ContributeToFingerprint(_fingerprinter);
        subqueryJoin.Accept(_visitor);
        _visitor.Received(1).Visit(subqueryJoin);
    }

    [Fact]
    public void LimitOffsetNode_PropertiesFingerprintAndAccept_Covered()
    {
        var limitOffset = new LimitOffsetNode(20, 40);
        limitOffset.Limit.Should().Be(20);
        limitOffset.Offset.Should().Be(40);
        limitOffset.ContributeToFingerprint(_fingerprinter);
        limitOffset.Accept(_visitor);
        _visitor.Received(1).Visit(limitOffset);
    }

    private sealed class DummySqlQuery(string sql) : ISqlQuery
    {
        public string? Tag => null;
        public SqlResult Build(ISqlCompiler compiler) => new(sql, new Dictionary<string, object?>());
        public override string ToString() => sql;
    }

    [Fact]
    public void ScalarSubquerySelectNode_PropertiesFingerprintAndAccept_Covered()
    {
        var rawQuery = new DummySqlQuery("SELECT COUNT(*) FROM orders");
        var node = new ScalarSubquerySelectNode(rawQuery, "order_count");
        node.Subquery.Should().BeSameAs(rawQuery);
        node.Alias.Should().Be("order_count");
        var fp1 = Substitute.For<IQueryFingerprinter>();
        ((ISqlNode)node).ContributeToFingerprint(fp1);
        fp1.Received(1).Contribute("ScalarSubquerySelectNode");
        fp1.Received(1).Contribute("order_count");
        fp1.Received(1).Contribute("SELECT COUNT(*) FROM orders");
        node.Accept(_visitor);
        _visitor.Received(1).Visit(node);

        var childNode = Substitute.For<ISqlNode>();
        var astQuery = Substitute.For<IAstQuery>();
        astQuery.Nodes.Returns(new[] { childNode });
        var astNode = new ScalarSubquerySelectNode(astQuery, "sub_alias");
        var fp2 = Substitute.For<IQueryFingerprinter>();
        ((ISqlNode)astNode).ContributeToFingerprint(fp2);
        fp2.Received(1).Contribute("ScalarSubquerySelectNode");
        fp2.Received(1).Contribute("sub_alias");
        childNode.Received(1).ContributeToFingerprint(fp2);

        var nullSubqueryNode = new ScalarSubquerySelectNode(null!, "null_alias");
        var fp3 = Substitute.For<IQueryFingerprinter>();
        ((ISqlNode)nullSubqueryNode).ContributeToFingerprint(fp3);
        fp3.Received(1).Contribute("ScalarSubquerySelectNode");
        fp3.Received(1).Contribute("null_alias");

        var nullStringQuery = new NullToStringQuery();
        var nullStringNode = new ScalarSubquerySelectNode(nullStringQuery, "null_str_alias");
        var fp4 = Substitute.For<IQueryFingerprinter>();
        ((ISqlNode)nullStringNode).ContributeToFingerprint(fp4);
        fp4.Received(1).Contribute("ScalarSubquerySelectNode");
        fp4.Received(1).Contribute("null_str_alias");
        fp4.Received(1).Contribute("");
    }

    private sealed class NullToStringQuery : ISqlQuery
    {
        public string? Tag => null;
        public SqlResult Build(ISqlCompiler compiler) => new SqlResult("", new Dictionary<string, object?>());
        public override string? ToString() => null;
    }

    [Fact]
    public void OrderByNodes_PropertiesFingerprintAndAccept_Covered()
    {
        Expression<Func<int, int>> expr1 = x => x;
        var orderBy = new OrderByNode(expr1, true, NullsPosition.Last);
        orderBy.KeySelector.Should().BeSameAs(expr1);
        orderBy.IsDescending.Should().BeTrue();
        orderBy.Nulls.Should().Be(NullsPosition.Last);
        orderBy.ContributeToFingerprint(_fingerprinter);
        orderBy.Accept(_visitor);
        _visitor.Received(1).Visit(orderBy);

        Expression<Func<int, string>> expr2 = x => x.ToString();
        var thenBy = new ThenByNode(expr2, false, NullsPosition.First);
        thenBy.KeySelector.Should().BeSameAs(expr2);
        thenBy.IsDescending.Should().BeFalse();
        thenBy.Nulls.Should().Be(NullsPosition.First);
        thenBy.ContributeToFingerprint(_fingerprinter);
        thenBy.Accept(_visitor);
        _visitor.Received(1).Visit(thenBy);

        var rawOrderBy = new RawOrderByNode("RANDOM()", true, new object?[] { 1 });
        rawOrderBy.Condition.Should().Be("RANDOM()");
        rawOrderBy.IsDescending.Should().BeTrue();
        rawOrderBy.Parameters.Should().Equal(new object?[] { 1 });
        rawOrderBy.ContributeToFingerprint(_fingerprinter);
        rawOrderBy.Accept(_visitor);
        _visitor.Received(1).Visit(rawOrderBy);
    }

    [Fact]
    public void SelectNodes_PropertiesFingerprintAndAccept_Covered()
    {
        var sel = new SelectNode(new[] { "id", "name" }, true);
        sel.Columns.Should().Equal("id", "name");
        sel.IsDistinct.Should().BeTrue();
        sel.ContributeToFingerprint(_fingerprinter);
        sel.Accept(_visitor);
        _visitor.Received(1).Visit(sel);

        Expression<Func<int, string>> expr = x => x.ToString();
        var exprSel = new ExpressionSelectNode(expr, true);
        exprSel.Selector.Should().BeSameAs(expr);
        exprSel.IsDistinct.Should().BeTrue();
        exprSel.ContributeToFingerprint(_fingerprinter);
        exprSel.Accept(_visitor);
        _visitor.Received(1).Visit(exprSel);

        var queryAlias = new QueryAliasNode("sub_tbl");
        queryAlias.Alias.Should().Be("sub_tbl");
        queryAlias.ContributeToFingerprint(_fingerprinter);
        queryAlias.Accept(_visitor);
        _visitor.Received(1).Visit(queryAlias);

        var distinctOn = new DistinctOnNode(new[] { "category_id" });
        distinctOn.Columns.Should().Equal("category_id");
        distinctOn.ContributeToFingerprint(_fingerprinter);
        distinctOn.Accept(_visitor);
        _visitor.Received(1).Visit(distinctOn);

        var rawSel = new RawSelectNode("COUNT(*)", new object?[] { 1 }, true);
        rawSel.RawSql.Should().Be("COUNT(*)");
        rawSel.Parameters.Should().Equal(new object?[] { 1 });
        rawSel.IsDistinct.Should().BeTrue();
        rawSel.ContributeToFingerprint(_fingerprinter);
        rawSel.Accept(_visitor);
        _visitor.Received(1).Visit(rawSel);
    }

    [Fact]
    public void SetOperationNodes_PropertiesFingerprintAndAccept_Covered()
    {
        var setOp = new SetOperationNode("UNION ALL", _mockQuery);
        setOp.Operation.Should().Be("UNION ALL");
        setOp.Query.Should().BeSameAs(_mockQuery);
        ((ISqlNode)setOp).ContributeToFingerprint(_fingerprinter);
        setOp.Accept(_visitor);
        _visitor.Received(1).Visit(setOp);
    }

    private record TestSqlExtensionNode : SqlExtensionNode
    {
        public override void Accept(ISqlVisitor visitor) => visitor.VisitExtension(this);
    }

    [Fact]
    public void SqlExtensionNode_PropertiesFingerprintAndAccept_Covered()
    {
        var ext = new TestSqlExtensionNode();
        ((ISqlNode)ext).ContributeToFingerprint(_fingerprinter);
        ext.Accept(_visitor);
        _visitor.Received(1).VisitExtension(ext);
    }

    [Fact]
    public void UpdateNodes_PropertiesFingerprintAndAccept_Covered()
    {
        var upd = new UpdateNode("users");
        upd.TableName.Should().Be("users");
        upd.ContributeToFingerprint(_fingerprinter);
        upd.Accept(_visitor);
        _visitor.Received(1).Visit(upd);

        var set = new SetNode("email", "test@test.com");
        set.Column.Should().Be("email");
        set.Value.Should().Be("test@test.com");
        set.RawExpression.Should().BeNull();
        set.Parameters.Should().BeNull();
        set.ContributeToFingerprint(_fingerprinter);
        set.Accept(_visitor);
        _visitor.Received(1).Visit(set);

        var setRaw = new SetNode(null, null, "col = col + 1", new object?[] { 1 });
        setRaw.Column.Should().BeNull();
        setRaw.Value.Should().BeNull();
        setRaw.RawExpression.Should().Be("col = col + 1");
        setRaw.Parameters.Should().Equal(new object?[] { 1 });
        setRaw.ContributeToFingerprint(_fingerprinter);
        setRaw.Accept(_visitor);
        _visitor.Received(1).Visit(setRaw);
    }

    [Fact]
    public void WhereNodes_PropertiesFingerprintAndAccept_Covered()
    {
        Expression<Func<int, bool>> expr = x => x > 5;
        var exprWhere = new ExpressionWhereNode(expr, IsOr: true);
        exprWhere.Expression.Should().BeSameAs(expr);
        exprWhere.IsOr.Should().BeTrue();
        exprWhere.ContributeToFingerprint(_fingerprinter);
        exprWhere.Accept(_visitor);
        _visitor.Received(1).Visit(exprWhere);

        var rawWhere = new RawWhereNode("age > {0}", new object?[] { 18 }, IsOr: false);
        rawWhere.Condition.Should().Be("age > {0}");
        rawWhere.Parameters.Should().Equal(new object?[] { 18 });
        rawWhere.IsOr.Should().BeFalse();
        rawWhere.ContributeToFingerprint(_fingerprinter);
        rawWhere.Accept(_visitor);
        _visitor.Received(1).Visit(rawWhere);
    }

    [Fact]
    public void WindowNodes_PropertiesFingerprintAndAccept_Covered()
    {
        var win = new WindowNode("w1", new[] { "dept_id" }, new[] { "salary DESC" });
        win.Name.Should().Be("w1");
        win.PartitionBy.Should().Equal("dept_id");
        win.OrderBy.Should().Equal("salary DESC");
        ((ISqlNode)win).ContributeToFingerprint(_fingerprinter);
        win.Accept(_visitor);
        _visitor.Received(1).Visit(win);

        var winPage = new WindowPageNode(1, 10, "id", true);
        winPage.PageNumber.Should().Be(1);
        winPage.PageSize.Should().Be(10);
        winPage.OrderByColumn.Should().Be("id");
        winPage.Descending.Should().BeTrue();
        ((ISqlNode)winPage).ContributeToFingerprint(_fingerprinter);
        winPage.Accept(_visitor);
        _visitor.Received(1).Visit(winPage);

        Expression<Func<int, bool>> filterExpr = x => x > 1000;
        var winFunc = new WindowFunctionNode(
            FunctionName: "ROW_NUMBER",
            ColumnName: null,
            Offset: 1,
            DefaultValue: 0,
            PartitionByColumns: new[] { "dept_id" },
            OrderByColumns: new[] { "salary" },
            OrderByDescending: new[] { true },
            Alias: "row_num",
            FilterExpression: filterExpr,
            FilterRaw: "salary > 1000",
            FilterRawArgs: new object?[] { 1000 });

        winFunc.FunctionName.Should().Be("ROW_NUMBER");
        winFunc.ColumnName.Should().BeNull();
        winFunc.Offset.Should().Be(1);
        winFunc.DefaultValue.Should().Be(0);
        winFunc.PartitionByColumns.Should().Equal("dept_id");
        winFunc.OrderByColumns.Should().Equal("salary");
        winFunc.OrderByDescending.Should().Equal(true);
        winFunc.Alias.Should().Be("row_num");
        winFunc.FilterExpression.Should().BeSameAs(filterExpr);
        winFunc.FilterRaw.Should().Be("salary > 1000");
        winFunc.FilterRawArgs.Should().Equal(new object?[] { 1000 });
        ((ISqlNode)winFunc).ContributeToFingerprint(_fingerprinter);
        winFunc.Accept(_visitor);
        _visitor.Received(1).Visit(winFunc);
    }
}


