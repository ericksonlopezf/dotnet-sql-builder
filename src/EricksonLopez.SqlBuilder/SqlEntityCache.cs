// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace EricksonLopez.SqlBuilder;

internal static class SqlEntityCache<T> where T : new()

{
    /// <summary>
    /// Gets the resolved table name for the entity.
    /// </summary>
    public static readonly string TableName;
    
    /// <summary>
    /// Gets the ordered array of column names associated with the entity.
    /// </summary>
    public static readonly string[] ColumnNames;
    
    /// <summary>
    /// Gets a dictionary mapping property names to their corresponding database column names.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> PropertyMap;
    
    /// <summary>
    /// Gets a set of column names that are indexed for fast lookup.
    /// </summary>
    public static readonly HashSet<string> IndexedColumns;

    static SqlEntityCache()
    {
        var type = typeof(T);
        IndexedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var instance = new T() as EricksonLopez.SqlBuilder.Annotations.ISqlEntity;
        if (instance != null)
        {
            TableName = instance.GetTableName();
            PropertyMap = instance.GetPropertyMap();
            ColumnNames = instance.GetColumnNames();
            var indexed = instance.GetIndexedColumns();
            if (indexed != null)
            {
                foreach (var col in indexed)
                {
                    IndexedColumns.Add(col);
                }
            }
        }
        else
        {
            // STAB-003: Throw exception for unannotated POCOs to prevent silent NativeAOT violations
            throw new InvalidOperationException(
                $"Type {type.Name} does not implement ISqlEntity. " +
                "NativeAOT paths require the [SqlEntity] attribute on all models. " +
                "To use unannotated POCOs, use Sql.From<T>(\"tableName\").");
        }
    }
}





