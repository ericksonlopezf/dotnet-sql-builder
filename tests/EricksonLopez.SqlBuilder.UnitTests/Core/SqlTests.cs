// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Testing;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class SqlTests
{

    private class DummyBulkEntity : EricksonLopez.SqlBuilder.Abstractions.Metadata.IStaticEntityMetadata<DummyBulkEntity>
    {
        public static string TableName => throw new System.NotImplementedException();
        public static string[] ColumnNames => throw new System.NotImplementedException();
        public static string[] AllColumnNames => throw new System.NotImplementedException();
        public static IReadOnlyDictionary<string, string> PropertyMap => throw new System.NotImplementedException();
        public static string[] IndexedColumns => throw new System.NotImplementedException();
        public static int ColumnCount => throw new System.NotImplementedException();
        
        public static string GetColumnName(int index) => throw new System.NotImplementedException();
        public static System.ReadOnlySpan<EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnMetadata> GetColumns() => throw new System.NotImplementedException();
        public static bool IsNull(DummyBulkEntity entity, int index) => throw new System.NotImplementedException();
        public static bool IsDefault(DummyBulkEntity entity, int index) => throw new System.NotImplementedException();
        public static bool AreEqual(DummyBulkEntity a, DummyBulkEntity b, int index) => throw new System.NotImplementedException();
        public static string BindParameter(DummyBulkEntity entity, int index, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameterManager) => throw new System.NotImplementedException();
        public static void ExtractColumnArrays(System.ReadOnlySpan<DummyBulkEntity> entities, System.ReadOnlySpan<bool> nullabilityMap, EricksonLopez.SqlBuilder.Abstractions.IParameterManager parameterManager) => throw new System.NotImplementedException();
        public static DummyBulkEntity FromReader(System.Data.IDataReader reader) => throw new System.NotImplementedException();
        public static System.Func<System.Data.IDataReader, DummyBulkEntity> GetReaderParser() => throw new System.NotImplementedException();
    }

    [Fact]
    public void From_CreatesSelectQuery()
    {
        var query = Sql.From<DummyEntity>();
        query.Should().BeOfType<SelectQuery<DummyEntity>>();
    }

    [Fact]
    public void Insert_CreatesInsertQuery()
    {
        var entity = new DummyEntity();
        var query = Sql.Insert(entity);
        query.Should().BeOfType<InsertQuery<DummyEntity>>();
    }

    [Fact]
    public void BulkInsert_CreatesInsertQuery()
    {
        var entities = new[] { new DummyEntity(), new DummyEntity() };
        var query = Sql.BulkInsert(entities);
        query.Should().BeOfType<InsertQuery<DummyEntity>>();
    }

    [Fact]
    public void Bulk_CreatesBulkBuilder()
    {
        var entities = new[] { new DummyBulkEntity(), new DummyBulkEntity() };
        var builder = Sql.Bulk(entities);
        builder.Should().BeOfType<EricksonLopez.SqlBuilder.Builders.Bulk.BulkBuilder<DummyBulkEntity>>();
    }

    [Fact]
    public void Update_CreatesUpdateQuery()
    {
        var query = Sql.Update<DummyEntity>().WhereAll();
        query.Should().BeOfType<UpdateQuery<DummyEntity>>();
    }

    [Fact]
    public void Update_WithEntity_CreatesUpdateSetBuilder()
    {
        var entity = new DummyEntity();
        var query = Sql.Update(entity);
        query.Should().NotBeNull();
    }



    [Fact]
    public void Delete_CreatesDeleteQuery()
    {
        var query = Sql.Delete<DummyEntity>().WhereAll();
        query.Should().BeOfType<DeleteQuery<DummyEntity>>();
    }





    [Fact]
    public void InsertFrom_CreatesInsertQuery()
    {
        var selectQuery = Sql.From<DummyEntity>();
        var query = Sql.InsertFrom<DummyEntity>(selectQuery, "Id");
        var node = query.Nodes.OfType<InsertSelectNode>().Single();
        node.Columns.Should().NotBeNull().And.ContainSingle().Which.Should().Be("Id");
    }

    [Fact]
    public void InsertFrom_WithoutColumns_CreatesInsertQuery()
    {
        var selectQuery = Sql.From<DummyEntity>();
        var query = Sql.InsertFrom<DummyEntity>(selectQuery);
        var node = query.Nodes.OfType<InsertSelectNode>().Single();
        node.Columns.Should().BeNull();
    }

    [Fact]
    public void RegisterTypeHandler_AddsToDictionary()
    {
        var handler = NSubstitute.Substitute.For<EricksonLopez.SqlBuilder.Abstractions.ITypeHandler>();
        Sql.RegisterTypeHandler<DummyEntity>(handler);
        Sql.TypeHandlers.Should().ContainKey(typeof(DummyEntity));
        Sql.TypeHandlers[typeof(DummyEntity)].Should().Be(handler);
    }

    [Fact]
    public void ILike_ThrowsInvalidOperationException()
    {
        System.Action act = () => Sql.ILike("column", "pattern");
        act.Should().Throw<System.InvalidOperationException>().WithMessage("*is for SQL expression building only*");
    }

    [Fact]
    public void Any_ThrowsInvalidOperationException()
    {
        System.Action act = () => Sql.Any(1, new[] { 1, 2, 3 });
        act.Should().Throw<System.InvalidOperationException>().WithMessage("*is for SQL expression building only*");
    }

    [Fact]
    public void All_ThrowsInvalidOperationException()
    {
        System.Action act = () => Sql.All(1, new[] { 1, 2, 3 });
        act.Should().Throw<System.InvalidOperationException>().WithMessage("*is for SQL expression building only*");
    }

    [Fact]
    public void Between_ThrowsInvalidOperationException()
    {
        System.Action act = () => Sql.Between(5, 1, 10);
        act.Should().Throw<System.InvalidOperationException>().WithMessage("*is for SQL expression building only*");
    }

    [Fact]
    public void Coalesce_ThrowsInvalidOperationException()
    {
        int? val = null;
        System.Action act = () => Sql.Coalesce(val, 10);
        act.Should().Throw<System.InvalidOperationException>().WithMessage("*is for SQL expression building only*");
    }

    [Fact]
    public void Raw_FormattableString_CreatesRawQuery()
    {
        var id = 1;
        var query = Sql.Raw((System.FormattableString)$"SELECT {id}");
        query.Should().BeOfType<RawQuery>();
        query.RawSql.Should().Be("SELECT @p0");
        
        var dict = query.Parameters as Dictionary<string, object?>;
        dict.Should().NotBeNull();
        dict!["@p0"].Should().Be(1);
    }
}




