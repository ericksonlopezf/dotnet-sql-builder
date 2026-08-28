// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.ColumnSelection;
using EricksonLopez.SqlBuilder.MariaDb;
using Xunit;

namespace EricksonLopez.SqlBuilder.MariaDb.Tests;

public class MariaDbRendererTests
{
    private readonly MariaDbRenderer _renderer = new(new MariaDbCompiler());

    [Fact]
    public void RenderBulkInsert_ThrowsNotSupportedException()
    {
        var act = () => _renderer.RenderBulkInsert(new[] { new DummyEntity() }, new List<IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Insert for MariaDB should use MySqlBatchStrategy*");
    }

    [Fact]
    public void RenderBulkUpdate_ThrowsNotSupportedException()
    {
        var act = () => _renderer.RenderBulkUpdate(new[] { new DummyEntity() }, new List<IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Update is not natively implemented for MariaDB*");
    }

    [Fact]
    public void RenderBulkMerge_ThrowsNotSupportedException()
    {
        var act = () => _renderer.RenderBulkMerge(new[] { new DummyEntity() }, new List<IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Merge is not supported for MariaDB*");
    }

    [Fact]
    public void RenderBulkUpsert_ThrowsNotSupportedException()
    {
        var act = () => _renderer.RenderBulkUpsert(new[] { new DummyEntity() }, new List<IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Upsert is not yet implemented for MariaDB*");
    }

    [Fact]
    public void RenderBulkInsertIgnore_ThrowsNotSupportedException()
    {
        var act = () => _renderer.RenderBulkInsertIgnore(new[] { new DummyEntity() }, new List<IColumnSelectionRule<DummyEntity>>(), 10);
        act.Should().Throw<NotSupportedException>().WithMessage("*AOT Bulk Insert Ignore is not yet implemented for MariaDB*");
    }
}
