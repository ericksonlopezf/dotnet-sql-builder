// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class RawQueryTests
{
    [Fact]
    public void Constructor_FormattableString_ExtractsParameters()
    {
        var id = 123;
        var name = "Test";
        var query = new RawQuery((FormattableString)$"SELECT * FROM Users WHERE Id = {id} AND Name = {name}");

        query.RawSql.Should().Be("SELECT * FROM Users WHERE Id = @p0 AND Name = @p1");
        
        var parameters = query.Parameters as IReadOnlyDictionary<string, object?>;
        parameters.Should().NotBeNull();
        parameters!["@p0"].Should().Be(123);
        parameters["@p1"].Should().Be("Test");
    }

    [Fact]
    public void Constructor_EmptySql_ThrowsArgumentException()
    {
        Action act = () => _ = new RawQuery("", new Dictionary<string, object?>());
        act.Should().Throw<ArgumentException>().WithMessage("SQL cannot be empty*");
    }

    [Fact]
    public void Constructor_NullParameters_CreatesEmptyDictionary()
    {
        var query = new RawQuery("SELECT 1", null);
        var parameters = query.Parameters as IReadOnlyDictionary<string, object?>;
        parameters.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ReadOnlyDictionary_UsesDirectly()
    {
        var dict = new Dictionary<string, object?> { { "@p0", 1 } };
        IReadOnlyDictionary<string, object?> readOnlyDict = dict;
        var query = new RawQuery("SELECT 1", readOnlyDict);
        query.Parameters.Should().BeSameAs(readOnlyDict);
    }

    [Fact]
    public void Constructor_Dictionary_UsesIReadOnlyDictionaryIfImplemented()
    {
        var dict = new Dictionary<string, object?> { { "@p0", 1 } };
        IDictionary<string, object?> idict = dict;
        var query = new RawQuery("SELECT 1", idict);
        query.Parameters.Should().BeSameAs(dict);
        query.Parameters.Should().BeAssignableTo<IReadOnlyDictionary<string, object?>>();
    }

    [Fact]
    public void Constructor_SqlEntity_ExtractsParameters()
    {
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var query = new RawQuery("SELECT 1", entity);
        
        var parameters = query.Parameters as IReadOnlyDictionary<string, object?>;
        parameters.Should().NotBeNull();
        parameters!["Id"].Should().Be(1);
        parameters["Name"].Should().Be("Test");
    }

    [Fact]
    public void Constructor_IEnumerableKeyValuePair_CreatesDictionary()
    {
        var list = new List<KeyValuePair<string, object?>>
        {
            new("Key1", "Value1")
        };

        var query = new RawQuery("SELECT 1", list);
        var parameters = query.Parameters as IReadOnlyDictionary<string, object?>;
        parameters.Should().NotBeNull();
        parameters!["Key1"].Should().Be("Value1");
    }

    [Fact]
    public void Constructor_UnsupportedType_ThrowsNotSupportedException()
    {
        var anonymous = new { Id = 1 };
        Action act = () => _ = new RawQuery("SELECT 1", anonymous);
        act.Should().Throw<NotSupportedException>().WithMessage("*not NativeAOT compliant*");
    }
    
    [Fact]
    public void Build_ReturnsSqlResult()
    {
        var query = new RawQuery("SELECT 1", new Dictionary<string, object?> { { "@p0", 1 } });
        var result = query.Build(new EricksonLopez.SqlBuilder.PostgreSql.PostgreSqlCompiler());
        result.Sql.Trim().Should().Be("SELECT 1");
        result.Parameters["p0"].Should().Be(1);
    }

    private class TestEntity : ISqlEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string GetTableName() => "TestEntities";
        public string[] GetColumnNames() => new[] { "Id", "Name" };
        public object?[] GetValues() => new object?[] { Id, Name };
        public string[] GetAllColumnNames() => GetColumnNames();
        public object?[] GetAllValues() => GetValues();
        public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>();
        public string[] GetIndexedColumns() => Array.Empty<string>();
    }
}



