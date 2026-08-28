// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.MariaDb.Tests;

/// <summary>
/// Minimal test entity for MariaDB dialect unit tests.
/// Avoids dependency on the main Testing library (which has complex build requirements).
/// </summary>
public class DummyEntity : ISqlEntity, IStaticEntityMetadata<DummyEntity>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public Guid RowGuid { get; set; }
    public bool IsActive { get; set; }

    // ISqlEntity implementation
    public string GetTableName() => "dummy_entity";
    public string[] GetColumnNames() => new[] { "id", "name" };
    public object?[] GetValues() => new object?[] { Id, Name };
    public string[] GetAllColumnNames() => GetColumnNames();
    public object?[] GetAllValues() => GetValues();
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string> { { "Id", "id" }, { "Name", "name" } };
    public string[] GetIndexedColumns() => Array.Empty<string>();

    // IStaticEntityMetadata<DummyEntity> implementation
    public static string TableName => "dummy";
    public static int ColumnCount => 2;

    public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[]
    {
        new ColumnMetadata(0, "Id", ColumnFlags.PrimaryKey),
        new ColumnMetadata(1, "Name", ColumnFlags.None)
    };

    public static bool IsNull(DummyEntity entity, int columnIndex) => false;
    public static bool IsDefault(DummyEntity entity, int columnIndex) => false;
    public static bool AreEqual(DummyEntity entity, DummyEntity snapshot, int columnIndex) => false;

    public static string GetColumnName(int columnIndex) => columnIndex == 0 ? "Id" : "Name";

    public static string BindParameter(DummyEntity entity, int columnIndex, IParameterManager parameters)
    {
        return parameters.Add(columnIndex == 0 ? (object)entity.Id : entity.Name);
    }

    public static void ExtractColumnArrays(ReadOnlySpan<DummyEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters)
    {
        if (activeColumns.Length > 0 && activeColumns[0])
        {
            var arr = new int[entities.Length];
            for (int i = 0; i < entities.Length; i++) arr[i] = entities[i].Id;
            parameters.Add(arr);
        }
        if (activeColumns.Length > 1 && activeColumns[1])
        {
            var arr = new string[entities.Length];
            for (int i = 0; i < entities.Length; i++) arr[i] = entities[i].Name;
            parameters.Add(arr);
        }
    }

    public static DummyEntity FromReader(System.Data.IDataReader reader) => new DummyEntity();
    public static Func<System.Data.IDataReader, DummyEntity> GetReaderParser() => (r) => new DummyEntity();
}

/// <summary>
/// Minimal test entity for LINQ expression tests. Does not require source generator.
/// </summary>
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}
