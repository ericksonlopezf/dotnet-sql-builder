// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class RequiresCapabilityAttributeTests
{
    [Fact]
    public void Constructor_SetsCapability()
    {
        // Arrange
        var capability = ProviderCapability.Returning;

        // Act
        var attribute = new RequiresCapabilityAttribute(capability);

        // Assert
        attribute.Capability.Should().Be(capability);
    }
}



