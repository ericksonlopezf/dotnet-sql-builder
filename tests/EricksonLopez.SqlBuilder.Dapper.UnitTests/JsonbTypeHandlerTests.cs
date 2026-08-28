// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using AwesomeAssertions;
using Dapper;
using EricksonLopez.SqlBuilder.Dapper;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace EricksonLopez.SqlBuilder.Dapper.UnitTests
{
public class JsonbTypeHandlerTests
{
    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private static void ResetHelper()
    {
        var helperType = typeof(DapperExtensions).Assembly.GetType("EricksonLopez.SqlBuilder.Dapper.NpgsqlParameterHelper");
        helperType?.GetField("_initialized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.SetValue(null, false);
        helperType?.GetField("_npgsqlDbTypeProperty", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.SetValue(null, null);
    }

    [Fact]
    public void SetValue_WithValidObject_SetsJsonbValue()
    {
        // Arrange
        ResetHelper();
        var handler = new JsonbTypeHandler<TestData>();
        var parameter = new NpgsqlParameter();
        var data = new TestData { Id = 1, Name = "Test" };

        // Act
        handler.SetValue(parameter, data);

        // Assert
        parameter.NpgsqlDbType.Should().Be(NpgsqlDbType.Jsonb);
        parameter.Value.Should().BeOfType<string>();
        parameter.Value!.ToString().Should().Contain("\"id\":1");
        parameter.Value!.ToString().Should().Contain("\"name\":\"Test\"");
    }

    [Fact]
    public void SetValue_WithNullObject_SetsDBNull()
    {
        // Arrange
        ResetHelper();
        var handler = new JsonbTypeHandler<TestData>();
        var parameter = new NpgsqlParameter();

        // Act
        handler.SetValue(parameter, null);

        // Assert
        parameter.Value.Should().Be(DBNull.Value);
    }

    [Fact]
    public void SetValue_SetsInitializedToTrue()
    {
        ResetHelper();
        var handler = new JsonbTypeHandler<TestData>();
        var parameter = new NpgsqlParameter();
        handler.SetValue(parameter, new TestData());

        var helperType = typeof(DapperExtensions).Assembly.GetType("EricksonLopez.SqlBuilder.Dapper.NpgsqlParameterHelper");
        var initialized = (bool?)helperType?.GetField("_initialized", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)?.GetValue(null);
        initialized.Should().BeTrue();
    }

    [Fact]
    public void SetValue_WithNonNpgsqlParameter_SetsValueWithoutSettingNpgsqlDbType()
    {
        // Arrange
        ResetHelper();
        var handler = new JsonbTypeHandler<TestData>();
        var parameter = new Microsoft.Data.Sqlite.SqliteParameter();
        var data = new TestData { Id = 1, Name = "Test" };

        // Act
        handler.SetValue(parameter, data);

        // Assert
        parameter.Value.Should().BeOfType<string>();
    }

    [Fact]
    public void SetValue_WithNpgsqlParameterWithoutNpgsqlDbTypeProperty_DoesNotThrow()
    {
        // Arrange
        ResetHelper();
        var handler = new JsonbTypeHandler<TestData>();
        var parameter = new Fakes.NpgsqlParameter();
        var data = new TestData { Id = 1, Name = "Test" };

        // Act
        handler.SetValue(parameter, data);

        // Assert
        parameter.Value.Should().BeOfType<string>();
    }

    [Fact]
    public void Parse_WithValidJson_ReturnsObject()
    {
        // Arrange
        var handler = new JsonbTypeHandler<TestData>();
        var json = "{\"id\": 2, \"name\": \"Parsed\"}";

        // Act
        var result = handler.Parse(json);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
        result.Name.Should().Be("Parsed");
    }

    [Fact]
    public void Parse_WithNull_ReturnsDefault()
    {
        // Arrange
        var handler = new JsonbTypeHandler<TestData>();

        // Act
        var result1 = handler.Parse(null!);
        var result2 = handler.Parse(DBNull.Value);

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
    }
}
}
