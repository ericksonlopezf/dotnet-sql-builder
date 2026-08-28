// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Specifies the materialization behavior of a Common Table Expression (CTE) in supporting dialects (e.g. PostgreSQL 12+).
/// </summary>
public enum MaterializationHint
{
    /// <summary>Default materialization behavior determined by the SQL query planner.</summary>
    Default = 0,

    /// <summary>Forces the CTE to be materialized as a temporary physical table (MATERIALIZED).</summary>
    Materialized = 1,

    /// <summary>Forces the CTE to be inlined into the outer query where possible (NOT MATERIALIZED).</summary>
    NotMaterialized = 2
}
