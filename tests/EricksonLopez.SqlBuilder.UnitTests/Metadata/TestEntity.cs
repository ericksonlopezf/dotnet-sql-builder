// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class TestEntity : IStaticEntityMetadata<TestEntity>, EricksonLopez.SqlBuilder.Annotations.ISqlEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int? Age { get; set; }

    public static string TableName => "test_entity";

    public string GetTableName() => TableName;
    public string[] GetColumnNames() => new[] { "Id", "Name", "Age" };
    public object?[] GetValues() => new object?[] { Id, Name, Age };
    public string[] GetAllColumnNames() => GetColumnNames();
    public object?[] GetAllValues() => GetValues();
    public IReadOnlyDictionary<string, string> GetPropertyMap() => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Id", "id" }, { "Name", "name" }, { "Age", "age" }
    };
    public string[] GetIndexedColumns() => Array.Empty<string>();

    public static int ColumnCount => 3;

    public static ReadOnlySpan<ColumnMetadata> GetColumns()
    {
        return new ColumnMetadata[]
        {
            new ColumnMetadata(0, "Id", ColumnFlags.PrimaryKey | ColumnFlags.GeneratedAlways),
            new ColumnMetadata(1, "Name", ColumnFlags.None),
            new ColumnMetadata(2, "Age", ColumnFlags.Nullable)
        };
    }

    public static bool IsNull(TestEntity entity, int columnIndex)
    {
        return columnIndex switch
        {
            1 => entity.Name == null,
            2 => entity.Age == null,
            _ => false
        };
    }

    public static bool IsDefault(TestEntity entity, int columnIndex)
    {
        return columnIndex switch
        {
            0 => entity.Id == 0,
            1 => entity.Name == null,
            2 => entity.Age == null,
            _ => false
        };
    }

    public static bool AreEqual(TestEntity entity, TestEntity snapshot, int columnIndex)
    {
        return columnIndex switch
        {
            0 => entity.Id == snapshot.Id,
            1 => entity.Name == snapshot.Name,
            2 => entity.Age == snapshot.Age,
            _ => false
        };
    }

    public static string GetColumnName(int columnIndex)
    {
        return columnIndex switch
        {
            0 => "Id",
            1 => "Name",
            2 => "Age",
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };
    }

    public static string BindParameter(TestEntity entity, int columnIndex, IParameterManager parameters)
    {
        var value = columnIndex switch
        {
            0 => (object)entity.Id,
            1 => entity.Name,
            2 => entity.Age,
            _ => null
        };
        return parameters.Add(value);
    }

    public static void ExtractColumnArrays(ReadOnlySpan<TestEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters)
    {
        if (activeColumns[0])
        {
            var arr = new int[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                arr[i] = entities[i].Id;
            }

            parameters.Add(arr);
        }
        if (activeColumns[1])
        {
            var arr = new string[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                arr[i] = entities[i].Name;
            }

            parameters.Add(arr);
        }
        if (activeColumns[2])
        {
            var arr = new int?[entities.Length];
            for (int i = 0; i < entities.Length; i++)
            {
                arr[i] = entities[i].Age;
            }

            parameters.Add(arr);
        }
    }

    public static TestEntity FromReader(System.Data.IDataReader reader)
    {
        return new TestEntity
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.IsDBNull(reader.GetOrdinal("Name")) ? null! : reader.GetString(reader.GetOrdinal("Name")),
            Age = reader.IsDBNull(reader.GetOrdinal("Age")) ? null : reader.GetInt32(reader.GetOrdinal("Age"))
        };
    }

    public static Func<System.Data.IDataReader, TestEntity> GetReaderParser() => FromReader;
}


