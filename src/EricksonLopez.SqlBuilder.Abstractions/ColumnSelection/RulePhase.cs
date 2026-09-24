// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.ColumnSelection;

/// <summary>
/// Defines the sequence phases in which column selection rules are evaluated.
/// Rules in lower phases execute before rules in higher phases.
/// </summary>
public enum RulePhase : byte
{
    /// <summary>
    /// Specifies the initial phase establishing the baseline selection (e.g., OnlyColumnsRule).
    /// </summary>
    Phase1Baseline = 0,
    
    /// <summary>
    /// Specifies the phase evaluating structural metadata attributes (e.g., ExcludeGenerated, ExcludePrimaryKeys).
    /// </summary>
    Phase2Structural = 1,
    
    /// <summary>
    /// Specifies the phase evaluating runtime property values (e.g., IgnoreNulls, IgnoreDefaults).
    /// </summary>
    Phase3ValueBased = 2,
    
    /// <summary>
    /// Specifies the final phase applying explicit user overrides (e.g., ExceptColumnsRule).
    /// </summary>
    Phase4Overrides = 3
}
