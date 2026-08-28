// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.ColumnSelection;
using EricksonLopez.SqlBuilder.ColumnSelection.Rules;
using Xunit;

namespace EricksonLopez.SqlBuilder.UnitTests;

public class ColumnSelectionEngineTests
{
    [Fact]
    public void ColumnSelectionContext_PropertiesAndMethods_Work()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        var snapshot = new TestEntity { Id = 1, Name = "Old", Age = 30 };
        var maskArray = new bool[3] { true, true, true };
        var mask = maskArray.AsSpan();

        var ctx = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Update, mask, snapshot);
        
        ctx.Snapshot.Should().BeSameAs(snapshot);
        ctx.Operation.Should().Be(SqlOperation.Update);
        
        ctx.Exclude(new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken(0));
        mask[0].Should().BeFalse();
        
        ctx.Include(new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken(0));
        mask[0].Should().BeTrue();
        
        ctx.IsDefault(1).Should().BeFalse();
        ctx.AreEqual(2).Should().BeTrue();

        var ctxNullSnapshot = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Update, mask, null);
        ctxNullSnapshot.AreEqual(0).Should().BeFalse();
    }

    [Fact]
    public void SelectColumns_WithNoRules_SelectsAllColumns()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        
        ColumnSelectionEngine<TestEntity>.SelectColumns(entity, SqlOperation.Insert, Array.Empty<IColumnSelectionRule<TestEntity>>(), mask);

        mask[0].Should().BeTrue();
        mask[1].Should().BeTrue();
        mask[2].Should().BeTrue();
    }

    [Fact]
    public void SelectColumns_WithExceptColumnsRule_UnsetsSpecifiedColumns()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        var rules = new IColumnSelectionRule<TestEntity>[] { new ExceptColumnsRule<TestEntity>(new[] { 1 }) }; // Name
        
        ColumnSelectionEngine<TestEntity>.SelectColumns(entity, SqlOperation.Insert, rules, mask);

        mask[0].Should().BeTrue();
        mask[1].Should().BeFalse();
        mask[2].Should().BeTrue();
    }
    [Fact]
    public void ExcludePrimaryKeysRule_WhenAlreadyExcluded_DoesNothing()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        mask.Fill(true);
        mask[0] = false; // Already excluded

        var ctx = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Update, mask, null);
        new ExcludePrimaryKeysRule<TestEntity>().Apply(ref ctx);

        mask[0].Should().BeFalse();
        mask[1].Should().BeTrue();
    }

    [Fact]
    public void OnlyColumnsRule_WhenAlreadyExcluded_DoesNothing()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        mask.Fill(true);
        mask[1] = false; // Already excluded Name

        var ctx = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Update, mask, null);
        new OnlyColumnsRule<TestEntity>(new[] { 1, 2 }).Apply(ref ctx);

        mask[0].Should().BeFalse(); // Because 0 is not in Only
        mask[1].Should().BeFalse(); // Was already excluded
        mask[2].Should().BeTrue();
    }
    [Fact]
    public void SelectColumns_WithExcludeGeneratedRule_UnsetsGeneratedColumns()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        var rules = new IColumnSelectionRule<TestEntity>[] { new ExcludeGeneratedRule<TestEntity>() };
        
        ColumnSelectionEngine<TestEntity>.SelectColumns(entity, SqlOperation.Insert, rules, mask);

        mask[0].Should().BeFalse(); // Id has ColumnFlags.Generated
        mask[1].Should().BeTrue();
        mask[2].Should().BeTrue();
    }

    [Fact]
    public void SelectColumns_WithExcludePrimaryKeysRule_UnsetsPrimaryKeys()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        var rules = new IColumnSelectionRule<TestEntity>[] { new ExcludePrimaryKeysRule<TestEntity>() };
        
        ColumnSelectionEngine<TestEntity>.SelectColumns(entity, SqlOperation.Update, rules, mask);

        mask[0].Should().BeFalse(); // Id has ColumnFlags.PrimaryKey
        mask[1].Should().BeTrue();
        mask[2].Should().BeTrue();
    }

    [Fact]
    public void SelectColumns_WithIgnoreNullsRule_UnsetsNullColumns()
    {
        var entity = new TestEntity { Id = 1, Name = null!, Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        var rules = new IColumnSelectionRule<TestEntity>[] { new IgnoreNullsRule<TestEntity>() };
        
        ColumnSelectionEngine<TestEntity>.SelectColumns(entity, SqlOperation.Update, rules, mask);

        mask[0].Should().BeTrue();
        mask[1].Should().BeFalse(); // Name is null
        mask[2].Should().BeTrue();
    }
    
    [Fact]
    public void SelectColumns_WithOnlyColumnsRule_UnsetsOtherColumns()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        var rules = new IColumnSelectionRule<TestEntity>[] { new OnlyColumnsRule<TestEntity>(new[] { 1 }) };
        
        ColumnSelectionEngine<TestEntity>.SelectColumns(entity, SqlOperation.Update, rules, mask);

        mask[0].Should().BeFalse();
        mask[1].Should().BeTrue();
        mask[2].Should().BeFalse();
    }

    private class Phase1IncludeRule : IColumnSelectionRule<TestEntity>
    {
        public RulePhase Phase => RulePhase.Phase1Baseline;
        public void Apply(ref ColumnSelectionContext<TestEntity> context)
        {
            context.Include(new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken(0));
        }
    }

    private class Phase2ExcludeRule : IColumnSelectionRule<TestEntity>
    {
        public RulePhase Phase => RulePhase.Phase2Structural;
        public void Apply(ref ColumnSelectionContext<TestEntity> context)
        {
            context.Exclude(new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken(0));
        }
    }

    [Fact]
    public void SelectColumns_ExecutesRulesInPhaseOrder()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        // Pass rules out of phase order
        var rules = new IColumnSelectionRule<TestEntity>[] { new Phase2ExcludeRule(), new Phase1IncludeRule() };
        
        ColumnSelectionEngine<TestEntity>.SelectColumns(entity, SqlOperation.Update, rules, mask);

        // Normally: Phase 1 runs Phase1IncludeRule (true), Phase 2 runs Phase2ExcludeRule (false). Result = false.
        // Mutated: Phase 3 runs Phase2ExcludeRule (false) then Phase1IncludeRule (true). Result = true.
        mask[0].Should().BeFalse();
    }

    [Fact]
    public void SelectColumns_WhenNoRules_FillsEntireMaskWithTrue()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        mask.Clear();

        ColumnSelectionEngine<TestEntity>.SelectColumns(entity, SqlOperation.Insert, ReadOnlySpan<IColumnSelectionRule<TestEntity>>.Empty, mask);

        for (int i = 0; i < mask.Length; i++)
        {
            mask[i].Should().BeTrue();
        }
    }

    [Fact]
    public void ColumnSelectionContext_EvaluatesEntityPredicatesCorrectly()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = null };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];
        mask.Fill(true);

        var context = new ColumnSelectionContext<TestEntity>(entity, SqlOperation.Insert, mask);

        context.Entity.Should().BeSameAs(entity);
        context.Operation.Should().Be(SqlOperation.Insert);
        context.Snapshot.Should().BeNull();

        // Index 2 is Age (null)
        context.IsNull(2).Should().BeTrue();
        // Index 0 is Id (1 -> not null)
        context.IsNull(0).Should().BeFalse();

        // Test include and exclude
        context.Exclude(new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken(0));
        mask[0].Should().BeFalse();
        context.Include(new EricksonLopez.SqlBuilder.Abstractions.Metadata.ColumnToken(0));
        mask[0].Should().BeTrue();
    }

    [Fact]
    public void ColumnSelectionContext_WithSnapshot_EvaluatesAreEqualCorrectly()
    {
        var current = new TestEntity { Id = 1, Name = "Alice", Age = 25 };
        var snapshot = new TestEntity { Id = 1, Name = "Alice", Age = 20 };
        Span<bool> mask = stackalloc bool[TestEntity.ColumnCount];

        var context = new ColumnSelectionContext<TestEntity>(current, SqlOperation.Update, mask, snapshot);

        context.Snapshot.Should().BeSameAs(snapshot);
        // Id and Name are identical between current and snapshot
        context.AreEqual(0).Should().BeTrue();
        context.AreEqual(1).Should().BeTrue();
        // Age is different (25 vs 20)
        context.AreEqual(2).Should().BeFalse();
    }

    [Fact]
    public void SelectColumns_WithIncorrectMaskLength_ThrowsArgumentException()
    {
        var entity = new TestEntity { Id = 1, Name = "Test", Age = 30 };
        // TestEntity.ColumnCount is 3, but we pass a mask of length 2
        Span<bool> mask = stackalloc bool[2];

        Action action = () =>
        {
            // Because ColumnSelectionEngine takes a Span, we can't use a lambda with standard Action easily if we want to capture the ref struct Span in some older C#, 
            // but we can just use a try-catch for Span since Action cannot capture ref structs.
        };

        // Standard way to test Span throwing ArgumentException
        try
        {
            ColumnSelectionEngine<TestEntity>.SelectColumns(entity, SqlOperation.Insert, Array.Empty<IColumnSelectionRule<TestEntity>>(), mask);
            Assert.Fail("Expected ArgumentException");
        }
        catch (ArgumentException ex)
        {
            ex.Message.Should().Contain("must exactly match TEntity.ColumnCount");
        }
    }
}




