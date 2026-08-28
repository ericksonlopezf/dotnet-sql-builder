// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using AwesomeAssertions;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlParameterTests
{
    [Fact]
    public void Parameter_ConstructsCorrectly()
    {
        // Arrange
        var p = new SqlParameter(42, DbType.Int32, "integer");

        // Act & Assert
        p.Value.Should().Be(42);
        p.DbType.Should().Be(DbType.Int32);
        p.DatabaseTypeName.Should().Be("integer");
    }
}


