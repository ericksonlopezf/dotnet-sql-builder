// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing;

/// <summary>
/// A 3-column static entity for testing dialect render methods with explicit column casings.
/// </summary>
public class ThreeColumnEntity : ISqlEntity, IStaticEntityMetadata<ThreeColumnEntity>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public static string TableName => "TestEntity";
    public string GetTableName() => TableName;
    public string[] GetColumnNames() => new[] { "Id", "Name", "Status" };
    public object?[] GetValues() => new object?[] { Id, Name, Status };
    public string[] GetAllColumnNames() => GetColumnNames();
    public object?[] GetAllValues() => GetValues();
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
    {
        { "Id", "Id" },
        { "Name", "Name" },
        { "Status", "Status" }
    };
    public string[] GetIndexedColumns() => Array.Empty<string>();

    public static int ColumnCount => 3;
    public static string GetColumnName(int columnIndex) => columnIndex switch { 0 => "Id", 1 => "Name", _ => "Status" };
    public static ReadOnlySpan<ColumnMetadata> GetColumns() => new ColumnMetadata[]
    {
        new(0, "Id", ColumnFlags.PrimaryKey),
        new(1, "Name", ColumnFlags.None),
        new(2, "Status", ColumnFlags.None)
    };
    public static string BindParameter(ThreeColumnEntity entity, int columnIndex, IParameterManager parameters) => parameters.Add(columnIndex switch
    {
        0 => (object)entity.Id,
        1 => entity.Name,
        _ => entity.Status
    });
    public static bool IsNull(ThreeColumnEntity entity, int columnIndex) => false;
    public static bool IsDefault(ThreeColumnEntity entity, int columnIndex) => false;
    public static bool AreEqual(ThreeColumnEntity entity, ThreeColumnEntity snapshot, int columnIndex) => false;
    public static void ExtractColumnArrays(ReadOnlySpan<ThreeColumnEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters) { }
    public static ThreeColumnEntity FromReader(IDataReader reader) => new();
    public static Func<IDataReader, ThreeColumnEntity> GetReaderParser() => _ => new ThreeColumnEntity();
}
