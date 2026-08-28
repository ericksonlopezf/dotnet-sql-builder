// Copyright © Erickson Lopez. MIT License.
using System;

namespace EricksonLopez.SqlBuilder.Abstractions
{
    /// <summary>
    /// Defines a serializer that extracts entity property values into an array for high-performance bulk operations.
    /// </summary>
    /// <typeparam name="T">The entity type to serialize.</typeparam>
    public interface IBulkSerializer<T>
    {
        /// <summary>
        /// Serializes the property values of the specified entity into a target array.
        /// </summary>
        /// <param name="entity">The entity instance to serialize.</param>
        /// <param name="values">The pre-allocated target array to store the serialized values.</param>
        void Serialize(T entity, object?[] values);
    }
}


