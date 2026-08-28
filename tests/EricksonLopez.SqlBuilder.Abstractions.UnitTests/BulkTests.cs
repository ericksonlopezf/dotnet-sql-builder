// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Builders.Bulk;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class BulkTests
{
    public class TestEntity { }

    [Fact]
    public void BulkInsertResult_Constructor_SetsProperties()
    {
        // Arrange
        var rows = 5;
        var entities = new List<TestEntity> { new TestEntity() };

        // Act
        var result = new BulkInsertResult<TestEntity>(rows, entities);

        // Assert
        result.RowsAffected.Should().Be(rows);
        result.InsertedEntities.Should().BeSameAs(entities);
    }

    [Fact]
    public void BulkInsertResult_WithoutIdentities_SetsEmptyList()
    {
        // Arrange & Act
        var result = BulkInsertResult<TestEntity>.WithoutIdentities(10);

        // Assert
        result.RowsAffected.Should().Be(10);
        result.InsertedEntities.Should().BeEmpty();
    }

    [Fact]
    public void BulkOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new BulkOptions();

        // Assert
        options.ReturnIdentities.Should().BeFalse();
        options.BatchSize.Should().Be(0);
        options.TimeoutSeconds.Should().Be(30);
    }
    
    [Fact]
    public void BulkOptions_Properties_CanBeSet()
    {
        // Arrange
        var options = new BulkOptions();
        
        // Act
        options.ReturnIdentities = true;
        options.BatchSize = 100;
        options.TimeoutSeconds = 60;
        
        // Assert
        options.ReturnIdentities.Should().BeTrue();
        options.BatchSize.Should().Be(100);
        options.TimeoutSeconds.Should().Be(60);
    }
    
    [Fact]
    public void BulkOptions_Default_ReturnsNewInstance()
    {
        // Arrange & Act
        var options1 = BulkOptions.Default;
        var options2 = BulkOptions.Default;
        
        // Assert
        options1.Should().NotBeNull();
        options2.Should().NotBeNull();
        options1.Should().BeSameAs(options2);
    }

    [Fact]
    public void BulkSqlResult_Constructor_SetsBatches()
    {
        // Arrange
        var batches = new List<SqlResult> { new SqlResult("SELECT 1", null) };

        // Act
        var result = new BulkSqlResult(batches);

        // Assert
        result.Batches.Should().BeSameAs(batches);
    }
}



