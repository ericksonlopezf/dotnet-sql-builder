// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Nodes;

/// <summary>
/// Represents a key column used in composite cursor (keyset) pagination.
/// </summary>
/// <param name="ColumnName">The SQL column name.</param>
/// <param name="Value">The anchor value for this column from the last/first row seen.</param>
/// <param name="IsDescending">
/// If <see langword="true"/>, this column is sorted descending; otherwise ascending.
/// </param>
public record CursorKey(string ColumnName, object? Value, bool IsDescending = false);
