// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Builders;

namespace EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

/// <summary>
/// Defines a contract for constructing a bulk SQL operation and delegating its compilation to a dialect-specific renderer.
/// </summary>
/// <typeparam name="T">The type of the entities involved in the bulk operation.</typeparam>
public interface IBulkOperation<T>
{
    /// <summary>
    /// Builds and compiles the bulk SQL operation using the provided renderer.
    /// </summary>
    /// <param name="dialect">The dialect-specific SQL renderer.</param>
    /// <returns>A <see cref="BulkSqlResult"/> containing the generated execution batches.</returns>
    BulkSqlResult Build(ISqlRenderer dialect);
}
