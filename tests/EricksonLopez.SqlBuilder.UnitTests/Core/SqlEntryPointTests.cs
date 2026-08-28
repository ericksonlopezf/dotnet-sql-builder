// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Builders.Bulk;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlEntryPointTests
{
    private sealed class TestEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public string GetTableName() => "test_entities";
        public string[] GetColumnNames() => new[] { "id", "name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
        {
            { "Id", "id" }, { "Name", "name" }
        };
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }

    [Fact]
    public void From_CreatesSelectQueryWithFromNode()
    {
        var query = Sql.From<TestEntity>();
        query.Should().NotBeNull();
        query.Nodes.Should().ContainSingle().Which.Should().BeOfType<FromNode>()
            .Which.TableName.Should().Be("test_entities");
    }

    [Fact]
    public void Insert_CreatesInsertQueryWithValuesNode()
    {
        var entity = new TestEntity { Id = 1, Name = "Alice" };
        var query = Sql.Insert(entity);
        query.Should().NotBeNull();
        query.Nodes.Should().HaveCount(2);
        query.Nodes[0].Should().BeOfType<InsertNode>();
        query.Nodes[1].Should().BeOfType<ValuesNode>();
    }

    [Fact]
    public void BulkInsert_CreatesInsertQueryWithBulkValues()
    {
        var list = new[] { new TestEntity { Id = 1, Name = "Alice" }, new TestEntity { Id = 2, Name = "Bob" } };
        var query = Sql.BulkInsert(list);
        query.Should().NotBeNull();
        query.Nodes.Should().HaveCount(2);
        query.Nodes[0].Should().BeOfType<InsertNode>();
        query.Nodes[1].Should().BeOfType<ValuesNode>();
    }

    [Fact]
    public void Update_WithoutEntity_ReturnsUpdateBuilder()
    {
        var builder = Sql.Update<TestEntity>();
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Update_WithEntity_ReturnsConfiguredUpdateBuilder()
    {
        var entity = new TestEntity { Id = 10, Name = "Updated" };
        var builder = Sql.Update(entity);
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Delete_ReturnsDeleteBuilder()
    {
        var builder = Sql.Delete<TestEntity>();
        builder.Should().NotBeNull();
    }

    [Fact]
    public void InsertFrom_WithExplicitColumns_ConfiguresInsertSelectNode()
    {
        var selectQuery = Sql.From<TestEntity>().Where(e => e.Id > 5);
        var query = Sql.InsertFrom<TestEntity>(selectQuery, "id", "name");
        query.Nodes.Should().ContainSingle().Which.Should().BeOfType<InsertSelectNode>();
        var node = (InsertSelectNode)query.Nodes[0];
        node.TableName.Should().Be("test_entities");
        node.Columns.Should().Equal("id", "name");
        node.SelectQuery.Should().BeSameAs(selectQuery);
    }

    [Fact]
    public void InsertFrom_WithoutColumns_ConfiguresInsertSelectNodeWithNullColumns()
    {
        var selectQuery = Sql.From<TestEntity>();
        var query = Sql.InsertFrom<TestEntity>(selectQuery);
        var node = (InsertSelectNode)query.Nodes[0];
        node.TableName.Should().Be("test_entities");
        node.Columns.Should().BeNull();
        node.SelectQuery.Should().BeSameAs(selectQuery);
    }

    [Fact]
    public void Raw_WithFormattableString_ReturnsRawQuery()
    {
        int id = 42;
        FormattableString formattable = $"SELECT * FROM users WHERE id = {id}";
        var query = Sql.Raw(formattable);
        query.Should().NotBeNull();
        query.RawSql.Should().Be("SELECT * FROM users WHERE id = @p0");
        var dict = (IReadOnlyDictionary<string, object?>)query.Parameters!;
        dict.Should().ContainKey("@p0").WhoseValue.Should().Be(42);
    }

    [Fact]
    public void Raw_WithStringAndDictionaryParameters_ReturnsRawQuery()
    {
        var dict = new Dictionary<string, object?> { { "status", "active" } };
        var query = Sql.Raw("SELECT * FROM users WHERE status = @status", dict);
        query.Should().NotBeNull();
        query.RawSql.Should().Be("SELECT * FROM users WHERE status = @status");
        var paramsDict = (IReadOnlyDictionary<string, object?>)query.Parameters!;
        paramsDict.Should().ContainKey("status").WhoseValue.Should().Be("active");
    }

    private struct CustomTypeForHandlerTest { }

    [Fact]
    public void RegisterTypeHandler_StoresHandlerInDictionary()
    {
        var mockHandler = Substitute.For<ITypeHandler>();
        Sql.RegisterTypeHandler<CustomTypeForHandlerTest>(mockHandler);

        Sql.TypeHandlers.Should().ContainKey(typeof(CustomTypeForHandlerTest)).WhoseValue.Should().BeSameAs(mockHandler);
    }

    [Fact]
    public void ExpressionHelpers_DirectInvocation_ThrowsInvalidOperationException()
    {
        var actILike = () => "hello".ILike("%ell%");
        actILike.Should().Throw<InvalidOperationException>().WithMessage("*Sql.ILike*");

        var actAny = () => 1.Any(new[] { 1, 2, 3 });
        actAny.Should().Throw<InvalidOperationException>().WithMessage("*Sql.Any*");

        var actAll = () => 1.All(new[] { 1, 2, 3 });
        actAll.Should().Throw<InvalidOperationException>().WithMessage("*Sql.All*");

        var actBetween = () => 5.Between(1, 10);
        actBetween.Should().Throw<InvalidOperationException>().WithMessage("*Sql.Between*");

        var actCoalesce1 = () => ((string?)"test").Coalesce("fallback");
        actCoalesce1.Should().Throw<InvalidOperationException>().WithMessage("*Sql.Coalesce*");

        var actCoalesce2 = () => Sql.Coalesce("a", "b", "fallback");
        actCoalesce2.Should().Throw<InvalidOperationException>().WithMessage("*Sql.Coalesce*");

        var actIsDistinct = () => Sql.IsDistinctFrom(1, 2);
        actIsDistinct.Should().Throw<InvalidOperationException>().WithMessage("*Sql.IsDistinctFrom*");

        var actIsNotDistinct = () => Sql.IsNotDistinctFrom(1, 2);
        actIsNotDistinct.Should().Throw<InvalidOperationException>().WithMessage("*Sql.IsNotDistinctFrom*");

        var actNullIf = () => Sql.NullIf(1, 1);
        actNullIf.Should().Throw<InvalidOperationException>().WithMessage("*Sql.NullIf*");

        var actOuter = () => Sql.Outer<TestEntity, int>(e => e.Id);
        actOuter.Should().Throw<InvalidOperationException>().WithMessage("*Sql.Outer*");
    }
}



