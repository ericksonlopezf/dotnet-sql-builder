// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Pagination.Abstractions;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.SqlBuilder.Samples.Level07_Scalability;

[SqlEntity("metrics")]
public partial class Metric
{
    [DatabaseGenerated] public int Id { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime RecordedAt { get; set; }
}

public static class ScalabilitySample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== LEVEL 7: SCALABILITY, PAGINATION AND PERFORMANCE ===");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        await connection.ExecuteAsync(@"
            CREATE TABLE metrics (
                id INTEGER PRIMARY KEY AUTOINCREMENT, 
                node_id TEXT NOT NULL, 
                value DECIMAL NOT NULL, 
                recorded_at DATETIME NOT NULL
            );
        ");

        // Seed data for pagination demos
        var compiler = new SqliteCompiler();
        var now = DateTime.UtcNow;
        for (int i = 0; i < 50; i++)
        {
            var metric = new Metric
            {
                NodeId = $"Node-{(i % 3) + 1}",
                Value = 10m + i,
                RecordedAt = now.AddMinutes(-i)
            };
            var ins = Sql.Insert(metric).Build(compiler);
            await connection.ExecuteAsync(ins.Sql, ins.Parameters);
        }

        // ────────────────────────────────────────────────────────────────────
        // 1. Immutability = Thread Safety
        //    SelectQuery<T> is an immutable record — safe to share across threads
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 1. Thread Safety — Immutable queries shared across threads");

        var baseQuery = Sql.From<Metric>().Where(m => m.Value > 20);

        var task1 = Task.Run(() =>
        {
            // Each thread extends the shared base — creates a NEW instance, never mutates baseQuery
            var node1Query = baseQuery.And(m => m.NodeId == "Node-1").Build(compiler);
            return node1Query.Sql;
        });

        var task2 = Task.Run(() =>
        {
            var node2Query = baseQuery.And(m => m.NodeId == "Node-2").Build(compiler);
            return node2Query.Sql;
        });

        var sqls = await Task.WhenAll(task1, task2);
        Console.WriteLine($"    Thread 1 SQL: {sqls[0]}");
        Console.WriteLine($"    Thread 2 SQL: {sqls[1]}");

        // ────────────────────────────────────────────────────────────────────
        // 2. Offset Pagination — Classic Limit/Offset
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 2. Offset Pagination — Limit / Offset");

        var page1 = await connection.QueryAsync<Metric>(
            Sql.From<Metric>().OrderBy(m => m.Id).Limit(10).Offset(0));
        Console.WriteLine($"    Page 1 (LIMIT 10 OFFSET 0): {page1.Count()} records");

        // ────────────────────────────────────────────────────────────────────
        // 3. PaginationParameters.Page() — Pagination with PaginationParameters object
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 3. PaginationParameters.Page() — Typed pagination");

        var paginationParams = PaginationParameters.Create(page: 2, pageSize: 10);
        var typedPage = await connection.QueryPagedAsync(
            Sql.From<Metric>().OrderBy(m => m.Id),
            paginationParams);

        // IPagedList<T> extends IReadOnlyList<T>, so Count gives item count
        Console.WriteLine($"    Page {typedPage.Page} — Items: {typedPage.Count}");
        Console.WriteLine($"    Total records: {typedPage.TotalCount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "N/A"}");

        // ────────────────────────────────────────────────────────────────────
        // 4. Cursor-Based Pagination (Seek Method) — Seek<T>
        //    Better performance than OFFSET for large datasets
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 4. Cursor Pagination with Seek<T> (keyset)");

        // First page: no cursor
        var firstPage = await connection.QueryAsync<Metric>(
            Sql.From<Metric>().OrderBy(m => m.Id).Limit(10));
        var lastMetric = firstPage.LastOrDefault();

        Console.WriteLine($"    First page: {firstPage.Count()} records. Last Id: {lastMetric?.Id}");

        if (lastMetric != null)
        {
            // Second page: seek forward after lastMetric.Id
            var secondPage = await connection.QueryAsync<Metric>(
                Sql.From<Metric>()
                    .Seek(m => (object)m.Id, lastMetric.Id, ascending: true, limit: 10));

            Console.WriteLine($"    Second page (seek): {secondPage.Count()} records.");
        }

        // ────────────────────────────────────────────────────────────────────
        // 5. Composite Cursor Pagination — SeekAfter / SeekBefore
        //    For multi-column keyset pagination
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 5. Composite Cursor Pagination — SeekAfter / SeekBefore");

        // SeekAfter with composite keys: (node_id ASC, id ASC) > ("Node-1", 5)
        // CursorKey(columnName, value, isDescending=false means ASC)
        var compositeSeek = Sql.From<Metric>()
            .OrderBy(m => m.NodeId)
            .ThenBy(m => m.Id)
            .SeekAfter(
                new CursorKey("node_id", "Node-1", IsDescending: false),
                new CursorKey("id", 5, IsDescending: false))
            .Limit(5);

        var compositeResult = compositeSeek.Build(compiler);
        Console.WriteLine($"    Composite cursor SQL:\n    {compositeResult.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 6. WindowPage — ROW_NUMBER() based pagination (deep-page optimization)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 6. WindowPage — ROW_NUMBER() pagination (better for deep pages)");

        var windowPageQuery = Sql.From<Metric>()
            .WindowPage(pageNumber: 3, pageSize: 10, orderByColumn: "id");

        var windowPageSql = windowPageQuery.Build(compiler);
        Console.WriteLine($"    WindowPage SQL:\n    {windowPageSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 7. OrderByDynamic — Dynamic sorting at runtime
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 7. OrderByDynamic — Dynamic sorting (SQL injection safe)");

        // OrderByDynamic safely resolves property name → snake_case column name
        string sortField = "NodeId"; // Could come from user input
        var dynamicSort = Sql.From<Metric>()
            .OrderByDynamic(sortField, descending: true)
            .Limit(5);

        var dynamicSortSql = dynamicSort.Build(compiler);
        Console.WriteLine($"    Dynamic sort SQL: {dynamicSortSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 8. QueryPagedRawAsync — Pagination with raw SQL (2 queries)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 8. QueryPagedRawAsync — Pagination with raw SQL");

        var rawParams = PaginationParameters.Create(page: 1, pageSize: 5);
        var rawPageResult = await connection.QueryPagedRawAsync<Metric>(
            sql: "SELECT * FROM metrics WHERE value > 15 ORDER BY id",
            countSql: "SELECT COUNT(*) FROM metrics WHERE value > 15",
            parameters: rawParams);

        Console.WriteLine($"    Raw paged result: {rawPageResult.Count} items, Total: {rawPageResult.TotalCount}");

        // ────────────────────────────────────────────────────────────────────
        // 9. AOT Optimization — Build once, execute many times
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 9. Optimization — Compile query once and reuse");

        // Build the SQL string once, execute multiple times with different params
        // This is the most efficient pattern for high-throughput scenarios
        var hotPath = Sql.From<Metric>()
            .Where(m => m.NodeId == "Node-1")
            .OrderBy(m => m.Id)
            .Limit(10)
            .Build(compiler);

        // Execute multiple times - zero re-compilation cost
        for (int run = 0; run < 3; run++)
        {
            var runResult = await connection.QueryAsync<Metric>(hotPath.Sql, hotPath.Parameters);
            Console.WriteLine($"    Run {run + 1}: {runResult.Count()} metrics for Node-1.");
        }
    }
}




