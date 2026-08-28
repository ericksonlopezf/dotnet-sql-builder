// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Annotations;

/// <summary>
/// Defines the contract for an entity that is mapped to a SQL database table.
/// </summary>
public interface ISqlEntity
{
    /// <summary>Gets the name of the mapped database table.</summary>
    /// <returns>The table name.</returns>
    string GetTableName();
    
    /// <summary>Gets the column names mapped to the entity properties, excluding generated ones.</summary>
    /// <returns>An array of column names.</returns>
    string[] GetColumnNames();
    
    /// <summary>Gets the current values of the entity properties, excluding generated ones.</summary>
    /// <returns>An array of property values.</returns>
    object?[] GetValues();
    
    /// <summary>Gets all column names mapped to the entity properties, including generated ones.</summary>
    /// <returns>An array of all column names.</returns>
    string[] GetAllColumnNames();
    
    /// <summary>Gets all current values of the entity properties, including generated ones.</summary>
    /// <returns>An array of all property values.</returns>
    object?[] GetAllValues();
    
    /// <summary>Gets a mapping from property names to their corresponding database column names.</summary>
    /// <returns>A dictionary of property-to-column mappings.</returns>
    IReadOnlyDictionary<string, string> GetPropertyMap();
    
    /// <summary>Gets the names of the columns that are indexed.</summary>
    /// <returns>An array of indexed column names.</returns>
    string[] GetIndexedColumns();
}
