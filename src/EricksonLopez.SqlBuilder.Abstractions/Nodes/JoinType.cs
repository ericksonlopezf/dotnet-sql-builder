// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>Specifies the type of a SQL JOIN clause.</summary>
public enum JoinType
{
    /// <summary>Specifies an INNER JOIN operation.</summary>
    Inner,
    /// <summary>Specifies a LEFT OUTER JOIN operation.</summary>
    Left,
    /// <summary>Specifies a RIGHT OUTER JOIN operation.</summary>
    Right,
    /// <summary>Specifies a CROSS JOIN operation.</summary>
    Cross,
    /// <summary>Specifies a FULL OUTER JOIN operation.</summary>
    Full,
    /// <summary>Specifies a CROSS APPLY or INNER JOIN LATERAL operation.</summary>
    CrossApply,
    /// <summary>Specifies an OUTER APPLY or LEFT JOIN LATERAL operation.</summary>
    OuterApply
}
