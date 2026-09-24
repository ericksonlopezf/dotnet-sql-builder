// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.UnitTests;

public class StaticEntitiesMetadataTests
{
    [Fact]
    public void DummyEntity_DefaultValues_AreCorrect()
    {
        var dummy = new DummyEntity();
        Assert.Equal(0, dummy.Id);
        Assert.Equal(string.Empty, dummy.Name);
        Assert.Equal(0, dummy.Version);
        Assert.Equal(Guid.Empty, dummy.RowGuid);
        Assert.False(dummy.IsActive);
    }

    [Fact]
    public void DummyEntity_ISqlEntity_Members_ReturnCorrectValues()
    {
        var dummy = new DummyEntity { Id = 42, Name = "Alice" };

        Assert.Equal("dummy_entity", dummy.GetTableName());

        var colNames = dummy.GetColumnNames();
        Assert.Equal(2, colNames.Length);
        Assert.Equal("id", colNames[0]);
        Assert.Equal("name", colNames[1]);

        var allColNames = dummy.GetAllColumnNames();
        Assert.Equal(2, allColNames.Length);
        Assert.Equal("id", allColNames[0]);
        Assert.Equal("name", allColNames[1]);

        var values = dummy.GetValues();
        Assert.Equal(2, values.Length);
        Assert.Equal(42, values[0]);
        Assert.Equal("Alice", values[1]);

        var allValues = dummy.GetAllValues();
        Assert.Equal(2, allValues.Length);
        Assert.Equal(42, allValues[0]);
        Assert.Equal("Alice", allValues[1]);

        var propMap = dummy.GetPropertyMap();
        Assert.Equal(2, propMap.Count);
        Assert.Equal("id", propMap["Id"]);
        Assert.Equal("name", propMap["Name"]);

        Assert.Empty(dummy.GetIndexedColumns());
    }

    [Fact]
    public void DummyEntity_IStaticEntityMetadata_Metadata_IsAccurate()
    {
        Assert.Equal("dummy", DummyEntity.TableName);
        Assert.Equal(2, DummyEntity.ColumnCount);

        var columns = DummyEntity.GetColumns();
        Assert.Equal(2, columns.Length);
        Assert.Equal("Id", columns[0].Name);
        Assert.Equal(ColumnFlags.PrimaryKey, columns[0].Flags);
        Assert.Equal("Name", columns[1].Name);
        Assert.Equal(ColumnFlags.None, columns[1].Flags);

        Assert.Equal("Id", DummyEntity.GetColumnName(0));
        Assert.Equal("Name", DummyEntity.GetColumnName(1));
        Assert.Equal("Name", DummyEntity.GetColumnName(99)); // Any non-zero returns Name

        var dummy = new DummyEntity { Id = 1, Name = "Test" };
        Assert.False(DummyEntity.IsNull(dummy, 0));
        Assert.False(DummyEntity.IsDefault(dummy, 0));
        Assert.False(DummyEntity.AreEqual(dummy, dummy, 0));
    }

    [Fact]
    public void DummyEntity_BindParameter_BindsIdAndNameSeparately()
    {
        var dummy = new DummyEntity { Id = 101, Name = "Target" };

        var pm0 = new ParameterManager();
        var p0 = DummyEntity.BindParameter(dummy, 0, pm0);
        Assert.NotNull(p0);
        Assert.Equal(101, pm0.GetParameters()[p0.TrimStart('@', ':')]);

        var pm1 = new ParameterManager();
        var p1 = DummyEntity.BindParameter(dummy, 1, pm1);
        Assert.NotNull(p1);
        Assert.Equal("Target", pm1.GetParameters()[p1.TrimStart('@', ':')]);
    }

    [Fact]
    public void DummyEntity_ExtractColumnArrays_RespectsActiveColumnMasks()
    {
        var entities = new[]
        {
            new DummyEntity { Id = 1, Name = "A" },
            new DummyEntity { Id = 2, Name = "B" }
        };

        // 1. Length 0 activeColumns -> no parameters
        var pm0 = new ParameterManager();
        DummyEntity.ExtractColumnArrays(entities, ReadOnlySpan<bool>.Empty, pm0);
        Assert.Empty(pm0.GetParameters());

        // 2. Length 1, false -> no parameters
        var pm1 = new ParameterManager();
        DummyEntity.ExtractColumnArrays(entities, new[] { false }, pm1);
        Assert.Empty(pm1.GetParameters());

        // 3. Length 1, true -> only Id array added
        var pm2 = new ParameterManager();
        DummyEntity.ExtractColumnArrays(entities, new[] { true }, pm2);
        Assert.Single(pm2.GetParameters());
        var idArr = Assert.IsType<int[]>(System.Linq.Enumerable.First(pm2.GetParameters().Values));
        Assert.Equal(new[] { 1, 2 }, idArr);

        // 4. Length 2, both false -> no parameters
        var pm3 = new ParameterManager();
        DummyEntity.ExtractColumnArrays(entities, new[] { false, false }, pm3);
        Assert.Empty(pm3.GetParameters());

        // 5. Length 2, only Name true -> only Name array added
        var pm4 = new ParameterManager();
        DummyEntity.ExtractColumnArrays(entities, new[] { false, true }, pm4);
        Assert.Single(pm4.GetParameters());
        var nameArr = Assert.IsType<string[]>(System.Linq.Enumerable.First(pm4.GetParameters().Values));
        Assert.Equal(new[] { "A", "B" }, nameArr);

        // 6. Length 2, both true -> both arrays added
        var pm5 = new ParameterManager();
        DummyEntity.ExtractColumnArrays(entities, new[] { true, true }, pm5);
        Assert.Equal(2, pm5.GetParameters().Count);
    }

