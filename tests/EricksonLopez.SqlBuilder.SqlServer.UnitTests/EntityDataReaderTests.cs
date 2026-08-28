// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.SqlServer;
using Xunit;

namespace EricksonLopez.SqlBuilder.SqlServer.Tests;

public class EntityDataReaderTests
{
    private sealed class DummyEntity : IStaticEntityMetadata<DummyEntity>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public byte ByteVal { get; set; }
        public char CharVal { get; set; }
        public Guid GuidVal { get; set; }
        public short ShortVal { get; set; }
        public long LongVal { get; set; }
        public float FloatVal { get; set; }
        public double DoubleVal { get; set; }
        public decimal DecimalVal { get; set; }
        public DateTime DateTimeVal { get; set; }

        public static string TableName => "dummies";
        public static int ColumnCount => 12;

        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new ColumnMetadata[]
        {
            new ColumnMetadata(0, "Id", ColumnFlags.PrimaryKey),
            new ColumnMetadata(1, "Name", ColumnFlags.Nullable),
            new ColumnMetadata(2, "IsActive", ColumnFlags.None),
            new ColumnMetadata(3, "ByteVal", ColumnFlags.None),
            new ColumnMetadata(4, "CharVal", ColumnFlags.None),
            new ColumnMetadata(5, "GuidVal", ColumnFlags.None),
            new ColumnMetadata(6, "ShortVal", ColumnFlags.None),
            new ColumnMetadata(7, "LongVal", ColumnFlags.None),
            new ColumnMetadata(8, "FloatVal", ColumnFlags.None),
            new ColumnMetadata(9, "DoubleVal", ColumnFlags.None),
            new ColumnMetadata(10, "DecimalVal", ColumnFlags.None),
            new ColumnMetadata(11, "DateTimeVal", ColumnFlags.None)
        };

        public static bool IsNull(DummyEntity entity, int columnIndex) => columnIndex switch
        {
            1 => entity.Name == null,
            _ => false
        };

        public static bool IsDefault(DummyEntity entity, int columnIndex) => false;
        public static bool AreEqual(DummyEntity entity, DummyEntity snapshot, int columnIndex) => false;

        public static string GetColumnName(int columnIndex) => columnIndex switch
        {
            0 => "Id",
            1 => "Name",
            2 => "IsActive",
            3 => "ByteVal",
            4 => "CharVal",
            5 => "GuidVal",
            6 => "ShortVal",
            7 => "LongVal",
            8 => "FloatVal",
            9 => "DoubleVal",
            10 => "DecimalVal",
            11 => "DateTimeVal",
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };

        public static string BindParameter(DummyEntity entity, int columnIndex, IParameterManager parameters)
        {
            object? val = columnIndex switch
            {
                0 => entity.Id,
                1 => (object?)entity.Name,
                2 => entity.IsActive,
                3 => entity.ByteVal,
                4 => entity.CharVal,
                5 => entity.GuidVal,
                6 => entity.ShortVal,
                7 => entity.LongVal,
                8 => entity.FloatVal,
                9 => entity.DoubleVal,
                10 => entity.DecimalVal,
                11 => entity.DateTimeVal,
                _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
            };
            return parameters.Add(val);
        }

