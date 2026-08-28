// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class AbstractionsTests
{
    [Fact]
    public void SqlResult_StoresPropertiesCorrectly()
    {
        // Arrange
        var sql = "SELECT * FROM users";
        var parameters = new Dictionary<string, object?> { { "Id", 1 } };

        // Act
        var result = new SqlResult(sql, parameters);

        // Assert
        result.Sql.Trim().Should().Be(sql);
        result.Parameters.Should().BeSameAs(parameters);
    }

    [Fact]
    public void SqlEntityAttribute_StoresTableNameCorrectly()
    {
        // Arrange
        var tableName = "users";

        // Act
        var attribute = new SqlEntityAttribute(tableName);

        // Assert
        attribute.TableName.Should().Be(tableName);
    }

    [Fact]
    public void PostgreSqlCompositeTypeAttribute_StoresTypeNameCorrectly()
    {
        // Arrange
        var typeName = "address";

        // Act
        var attribute = new PostgreSqlCompositeTypeAttribute(typeName);

        // Assert
        attribute.TypeName.Should().Be(typeName);
    }

    [Fact]
    public void PostgreSqlEnumAttribute_StoresTypeNameCorrectly()
    {
        // Arrange
        var typeName = "user_status";

        // Act
        var attribute = new PostgreSqlEnumAttribute(typeName);

        // Assert
        attribute.TypeName.Should().Be(typeName);
    }
}




