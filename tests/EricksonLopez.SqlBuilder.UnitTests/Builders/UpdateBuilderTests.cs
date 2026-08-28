// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Builders.Update;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class UpdateBuilderTests
{
    private readonly MockSqlRenderer _renderer = new();

    [Fact]
    public void Build_WithValidEntity_Succeeds()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var builder = new UpdateBuilder<TestEntity>(entity);

        var result = builder.Build(_renderer);
        result.Sql.Should().Be("UPDATE");
        _renderer.LastSetMask![0].Should().BeFalse(); // PK
        _renderer.LastSetMask![1].Should().BeTrue();
        _renderer.LastSetMask![2].Should().BeTrue();
    }

    [Fact]
    public void IgnoreNulls_AddsRule()
    {
        var entity = new TestEntity { Id = 1, Name = "Test" };
        var builder = new UpdateBuilder<TestEntity>(entity)
            .IgnoreNulls();

        var result = builder.Build(_renderer);
        result.Sql.Should().Be("UPDATE");
        _renderer.LastSetMask![0].Should().BeFalse(); // PK
        _renderer.LastSetMask![1].Should().BeTrue();
        _renderer.LastSetMask![2].Should().BeFalse(); // Age is null
    }

    [Fact]
    public void Build_NoSetColumns_ThrowsInvalidOperationException()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        // Empty Set rules => No columns selected for UPDATE
        var builder = new UpdateBuilder<TestEntity>(entity)
            .Except(1, 2);

        Action act = () => builder.Build(_renderer);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("No columns selected for SET clause.");
    }

    [Fact]
    public void Entity_Property_ReturnsEntity()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var builder = new UpdateBuilder<TestEntity>(entity);
        builder.Entity.Should().BeSameAs(entity);
    }

    private class DummyRule : EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<TestEntity>
    {
        public EricksonLopez.SqlBuilder.ColumnSelection.RulePhase Phase => EricksonLopez.SqlBuilder.ColumnSelection.RulePhase.Phase4Overrides;
        public void Apply(ref EricksonLopez.SqlBuilder.ColumnSelection.ColumnSelectionContext<TestEntity> context) 
        {
            context.Exclude(new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken(1));
        }
    }

    [Fact]
    public void AddRule_AddsCustomRule()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var builder = new UpdateBuilder<TestEntity>(entity);
        builder.AddRule(new DummyRule());
        var result = builder.Build(_renderer);
        result.Sql.Should().Be("UPDATE");
        _renderer.LastSetMask![1].Should().BeFalse();
    }

    [Fact]
    public void Only_AddsOnlyRule()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var builder = new UpdateBuilder<TestEntity>(entity).Only(1);
        var result = builder.Build(_renderer);
        result.Sql.Should().Be("UPDATE");
        _renderer.LastSetMask![0].Should().BeFalse(); // PK
        _renderer.LastSetMask![1].Should().BeTrue();
        _renderer.LastSetMask![2].Should().BeFalse();
    }

    private class NoPkEntity : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<NoPkEntity>
    {
        public static string TableName => "NoPkEntity";
        public static int ColumnCount => 1;
        public static System.ReadOnlySpan<EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata> GetColumns()
        {
            return new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata[]
            {
                new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata(0, "Name", EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags.None)
            };
        }
        public static object?[] GetValues(NoPkEntity entity) => new object?[] { entity.Name };
        public static object? GetBoxedValue(NoPkEntity entity, int columnIndex) => entity.Name;
        public static bool AreEqual(NoPkEntity entity1, NoPkEntity entity2, int columnIndex) => entity1.Name == entity2.Name;
        public static void BindColumns(NoPkEntity entity, System.ReadOnlySpan<bool> mask, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) {}
        public static bool IsNull(NoPkEntity entity, int columnIndex) => entity.Name == null;
        public static bool IsDefault(NoPkEntity entity, int columnIndex) => entity.Name == null;
        public static string GetColumnName(int columnIndex) => "Name";
        public static string BindParameter(NoPkEntity entity, int columnIndex, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) => parameters.Add(entity.Name);
        public static void ExtractColumnArrays(System.ReadOnlySpan<NoPkEntity> entities, System.ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) {}
        public static NoPkEntity FromReader(System.Data.IDataReader reader) => new NoPkEntity { Name = reader.GetString(0) };
        public static Func<System.Data.IDataReader, NoPkEntity> GetReaderParser() => FromReader;
        
        public string Name { get; set; } = "Test";
    }

    private class CustomEntity : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<CustomEntity>
    {
        public static string TableName => "CustomEntity";
        public static int ColumnCount => 2;
        public static System.ReadOnlySpan<EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata> GetColumns()
        {
            return new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata[]
            {
                new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata(0, "NonGenPk", EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags.PrimaryKey),
                new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata(1, "GenNonPk", EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnFlags.GeneratedAlways)
            };
        }
        public static object?[] GetValues(CustomEntity entity) => new object?[] { entity.NonGenPk, entity.GenNonPk };
        public static object? GetBoxedValue(CustomEntity entity, int columnIndex) => columnIndex == 0 ? entity.NonGenPk : entity.GenNonPk;
        public static bool AreEqual(CustomEntity entity1, CustomEntity entity2, int columnIndex) => true;
        public static void BindColumns(CustomEntity entity, System.ReadOnlySpan<bool> mask, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) {}
        public static bool IsNull(CustomEntity entity, int columnIndex) => false;
        public static bool IsDefault(CustomEntity entity, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => columnIndex == 0 ? "NonGenPk" : "GenNonPk";
        public static string BindParameter(CustomEntity entity, int columnIndex, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) => "?";
        public static void ExtractColumnArrays(System.ReadOnlySpan<CustomEntity> entities, System.ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) {}
        public static CustomEntity FromReader(System.Data.IDataReader reader) => new CustomEntity { NonGenPk = reader.GetInt32(0), GenNonPk = reader.GetInt32(1) };
        public static Func<System.Data.IDataReader, CustomEntity> GetReaderParser() => FromReader;
        
        public int NonGenPk { get; set; } = 1;
        public int GenNonPk { get; set; } = 2;
    }

    [Fact]
    public void Build_WithCustomEntity_ProperlyExcludesColumns()
    {
        var entity = new CustomEntity();
        var builder = new UpdateBuilder<CustomEntity>(entity);
        
        Action act = () => builder.Build(_renderer);
        // Both columns are excluded by default rules (one is PK, one is Generated), so SET is empty!
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("No columns selected for SET clause.");
    }

    [Fact]
    public void Build_NoWhereColumns_ThrowsInvalidOperationException()
    {
        var entity = new NoPkEntity();
        var builder = new UpdateBuilder<NoPkEntity>(entity);
        
        Action act = () => builder.Build(_renderer);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("No columns selected for WHERE clause. Unconditional updates must use the AST query builder.");
    }
}



