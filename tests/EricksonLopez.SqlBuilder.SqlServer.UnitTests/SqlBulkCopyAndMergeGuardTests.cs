// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.SqlServer;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.SqlServer.Tests;

public class SqlBulkCopyAndMergeGuardTests
{
    private sealed class DummyEntity : IStaticEntityMetadata<DummyEntity>
    {
        public int Id { get; set; }
        public static string TableName => "dummies";
        public static int ColumnCount => 1;
        public static ReadOnlySpan<ColumnMetadata> GetColumns() => new[] { new ColumnMetadata(0, "Id", ColumnFlags.PrimaryKey) };
        public static bool IsNull(DummyEntity entity, int columnIndex) => false;
        public static bool IsDefault(DummyEntity entity, int columnIndex) => false;
        public static bool AreEqual(DummyEntity entity, DummyEntity snapshot, int columnIndex) => false;
        public static string GetColumnName(int columnIndex) => "Id";
        public static string BindParameter(DummyEntity entity, int columnIndex, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) => "@p0";
        public static void ExtractColumnArrays(ReadOnlySpan<DummyEntity> entities, ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) { }
        public static Func<IDataReader, DummyEntity> GetReaderParser() => _ => new DummyEntity();
        public static DummyEntity FromReader(IDataReader reader) => new DummyEntity();
    }

    [Fact]
    public async Task BulkInsertAsync_NonSqlConnection_ThrowsInvalidOperationException()
    {
        var nonSqlConn = Substitute.For<IDbConnection>();
        var entities = new List<DummyEntity> { new DummyEntity { Id = 1 } };

        var act = () => SqlBulkCopyStrategy.BulkInsertAsync(nonSqlConn, entities);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires a SqlConnection.*");
    }

    [Fact]
    public async Task BulkMergeAsync_NonSqlConnection_ThrowsInvalidOperationException()
    {
        var nonSqlConn = Substitute.For<IDbConnection>();
        var entities = new List<DummyEntity> { new DummyEntity { Id = 1 } };

        var act = () => SqlBulkMergeStrategy.BulkMergeAsync(nonSqlConn, entities);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*requires a SqlConnection.*");
    }
}
