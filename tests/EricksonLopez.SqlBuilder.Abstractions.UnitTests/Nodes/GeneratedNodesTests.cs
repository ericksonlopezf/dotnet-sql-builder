// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using NSubstitute;
using static VerifyXunit.Verifier;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests.Nodes;

public class GeneratedNodesTests
{
    public static T CreateMockQuery<T>() where T : class, ISqlQuery
    {
        var mock = Substitute.For<T>();
        mock.When(x => x.ContributeToFingerprint(Arg.Any<IQueryFingerprinter>()))
            .Do(ci => ci.Arg<IQueryFingerprinter>().Contribute("mock_query"));
        return mock;
    }

    [Fact]
    public void CaseNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new CaseNode(null!, "test", new object[] { 1 }, "test");
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void CaseNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new CaseNode(null!, "test", new object[] { 1 }, "test");
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: CaseNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void CompositeCursorNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new CompositeCursorNode(null!, true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void CompositeCursorNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new CompositeCursorNode(null!, true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: CompositeCursorNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void ConcurrencyTokenNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new ConcurrencyTokenNode("test", null!, null!, true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void ConcurrencyTokenNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new ConcurrencyTokenNode("test", null!, null!, true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: ConcurrencyTokenNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void CteNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new CteNode("test", CreateMockQuery<ISqlQuery>(), true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void CteNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new CteNode("test", CreateMockQuery<ISqlQuery>(), true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: CteNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void DefaultValuesNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new DefaultValuesNode();
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void DefaultValuesNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new DefaultValuesNode();
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: DefaultValuesNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void DeleteNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new DeleteNode("test");
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void DeleteNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new DeleteNode("test");
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: DeleteNode
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void DistinctOnNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new DistinctOnNode(new string[] { "a" });
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void DistinctOnNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new DistinctOnNode(new string[] { "a" });
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: DistinctOnNode
string: a
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void ExistsWhereNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new ExistsWhereNode(CreateMockQuery<ISqlQuery>(), true, true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void ExistsWhereNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new ExistsWhereNode(CreateMockQuery<ISqlQuery>(), true, true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: ExistsWhereNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void ExpressionHavingNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new ExpressionHavingNode(Expression.Constant(1), true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void ExpressionHavingNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new ExpressionHavingNode(Expression.Constant(1), true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: ExpressionHavingNode
bool: True
string: ?
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }



    [Fact]
    public void ExpressionSelectNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new ExpressionSelectNode(Expression.Constant(1), true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void ExpressionSelectNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new ExpressionSelectNode(Expression.Constant(1), true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: ExpressionSelectNode
bool: True
string: ?
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void ExpressionWhereNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new ExpressionWhereNode(Expression.Constant(1), true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void ExpressionWhereNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new ExpressionWhereNode(Expression.Constant(1), true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: ExpressionWhereNode
bool: True
string: ?
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void FromNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new FromNode("test", "test");
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void FromNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new FromNode("test", "test");
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: FromNode
string: test
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void GroupByNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new GroupByNode(new List<string> { "a" });
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void GroupByNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new GroupByNode(new List<string> { "a" });
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: GroupByNode
string: Standard
string: a
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void InsertNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new InsertNode("test", new List<string> { "a" });
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void InsertNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new InsertNode("test", new List<string> { "a" });
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: InsertNode
string: test
string: a
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void InsertSelectNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new InsertSelectNode("test", new string[] { "a" }, CreateMockQuery<ISqlQuery>());
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void InsertSelectNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new InsertSelectNode("test", new string[] { "a" }, CreateMockQuery<ISqlQuery>());
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: InsertSelectNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void JoinNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new JoinNode(default(JoinType), "test", "test", "test", Expression.Constant(1));
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void JoinNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new JoinNode(default(JoinType), "test", "test", "test", Expression.Constant(1));
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: JoinNode
int: 0
string: test
string: test
string: test
string: ?
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void LimitOffsetNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new LimitOffsetNode(1, 1);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void LimitOffsetNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new LimitOffsetNode(1, 1);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: LimitOffsetNode
bool: True
bool: True
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    private sealed class DummySqlQuery(string sql = "SELECT 1") : ISqlQuery
    {
        public string? Tag => null;
        public SqlResult Build(ISqlCompiler compiler) => new(sql, new Dictionary<string, object?>());
        public override string ToString() => sql;
    }

    [Fact]
    public void ScalarSubquerySelectNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var subquery = new DummySqlQuery("SELECT 1");
        var node = new ScalarSubquerySelectNode(subquery, "alias");
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void ScalarSubquerySelectNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var subquery = new DummySqlQuery("SELECT 1");
        var node = new ScalarSubquerySelectNode(subquery, "alias");
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: ScalarSubquerySelectNode
string: alias
string: SELECT 1
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void OnConflictNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new OnConflictNode(new string[] { "a" }, "test", Expression.Constant(1), new object[] { 1 });
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void OnConflictNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new OnConflictNode(new string[] { "a" }, "test", Expression.Constant(1), new object[] { 1 });
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: OnConflictNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void OrderByNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new OrderByNode(Expression.Constant(1), true, default(NullsPosition));
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void OrderByNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new OrderByNode(Expression.Constant(1), true, default(NullsPosition));
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: OrderByNode
bool: True
int: 0
string: ?
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void QueryAliasNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new QueryAliasNode("test");
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void QueryAliasNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new QueryAliasNode("test");
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: QueryAliasNode
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void RawHavingNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new RawHavingNode("test", new object[] { 1 }, true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void RawHavingNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new RawHavingNode("test", new object[] { 1 }, true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: RawHavingNode
bool: True
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void RawJoinNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new RawJoinNode("test", new object[] { 1 });
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void RawJoinNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new RawJoinNode("test", new object[] { 1 });
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: RawJoinNode
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }



    [Fact]
    public void RawOrderByNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new RawOrderByNode("test", true, new object[] { 1 });
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void RawOrderByNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new RawOrderByNode("test", true, new object[] { 1 });
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: RawOrderByNode
bool: True
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void RawSelectNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new RawSelectNode("test", new object[] { 1 }, true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void RawSelectNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new RawSelectNode("test", new object[] { 1 }, true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: RawSelectNode
bool: True
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void RawWhereNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new RawWhereNode("test", new object[] { 1 }, true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void RawWhereNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new RawWhereNode("test", new object[] { 1 }, true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: RawWhereNode
bool: True
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void ReturningNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new ReturningNode(new string[] { "a" });
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void ReturningNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new ReturningNode(new string[] { "a" });
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: ReturningNode
string: a
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void SelectNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new SelectNode(new string[] { "a" }, true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void SelectNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new SelectNode(new string[] { "a" }, true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: SelectNode
bool: True
string: a
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void SetNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new SetNode("test", null!, "test", new object[] { 1 });
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void SetNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new SetNode("test", null!, "test", new object[] { 1 });
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: SetNode
string: test
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void SetOperationNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new SetOperationNode("test", CreateMockQuery<ISqlQuery>());
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void SetOperationNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new SetOperationNode("test", CreateMockQuery<ISqlQuery>());
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: SetOperationNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void SubqueryFromNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new SubqueryFromNode(CreateMockQuery<ISqlQuery>(), "test");
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void SubqueryFromNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new SubqueryFromNode(CreateMockQuery<ISqlQuery>(), "test");
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: SubqueryFromNode
string: mock_query
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void SubqueryJoinNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new SubqueryJoinNode(default(JoinType), CreateMockQuery<IAstQuery>(), "test", "test", true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void SubqueryJoinNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new SubqueryJoinNode(default(JoinType), CreateMockQuery<IAstQuery>(), "test", "test", true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: SubqueryJoinNode
int: 0
string: mock_query
string: test
string: test
bool: True
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void ThenByNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new ThenByNode(Expression.Constant(1), true, default(NullsPosition));
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void ThenByNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new ThenByNode(Expression.Constant(1), true, default(NullsPosition));
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: ThenByNode
bool: True
int: 0
string: ?
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void UnnestNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new UnnestNode(new object[] { 1 }, "test");
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void UnnestNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new UnnestNode(new object[] { 1 }, "test");
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: UnnestNode
string: test
int: 1
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void UpdateNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new UpdateNode("test");
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void UpdateNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new UpdateNode("test");
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: UpdateNode
string: test
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void ValuesNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new ValuesNode(null!);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void ValuesNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new ValuesNode(null!);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: ValuesNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void WindowFunctionNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new WindowFunctionNode("test", "test", 1, null!, new string[] { "a" }, new string[] { "a" }, null!, "test");
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void WindowFunctionNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new WindowFunctionNode("test", "test", 1, null!, new string[] { "a" }, new string[] { "a" }, null!, "test");
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: WindowFunctionNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void WindowNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new WindowNode("test", new string[] { "a" }, new string[] { "a" });
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void WindowNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new WindowNode("test", new string[] { "a" }, new string[] { "a" });
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: WindowNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

    [Fact]
    public void WindowPageNode_Accept_CallsVisitor()
    {
        var visitor = Substitute.For<ISqlVisitor>();
        var node = new WindowPageNode(1, 1, "test", true);
        node.Accept(visitor);
        visitor.Received(1).Visit(node);
    }

    [Fact]
    public void WindowPageNode_ContributeToFingerprint_MatchesSnapshot()
    {
        var fingerprinter = new TestFingerprinter();
        var node = new WindowPageNode(1, 1, "test", true);
        ((ISqlNode)node).ContributeToFingerprint(fingerprinter);
        var expected = @"string: WindowPageNode
";
        Assert.Equal(expected.Replace("\r\n", "\n"), fingerprinter.ToString().Replace("\r\n", "\n"));
    }

}

public class TestFingerprinter : IQueryFingerprinter
{
    private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder();
    public void Contribute(string? value) => _sb.AppendLine("string: " + (value ?? "null"));
    public void Contribute(int value) => _sb.AppendLine($"int: {value}");
    public void Contribute(bool value) => _sb.AppendLine($"bool: {value}");
    public void Contribute(System.Type? value) => _sb.AppendLine("Type: " + (value?.Name ?? "null"));
    public override string ToString() => _sb.ToString();
}


