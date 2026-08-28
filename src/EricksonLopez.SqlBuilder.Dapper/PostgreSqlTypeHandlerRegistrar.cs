// Copyright © Erickson Lopez. MIT License.
using System;
using Dapper;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Convenience methods for registering PostgreSQL specific type handlers.
/// </summary>
public static class PostgreSqlTypeHandlerRegistrar
{
    /// <summary>
    /// Registers a JSONB type handler for the specified type.
    /// Call once per type at application startup.
    /// </summary>
    /// <typeparam name="T">The CLR type to associate with the JSONB handler.</typeparam>
    public static void RegisterJsonbHandler<T>()
        => SqlMapper.AddTypeHandler(new JsonbTypeHandler<T>());

    /// <summary>
    /// Executes a sequence of handler registration delegates, each registering a JSONB type handler.
    /// </summary>
    /// <param name="registrations">One or more registration delegates, typically produced by calls to <see cref="RegisterJsonbHandler{T}"/>.</param>
    public static void RegisterJsonbHandlers(params Action[] registrations)
    {
        foreach (var registration in registrations)
        {
            registration();
        }
    }
}
