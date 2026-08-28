// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.SqlServer;
using Xunit;

namespace EricksonLopez.SqlBuilder.SqlServer.Tests;

public class SqlBulkMergeStrategyTests
{
    private sealed class MergeTestEntity : IStaticEntityMetadata<MergeTestEntity>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public int RowVersion { get; set; }

        public static string TableName => "customers";
        public static int ColumnCount => 4;

        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new ColumnMetadata[]
        {
            new ColumnMetadata(0, "Id", ColumnFlags.PrimaryKey),
            new ColumnMetadata(1, "Name", ColumnFlags.None),
            new ColumnMetadata(2, "Age", ColumnFlags.Nullable),
            new ColumnMetadata(3, "RowVersion", ColumnFlags.Identity)
        };

        public static bool IsNull(MergeTestEntity entity, int columnIndex) => false;
        public static bool IsDefault(MergeTestEntity entity, int columnIndex) => false;
        public static bool AreEqual(MergeTestEntity entity, MergeTestEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => columnIndex switch
        {
            0 => "Id",
            1 => "Name",
            2 => "Age",
            3 => "RowVersion",
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };
        public static string BindParameter(MergeTestEntity entity, int columnIndex, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) => "@p0";
        public static void ExtractColumnArrays(ReadOnlySpan<MergeTestEntity> entities, ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) { }
        public static Func<System.Data.IDataReader, MergeTestEntity> GetReaderParser() => _ => new MergeTestEntity();
        public static MergeTestEntity FromReader(System.Data.IDataReader reader) => new MergeTestEntity();
    }

    [Fact]
    public void BuildCreateStagingTableSql_GeneratesTopZeroSelect()
    {
        var sql = SqlBulkMergeStrategy.BuildCreateStagingTableSql("customers", "#staging_customers_123");
        sql.Should().Be("SELECT TOP 0 * INTO #staging_customers_123 FROM [customers]");
    }

    [Fact]
    public void BuildMergeSql_GeneratesCompleteStructuralMerge()
    {
        var columns = MergeTestEntity.GetColumns();
        var sql = SqlBulkMergeStrategy.BuildMergeSql<MergeTestEntity>("customers", "#staging_customers_123", columns);

        var normalizedSql = sql.Replace("\r\n", "\n");
        var expected =
            "MERGE INTO [customers] AS target USING #staging_customers_123 AS source\n" +
            "ON (target.[Id] = source.[Id])\n" +
            "WHEN MATCHED THEN UPDATE SET\n" +
            "    target.[Name] = source.[Name]\n" +
            ",    target.[Age] = source.[Age]\n" +
            "WHEN NOT MATCHED BY TARGET THEN INSERT (\n" +
            "[Id], [Name], [Age]) VALUES (\n" +
            "source.[Id], source.[Name], source.[Age]);";

        normalizedSql.Should().Be(expected);
    }

    private sealed class CompositeMergeEntity : IStaticEntityMetadata<CompositeMergeEntity>
    {
        public static string TableName => "order_items";
        public static int ColumnCount => 3;

        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[]
        {
            new ColumnMetadata(0, "OrderId", ColumnFlags.PrimaryKey),
            new ColumnMetadata(1, "ItemId", ColumnFlags.PrimaryKey),
            new ColumnMetadata(2, "Quantity", ColumnFlags.None)
        };

        public static bool IsNull(CompositeMergeEntity entity, int columnIndex) => false;
        public static bool IsDefault(CompositeMergeEntity entity, int columnIndex) => false;
        public static bool AreEqual(CompositeMergeEntity entity, CompositeMergeEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => columnIndex switch
        {
            0 => "OrderId",
            1 => "ItemId",
            2 => "Quantity",
            _ => throw new ArgumentOutOfRangeException(nameof(columnIndex))
        };
        public static string BindParameter(CompositeMergeEntity entity, int columnIndex, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) => "@p0";
        public static void ExtractColumnArrays(ReadOnlySpan<CompositeMergeEntity> entities, ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) { }
        public static Func<System.Data.IDataReader, CompositeMergeEntity> GetReaderParser() => _ => new CompositeMergeEntity();
        public static CompositeMergeEntity FromReader(System.Data.IDataReader reader) => new CompositeMergeEntity();
    }

    [Fact]
    public void BuildMergeSql_WithCompositePrimaryKey_JoinsKeysWithAnd()
    {
        var columns = CompositeMergeEntity.GetColumns();
        var sql = SqlBulkMergeStrategy.BuildMergeSql<CompositeMergeEntity>("order_items", "#staging_order_items", columns);

        var normalizedSql = sql.Replace("\r\n", "\n");
        var expected =
            "MERGE INTO [order_items] AS target USING #staging_order_items AS source\n" +
            "ON (target.[OrderId] = source.[OrderId] AND target.[ItemId] = source.[ItemId])\n" +
            "WHEN MATCHED THEN UPDATE SET\n" +
            "    target.[Quantity] = source.[Quantity]\n" +
            "WHEN NOT MATCHED BY TARGET THEN INSERT (\n" +
            "[OrderId], [ItemId], [Quantity]) VALUES (\n" +
            "source.[OrderId], source.[ItemId], source.[Quantity]);";

        normalizedSql.Should().Be(expected);
    }
}
