// Copyright © Erickson Lopez. MIT License.
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Annotations;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.Tests;

public class AnnotationsTests
{
    [Fact]
    public void SqlEntityAttribute_SetsTableName()
    {
        var sut = new SqlEntityAttribute("Users");
        sut.TableName.Should().Be("Users");
    }

    [Fact]
    public void PostgreSqlCompositeTypeAttribute_SetsTypeName()
    {
        var sut = new PostgreSqlCompositeTypeAttribute("my_type");
        sut.TypeName.Should().Be("my_type");
    }

    [Fact]
    public void PostgreSqlEnumAttribute_SetsTypeName()
    {
        var sut = new PostgreSqlEnumAttribute("my_enum");
        sut.TypeName.Should().Be("my_enum");
    }

    [Fact]
    public void ParameterlessAttributes_CanBeInstantiated()
    {
        var indexed = new IndexedAttribute();
        indexed.Should().NotBeNull();

        var dbGen = new DatabaseGeneratedAttribute();
        dbGen.Should().NotBeNull();

        var genCol = new GeneratedColumnAttribute();
        genCol.Should().NotBeNull();
    }
}


