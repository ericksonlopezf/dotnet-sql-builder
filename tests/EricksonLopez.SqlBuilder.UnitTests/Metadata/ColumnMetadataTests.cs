// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class ColumnMetadataTests
{
    [Fact]
    public void ColumnMetadata_ConstructorAndProperties_Work()
    {
        var meta = new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata(1, "Name", EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags.PrimaryKey);
        meta.Index.Should().Be(1);
        meta.Name.Should().Be("Name");
        meta.Flags.Should().Be(EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags.PrimaryKey);
    }

    [Fact]
    public void InternalColumnMetadata_ConstructorAndProperties_Work()
    {
        var meta = new EricksonLopez.SqlBuilder.Metadata.ColumnMetadata("col", "prop", EricksonLopez.SqlBuilder.Metadata.ColumnFlags.Generated);
        meta.ColumnName.Should().Be("col");
        meta.PropertyName.Should().Be("prop");
        meta.Flags.Should().Be(EricksonLopez.SqlBuilder.Metadata.ColumnFlags.Generated);
    }

    [Fact]
    public void ColumnToken_Properties_Work()
    {
        var token = new EricksonLopez.SqlBuilder.Metadata.ColumnToken(5, "Test");
        token.Index.Should().Be(5);
        token.Name.Should().Be("Test");
    }

    private class MockEntityMetadata : EricksonLopez.SqlBuilder.Metadata.IEntityMetadata<MockEntity>
    {
        public string TableName => "mock";
        public System.ReadOnlySpan<EricksonLopez.SqlBuilder.Metadata.ColumnMetadata> Columns => System.Array.Empty<EricksonLopez.SqlBuilder.Metadata.ColumnMetadata>();
        public System.ReadOnlySpan<EricksonLopez.SqlBuilder.Metadata.ColumnToken> PrimaryKeys => default;
        public object?[] GetValues(MockEntity entity) => System.Array.Empty<object?>();
        public void BindColumns(MockEntity entity, System.ReadOnlySpan<bool> mask, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) {}
        public bool IsNull(MockEntity entity, int columnIndex) => false;
        public bool IsDefault(MockEntity entity, int columnIndex) => false;
        public object? GetBoxedValue(MockEntity entity, int columnIndex) => null;
        public void ExtractColumnArrays(System.ReadOnlySpan<MockEntity> entities, System.ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) {}
    }

    private class MockEntity : EricksonLopez.SqlBuilder.Metadata.IEntityMetadataProvider<MockEntity>
    {
        public static EricksonLopez.SqlBuilder.Metadata.IEntityMetadata<MockEntity> Metadata { get; } = new MockEntityMetadata();
    }

    [Fact]
    public void EntityMetadataResolver_Get_ReturnsMetadata()
    {
        var meta = EricksonLopez.SqlBuilder.Metadata.EntityMetadataResolver.Get<MockEntity>();
        meta.Should().NotBeNull();
        meta.TableName.Should().Be("mock");
    }
}



