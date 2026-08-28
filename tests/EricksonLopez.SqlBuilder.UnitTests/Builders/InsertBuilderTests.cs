// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Builders.Insert;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class InsertBuilderTests
{
    private readonly MockSqlRenderer _renderer = new();

    [Fact]
    public void Build_WithAllColumns_Succeeds()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var builder = new InsertBuilder<TestEntity>(entity);

        var result = builder.Build(_renderer);
        result.Sql.Should().Be("INSERT");
        _renderer.LastInsertMask![0].Should().BeFalse(); // Excluded by ExcludeGeneratedRule
        _renderer.LastInsertMask![1].Should().BeTrue();
        _renderer.LastInsertMask![2].Should().BeTrue();
    }

    [Fact]
    public void Build_WithIgnoreNulls_ExcludesNullColumns()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = null };
        var builder = new InsertBuilder<TestEntity>(entity).IgnoreNulls();

        var result = builder.Build(_renderer);
        result.Sql.Should().Be("INSERT");
        _renderer.LastInsertMask![0].Should().BeFalse();
        _renderer.LastInsertMask![1].Should().BeTrue();
        _renderer.LastInsertMask![2].Should().BeFalse();
    }

    [Fact]
    public void Build_WithOnly_IncludesOnlySpecifiedColumns()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var builder = new InsertBuilder<TestEntity>(entity).Only(1); // Name

        var result = builder.Build(_renderer);
        result.Sql.Should().Be("INSERT");
        _renderer.LastInsertMask![0].Should().BeFalse();
        _renderer.LastInsertMask![1].Should().BeTrue();
        _renderer.LastInsertMask![2].Should().BeFalse();
    }

    [Fact]
    public void Build_WithExcept_ExcludesSpecifiedColumns()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var builder = new InsertBuilder<TestEntity>(entity).Except(1); // Name

        var result = builder.Build(_renderer);
        result.Sql.Should().Be("INSERT");
        _renderer.LastInsertMask![0].Should().BeFalse();
        _renderer.LastInsertMask![1].Should().BeFalse();
        _renderer.LastInsertMask![2].Should().BeTrue();
    }

    [Fact]
    public void Build_WithNoColumnsSelected_ThrowsInvalidOperationException()
    {
        var entity = new TestEntity { Id = 0, Name = null!, Age = null };
        var builder = new InsertBuilder<TestEntity>(entity).Except(0, 1, 2);

        Action act = () => builder.Build(_renderer);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("No columns selected for insert.");
    }

    [Fact]
    public void Entity_Property_ReturnsEntity()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var builder = new InsertBuilder<TestEntity>(entity);
        builder.Entity.Should().BeSameAs(entity);
    }

    private class DummyRule : EricksonLopez.SqlBuilder.ColumnSelection.IColumnSelectionRule<TestEntity>
    {
        public EricksonLopez.SqlBuilder.ColumnSelection.RulePhase Phase => EricksonLopez.SqlBuilder.ColumnSelection.RulePhase.Phase4Overrides;
        public void Apply(ref EricksonLopez.SqlBuilder.ColumnSelection.ColumnSelectionContext<TestEntity> context) 
        {
            context.Exclude(new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken(1));
        }
    }

    [Fact]
    public void AddRule_AddsCustomRule()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var builder = new InsertBuilder<TestEntity>(entity);
        builder.AddRule(new DummyRule());
        var result = builder.Build(_renderer);
        result.Sql.Should().Be("INSERT");
        _renderer.LastInsertMask![1].Should().BeFalse();
    }
}



