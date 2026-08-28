// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Text.Json;
using Dapper;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Dapper type handler for PostgreSQL JSONB columns.
/// Serializes/deserializes .NET objects to/from JSONB using System.Text.Json.
/// </summary>
public sealed class JsonbTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes the value to a JSON string and assigns it to the parameter, also setting the JSONB database type.
    /// </summary>
    /// <param name="parameter">The database parameter to configure.</param>
    /// <param name="value">The value to serialize as JSONB. When <see langword="null"/>, sets <see cref="DBNull.Value"/>.</param>
    public override void SetValue(IDbDataParameter parameter, T? value)
    {
        parameter.Value = value is null
            ? DBNull.Value
            : JsonSerializer.Serialize(value, _options);

        NpgsqlParameterHelper.SetJsonb(parameter);
    }

    /// <summary>
    /// Deserializes a JSONB string value from the database back to an instance of <typeparamref name="T"/>.
    /// </summary>
    /// <param name="value">The raw database value containing the JSON string.</param>
    /// <returns>
    /// The deserialized instance of <typeparamref name="T"/>, or the default value
    /// when <paramref name="value"/> is <see langword="null"/> or <see cref="DBNull"/>.
    /// </returns>
    public override T? Parse(object value)
    {
        if (value is DBNull or null)
        {
            return default;
        }

        var json = value.ToString()!;
        return JsonSerializer.Deserialize<T>(json, _options);
    }
}
