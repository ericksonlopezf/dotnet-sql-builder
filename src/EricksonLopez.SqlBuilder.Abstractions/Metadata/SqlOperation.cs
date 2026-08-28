// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions.Metadata;

/// <summary>
/// Specifies the type of SQL operation being constructed or executed.
/// </summary>
public enum SqlOperation
{
    /// <summary>
    /// Represents a data retrieval operation (SELECT).
    /// </summary>
    Select = 0,
    
    /// <summary>
    /// Represents a data insertion operation (INSERT).
    /// </summary>
    Insert = 1,
    
    /// <summary>
    /// Represents a data modification operation (UPDATE).
    /// </summary>
    Update = 2,
    
    /// <summary>
    /// Represents a data deletion operation (DELETE).
    /// </summary>
    Delete = 3,
    
    /// <summary>
    /// Represents a data merge or upsert operation (MERGE).
    /// </summary>
    Merge = 4
}


