// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class DbConcurrencyExceptionTests
{
    [Fact]
    public void Constructor_WithEntityTypeNameAndRowsAffected_SetsProperties()
    {
        // Arrange
        var entityName = "User";
        var rows = 0;

        // Act
        var exception = new DbConcurrencyException(entityName, rows);

        // Assert
        exception.EntityTypeName.Should().Be(entityName);
        exception.RowsAffected.Should().Be(rows);
        exception.Message.Should().Be("Optimistic concurrency conflict detected for entity 'User'. The record was modified or deleted by another process. RowsAffected=0. Reload the entity and retry the operation.");
    }

    [Fact]
    public void Constructor_WithEntityTypeNameRowsAffectedAndInnerException_SetsProperties()
    {
        // Arrange
        var entityName = "Order";
        var rows = 0;
        var innerException = new Exception("Inner error");

        // Act
        var exception = new DbConcurrencyException(entityName, rows, innerException);

        // Assert
        exception.EntityTypeName.Should().Be(entityName);
        exception.RowsAffected.Should().Be(rows);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Message.Should().Be("Optimistic concurrency conflict detected for entity 'Order'. RowsAffected=0.");
    }
}


