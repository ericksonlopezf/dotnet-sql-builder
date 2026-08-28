// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class InsertQueryTests
{

    [Fact]
    public void FromSelect_WithColumns_SetsColumnsArray()
    {
        var selectQuery = Sql.From<DummyEntity>();
        var insertQuery = new InsertQuery<DummyEntity>().FromSelect(selectQuery, "Col1", "Col2");

        var node = insertQuery.Nodes.OfType<InsertSelectNode>().Single();
        node.Columns.Should().NotBeNull();
        node.Columns.Should().BeEquivalentTo("Col1", "Col2");
    }

    [Fact]
    public void FromSelect_WithoutColumns_SetsColumnsToNull()
    {
        var selectQuery = Sql.From<DummyEntity>();
        var insertQuery = new InsertQuery<DummyEntity>().FromSelect(selectQuery);

        var node = insertQuery.Nodes.OfType<InsertSelectNode>().Single();
        node.Columns.Should().BeNull();
    }
}


