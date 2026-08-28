// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Result;

namespace EricksonLopez.SqlBuilder.Dapper;

using System.Data;
using global::Dapper;
using EricksonLopez.SqlBuilder.Builders.Bulk.Operations;

/// <summary>
/// Provides Dapper extension methods for executing bulk SQL operations.
/// </summary>
public static class SqlBuilderDapperBulkExtensions
{
    /// <summary>
    /// Executes a Bulk operation asynchronously by iterating over its generated batches.
    /// </summary>
    /// <param name="connection">The database connection used to execute each batch.</param>
    /// <param name="bulkResult">The <see cref="BulkSqlResult"/> containing the generated SQL batches and their parameters.</param>
    /// <param name="transaction">An optional transaction to execute within.</param>
    /// <param name="commandTimeout">An optional command timeout in seconds applied to each batch.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the total number of rows affected across all batches.</returns>
    public static async Task<int> ExecuteBulkAsync(
        this IDbConnection connection, 
        BulkSqlResult bulkResult,
        IDbTransaction? transaction = null, 
        int? commandTimeout = null)
    {
        int totalRowsAffected = 0;
        
        foreach (var batch in bulkResult.Batches)
        {
            var dynamicParams = new DynamicParameters();
            
            // Map parameters to Dapper DynamicParameters
            if (batch.Parameters != null)
            {
                foreach(var p in batch.Parameters)
                {
                    dynamicParams.Add(p.Key, p.Value);
                }
            }
            
            totalRowsAffected += await connection.ExecuteAsync(
                sql: batch.Sql, 
                param: dynamicParams, 
                transaction: transaction, 
                commandTimeout: commandTimeout).ConfigureAwait(false);
        }
        
        return totalRowsAffected;
    }
}





