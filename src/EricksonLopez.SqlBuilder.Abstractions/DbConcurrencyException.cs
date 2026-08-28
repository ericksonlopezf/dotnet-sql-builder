// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions;

/// <summary>
/// Represents the exception thrown when an optimistic concurrency conflict is detected during an UPDATE operation.
/// </summary>
/// <remarks>
/// <para>
/// This exception is raised when an UPDATE query with a concurrency token returns 0 rows affected,
/// indicating that the target record was modified or deleted concurrently.
/// </para>
/// </remarks>
public sealed class DbConcurrencyException : Exception
{
    /// <summary>
    /// Gets the number of rows affected by the update operation (expected to be 0).
    /// </summary>
    public int RowsAffected { get; }

    /// <summary>
    /// Gets the name of the entity type for which the concurrency conflict occurred.
    /// </summary>
    public string EntityTypeName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConcurrencyException"/> class with entity type information.
    /// </summary>
    /// <param name="entityTypeName">The name of the entity type that caused the conflict.</param>
    /// <param name="rowsAffected">The number of rows affected by the operation.</param>
    public DbConcurrencyException(string entityTypeName, int rowsAffected = 0)
        : base("Optimistic concurrency conflict detected for entity '" + entityTypeName + "'. " +
               "The record was modified or deleted by another process. " +
               "RowsAffected=" + rowsAffected + ". Reload the entity and retry the operation.")
    {
        RowsAffected = rowsAffected;
        EntityTypeName = entityTypeName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbConcurrencyException"/> class with a specified inner exception.
    /// </summary>
    /// <param name="entityTypeName">The name of the entity type that caused the conflict.</param>
    /// <param name="rowsAffected">The number of rows affected by the operation.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DbConcurrencyException(string entityTypeName, int rowsAffected, Exception innerException)
        : base("Optimistic concurrency conflict detected for entity '" + entityTypeName + "'. RowsAffected=" + rowsAffected + ".", innerException)
    {
        RowsAffected = rowsAffected;
        EntityTypeName = entityTypeName;
    }
}


