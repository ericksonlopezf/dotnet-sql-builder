// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Metadata;

/// <summary>
/// Defines a contract for entity types that expose AOT-compatible static metadata.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IEntityMetadataProvider<T>
{
    /// <summary>
    /// Gets the static, AOT-optimized metadata instance for the entity type.
    /// </summary>
    static abstract IEntityMetadata<T> Metadata { get; }
}
