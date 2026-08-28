// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.SqlBuilder.Samples.Level06_ErrorHandling;

[SqlEntity("jobs")]
public partial class Job
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

[SqlEntity("versioned_items")]
public partial class VersionedItem
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
}

public static class ErrorHandlingSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== NIVEL 6: MANEJO DE ERRORES, RESILIENCIA Y CONCURRENCIA ===");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        await connection.ExecuteAsync(@"
            CREATE TABLE jobs (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, status TEXT NOT NULL);
            CREATE TABLE versioned_items (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL, version INTEGER NOT NULL DEFAULT 0);
            INSERT INTO versioned_items (name, version) VALUES ('Item A', 1), ('Item B', 2);
        ");

        // ────────────────────────────────────────────────────────────────────
        // 1. Retry + Exponential Backoff (errores transitorios vs permanentes)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 1. Execution with Retry and Exponential Backoff");

        var query = Sql.Insert(new Job { Name = "Data Processing", Status = "Pending" });
        var sqlResult = query.Build(new SqliteCompiler());

        int maxRetries = 3;
        int delayMs = 100;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await connection.ExecuteAsync(sqlResult.Sql, sqlResult.Parameters);
                Console.WriteLine($"    [Attempt {attempt}] Job insertado correctamente.");
                break;
            }
            catch (SqliteException ex) when (IsTransient(ex))
            {
                Console.WriteLine($"    [Attempt {attempt}] Transient error (code {ex.SqliteErrorCode}). Retrying in {delayMs}ms...");
                if (attempt == maxRetries)
                {
                    Console.WriteLine("    Retries exhausted. Moving to Dead Letter.");
                    throw;
                }
                await Task.Delay(delayMs);
                delayMs *= 2; // Exponential Backoff
            }
            catch (Exception ex)
            {
                // Permanent errors (e.g., constraint violation, syntax error)
                Console.WriteLine($"    [Attempt {attempt}] Error permanente: {ex.Message}. Cancelando.");
                break;
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 2. WithConcurrencyToken — Optimistic Concurrency (integer version)
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 2. WithConcurrencyToken — Optimistic Concurrency (int auto-increment)");

        // Update Item A (version = 1). If someone else already changed it, rows affected = 0.
        // WithConcurrencyToken<int>: adds AND version = @expected in WHERE and SET version = version + 1
        var concurrencyUpdate = Sql.Update<VersionedItem>()
            .Set(v => v.Name, "Item A (Updated)")
            .Where(v => v.Id == 1)
            .WithConcurrencyToken(v => v.Version, expectedValue: 1);

        var concurrencySql = concurrencyUpdate.Build(new SqliteCompiler());
        Console.WriteLine($"    Concurrency SQL:\n    {concurrencySql.Sql}");

        // Execute and check rows affected
        var rowsAffected = await connection.ExecuteAsync(concurrencyUpdate);
        Console.WriteLine($"    Rows affected: {rowsAffected} (1 = success, 0 = concurrency conflict)");

        // ────────────────────────────────────────────────────────────────────
        // 3. ExecuteWithConcurrencyCheckAsync — Throws DbConcurrencyException
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 3. ExecuteWithConcurrencyCheckAsync — Throws DbConcurrencyException");

        // Now try with a stale version (version = 1, but we already updated to version = 2)
        var staleUpdate = Sql.Update<VersionedItem>()
            .Set(v => v.Name, "Item A (Stale)")
            .Where(v => v.Id == 1)
            .WithConcurrencyToken(v => v.Version, expectedValue: 1); // stale!

        try
        {
            await connection.ExecuteWithConcurrencyCheckAsync<VersionedItem>(staleUpdate);
            Console.WriteLine("    UPDATE executed (no conflict).");
        }
        catch (DbConcurrencyException ex)
        {
            Console.WriteLine($"    [!] DbConcurrencyException capturada: {ex.Message}");
            Console.WriteLine($"        EntityType: {ex.EntityTypeName}, RowsAffected: {ex.RowsAffected}");
        }

        // ────────────────────────────────────────────────────────────────────
        // 4. WithConcurrencyToken (Guid) — explicit new token
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 4. WithConcurrencyToken — With explicit token (Guid pattern demo)");

        // For Guid tokens, you must supply both old and new value
        // Here we demonstrate the API shape using int (same pattern works with Guid)
        var newVersion = 99;
        var explicitTokenUpdate = Sql.Update<VersionedItem>()
            .Set(v => v.Name, "Item B (Explicit Token)")
            .Where(v => v.Id == 2)
            .WithConcurrencyToken(v => v.Version, expectedValue: 2, newValue: newVersion);

        var explicitSql = explicitTokenUpdate.Build(new SqliteCompiler());
        Console.WriteLine($"    Explicit token SQL:\n    {explicitSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 5. RETURNING clause — recuperar valores generados
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 5. RETURNING clause en INSERT y UPDATE");

        // INSERT ... RETURNING id — to retrieve generated IDs
        var insertWithReturning = Sql.Insert(new Job { Name = "Return Test", Status = "New" })
            .Returning(j => j.Id);
        var returningResult = insertWithReturning.Build(new SqliteCompiler());
        Console.WriteLine($"    INSERT RETURNING SQL: {returningResult.Sql}");

        // UPDATE ... RETURNING — to confirm changes
        var updateWithReturning = Sql.Update<Job>()
            .Set(j => j.Status, "Done")
            .Where(j => j.Id == 1)
            .Returning("id", "status");
        var updateReturningSql = updateWithReturning.Build(new SqliteCompiler());
        Console.WriteLine($"    UPDATE RETURNING SQL: {updateReturningSql.Sql}");

        // ────────────────────────────────────────────────────────────────────
        // 6. Diagnostics — SqlBuilderDiagnostics
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 6. SqlBuilderDiagnostics — Observabilidad de errores");

        // Access diagnostic counters
        Console.WriteLine($"    Queries ejecutados (contador acumulado): {SqlBuilderDiagnostics.QueryExecutionCounter}");
        Console.WriteLine($"    Slow query threshold: {SqlBuilderDiagnostics.SlowQueryThresholdMs}ms");
        Console.WriteLine($"    Log Parameters: {SqlBuilderDiagnostics.LogParameters}");
    }

    private static bool IsTransient(SqliteException ex)
    {
        // In SQLite: SQLITE_BUSY (5) and SQLITE_LOCKED (6) are transient errors
        return ex.SqliteErrorCode == 5 || ex.SqliteErrorCode == 6;
    }
}




