// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Queries;

public class DeleteQueryTests
{

    private class OtherEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public string GetTableName() => "otherentitys";
        public string[] GetColumnNames() => new[] { "id" };
        public object?[] GetValues() => new object?[] { 1 };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
    }

    [Fact]
    public void Delete_Default_AddsDeleteNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Delete();

        var node = ((IAstQuery)result).Nodes.OfType<DeleteNode>().Last();
        node.TableName.Should().Be("dummy_entity");
    }

    [Fact]
    public void Delete_TableName_AddsDeleteNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Delete("Users");

        var node = ((IAstQuery)result).Nodes.OfType<DeleteNode>().Last();
        node.TableName.Should().Be("Users");
    }

    [Fact]
    public void Using_String_AddsFromNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Using("OtherTable", "o");

        var node = ((IAstQuery)result).Nodes.OfType<FromNode>().Single();
        node.TableName.Should().Be("OtherTable");
        node.Alias.Should().Be("o");
    }

    [Fact]
    public void Using_Generic_AddsFromNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Using<OtherEntity>("o");

        var node = ((IAstQuery)result).Nodes.OfType<FromNode>().Single();
        node.TableName.Should().Be("otherentitys");
        node.Alias.Should().Be("o");
    }

    [Fact]
    public void Join_Raw_AddsJoinNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Join("OtherTable", "o", "o.Id = dummyentitys.Id");

        var node = ((IAstQuery)result).Nodes.OfType<JoinNode>().Single();
        node.TableName.Should().Be("OtherTable");
        node.Alias.Should().Be("o");
        node.RawCondition.Should().Be("o.Id = dummyentitys.Id");
    }

    [Fact]
    public void Join_Expression_AddsJoinNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Join<DummyEntity>((a, b) => a.Id == b.Id);

        var node = ((IAstQuery)result).Nodes.OfType<JoinNode>().Single();
        node.TableName.Should().Be("dummy_entity");
        node.ExpressionCondition.Should().NotBeNull();
    }

    [Fact]
    public void Where_Expression_AddsExpressionWhereNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Where(x => x.Id == 1);

        var node = result.Nodes.OfType<ExpressionWhereNode>().Single();
        node.Expression.Should().NotBeNull();
        node.IsOr.Should().BeFalse();
    }

    [Fact]
    public void Where_Raw_AddsRawWhereNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Where((System.FormattableString)$"Id = {1}");

        var node = result.Nodes.OfType<RawWhereNode>().Single();
        node.Condition.Should().Be("Id = {0}");
        node.Parameters.Should().BeEquivalentTo(new object?[] { 1 });
    }

    [Fact]
    public void And_AddsExpressionWhereNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.And(x => x.Id == 1);

        var node = result.Nodes.OfType<ExpressionWhereNode>().Single();
        node.IsOr.Should().BeFalse();
    }

    [Fact]
    public void Or_AddsExpressionWhereNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Or(x => x.Id == 1);

        var node = result.Nodes.OfType<ExpressionWhereNode>().Single();
        node.IsOr.Should().BeTrue();
    }

    [Fact]
    public void Returning_Params_AddsReturningNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Returning("Id", "Name");

        var node = result.Nodes.OfType<ReturningNode>().Single();
        node.Columns.Should().BeEquivalentTo(new[] { "Id", "Name" });
    }

    [Fact]
    public void Returning_Expression_AddsReturningNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Returning(x => new { x.Id, x.Name });

        var node = result.Nodes.OfType<ReturningNode>().Single();
        node.Columns.Should().BeEquivalentTo(new[] { "id", "name" });
    }

    [Fact]
    public void Returning_Expression_MemberExpression_AddsReturningNode()
    {
        var query = new DeleteQuery<DummyEntity>();
        var result = query.Returning(x => x.Id);

        var node = result.Nodes.OfType<ReturningNode>().Single();
        node.Columns.Should().BeEquivalentTo(new[] { "id" });
    }
    [Fact]
    public void WhereExists_SetsIsNotAndIsOrToFalse()
    {
        var subquery = Sql.From<DummyEntity>();
        var query = new DeleteQuery<DummyEntity>().WhereExists(subquery);

        var node = query.Nodes.OfType<ExistsWhereNode>().Single();
        node.IsNot.Should().BeFalse();
        node.IsOr.Should().BeFalse();
    }

    [Fact]
    public void WhereNotExists_SetsIsNotToTrueAndIsOrToFalse()
    {
        var subquery = Sql.From<DummyEntity>();
        var query = new DeleteQuery<DummyEntity>().WhereNotExists(subquery);

        var node = query.Nodes.OfType<ExistsWhereNode>().Single();
        node.IsNot.Should().BeTrue();
        node.IsOr.Should().BeFalse();
    }
}




