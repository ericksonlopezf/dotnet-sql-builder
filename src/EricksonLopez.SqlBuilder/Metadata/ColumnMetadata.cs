// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Metadata;

/// <summary>
/// Contains metadata information for a single entity property mapped to a database column.
/// </summary>
public sealed class ColumnMetadata
{
    /// <summary>
    /// Gets the name of the column in the database.
    /// </summary>
    public string ColumnName { get; }
    
    /// <summary>
    /// Gets the name of the property on the entity class.
    /// </summary>
    public string PropertyName { get; }
    
    /// <summary>
    /// Gets the bitwise flags describing the column's characteristics.
    /// </summary>
    public ColumnFlags Flags { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColumnMetadata"/> class.
    /// </summary>
    /// <param name="columnName">The name of the database column.</param>
    /// <param name="propertyName">The name of the entity property.</param>
    /// <param name="flags">The characteristics of the column.</param>
    public ColumnMetadata(string columnName, string propertyName, ColumnFlags flags)
    {
        ColumnName = columnName;
        PropertyName = propertyName;
        Flags = flags;
    }
}
