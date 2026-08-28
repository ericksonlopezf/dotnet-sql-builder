// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.SqlBuilder.Samples.Level05_Processing;

[SqlEntity("logs")]
public partial class LogEntry
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

[SqlEntity("events")]
public partial class EventRecord
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime OccurredAt { get; set; }
}

public static class ProcessingSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== NIVEL 5: PROCESAMIENTO, BATCHING Y STREAMING ===");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        await connection.ExecuteAsync(@"
            CREATE TABLE logs (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL, created_at DATETIME NOT NULL);
            CREATE TABLE events (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, priority INTEGER NOT NULL, occurred_at DATETIME NOT NULL);
        ");

        // ────────────────────────────────────────────────────────────────────
        // 1. BulkInsertAsync — Fallback INSERT masivo via DapperExtensions
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 1. BulkInsertAsync — Bulk entity insertion");

        var logsToInsert = new List<LogEntry>();
        for (int i = 0; i < 500; i++)
        {
            logsToInsert.Add(new LogEntry { Message = $"Log #{i}", CreatedAt = DateTime.UtcNow });
        }

        // BulkInsertAsync uses IBulkStrategy if registered, falls back to batched INSERT
        await connection.BulkInsertAsync(logsToInsert);
        Console.WriteLine($"    Inserted {logsToInsert.Count} records in bulk.");

        // ────────────────────────────────────────────────────────────────────
        // 2. Sql.BulkInsert<T> — INSERT masivo directo via InsertQuery
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 2. Sql.BulkInsert<T> — INSERT masivo via InsertQuery");

        var eventBatch = new List<EventRecord>();
        for (int i = 0; i < 50; i++)
        {
            eventBatch.Add(new EventRecord 
            { 
                Name = $"Event-{i}", 
                Priority = i % 3, 
                OccurredAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        // Sql.BulkInsert<T> creates an InsertQuery<T> with all values in a multi-row INSERT
        var bulkInsertQuery = Sql.BulkInsert(eventBatch);
        var bulkResult = bulkInsertQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    Generated SQL for bulk insert (first 120 chars):\n    {bulkResult.Sql.Substring(0, Math.Min(120, bulkResult.Sql.Length))}...");

        await connection.ExecuteAsync(bulkInsertQuery);
        Console.WriteLine($"    {eventBatch.Count} eventos insertados.");

        // ────────────────────────────────────────────────────────────────────
        // 3. QueryAotAsync — Zero reflection, manual mapper
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 3. QueryAotAsync — Zero-reflection read for NativeAOT");

        var aotQuery = Sql.From<LogEntry>().Limit(5).OrderBy(l => l.Id);

        // QueryAotAsync allows manual column mapping, bypassing Dapper reflection
        var aotResults = await connection.QueryAotAsync<LogEntry>(
            aotQuery,
            reader => new LogEntry
            {
                Id = reader.GetInt32(0),
                Message = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2)
            });

        Console.WriteLine($"    AOT Query: {aotResults.Count()} records retrieved without reflection.");

        // ────────────────────────────────────────────────────────────────────
        // 4. QueryFirstOrDefaultAotAsync — AOT with first result
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 4. QueryFirstOrDefaultAotAsync — First AOT result");

        var firstQuery = Sql.From<LogEntry>()
            .Where(l => l.Id == 1)
            .Limit(1);

        var firstLog = await connection.QueryFirstOrDefaultAotAsync<LogEntry>(
            firstQuery,
            reader => new LogEntry
            {
                Id = reader.GetInt32(0),
                Message = reader.GetString(1),
                CreatedAt = reader.GetDateTime(2)
            });

        Console.WriteLine($"    First AOT record: Id={firstLog?.Id}, Message={firstLog?.Message}");

        // ────────────────────────────────────────────────────────────────────
        // 5. QuerySequentialAsync — SequentialAccess for large columns
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 5. QuerySequentialAsync — For LOB columns (text/blob)");

        var seqQuery = Sql.From<LogEntry>().Limit(3);

        var seqResults = await connection.QuerySequentialAsync<string>(
            seqQuery,
            reader => reader.GetString(1)); // Read only the 'message' column

        Console.WriteLine($"    Sequential: {seqResults.Count()} messages read efficiently.");

        // ────────────────────────────────────────────────────────────────────
        // 6. CancellationToken — Long-running operation cancellation
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 6. CancellationToken — Operation cancellation");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            var cancelableQuery = Sql.From<LogEntry>().Limit(100);
            var results = await connection.QueryAsync<LogEntry>(
                cancelableQuery,
                cancellationToken: cts.Token);
            Console.WriteLine($"    Cancelable query completed: {results.Count()} records.");
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("    [!] The query was canceled by the CancellationToken.");
        }

        // ────────────────────────────────────────────────────────────────────
        // 7. QueryStreamAsync — Streaming IAsyncEnumerable<T>
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 7. QueryStreamAsync — Streaming sin buffering en memoria");

        var streamQuery = Sql.From<LogEntry>()
            .OrderBy(l => l.Id)
            .Limit(10);

        // ToStreamAsync is an extension method on SelectQuery<T>, called as: query.ToStreamAsync(connection)
        int streamCount = 0;
        await foreach (var logEntry in streamQuery.ToStreamAsync(connection))
        {
            streamCount++;
        }
        Console.WriteLine($"    Streaming completado: {streamCount} entradas procesadas sin buffer.");

        // ────────────────────────────────────────────────────────────────────
        // 8. BulkDeleteAsync — Bulk delete with base query
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 8. BulkDeleteAsync — Bulk deletion");

        var deleteQuery = Sql.Delete<LogEntry>().Where(l => l.Id <= 10);
        // BulkDeleteAsync executes the DELETE in a bulk batch
        var deleteResult = deleteQuery.Build(new SqliteCompiler());
        Console.WriteLine($"    Generated DELETE SQL: {deleteResult.Sql}");

        // Execute via standard ExecuteAsync
        var deletedCount = await connection.ExecuteAsync(deleteQuery);
        Console.WriteLine($"    Deleted records: {deletedCount}");
    }
}



