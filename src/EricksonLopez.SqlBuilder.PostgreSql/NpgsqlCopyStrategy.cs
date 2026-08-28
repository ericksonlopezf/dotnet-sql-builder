// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions.Metadata;
using EricksonLopez.SqlBuilder.Builders.Bulk;
using Npgsql;
using NpgsqlTypes;

namespace EricksonLopez.SqlBuilder.PostgreSql;

/// <summary>
/// Provides a high-performance native bulk INSERT strategy for PostgreSQL
/// using the <c>COPY</c> binary protocol via <see cref="NpgsqlBinaryImporter"/>.
/// </summary>
/// <remarks>
/// <para>
/// PostgreSQL's COPY protocol is the fastest way to insert large volumes of data.
/// This strategy uses the binary format, which is even faster than text-format COPY.
/// </para>
/// <para>
/// Requires <see cref="NpgsqlConnection"/> from the <c>Npgsql</c> package.
/// </para>
/// </remarks>
public static class NpgsqlCopyStrategy
{
    /// <summary>
    /// Bulk-inserts a collection of entities into the PostgreSQL table associated with <typeparamref name="T"/>
    /// using the binary COPY protocol.
    /// </summary>
    /// <typeparam name="T">
    /// The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.
    /// </typeparam>
    /// <param name="connection">An open <see cref="NpgsqlConnection"/>.</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="options">Optional bulk operation options.</param>
    /// <param name="transaction">An optional open <see cref="NpgsqlTransaction"/> to enlist.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="BulkInsertResult{T}"/> with the total number of rows inserted.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live PostgreSQL; covered by integration tests.")]
    public static async Task<BulkInsertResult<T>> BulkInsertAsync<T>(
        NpgsqlConnection connection,
        IEnumerable<T> entities,
        BulkOptions? options = null,
        NpgsqlTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : IStaticEntityMetadata<T>
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(entities);

        options ??= BulkOptions.Default;

        var list = entities as IList<T> ?? new List<T>(entities);
        if (list.Count == 0)
        {
            return BulkInsertResult<T>.WithoutIdentities(0);
        }

        var columnsArr = T.GetColumns().ToArray(); // materialize before first await
        var columnNames = BuildActiveColumnNames(columnsArr, options);

        // Build COPY command: COPY "table_name" ("col1", "col2", ...) FROM STDIN (FORMAT BINARY)
        var copyCommand = BuildCopyCommand(T.TableName, columnNames);

        using var writer = await connection.BeginBinaryImportAsync(copyCommand, cancellationToken)
            .ConfigureAwait(false);

        foreach (var entity in list)
        {
            await writer.StartRowAsync(cancellationToken).ConfigureAwait(false);

            for (int i = 0; i < columnsArr.Length; i++)
            {
                var col = columnsArr[i];
                // Skip identity columns unless ReturnIdentities is requested
                if (!options.ReturnIdentities && col.HasFlag(ColumnFlags.Identity))
                {
                    continue;
                }

                if (T.IsNull(entity, col.Index))
                {
                    await writer.WriteNullAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Extract value via parameter binding
                    var pm = new EricksonLopez.SqlBuilder.ParameterManager();
                    T.BindParameter(entity, col.Index, pm);
                    var paramDict = pm.GetParameters();
                    object? value = paramDict.Values.FirstOrDefault();
                    await writer.WriteAsync(value, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        await writer.CompleteAsync(cancellationToken).ConfigureAwait(false);

        return BulkInsertResult<T>.WithoutIdentities(list.Count);
    }

    /// <summary>
    /// Bulk-inserts a collection of entities using a generic database connection.
    /// </summary>
    /// <typeparam name="T">The entity type. Must implement <see cref="IStaticEntityMetadata{T}"/>.</typeparam>
    /// <param name="connection">An open database connection (must be an <see cref="NpgsqlConnection"/>).</param>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="options">Optional bulk operation options.</param>
    /// <param name="transaction">An optional database transaction.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="BulkInsertResult{T}"/> with the total number of rows inserted.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="connection"/> is not an instance of <see cref="NpgsqlConnection"/></exception>
    [ExcludeFromCodeCoverage(Justification = "Requires live PostgreSQL; covered by integration tests.")]
    public static Task<BulkInsertResult<T>> BulkInsertAsync<T>(
        IDbConnection connection,
        IEnumerable<T> entities,
        BulkOptions? options = null,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
        where T : IStaticEntityMetadata<T>
    {
        if (connection is not NpgsqlConnection npgConn)
        {
            throw new InvalidOperationException(
                $"{nameof(NpgsqlCopyStrategy)}.{nameof(BulkInsertAsync)} requires an {nameof(NpgsqlConnection)}.");
        }

        return BulkInsertAsync<T>(npgConn, entities, options, transaction as NpgsqlTransaction, cancellationToken);
    }

    // ─── Internal helpers ──────────────────────────────────────────────────────
    internal static List<string> BuildActiveColumnNames(
        ReadOnlySpan<ColumnMetadata> columns,
        BulkOptions options)
    {
        var names = new List<string>(columns.Length);
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            if (!options.ReturnIdentities && col.HasFlag(ColumnFlags.Identity))
            {
                continue;
            }
            names.Add(col.Name);
        }
        return names;
    }

    internal static string BuildCopyCommand(string tableName, List<string> columnNames)
    {
        // COPY "schema"."table" ("col1", "col2") FROM STDIN (FORMAT BINARY)
        var cols = string.Join(", ", columnNames.ConvertAll(c => $"\"{c}\""));
        return $"COPY \"{tableName}\" ({cols}) FROM STDIN (FORMAT BINARY)";
    }
}




