// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class ParamTests
{
    private enum DummyEnum { ValueA, ValueB }

    [Fact]
    public void Json_ReturnsSqlParameterWithJsonType()
    {
        var p = Param.Json(new { A = 1 });
        p.DatabaseTypeName.Should().Be("json");
    }

    [Fact]
    public void Jsonb_ReturnsSqlParameterWithJsonbType()
    {
        var p = Param.Jsonb(new { A = 1 });
        p.DatabaseTypeName.Should().Be("jsonb");
    }

    [Fact]
    public void Array_ReturnsSqlParameter()
    {
        var p = Param.Array(new[] { 1, 2, 3 });
        p.Value.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void In_ReturnsSqlParameter()
    {
        var list = new List<int> { 1, 2, 3 };
        var p = Param.In(list);
        p.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public void Composite_ReturnsSqlParameterWithCompositeType()
    {
        var p = Param.Composite(new { A = 1 }, "my_type");
        p.DatabaseTypeName.Should().Be("my_type");
    }

    [Fact]
    public void EnumAsString_ReturnsSqlParameterWithString()
    {
        var p = Param.EnumAsString(DummyEnum.ValueB);
        p.Value.Should().Be("ValueB");
    }
}



