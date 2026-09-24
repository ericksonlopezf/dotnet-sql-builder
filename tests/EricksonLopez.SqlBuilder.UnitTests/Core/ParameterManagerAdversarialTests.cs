// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests.Core;

public class ParameterManagerAdversarialTests
{
    [Fact]
    public void Add_WhenNamedParameterExistsWithSamePrefix_SkipsCollision()
    {
        var pm = new ParameterManager();
        // Register named parameter @p0 first
        pm.AddNamed("p0", "named_value");

        // When auto-generating, it must NOT overwrite p0
        var autoParam = pm.Add("auto_value");

        autoParam.Should().Be("@p1");
        pm.GetParameters()["p0"].Should().Be("named_value");
        pm.GetParameters()["p1"].Should().Be("auto_value");
    }

    [Fact]
    public void AddNamed_WhenDuplicateKeyWithDifferentValue_ThrowsInvalidOperationException()
    {
        var pm = new ParameterManager();
        pm.AddNamed("userId", 42);

        Action act = () => pm.AddNamed("userId", 999);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Parameter collision detected*userId*");
    }

    [Fact]
    public void AddNamed_WhenDuplicateKeyWithSameValue_ReturnsExistingParameterIdempotently()
    {
        var pm = new ParameterManager();
        var p1 = pm.AddNamed("userId", 42);
        var p2 = pm.AddNamed("userId", 42);

        p1.Should().Be("@userId");
        p2.Should().Be("@userId");
        pm.GetParameters().Count.Should().Be(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddNamed_WhenNameNullOrWhitespace_ThrowsArgumentException(string? invalidName)
    {
        var pm = new ParameterManager();
        Action act = () => pm.AddNamed(invalidName!, "val");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ParameterManager_DeeplyNestedParameters_NoCollisions()
    {
        var pm = new ParameterManager();
        for (int i = 0; i < 500; i++)
        {
            var pName = pm.Add($"value_{i}");
            pName.Should().Be($"@p{i}");
        }

        pm.GetParameters().Count.Should().Be(500);
        for (int i = 0; i < 500; i++)
        {
            pm.GetParameters()[$"p{i}"].Should().Be($"value_{i}");
        }
    }
}
