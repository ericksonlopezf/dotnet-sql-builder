// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.ColumnSelection.Rules;

/// <summary>
/// A column selection rule that automatically excludes primary key columns from the SQL operation.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being processed.</typeparam>
public readonly struct ExcludePrimaryKeysRule<TEntity> : IColumnSelectionRule<TEntity> 
    where TEntity : IStaticEntityMetadata<TEntity>
{
    /// <inheritdoc />
    public RulePhase Phase => RulePhase.Phase2Structural;

    /// <inheritdoc />
    public void Apply(ref ColumnSelectionContext<TEntity> context)
    {
        var columns = TEntity.GetColumns();
        for (int i = 0; i < context.IncludedColumns.Length; i++)
        {
            if (context.IncludedColumns[i] && columns[i].HasFlag(ColumnFlags.PrimaryKey))
            {
                context.IncludedColumns[i] = false;
            }
        }
    }
}