    [Fact]
    public void DummyEntity_ReaderParsers_ReturnInstances()
    {
        var reader = Substitute.For<IDataReader>();
        Assert.NotNull(DummyEntity.FromReader(reader));

        var parser = DummyEntity.GetReaderParser();
        Assert.NotNull(parser);
        Assert.NotNull(parser(reader));
    }

    [Fact]
    public void ThreeColumnEntity_DefaultValues_AreCorrect()
    {
        var three = new ThreeColumnEntity();
        Assert.Equal(string.Empty, three.Id);
        Assert.Equal(string.Empty, three.Name);
        Assert.Equal(string.Empty, three.Status);
    }

    [Fact]
    public void ThreeColumnEntity_ISqlEntity_Members_ReturnCorrectValues()
    {
        var three = new ThreeColumnEntity { Id = "ID1", Name = "N1", Status = "S1" };

        Assert.Equal("TestEntity", three.GetTableName());
        Assert.Equal("TestEntity", ThreeColumnEntity.TableName);

        var colNames = three.GetColumnNames();
        Assert.Equal(3, colNames.Length);
        Assert.Equal("Id", colNames[0]);
        Assert.Equal("Name", colNames[1]);
        Assert.Equal("Status", colNames[2]);

        var allColNames = three.GetAllColumnNames();
        Assert.Equal(3, allColNames.Length);
        Assert.Equal("Id", allColNames[0]);
        Assert.Equal("Name", allColNames[1]);
        Assert.Equal("Status", allColNames[2]);

        var values = three.GetValues();
        Assert.Equal(3, values.Length);
        Assert.Equal("ID1", values[0]);
        Assert.Equal("N1", values[1]);
        Assert.Equal("S1", values[2]);

        var allValues = three.GetAllValues();
        Assert.Equal(3, allValues.Length);
        Assert.Equal("ID1", allValues[0]);
        Assert.Equal("N1", allValues[1]);
        Assert.Equal("S1", allValues[2]);

        var propMap = three.GetPropertyMap();
        Assert.Equal(3, propMap.Count);
        Assert.Equal("Id", propMap["Id"]);
        Assert.Equal("Name", propMap["Name"]);
        Assert.Equal("Status", propMap["Status"]);

        Assert.Empty(three.GetIndexedColumns());
    }

    [Fact]
    public void ThreeColumnEntity_IStaticEntityMetadata_Metadata_IsAccurate()
    {
        Assert.Equal(3, ThreeColumnEntity.ColumnCount);

        var columns = ThreeColumnEntity.GetColumns();
        Assert.Equal(3, columns.Length);
        Assert.Equal("Id", columns[0].Name);
        Assert.Equal(ColumnFlags.PrimaryKey, columns[0].Flags);
        Assert.Equal("Name", columns[1].Name);
        Assert.Equal(ColumnFlags.None, columns[1].Flags);
        Assert.Equal("Status", columns[2].Name);
        Assert.Equal(ColumnFlags.None, columns[2].Flags);

        Assert.Equal("Id", ThreeColumnEntity.GetColumnName(0));
        Assert.Equal("Name", ThreeColumnEntity.GetColumnName(1));
        Assert.Equal("Status", ThreeColumnEntity.GetColumnName(2));
        Assert.Equal("Status", ThreeColumnEntity.GetColumnName(99));

        var three = new ThreeColumnEntity();
        Assert.False(ThreeColumnEntity.IsNull(three, 0));
        Assert.False(ThreeColumnEntity.IsDefault(three, 0));
        Assert.False(ThreeColumnEntity.AreEqual(three, three, 0));

        ThreeColumnEntity.ExtractColumnArrays(ReadOnlySpan<ThreeColumnEntity>.Empty, ReadOnlySpan<bool>.Empty, new ParameterManager());
    }

    [Fact]
    public void ThreeColumnEntity_BindParameter_BindsEachColumnCorrectly()
    {
        var three = new ThreeColumnEntity { Id = "ID_VAL", Name = "NAME_VAL", Status = "STATUS_VAL" };

        var pm0 = new ParameterManager();
        var p0 = ThreeColumnEntity.BindParameter(three, 0, pm0);
        Assert.Equal("ID_VAL", pm0.GetParameters()[p0.TrimStart('@', ':')]);

        var pm1 = new ParameterManager();
        var p1 = ThreeColumnEntity.BindParameter(three, 1, pm1);
        Assert.Equal("NAME_VAL", pm1.GetParameters()[p1.TrimStart('@', ':')]);

        var pm2 = new ParameterManager();
        var p2 = ThreeColumnEntity.BindParameter(three, 2, pm2);
        Assert.Equal("STATUS_VAL", pm2.GetParameters()[p2.TrimStart('@', ':')]);
    }

    [Fact]
    public void ThreeColumnEntity_ReaderParsers_ReturnInstances()
    {
        var reader = Substitute.For<IDataReader>();
        Assert.NotNull(ThreeColumnEntity.FromReader(reader));

        var parser = ThreeColumnEntity.GetReaderParser();
        Assert.NotNull(parser);
        Assert.NotNull(parser(reader));
    }
}
