// Copyright © Erickson Lopez. MIT License.
using System.Data;
using System.Reflection;

namespace EricksonLopez.SqlBuilder.Dapper;

internal static class NpgsqlParameterHelper
{
    private static PropertyInfo? _npgsqlDbTypeProperty;
    private static bool _initialized;

    public static void SetJsonb(IDbDataParameter parameter)
    {
        if (parameter.GetType().Name != "NpgsqlParameter") return;

        if (!_initialized)
        {
            _initialized = true;
            _npgsqlDbTypeProperty = parameter.GetType().GetProperty("NpgsqlDbType");
        }

        // NpgsqlDbType.Jsonb is 36
        _npgsqlDbTypeProperty?.SetValue(parameter, 36);
    }
}
