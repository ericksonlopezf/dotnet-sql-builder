// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.ColumnSelection.Rules;

/// <summary>
/// A column selection rule that acts as a whitelist, excluding any columns not explicitly specified.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being processed.</typeparam>
public readonly struct OnlyColumnsRule<TEntity> : IColumnSelectionRule<TEntity> 
    where TEntity : IStaticEntityMetadata<TEntity>
{
    /// <inheritdoc />
    public RulePhase Phase => RulePhase.Phase1Baseline;
    private readonly int[] _allowedIndexes;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnlyColumnsRule{TEntity}"/> struct.
    /// </summary>
    /// <param name="allowedIndexes">An array of integer identifiers representing the only columns to include.</param>
    public OnlyColumnsRule(int[] allowedIndexes)
    {
        _allowedIndexes = allowedIndexes;
    }

    /// <inheritdoc />
    public void Apply(ref ColumnSelectionContext<TEntity> context)
    {
        var allowedSpan = _allowedIndexes.AsSpan();
        for (int i = 0; i < context.IncludedColumns.Length; i++)
        {
            if (context.IncludedColumns[i] && !allowedSpan.Contains(i))
            {
                context.IncludedColumns[i] = false;
            }
        }
    }
}
