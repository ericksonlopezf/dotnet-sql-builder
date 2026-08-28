// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.ColumnSelection.Rules;

/// <summary>
/// A column selection rule that dynamically excludes any columns whose underlying property value is <see langword="null"/>.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being processed.</typeparam>
public readonly struct IgnoreNullsRule<TEntity> : IColumnSelectionRule<TEntity> 
    where TEntity : IStaticEntityMetadata<TEntity>
{
    /// <inheritdoc />
    public RulePhase Phase => RulePhase.Phase3ValueBased;

    /// <inheritdoc />
    public void Apply(ref ColumnSelectionContext<TEntity> context)
    {
        for (int i = 0; i < context.IncludedColumns.Length; i++)
        {
            if (context.IncludedColumns[i] && context.IsNull(i))
            {
                context.IncludedColumns[i] = false;
            }
        }
    }
}


