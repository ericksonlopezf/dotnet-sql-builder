// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.ColumnSelection;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class PostgreSqlCompilerBulkTests
{
    private class MockBulkEntity : IStaticEntityMetadata<MockBulkEntity>
    {
        public static string TableName => "mock_bulk_table";
        public static int ColumnCount => 2;

        public static string GetColumnName(int index) => index switch
        {
            0 => "id",
            1 => "name",
            _ => throw new IndexOutOfRangeException()
        };

        public static ReadOnlySpan<EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata> GetColumns() => new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata[0];
        
        public static string BindParameter(MockBulkEntity entity, int columnIndex, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) => throw new NotImplementedException();
        public static bool IsNull(MockBulkEntity entity, int columnIndex) => false;
        public static bool IsDefault(MockBulkEntity entity, int columnIndex) => false;
        public static bool AreEqual(MockBulkEntity entity, MockBulkEntity snapshot, int columnIndex) => false;

        public int Id { get; set; }
        public string? Name { get; set; }

        public static void ExtractColumnArrays(System.ReadOnlySpan<MockBulkEntity> entities, System.ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters)
        {
            if (activeColumns[0])
            {
                parameters.AddNamed("C0", new int[] { 1, 2 });
            }

            if (activeColumns[1])
            {
                parameters.AddNamed("C1", new string[] { "A", "B" });
            }
        }
        public static MockBulkEntity FromReader(System.Data.IDataReader reader) => new MockBulkEntity();
        public static System.Func<System.Data.IDataReader, MockBulkEntity> GetReaderParser() => (r) => new MockBulkEntity();
    }

    [Fact]
    public void RenderBulkInsert_ShouldGenerateUnnestSql()
    {
        // Arrange
        var compiler = new PostgreSqlCompiler();
        var entities = new List<MockBulkEntity> 
        { 
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        };
        var rules = new List<IColumnSelectionRule<MockBulkEntity>>();

        // Act
        var result = compiler.RenderBulkInsert(entities, rules, 100);

        // Assert
        result.Should().NotBeNull();
        result.Batches.Should().HaveCount(1);
        
        var batch = result.Batches[0];
        batch.Sql.Should().Be("INSERT INTO \"mock_bulk_table\" (\"id\", \"name\") SELECT * FROM UNNEST(@C0, @C1)");
        
        batch.Parameters.Should().ContainKey("C0");
        batch.Parameters.Should().ContainKey("C1");
    }

    [Fact]
    public void RenderBulkInsert_EmptyEntities_ShouldThrowInvalidOperationException()
    {
        var compiler = new PostgreSqlCompiler();
        var entities = new List<MockBulkEntity>();
        var rules = new List<IColumnSelectionRule<MockBulkEntity>>();
        
        Action act = () => compiler.RenderBulkInsert(entities, rules, 100);
        
        act.Should().Throw<InvalidOperationException>().WithMessage("Collection is empty.");
    }
}



