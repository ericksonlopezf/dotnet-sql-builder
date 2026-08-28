// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Result;

namespace EricksonLopez.SqlBuilder.Builders.Bulk;

/// <summary>
/// Represents the result of a native bulk INSERT operation.
/// </summary>
/// <typeparam name="T">The type of entity inserted.</typeparam>
public sealed class BulkInsertResult<T>
{
    /// <summary>
    /// Gets the total number of rows that were inserted.
    /// </summary>
    public int RowsAffected { get; }

    /// <summary>
    /// Gets the collection of inserted entities with their identity columns populated.
    /// </summary>
    /// <remarks>
    /// This collection is only populated when <see cref="BulkOptions.ReturnIdentities"/> was
    /// set to <see langword="true"/> on the originating operation. Otherwise, returns an empty list.
    /// </remarks>
    public IReadOnlyList<T> InsertedEntities { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="BulkInsertResult{T}"/>.
    /// </summary>
    /// <param name="rowsAffected">The number of rows inserted.</param>
    /// <param name="insertedEntities">The entities with their generated identities. Pass an empty list if identities were not requested.</param>
    public BulkInsertResult(int rowsAffected, IReadOnlyList<T> insertedEntities)
    {
        RowsAffected = rowsAffected;
        InsertedEntities = insertedEntities;
    }

    /// <summary>
    /// Creates a <see cref="BulkInsertResult{T}"/> for operations that do not return identity values.
    /// </summary>
    /// <param name="rowsAffected">The number of rows inserted.</param>
    /// <returns>A new <see cref="BulkInsertResult{T}"/> instance with an empty entity list.</returns>
    public static BulkInsertResult<T> WithoutIdentities(int rowsAffected)
        => new BulkInsertResult<T>(rowsAffected, System.Array.Empty<T>());
}



