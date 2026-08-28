// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions
{
    /// <summary>
    /// Represents specific capabilities that a SQL provider might or might not support.
    /// </summary>
    [Flags]
    public enum ProviderCapability
    {
        /// <summary>No special capabilities.</summary>
        None = 0,
        
        /// <summary>Supports CROSS APPLY and OUTER APPLY.</summary>
        Apply = 1 << 0,
        
        /// <summary>Supports INNER JOIN LATERAL and LEFT JOIN LATERAL.</summary>
        Lateral = 1 << 1,
        
        /// <summary>Supports INSERT ... RETURNING or OUTPUT.</summary>
        Returning = 1 << 2,
        
        /// <summary>Supports the MERGE (UPSERT) statement.</summary>
        Merge = 1 << 3,
        
        /// <summary>Supports Window Functions (OVER, PARTITION BY, etc.).</summary>
        WindowFunctions = 1 << 4,
        
        /// <summary>Supports Common Table Expressions (WITH).</summary>
        Cte = 1 << 5
    }
}

