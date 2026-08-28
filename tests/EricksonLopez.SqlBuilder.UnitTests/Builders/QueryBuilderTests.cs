// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Testing.DataBuilders;
using EricksonLopez.SqlBuilder.Testing.Domain;
using System.Threading.Tasks;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class QueryBuilderTests
{
    [Fact]
    public void Sql_FactoryMethods_ShouldReturnCorrectTypes()
    {
        // Act & Assert
        Sql.From<User>().Should().BeOfType<SelectQuery<User>>();
        Sql.Insert(ObjectMother.CreateUser()).Should().BeOfType<InsertQuery<User>>();
        Sql.Update<User>().Should().BeAssignableTo<EricksonLopez.SqlBuilder.Abstractions.IUpdateSetBuilder<User>>();
        Sql.Delete<User>().Should().BeAssignableTo<EricksonLopez.SqlBuilder.Abstractions.IDeleteFromBuilder<User>>();
    }

    [Fact]
    public void DeleteQuery_Methods_ReturnQuery()
    {
        var q = new DeleteQuery<User>();
        
        ((IAstQuery)q.Delete()).Nodes.Last().GetType().Name.Should().Be("DeleteNode");
        ((IAstQuery)q.Using("table", "alias")).Nodes.Last().GetType().Name.Should().Be("FromNode");
        ((IAstQuery)q.Using<User>("alias")).Nodes.Last().GetType().Name.Should().Be("FromNode");
        ((IAstQuery)q.Join("table2", "t2", "t2.id = table.id")).Nodes.Last().GetType().Name.Should().Be("JoinNode");
        ((IAstQuery)q.Join("table2", "t2", "1=1")).Nodes.Last().GetType().Name.Should().Be("JoinNode");
        ((IAstQuery)q.Where(x => x.Id == 1)).Nodes.Last().GetType().Name.Should().Be("ExpressionWhereNode");
        ((IAstQuery)q.Where((FormattableString)$"id = {1}")).Nodes.Last().GetType().Name.Should().Be("RawWhereNode");
        ((IAstQuery)q.And(x => x.Id == 2)).Nodes.Last().GetType().Name.Should().Be("ExpressionWhereNode");
        ((IAstQuery)q.Or(x => x.Id == 3)).Nodes.Last().GetType().Name.Should().Be("ExpressionWhereNode");
        ((IAstQuery)q.Returning("id")).Nodes.Last().GetType().Name.Should().Be("ReturningNode");
        ((IAstQuery)q.Returning(x => new { x.Id, x.Username })).Nodes.Last().GetType().Name.Should().Be("ReturningNode");
        ((IAstQuery)q.Returning(x => x.Id)).Nodes.Last().GetType().Name.Should().Be("ReturningNode");
    }

    [Fact]
    public void UpdateQuery_Methods_ReturnQuery()
    {
        var q = new UpdateQuery<User>();
        
        ((IAstQuery)q.Update()).Nodes.Last().GetType().Name.Should().Be("UpdateNode");
        ((IAstQuery)q.Set(ObjectMother.CreateUser(), true)).Nodes.Last().GetType().Name.Should().Be("SetNode");
        ((IAstQuery)q.Set(x => x.Username, "test")).Nodes.Last().GetType().Name.Should().Be("SetNode");
        ((IAstQuery)q.Set((FormattableString)$"name = {"test"}")).Nodes.Last().GetType().Name.Should().Be("SetNode");
        ((IAstQuery)q.From("table", "alias")).Nodes.Last().GetType().Name.Should().Be("FromNode");
        ((IAstQuery)q.Join("table2", "t2", "t2.id = table.id")).Nodes.Last().GetType().Name.Should().Be("JoinNode");
        ((IAstQuery)q.Join("table2", "t2", "1=1")).Nodes.Last().GetType().Name.Should().Be("JoinNode");
        ((IAstQuery)q.Where(x => x.Id == 1)).Nodes.Last().GetType().Name.Should().Be("ExpressionWhereNode");
        ((IAstQuery)q.Where((FormattableString)$"id = {1}")).Nodes.Last().GetType().Name.Should().Be("RawWhereNode");
        ((IAstQuery)q.And(x => x.Id == 2)).Nodes.Last().GetType().Name.Should().Be("ExpressionWhereNode");
        ((IAstQuery)q.Or(x => x.Id == 3)).Nodes.Last().GetType().Name.Should().Be("ExpressionWhereNode");
        ((IAstQuery)q.Returning("id")).Nodes.Last().GetType().Name.Should().Be("ReturningNode");
        ((IAstQuery)q.Returning(x => new { x.Id, x.Username })).Nodes.Last().GetType().Name.Should().Be("ReturningNode");
        ((IAstQuery)q.Returning(x => x.Id)).Nodes.Last().GetType().Name.Should().Be("ReturningNode");
    }

    [Fact]
    public void InsertQuery_Methods_ReturnQuery()
    {
        var q = new InsertQuery<User>();
        
        q.Into("table").Nodes.Last().GetType().Name.Should().Be("InsertNode");
        q.Values(ObjectMother.CreateUser()).Nodes.Last().GetType().Name.Should().Be("ValuesNode");
        q.Values(ObjectMother.CreateUser(), ignoreNulls: false).Nodes.Last().GetType().Name.Should().Be("ValuesNode");
        q.Values(new[] { ObjectMother.CreateUser(), ObjectMother.CreateUser() }).Nodes.Last().GetType().Name.Should().Be("ValuesNode");
        q.Bulk(new[] { ObjectMother.CreateUser(), ObjectMother.CreateUser() }, ignoreNulls: true).Nodes.Last().GetType().Name.Should().Be("ValuesNode");
        q.Values("id", "name").Nodes.Last().GetType().Name.Should().Be("ValuesNode");
        q.DefaultValues().Nodes.Last().GetType().Name.Should().Be("DefaultValuesNode");
        q.Returning("id").Nodes.Last().GetType().Name.Should().Be("ReturningNode");
        q.Returning(x => new { x.Id, x.Username }).Nodes.Last().GetType().Name.Should().Be("ReturningNode");
        q.Returning(x => x.Id).Nodes.Last().GetType().Name.Should().Be("ReturningNode");
        q.OnConflict("id").Nodes.Last().GetType().Name.Should().Be("OnConflictNode");
        q.OnConflict(x => x.Id).Nodes.Last().GetType().Name.Should().Be("OnConflictNode");
        q.OnConflict(x => new { x.Id, x.Username }).Nodes.Last().GetType().Name.Should().Be("OnConflictNode");
        q.OnConflict(x => (object)1).Nodes.Last().GetType().Name.Should().Be("OnConflictNode");
        q.OnConflict(x => "abc").Nodes.Last().GetType().Name.Should().Be("OnConflictNode");
        q.OnConflict("id").DoNothing().Nodes.Last().GetType().Name.Should().Be("OnConflictNode");
        q.OnConflict("id").DoUpdate(x => new { x.Username }).Nodes.Last().GetType().Name.Should().Be("OnConflictNode");
        q.OnConflict("id").DoUpdate((FormattableString)$"name = {"test"}").Nodes.Last().GetType().Name.Should().Be("OnConflictNode");
    }

    [Fact]
    public void SelectQuery_Methods_ReturnQuery()
    {
        var q = new SelectQuery<User>();
        
        q.Select("id", "name").Nodes.Last().GetType().Name.Should().Be("SelectNode");
        q.Select(x => x.Id).Nodes.Last().GetType().Name.Should().Be("ExpressionSelectNode");
        q.RawSelect((FormattableString)$"SELECT * FROM table").Nodes.Last().GetType().Name.Should().Be("RawSelectNode");
        q.Distinct().Nodes.Last().GetType().Name.Should().Be("SelectNode");
        
        // Ensure Distinct hits both paths (SelectNode and ExpressionSelectNode)
        var q2 = new SelectQuery<User>().Select("id").Distinct();
        q2.Nodes.Should().NotBeEmpty();
        var q3 = new SelectQuery<User>().Select(x => x.Id).Distinct();
        q3.Nodes.Should().NotBeEmpty();
        
        q.From("table", "alias").Nodes.Last().GetType().Name.Should().Be("FromNode");
        q.From(new SelectQuery<User>(), "alias").Nodes.Last().GetType().Name.Should().Be("SubqueryFromNode");
        q.Alias("alias").Nodes.Last().GetType().Name.Should().Be("QueryAliasNode");
        
        q.Join("table2", "t2", "t2.id = table.id").Nodes.Last().GetType().Name.Should().Be("JoinNode");
        q.Join("table2", "t2", "1=1").Nodes.Last().GetType().Name.Should().Be("JoinNode");
        q.InnerJoin("table2", "t2", "t2.id = table.id").Nodes.Last().GetType().Name.Should().Be("JoinNode");
        q.LeftJoin("table2", "t2", "t2.id = table.id").Nodes.Last().GetType().Name.Should().Be("JoinNode");
        q.RightJoin("table2", "t2", "t2.id = table.id").Nodes.Last().GetType().Name.Should().Be("JoinNode");
        q.CrossJoin("table2", "t2").Nodes.Last().GetType().Name.Should().Be("JoinNode");
        q.FullJoin("table2", "t2", "t2.id = table.id").Nodes.Last().GetType().Name.Should().Be("JoinNode");
        q.RawJoin((FormattableString)$"JOIN table2 ON 1=1").Nodes.Last().GetType().Name.Should().Be("RawJoinNode");
        
        q.Where(x => x.Id == 1).Nodes.Last().GetType().Name.Should().Be("ExpressionWhereNode");
        q.Where((FormattableString)$"id = {1}").Nodes.Last().GetType().Name.Should().Be("RawWhereNode");
        q.And(x => x.Id == 2).Nodes.Last().GetType().Name.Should().Be("ExpressionWhereNode");
        q.Or(x => x.Id == 3).Nodes.Last().GetType().Name.Should().Be("ExpressionWhereNode");
        
        q.GroupBy("id").Nodes.Last().GetType().Name.Should().Be("GroupByNode");
        q.Having(x => x.Id > 1).Nodes.Last().GetType().Name.Should().Be("ExpressionHavingNode");
        q.Having((FormattableString)$"id > {1}").Nodes.Last().GetType().Name.Should().Be("RawHavingNode");
        
        q.OrderBy(x => x.Id).Nodes.Last().GetType().Name.Should().Contain("ByNode");
        q.OrderByDescending(x => x.Id).Nodes.Last().GetType().Name.Should().Contain("ByNode");
        q.ThenBy(x => x.Username).Nodes.Last().GetType().Name.Should().Contain("ByNode");
        q.ThenByDescending(x => x.Username).Nodes.Last().GetType().Name.Should().Contain("ByNode");
        q.OrderBy((FormattableString)$"id").Nodes.Last().GetType().Name.Should().Be("RawOrderByNode");
        q.OrderByDescending((FormattableString)$"id").Nodes.Last().GetType().Name.Should().Be("RawOrderByNode");
        
        q.Limit(10).Nodes.Last().GetType().Name.Should().Be("LimitOffsetNode");
        q.Offset(5).Nodes.Last().GetType().Name.Should().Be("LimitOffsetNode");
        q.Fetch(10).Nodes.Last().GetType().Name.Should().Be("LimitOffsetNode");
        

        
        q.CTE("cte", new SelectQuery<User>()).Nodes.Last().GetType().Name.Should().Be("CteNode");
        q.RecursiveCTE("cte", new SelectQuery<User>()).Nodes.Last().GetType().Name.Should().Be("CteNode");
        q.Window("w", new[] { "id" }, new[] { "id" }).Nodes.Last().GetType().Name.Should().Be("WindowNode");
        
        q.Union(new SelectQuery<User>()).Nodes.Last().GetType().Name.Should().Be("SetOperationNode");
        q.UnionAll(new SelectQuery<User>()).Nodes.Last().GetType().Name.Should().Be("SetOperationNode");
        q.Intersect(new SelectQuery<User>()).Nodes.Last().GetType().Name.Should().Be("SetOperationNode");
        q.Except(new SelectQuery<User>()).Nodes.Last().GetType().Name.Should().Be("SetOperationNode");
        
        q.WindowPage(1, 10, "id", false).Nodes.Last().GetType().Name.Should().Be("WindowPageNode");
        System.Action act = () => q.WindowPage(0, 0, "id", true);
        act.Should().Throw<System.ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RawQuery_WhenGivenFormattableString_ShouldBuildCorrectly()
    {
        // Arrange
        int id = 1;
        var q = Sql.Raw((FormattableString)$"SELECT * FROM users WHERE id = {id}");

        // Act
        var built = q.Build(new EricksonLopez.SqlBuilder.Testing.MockSqlCompiler());

        // Assert
        q.RawSql.Should().Be("SELECT * FROM users WHERE id = @p0");
        q.Parameters.Should().NotBeNull();
    }

    private class MockDictionary : IDictionary<string, object?>
    {
        private readonly Dictionary<string, object?> _inner = new();
        public object? this[string key] { get => _inner[key]; set => _inner[key] = value; }
        public ICollection<string> Keys => _inner.Keys;
        public ICollection<object?> Values => _inner.Values;
        public int Count => _inner.Count;
        public bool IsReadOnly => false;
        public void Add(string key, object? value) => _inner.Add(key, value);
        public void Add(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)_inner).Add(item);
        public void Clear() => _inner.Clear();
        public bool Contains(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)_inner).Contains(item);
        public bool ContainsKey(string key) => _inner.ContainsKey(key);
        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) => ((IDictionary<string, object?>)_inner).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _inner.GetEnumerator();
        public bool Remove(string key) => _inner.Remove(key);
        public bool Remove(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)_inner).Remove(item);
        public bool TryGetValue(string key, out object? value) => _inner.TryGetValue(key, out value);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();
    }

    [Fact]
    public void RawQuery_WithIDictionary_Works()
    {
        var dict = new MockDictionary();
        dict.Add("k", "v");
        var q = new RawQuery("SELECT 1", dict);
        var p = (Dictionary<string, object?>)q.Parameters!;
        p["k"].Should().Be("v");
    }

    [Fact]
    public void RawQuery_WithIEnumerableKvp_Works()
    {
        var list = new List<KeyValuePair<string, object?>>
        {
            new("k2", "v2")
        };
        var q = new RawQuery("SELECT 1", list);
        var p = (Dictionary<string, object?>)q.Parameters!;
        p["k2"].Should().Be("v2");
    }

    [Fact]
    public void RawQuery_WithSqlEntity_Works()
    {
        var user = ObjectMother.CreateUser();
        var q = new RawQuery("SELECT 1", user);
        var p = (Dictionary<string, object?>)q.Parameters!;
        p["username"].Should().Be(user.Username);
    }
}










