// Copyright © Erickson Lopez. MIT License.
using Dapper;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder.Abstractions;

namespace EricksonLopez.SqlBuilder.Dapper;

/// <summary>
/// Provides extension methods on <see cref="SqlResult"/> for Dapper integration.
/// </summary>
internal static class SqlResultExtensions
{
    /// <summary>
    /// Converts the <see cref="SqlResult"/> parameters to a Dapper <see cref="DynamicParameters"/> instance.
    /// </summary>
    public static DynamicParameters ToDynamicParameters(this SqlResult result)
    {
        var dynamicParams = new DynamicParameters();
        foreach (var param in result.Parameters)
        {
            dynamicParams.Add(param.Key, param.Value);
        }
        return dynamicParams;
    }
}

