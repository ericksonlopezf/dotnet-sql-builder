// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Exceptions;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class SqlValidationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Invalid AST node";

        // Act
        var exception = new SqlValidationException(message);

        // Assert
        exception.Message.Should().Be(message);
    }
}




