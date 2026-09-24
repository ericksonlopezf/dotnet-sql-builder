// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Annotations;

namespace EricksonLopez.SqlBuilder.Testing;

/// <summary>
/// Represents a three-column static entity designed for testing dialect render methods with explicit column casings.
/// </summary>
public class ThreeColumnEntity : ISqlEntity, IStaticEntityMetadata<ThreeColumnEntity>
{
    /// <summary>Gets or sets the identifier value.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the name value.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the status value.</summary>
    public string Status { get; set; } = string.Empty;

    /// <inheritdoc/>
    public static string TableName => "TestEntity";

    /// <inheritdoc/>
    public string GetTableName() => TableName;

    /// <inheritdoc/>
    public string[] GetColumnNames() => new[] { "Id", "Name", "Status" };

    /// <inheritdoc/>
    public object?[] GetValues() => new object?[] { Id, Name, Status };

    /// <inheritdoc/>
    public string[] GetAllColumnNames() => GetColumnNames();

    /// <inheritdoc/>
    public object?[] GetAllValues() => GetValues();

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>
    {
        { "Id", "Id" },
        { "Name", "Name" },
        { "Status", "Status" }
    };

    /// <inheritdoc/>
    public string[] GetIndexedColumns() => Array.Empty<string>();

    /// <inheritdoc/>
    public static int ColumnCount => 3;

    /// <inheritdoc/>
    public static string GetColumnName(int columnIndex) => columnIndex switch { 0 => "Id", 1 => "Name", _ => "Status" };

    /// <inheritdoc/>
    public static ReadOnlySpan<ColumnMetadata> GetColumns() => new ColumnMetadata[]
    {
        new(0, "Id", ColumnFlags.PrimaryKey),
        new(1, "Name", ColumnFlags.None),
        new(2, "Status", ColumnFlags.None)
    };

    /// <inheritdoc/>
    public static string BindParameter(ThreeColumnEntity entity, int columnIndex, IParameterManager parameters) => parameters.Add(columnIndex switch
    {
        0 => (object)entity.Id,
        1 => entity.Name,
        _ => entity.Status
    });

    /// <inheritdoc/>
    public static bool IsNull(ThreeColumnEntity entity, int columnIndex) => false;

    /// <inheritdoc/>
    public static bool IsDefault(ThreeColumnEntity entity, int columnIndex) => false;

    /// <inheritdoc/>
    public static bool AreEqual(ThreeColumnEntity entity, ThreeColumnEntity snapshot, int columnIndex) => false;

    /// <inheritdoc/>
    public static void ExtractColumnArrays(ReadOnlySpan<ThreeColumnEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters) { }

    /// <inheritdoc/>
    public static ThreeColumnEntity FromReader(IDataReader reader) => new();

    /// <inheritdoc/>
    public static Func<IDataReader, ThreeColumnEntity> GetReaderParser() => _ => new ThreeColumnEntity();
}
