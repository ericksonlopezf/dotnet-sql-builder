// Copyright © Erickson Lopez. MIT License.
using System.Collections.Generic;

namespace EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

/// <summary>
/// Exposes the underlying entities associated with a bulk operation.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IEntitySource<out T>
{
    /// <summary>
    /// Gets the entities involved in the bulk operation.
    /// </summary>
    IEnumerable<T> Entities { get; }
}
