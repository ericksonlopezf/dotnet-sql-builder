// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;
using EricksonLopez.SqlBuilder.ColumnSelection;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class MockSqlRenderer : ISqlRenderer
{
    public bool[]? LastInsertMask { get; private set; }
    public bool[]? LastSetMask { get; private set; }
    public bool[]? LastWhereMask { get; private set; }
    public List<object>? LastRules { get; private set; }

    public SqlResult RenderInsert<T>(T entity, Span<bool> insertMask) where T : IStaticEntityMetadata<T>
    {
        LastInsertMask = insertMask.ToArray();
        return new SqlResult("INSERT", null!);
    }

    public SqlResult RenderUpdate<T>(T entity, Span<bool> setMask, Span<bool> whereMask) where T : IStaticEntityMetadata<T>
    {
        LastSetMask = setMask.ToArray();
        LastWhereMask = whereMask.ToArray();
        return new SqlResult("UPDATE", null!);
    }

    public BulkSqlResult RenderBulkInsert<T>(IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>
    {
        entities.Should().NotBeNull();
        rules.Should().NotBeNull();
        batchSize.Should().BeGreaterThan(0);
        LastRules = new List<object>(rules);
        return new BulkSqlResult(new[] { new SqlResult("BULK INSERT", null!) });
    }

    public BulkSqlResult RenderBulkUpdate<T>(IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>
    {
        entities.Should().NotBeNull();
        rules.Should().NotBeNull();
        batchSize.Should().BeGreaterThan(0);
        LastRules = new List<object>(rules);
        return new BulkSqlResult(new[] { new SqlResult("BULK UPDATE", null!) });
    }

    public BulkSqlResult RenderBulkMerge<T>(IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>
    {
        entities.Should().NotBeNull();
        rules.Should().NotBeNull();
        batchSize.Should().BeGreaterThan(0);
        LastRules = new List<object>(rules);
        return new BulkSqlResult(new[] { new SqlResult("BULK MERGE", null!) });
    }

    public BulkSqlResult RenderBulkUpsert<T>(IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>
    {
        entities.Should().NotBeNull();
        rules.Should().NotBeNull();
        batchSize.Should().BeGreaterThan(0);
        LastRules = new List<object>(rules);
        return new BulkSqlResult(new[] { new SqlResult("BULK UPSERT", null!) });
    }

    public BulkSqlResult RenderBulkInsertIgnore<T>(IEnumerable<T> entities, List<IColumnSelectionRule<T>> rules, int batchSize) where T : IStaticEntityMetadata<T>
    {
        entities.Should().NotBeNull();
        rules.Should().NotBeNull();
        batchSize.Should().BeGreaterThan(0);
        LastRules = new List<object>(rules);
        return new BulkSqlResult(new[] { new SqlResult("BULK INSERT IGNORE", null!) });
    }
}



