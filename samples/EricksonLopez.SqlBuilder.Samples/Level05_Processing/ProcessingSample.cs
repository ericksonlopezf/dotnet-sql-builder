// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.SqlServer;
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
        Console.WriteLine("\n=== LEVEL 5: PROCESSING, BATCHING, AND STREAMING ===");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        await connection.ExecuteAsync(@"
            CREATE TABLE logs (id INTEGER PRIMARY KEY AUTOINCREMENT, message TEXT NOT NULL, created_at DATETIME NOT NULL);
            CREATE TABLE events (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, priority INTEGER NOT NULL, occurred_at DATETIME NOT NULL);
        ");

        // ────────────────────────────────────────────────────────────────────
        // 1. BulkInsertAsync — Fallback bulk INSERT via DapperExtensions
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
        // 2. Sql.BulkInsert<T> — Direct bulk INSERT via InsertQuery
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 2. Sql.BulkInsert<T> — Bulk INSERT via InsertQuery");

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
        Console.WriteLine("\n[+] 7. QueryStreamAsync — Streaming without in-memory buffering");

        var streamQuery = Sql.From<LogEntry>()
            .OrderBy(l => l.Id)
            .Limit(10);

        // ToStreamAsync is an extension method on SelectQuery<T>, called as: query.ToStreamAsync(connection)
        int streamCount = 0;
        await foreach (var logEntry in streamQuery.ToStreamAsync(connection))
        {
            streamCount++;
        }
        Console.WriteLine($"    Streaming completed: {streamCount} entries processed without buffering.");

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

        // ────────────────────────────────────────────────────────────────────
        // 9. SeekAfter / SeekBefore — Composite keyset (cursor) pagination
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 9. SeekAfter / SeekBefore — Composite keyset cursor pagination");

        // CursorKey(column, value) defines an anchor key from the last retrieved row.
        // SeekAfter generates: WHERE (col1 > @p0 OR (col1 = @p0 AND col2 > @p1))
        var seekAfterSql = Sql.From<EventRecord>()
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.Id)
            .SeekAfter(
                new CursorKey("priority", 2),  // last seen priority
                new CursorKey("id", 50))        // last seen id
            .Limit(10)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    SeekAfter SQL:\n    {seekAfterSql.Sql}");

        // SeekBefore — backward cursor: WHERE (col1 < @p0 OR (col1 = @p0 AND col2 < @p1))
        var seekBeforeSql = Sql.From<EventRecord>()
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.Id)
            .SeekBefore(
                new CursorKey("priority", 2),
                new CursorKey("id", 50))
            .Limit(10)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    SeekBefore SQL:\n    {seekBeforeSql.Sql}");

        // Single-column cursor (most common case)
        var singleKeyCursorSql = Sql.From<LogEntry>()
            .OrderBy(l => l.Id)
            .SeekAfter(new CursorKey("id", 100))
            .Limit(20)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    Single-key SeekAfter SQL: {singleKeyCursorSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 10. WindowPage — ROW_NUMBER-based pagination (deep-page performance)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 10. WindowPage — ROW_NUMBER window-based pagination");

        // WindowPage wraps the query in a ROW_NUMBER() OVER (ORDER BY col) CTE
        // and filters to the requested page — avoids OFFSET performance issues on large tables.
        var windowPageSql = Sql.From<LogEntry>()
            .WindowPage(pageNumber: 3, pageSize: 10, orderByColumn: "id", descending: false)
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    WindowPage SQL (SQL Server):\n    {windowPageSql.Sql}");

        // Descending order window page
        var windowPageDescSql = Sql.From<EventRecord>()
            .WindowPage(pageNumber: 2, pageSize: 5, orderByColumn: "occurred_at", descending: true)
            .Build(new EricksonLopez.SqlBuilder.SqlServer.SqlServerCompiler());
        Console.WriteLine($"    WindowPage DESC SQL (first 120):\n    {windowPageDescSql.Sql.Substring(0, Math.Min(120, windowPageDescSql.Sql.Length))}...");

        // ────────────────────────────────────────────────────────────────────
        // 11. CursorPaginationExtensions.Seek<T> — Single-key cursor pagination
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 11. Seek<T>() — Single-key cursor pagination via CursorPaginationExtensions");

        // Seek generates: WHERE id > @lastValue ORDER BY id LIMIT @limit
        var seekAscSql = Sql.From<LogEntry>()
            .Seek(l => l.Id, lastValue: 50, ascending: true, limit: 20)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    Seek (ASC) SQL: {seekAscSql.Sql}");

        // Descending seek (WHERE id < @lastValue ORDER BY id DESC LIMIT @limit)
        var seekDescSql = Sql.From<LogEntry>()
            .Seek(l => l.Id, lastValue: 100, ascending: false, limit: 10)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    Seek (DESC) SQL: {seekDescSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 12. OrderByDynamic — Dynamic sort by user-supplied column name string
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 12. OrderByDynamic — String-based dynamic sorting");

        // OrderByDynamic safely resolves property names to DB column names
        // using the entity metadata cache — prevents SQL injection
        var dynAscSql = Sql.From<EventRecord>()
            .OrderByDynamic("Priority", descending: false)
            .Limit(5)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrderByDynamic (ASC) SQL: {dynAscSql.Sql}");

        var dynDescSql = Sql.From<EventRecord>()
            .OrderByDynamic("OccurredAt", descending: true)
            .Limit(5)
            .Build(new SqliteCompiler());
        Console.WriteLine($"    OrderByDynamic (DESC) SQL: {dynDescSql.Sql}");
    }
}
