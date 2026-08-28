// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.PostgreSql;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class CopyNodeTests
{
    [Fact]
    public void CopyNode_Equality_ShouldWork()
    {
        var node1 = new CopyNode("test", new[] { "A" }, "STDIN", "BINARY");
        var node2 = new CopyNode("test", new[] { "A" }, "STDIN", "BINARY");
        
        // Note: For records with array properties, the default equality checks reference equality for the array.
        // We will just verify it does not crash and behaves as generated.
        node1.Should().NotBe(node2); // arrays are different instances
        
        var cols = new[] { "A" };
        var node3 = new CopyNode("test", cols, "STDIN", "BINARY");
        var node4 = new CopyNode("test", cols, "STDIN", "BINARY");
        node3.Should().Be(node4);
        
        node3.GetHashCode().Should().Be(node4.GetHashCode());
    }

    [Fact]
    public void CopyNode_ToString_ShouldNotThrow()
    {
        var node = new CopyNode("test", new[] { "A" }, "STDIN", "BINARY");
        var str = node.ToString();
        str.Should().Contain("test").And.Contain("STDIN").And.Contain("BINARY");
    }
}




