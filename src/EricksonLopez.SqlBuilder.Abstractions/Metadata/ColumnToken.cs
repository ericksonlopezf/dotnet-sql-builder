// Copyright © Erickson Lopez. MIT License.
namespace EricksonLopez.SqlBuilder.Abstractions.Metadata;

/// <summary>
/// Represents a lightweight token identifying a column index for metadata operations.
/// </summary>
/// <param name="Index">The zero-based index of the column.</param>
public readonly record struct ColumnToken(int Index);

