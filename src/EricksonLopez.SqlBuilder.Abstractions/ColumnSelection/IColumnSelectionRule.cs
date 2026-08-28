// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;

namespace EricksonLopez.SqlBuilder.ColumnSelection;

/// <summary>
/// Defines a contract for rules that determine which entity columns are included in a SQL statement.
/// </summary>
/// <typeparam name="TEntity">The type of the entity being processed.</typeparam>
public interface IColumnSelectionRule<TEntity> where TEntity : IStaticEntityMetadata<TEntity>
{
    /// <summary>
    /// Gets the execution phase indicating when this rule should be evaluated by the engine.
    /// </summary>
    RulePhase Phase { get; }
    
    /// <summary>
    /// Applies the selection logic to the given execution context.
    /// </summary>
    /// <param name="context">A reference to the context containing the entity and the current selection mask.</param>
    void Apply(ref ColumnSelectionContext<TEntity> context);
}


