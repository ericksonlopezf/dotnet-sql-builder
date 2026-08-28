// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using Dapper;

namespace EricksonLopez.SqlBuilder.Testing.Infrastructure;

/// <summary>
/// Dapper type handler that maps <see cref="bool"/> to Oracle NUMBER(1) integers.
/// </summary>
public sealed class OracleBooleanHandler : SqlMapper.TypeHandler<bool>
{
    /// <inheritdoc/>
    public override void SetValue(IDbDataParameter parameter, bool value)
    {
        if (parameter != null)
        {
            parameter.Value = value ? 1 : 0;
            parameter.DbType = DbType.Int32;
        }
    }

    /// <inheritdoc/>
    public override bool Parse(object value)
    {
        if (value is null || value is DBNull)
        {
            return false;
        }

        return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture) == 1;
    }
}
