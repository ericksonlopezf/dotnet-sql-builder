// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Annotations;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

    public partial class SortEntity : EricksonLopez.SqlBuilder.Annotations.ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public static readonly IReadOnlyDictionary<string, string> PropertyMap = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "Id", "id" },
            { "Name", "name" }
        };
        public string GetTableName() => "sort_entity";
        public string[] GetColumnNames() => new[] { "id", "name" };
        public string[] GetIndexedColumns() => System.Array.Empty<string>();
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => PropertyMap;
    }

public class DynamicSortingExtensionsTests
{

    [Fact]
    public void OrderByDynamic_EmptySortBy_ReturnsSameQuery()
    {
        var query = new SelectQuery<SortEntity>();
        var result = query.OrderByDynamic("");
        result.Nodes.Should().BeEmpty();
    }

    [Fact]
    public void OrderByDynamic_ValidProperty_AddsRawOrderByNode()
    {
        var query = new SelectQuery<SortEntity>();
        var result = query.OrderByDynamic("Name");
        result.Nodes.Should().ContainSingle().Which.Should().BeOfType<EricksonLopez.SqlBuilder.Abstractions.Nodes.RawOrderByNode>();
        var node = (EricksonLopez.SqlBuilder.Abstractions.Nodes.RawOrderByNode)result.Nodes[0];
        node.Condition.Should().Be("name");
    }

    [Fact]
    public void OrderByDynamic_ValidPropertyWithAlias_AddsRawOrderByNodeWithPrefix()
    {
        var query = new SelectQuery<SortEntity>();
        var result = query.OrderByDynamic("u.Name");
        result.Nodes.Should().ContainSingle().Which.Should().BeOfType<EricksonLopez.SqlBuilder.Abstractions.Nodes.RawOrderByNode>();
        var node = (EricksonLopez.SqlBuilder.Abstractions.Nodes.RawOrderByNode)result.Nodes[0];
        node.Condition.Should().Be("u.name");
    }

    [Fact]
    public void OrderByDynamic_InvalidProperty_ValidAlphanumeric_AddsRawOrderByNodeWithSnakeCase()
    {
        var query = new SelectQuery<SortEntity>();
        var result = query.OrderByDynamic("NotMappedProperty");
        var node = (EricksonLopez.SqlBuilder.Abstractions.Nodes.RawOrderByNode)result.Nodes[0];
        node.Condition.Should().Be("not_mapped_property");
    }

    [Fact]
    public void OrderByDynamic_InvalidProperty_InvalidAlphanumeric_ThrowsArgumentException()
    {
        var query = new SelectQuery<SortEntity>();
        Action act = () => query.OrderByDynamic("Not Mapped!");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void OrderByDynamic_Descending_SetsDescendingTrue()
    {
        var query = new SelectQuery<SortEntity>();
        var result = query.OrderByDynamic("Name", descending: true);
        var node = (EricksonLopez.SqlBuilder.Abstractions.Nodes.RawOrderByNode)result.Nodes[0];
        node.IsDescending.Should().BeTrue();
    }
}





