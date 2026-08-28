// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.ColumnSelection;
using Xunit;

namespace EricksonLopez.SqlBuilder.Abstractions.UnitTests;

public class ColumnSelectionContextTests
{
    public class TestEntity : IStaticEntityMetadata<TestEntity>
    {
        public static int ColumnCount => 3;
        public static string TableName => "Test";
        public static bool IsNull(TestEntity entity, int columnIndex) => false;
        public static bool IsDefault(TestEntity entity, int columnIndex) => false;
        public static bool AreEqual(TestEntity left, TestEntity right, int columnIndex) => columnIndex == 0;
        public static System.ReadOnlySpan<ColumnMetadata> GetColumns() => new ColumnMetadata[0];
        public static string GetColumnName(int columnIndex) => "A";
        public static string BindParameter(TestEntity entity, int columnIndex, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) => "@p0";
        public static void ExtractColumnArrays(System.ReadOnlySpan<TestEntity> entities, System.ReadOnlySpan<bool> activeColumns, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameters) { }
        public static TestEntity FromReader(System.Data.IDataReader reader) => new TestEntity();
        public static System.Func<System.Data.IDataReader, TestEntity> GetReaderParser() => (r) => new TestEntity();
    }

    [Fact]
    public void Exclude_SetsBitToFalse()
    {
        // Arrange
        var entity = new TestEntity();
        var bools = new bool[] { true, true, true };
        var context = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Update, bools, null);
        var token = new ColumnToken(1);

        // Act
        context.Exclude(token);

        // Assert
        context.IncludedColumns[1].Should().BeFalse();
    }

    [Fact]
    public void Include_SetsBitToTrue()
    {
        // Arrange
        var entity = new TestEntity();
        var bools = new bool[] { false, false, false };
        var context = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Update, bools, null);
        var token = new ColumnToken(1);

        // Act
        context.Include(token);

        // Assert
        context.IncludedColumns[1].Should().BeTrue();
    }

    [Fact]
    public void AreEqual_ReturnsTrue_WhenSnapshotIsNotNullAndEntitiesMatch()
    {
        // Arrange
        var entity = new TestEntity();
        var snapshot = new TestEntity();
        var bools = new bool[] { false, false, false };
        var context = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Update, bools, snapshot);

        // Act
        var result = context.AreEqual(0);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AreEqual_ReturnsFalse_WhenSnapshotIsNotNullAndEntitiesDoNotMatch()
    {
        // Arrange
        var entity = new TestEntity();
        var snapshot = new TestEntity();
        var bools = new bool[] { false, false, false };
        var context = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Update, bools, snapshot);

        // Act
        var result = context.AreEqual(1);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void AreEqual_ReturnsFalse_WhenSnapshotIsNull()
    {
        // Arrange
        var entity = new TestEntity();
        var bools = new bool[] { false, false, false };
        var context = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Update, bools, null);

        // Act
        var result = context.AreEqual(0);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsNull_And_IsDefault_DelegateToEntityMetadata()
    {
        var entity = new TestEntity();
        var bools = new bool[] { true, true, true };
        var context = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Insert, bools);

        context.Entity.Should().BeSameAs(entity);
        context.Operation.Should().Be(SqlOperation.Insert);
        context.Snapshot.Should().BeNull();
        context.IsNull(0).Should().BeFalse();
        context.IsDefault(0).Should().BeFalse();
    }
}




