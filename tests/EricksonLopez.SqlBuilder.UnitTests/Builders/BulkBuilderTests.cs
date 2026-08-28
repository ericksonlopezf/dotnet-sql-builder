// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Builders.Bulk;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class BulkBuilderTests
{
    private readonly MockSqlRenderer _renderer = new();

    [Fact]
    public void Insert_WithBatchSizeAndIgnoreNulls_Succeeds()
    {
        var entities = new[] { new TestEntity { Id = 1, Name = "Test" } };

        var result = Sql.Bulk(entities)
            .WithBatchSize(100)
            .IgnoreNulls()
            .Insert()
            .Build(_renderer);

        result.Batches[0].Sql.Should().Be("BULK INSERT");
        _renderer.LastRules.Should().NotBeNull();
        _renderer.LastRules.Should().ContainSingle(r => r.GetType().Name == "IgnoreNullsRule`1");
    }

    [Fact]
    public void Update_WithOnly_Succeeds()
    {
        var entities = new[] { new TestEntity { Id = 1, Name = "Test" } };

        var result = Sql.Bulk(entities)
            .Only(1)
            .Update()
            .Build(_renderer);

        result.Batches[0].Sql.Should().Be("BULK UPDATE");
        _renderer.LastRules.Should().NotBeNull();
        _renderer.LastRules.Should().ContainSingle(r => r.GetType().Name == "OnlyColumnsRule`1");
    }

    [Fact]
    public void Merge_WithExcludeGenerated_Succeeds()
    {
        var entities = new[] { new TestEntity { Id = 1, Name = "Test" } };

        var result = Sql.Bulk(entities)
            .ExcludeGenerated()
            .Merge()
            .Build(_renderer);

        result.Batches[0].Sql.Should().Be("BULK MERGE");
        _renderer.LastRules.Should().NotBeNull();
        _renderer.LastRules.Should().HaveCount(2);
        _renderer.LastRules![1].GetType().Name.Should().Be("ExcludeGeneratedRule`1");
    }

    [Fact]
    public void Upsert_Succeeds()
    {
        var entities = new[] { new TestEntity { Id = 1, Name = "Test" } };

        var result = Sql.Bulk(entities)
            .Upsert()
            .Build(_renderer);

        result.Batches[0].Sql.Should().Be("BULK UPSERT");
        _renderer.LastRules.Should().NotBeNull();
        _renderer.LastRules.Should().ContainSingle(r => r.GetType().Name == "ExcludeGeneratedRule`1");
    }

    [Fact]
    public void InsertIgnore_Succeeds()
    {
        var entities = new[] { new TestEntity { Id = 1, Name = "Test" } };

        var result = Sql.Bulk(entities)
            .InsertIgnore()
            .Build(_renderer);

        result.Batches[0].Sql.Should().Be("BULK INSERT IGNORE");
        _renderer.LastRules.Should().NotBeNull();
        _renderer.LastRules.Should().ContainSingle(r => r.GetType().Name == "ExcludeGeneratedRule`1");
    }
}



