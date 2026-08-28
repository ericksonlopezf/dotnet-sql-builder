// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.ColumnSelection.Rules;

/// <summary>
/// A column selection rule that explicitly excludes specific columns from the SQL operation.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being processed.</typeparam>
public readonly struct ExceptColumnsRule<TEntity> : IColumnSelectionRule<TEntity> 
    where TEntity : IStaticEntityMetadata<TEntity>
{
    /// <inheritdoc />
    public RulePhase Phase => RulePhase.Phase4Overrides;
    private readonly int[] _deniedIndexes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptColumnsRule{TEntity}"/> struct.
    /// </summary>
    /// <param name="deniedIndexes">An array of integer identifiers representing the columns to exclude.</param>
    public ExceptColumnsRule(int[] deniedIndexes)
    {
        _deniedIndexes = deniedIndexes;
    }

    /// <inheritdoc />
    public void Apply(ref ColumnSelectionContext<TEntity> context)
    {
        var deniedSpan = _deniedIndexes.AsSpan();
        for (int i = 0; i < context.IncludedColumns.Length; i++)
        {
            if (context.IncludedColumns[i] && deniedSpan.Contains(i))
            {
                context.IncludedColumns[i] = false;
            }
        }
    }
}
