// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class StringBuilderPoolTests
{
    [Fact]
    public void StringBuilderPool_Get_ReturnsStringBuilderWithExpectedInitialCapacity()
    {
        // Act
        var sb = StringBuilderPool.Get();

        // Assert
        // Kills mutation: "Object initializer mutation" on InitialCapacity = 512
        sb.Capacity.Should().BeGreaterThanOrEqualTo(512);

        // Cleanup
        StringBuilderPool.Return(sb);
    }

    [Fact]
    public void StringBuilderPool_Return_DoesNotRetainLargeStringBuilders()
    {
        // Act
        var sb = StringBuilderPool.Get();
        
        // Append string larger than MaximumRetainedCapacity (4096)
        sb.Append(new string('a', 5000));
        StringBuilderPool.Return(sb);

        // Get another instance. The previous one should have been dropped
        var nextSb = StringBuilderPool.Get();

        // Assert
        // If MaximumRetainedCapacity = 4096 is respected, nextSb should be a new instance
        // or its capacity should be back to initial or less than 5000.
        // Actually, if it was dropped, a new one is created with initial capacity.
        // Kills mutation: "Object initializer mutation" on MaximumRetainedCapacity = 4096
        nextSb.Capacity.Should().BeLessThan(5000);

        // Cleanup
        StringBuilderPool.Return(nextSb);
    }
}


