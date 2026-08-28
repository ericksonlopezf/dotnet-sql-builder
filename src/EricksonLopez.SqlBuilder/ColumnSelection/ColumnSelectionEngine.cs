// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.ColumnSelection;

/// <summary>
/// Provides a high-performance, allocation-free engine for executing column selection rules.
/// </summary>
/// <typeparam name="TEntity">The type of the entity whose columns are being selected.</typeparam>
public ref struct ColumnSelectionEngine<TEntity> where TEntity : IStaticEntityMetadata<TEntity>
{
    /// <summary>
    /// Executes a set of column selection rules in phase order to determine the final bitmask of included columns.
    /// </summary>
    /// <param name="entity">The entity instance containing the data.</param>
    /// <param name="operation">The SQL operation being performed (e.g., Insert, Update).</param>
    /// <param name="rules">The span of rules to apply.</param>
    /// <param name="resultMask">A pre-allocated bitmask span that will be mutated to reflect the final selection.</param>
    /// <exception cref="ArgumentException"><paramref name="resultMask"/> length does not match <c>TEntity.ColumnCount</c></exception>
    public static void SelectColumns(
        TEntity entity, 
        SqlOperation operation,
        ReadOnlySpan<IColumnSelectionRule<TEntity>> rules, 
        Span<bool> resultMask)
    {
        if (resultMask.Length != TEntity.ColumnCount)
        {
            throw new ArgumentException($"The length of resultMask ({resultMask.Length}) must exactly match TEntity.ColumnCount ({TEntity.ColumnCount}).", nameof(resultMask));
        }

        resultMask.Fill(true); 
        
        var context = new ColumnSelectionContext<TEntity>(entity, operation, resultMask);

        // Execute rules ordered by phase
        for (int phase = 0; phase <= 3; phase++)
        {
            foreach (var rule in rules)
            {
                if ((int)rule.Phase == phase)
                {
                    rule.Apply(ref context);
                }
            }
        }
    }
}
