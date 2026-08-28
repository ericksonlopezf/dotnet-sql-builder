// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Builders;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing;
using NSubstitute;
using Xunit;

namespace EricksonLopez.SqlBuilder.PostgreSql.UnitTests;

public class PostgreSqlRendererTests
{

    [Fact]
    public void RenderInsert_ShouldRenderCorrectly()
    {
        var compiler = Substitute.For<ISqlCompiler>();
        compiler.Escape(Arg.Any<string>()).Returns(x => $"\"{x.Arg<string>()}\"");
        var pm = new ParameterManager();
        compiler.CreateParameterManager().Returns(pm);
        var renderer = new PostgreSqlRenderer(compiler);
        var entity = new DummyEntity { Id = 1, Name = "Test" };
        var mask = new bool[] { true, true };

        var result = renderer.RenderInsert(entity, mask);

        result.Sql.Should().Be("INSERT INTO \"dummy\" (\"Id\", \"Name\") VALUES (@p0, @p1) RETURNING *");
    }

    [Fact]
    public void RenderUpdate_ShouldRenderCorrectly()
    {
        var compiler = Substitute.For<ISqlCompiler>();
        compiler.Escape(Arg.Any<string>()).Returns(x => $"\"{x.Arg<string>()}\"");
        var pm = new ParameterManager();
        compiler.CreateParameterManager().Returns(pm);
        var renderer = new PostgreSqlRenderer(compiler);
        var entity = new DummyEntity { Id = 1, Name = "Test" };
        var setMask = new bool[] { false, true };
        var whereMask = new bool[] { true, false };

        var result = renderer.RenderUpdate(entity, setMask, whereMask);

        result.Sql.Should().Be("UPDATE \"dummy\" SET \"Name\" = @p0 WHERE \"Id\" = @p1 RETURNING *");
    }

    [Fact]
    public void RenderBulkInsert_ShouldRenderCorrectly()
    {
        var compiler = Substitute.For<ISqlCompiler>();
        compiler.Escape(Arg.Any<string>()).Returns(x => $"\"{x.Arg<string>()}\"");
        var pm = new ParameterManager();
        compiler.CreateParameterManager().Returns(pm);
        var renderer = new PostgreSqlRenderer(compiler);
        var entities = new[] { new DummyEntity { Id = 1, Name = "Test" } };
        var rules = new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<DummyEntity>>();

        var result = renderer.RenderBulkInsert(entities, rules, 100);

        result.Batches.Count.Should().Be(1);
        result.Batches[0].Sql.Should().Be("INSERT INTO \"dummy\" (\"Id\", \"Name\") SELECT * FROM UNNEST(@C0, @C1)");
    }
    
    [Fact]
    public void RenderBulkInsert_Empty_ShouldThrow()
    {
        var compiler = Substitute.For<ISqlCompiler>();
        var renderer = new PostgreSqlRenderer(compiler);
        var rules = new List<EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<DummyEntity>>();

        Action act = () => renderer.RenderBulkInsert(Array.Empty<DummyEntity>(), rules, 100);
        act.Should().Throw<InvalidOperationException>();
    }
}



