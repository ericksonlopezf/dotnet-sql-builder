// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>Specifies the type of a SQL JOIN clause.</summary>
public enum JoinType
{
    /// <summary>INNER JOIN</summary>
    Inner,
    /// <summary>LEFT [OUTER] JOIN</summary>
    Left,
    /// <summary>RIGHT [OUTER] JOIN</summary>
    Right,
    /// <summary>CROSS JOIN</summary>
    Cross,
    /// <summary>FULL [OUTER] JOIN</summary>
    Full,
    /// <summary>CROSS APPLY (SQL Server) / equivalent to INNER JOIN LATERAL (PostgreSQL).</summary>
    CrossApply,
    /// <summary>OUTER APPLY (SQL Server) / equivalent to LEFT JOIN LATERAL (PostgreSQL).</summary>
    OuterApply
}
