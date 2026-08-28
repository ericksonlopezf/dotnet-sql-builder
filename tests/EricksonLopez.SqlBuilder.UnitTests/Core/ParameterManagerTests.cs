// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Core;

public class ParameterManagerTests
{
    [Fact]
    public void ParameterManager_WhenExceedingMaxParameters_ThrowsInvalidOperationException()
    {
        var pm = new ParameterManager("@", maxParameters: 3);

        pm.Add("val1"); // @p0
        pm.Add("val2"); // @p1
        pm.Add("val3"); // @p2

        Action act = () => pm.Add("val4");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Maximum number of parameters (3) exceeded.*");
    }

    [Fact]
    public void ParameterManager_WithCustomPrefix_FormatsKeysWithPrefix()
    {
        var pm = new ParameterManager(":", maxParameters: 100);

        var param1 = pm.Add("test1");
        var param2 = pm.Add("test2");

        param1.Should().Be(":p0");
        param2.Should().Be(":p1");

        var dict = pm.GetParameters();
        dict.Should().ContainKey("p0").WhoseValue.Should().Be("test1");
        dict.Should().ContainKey("p1").WhoseValue.Should().Be("test2");
    }

    [Fact]
    public void ParameterManager_WithNullValue_StoresNullCorrectly()
    {
        var pm = new ParameterManager();
        var pName = pm.Add(null);

        pName.Should().Be("@p0");
        var dict = pm.GetParameters();
        dict.Should().ContainKey("p0");
        dict["p0"].Should().BeNull();
    }

    [Fact]
    public void ParameterManager_AddNamed_StoresCustomNameAndValue()
    {
        var pm = new ParameterManager("@");
        var pName = pm.AddNamed("custom_param", 12345);

        pName.Should().Be("@custom_param");
        var dict = pm.GetParameters();
        dict.Should().ContainKey("custom_param").WhoseValue.Should().Be(12345);
    }
}