        public static void ExtractColumnArrays(ReadOnlySpan<DummyEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters) { }
        public static Func<IDataReader, DummyEntity> GetReaderParser() => _ => new DummyEntity();
        public static DummyEntity FromReader(IDataReader reader) => new DummyEntity();
    }

    [Fact]
    public void ReadAndAccessAllTypedFields_Succeeds()
    {
        var now = DateTime.UtcNow;
        var guid = Guid.NewGuid();
        var entities = new List<DummyEntity>
        {
            new DummyEntity
            {
                Id = 100,
                Name = "Alice",
                IsActive = true,
                ByteVal = 0x1A,
                CharVal = 'Z',
                GuidVal = guid,
                ShortVal = 42,
                LongVal = 9999999999L,
                FloatVal = 3.14f,
                DoubleVal = 6.28,
                DecimalVal = 199.99m,
                DateTimeVal = now
            },
            new DummyEntity
            {
                Id = 200,
                Name = null, // tests DBNull
                IsActive = false
            }
        };

        using var reader = new EntityDataReader<DummyEntity>(entities);

        reader.FieldCount.Should().Be(12);
        reader.Depth.Should().Be(0);
        reader.RecordsAffected.Should().Be(-1);
        reader.IsClosed.Should().BeFalse();
        reader.GetSchemaTable().Should().BeNull();
        reader.NextResult().Should().BeFalse();

        // Row 1
        reader.Read().Should().BeTrue();
        reader.GetInt32(0).Should().Be(100);
        reader.GetString(1).Should().Be("Alice");
        reader.GetBoolean(2).Should().BeTrue();
        reader.GetByte(3).Should().Be(0x1A);
        reader.GetChar(4).Should().Be('Z');
        reader.GetGuid(5).Should().Be(guid);
        reader.GetInt16(6).Should().Be(42);
        reader.GetInt64(7).Should().Be(9999999999L);
        reader.GetFloat(8).Should().Be(3.14f);
        reader.GetDouble(9).Should().Be(6.28);
        reader.GetDecimal(10).Should().Be(199.99m);
        reader.GetDateTime(11).Should().Be(now);

        reader.GetFieldType(0).Should().Be<object>();
        reader.GetName(0).Should().Be("Id");
        reader.GetName(1).Should().Be("Name");
        reader.GetOrdinal("Id").Should().Be(0);
        reader.GetOrdinal("Name").Should().Be(1);
        reader.GetOrdinal("NonExistent").Should().Be(-1);

        reader.IsDBNull(0).Should().BeFalse();
        reader.IsDBNull(1).Should().BeFalse();

        reader[0].Should().Be(100);
        reader["Id"].Should().Be(100);
        reader["Name"].Should().Be("Alice");

        var values = new object[12];
        reader.GetValues(values).Should().Be(12);
        values[0].Should().Be(100);
        values[1].Should().Be("Alice");

        // Row 2
        reader.Read().Should().BeTrue();
        reader.GetInt32(0).Should().Be(200);
        reader.IsDBNull(1).Should().BeTrue();
        reader.GetValue(1).Should().Be(DBNull.Value);

        // EOF
        reader.Read().Should().BeFalse();

        reader.Close();
        reader.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void UnsupportedMethods_ThrowNotSupportedException()
    {
        var entities = new List<DummyEntity> { new DummyEntity() };
        using var reader = new EntityDataReader<DummyEntity>(entities);
        reader.Read();

        var actDataType = () => reader.GetDataTypeName(0);
        actDataType.Should().Throw<NotSupportedException>();

        var actBytes = () => reader.GetBytes(0, 0, null, 0, 0);
        actBytes.Should().Throw<NotSupportedException>();

        var actChars = () => reader.GetChars(0, 0, null, 0, 0);
        actChars.Should().Throw<NotSupportedException>();

        var actData = () => reader.GetData(0);
        actData.Should().Throw<NotSupportedException>();
    }

    private sealed class SpecialBindingEntity : IStaticEntityMetadata<SpecialBindingEntity>
    {
        public static string TableName => "specials";
        public static int ColumnCount => 2;
        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[]
        {
            new ColumnMetadata(0, "NullParamCol", ColumnFlags.None),
            new ColumnMetadata(1, "EmptyParamCol", ColumnFlags.None)
        };
        public static bool IsNull(SpecialBindingEntity entity, int columnIndex) => false;
        public static bool IsDefault(SpecialBindingEntity entity, int columnIndex) => false;
        public static bool AreEqual(SpecialBindingEntity entity, SpecialBindingEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => columnIndex == 0 ? "NullParamCol" : "EmptyParamCol";
        public static string BindParameter(SpecialBindingEntity entity, int columnIndex, IParameterManager parameters)
        {
            if (columnIndex == 0)
            {
                return parameters.Add(null); // adds null parameter value to pm
            }
            return "@dummy"; // adds nothing to pm!
        }
        public static void ExtractColumnArrays(ReadOnlySpan<SpecialBindingEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters) { }
        public static Func<IDataReader, SpecialBindingEntity> GetReaderParser() => _ => new SpecialBindingEntity();
        public static SpecialBindingEntity FromReader(IDataReader reader) => new SpecialBindingEntity();
    }

    [Fact]
    public void GetValue_WithNullValueOrEmptyParameters_ReturnsDBNull()
    {
        var entities = new List<SpecialBindingEntity> { new SpecialBindingEntity() };
        using var reader = new EntityDataReader<SpecialBindingEntity>(entities);
        reader.Read().Should().BeTrue();

        reader.GetValue(0).Should().Be(DBNull.Value);
        reader.GetValue(1).Should().Be(DBNull.Value);
    }

    private sealed class ThrowingOnNullEntity : IStaticEntityMetadata<ThrowingOnNullEntity>
    {
        public static string TableName => "throwing";
        public static int ColumnCount => 1;
        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[] { new ColumnMetadata(0, "Col", ColumnFlags.None) };
        public static bool IsNull(ThrowingOnNullEntity entity, int columnIndex) => true;
        public static bool IsDefault(ThrowingOnNullEntity entity, int columnIndex) => false;
        public static bool AreEqual(ThrowingOnNullEntity entity, ThrowingOnNullEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => "Col";
        public static string BindParameter(ThrowingOnNullEntity entity, int columnIndex, IParameterManager parameters) => throw new InvalidOperationException("Should not bind when IsNull is true");
        public static void ExtractColumnArrays(ReadOnlySpan<ThrowingOnNullEntity> entities, ReadOnlySpan<bool> activeColumns, IParameterManager parameters) { }
        public static Func<IDataReader, ThrowingOnNullEntity> GetReaderParser() => _ => new ThrowingOnNullEntity();
        public static ThrowingOnNullEntity FromReader(IDataReader reader) => new ThrowingOnNullEntity();
    }

    [Fact]
    public void GetValue_WhenIsNullIsTrue_ReturnsDBNullWithoutCallingBindParameter()
    {
        var entities = new List<ThrowingOnNullEntity> { new ThrowingOnNullEntity() };
        using var reader = new EntityDataReader<ThrowingOnNullEntity>(entities);
        reader.Read().Should().BeTrue();
        reader.GetValue(0).Should().Be(DBNull.Value);
    }
}
