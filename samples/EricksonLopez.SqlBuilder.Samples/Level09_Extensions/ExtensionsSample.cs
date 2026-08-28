// Copyright © Erickson Lopez. MIT License.
using EricksonLopez.SqlBuilder.Abstractions.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.Result;
using EricksonLopez.SqlBuilder;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Annotations;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.SqlBuilder.Samples.Level09_Extensions;

[SqlEntity("reports")]
public partial class Report
{
    [DatabaseGenerated] public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Year { get; set; }
    public decimal Revenue { get; set; }
}

[SqlEntity("summary")]
public partial class ReportSummary
{
    public string Title { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public static class ExtensionsSample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== NIVEL 9: EXTENSIONES Y UTILITARIOS AVANZADOS ===");

        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());

        await connection.ExecuteAsync(@"
            CREATE TABLE reports (id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT NOT NULL, year INTEGER NOT NULL, revenue DECIMAL NOT NULL);
            INSERT INTO reports (title, year, revenue) VALUES
                ('Q1 Report', 2024, 150000),
                ('Q2 Report', 2024, 180000),
                ('Q3 Report', 2024, 210000),
                ('Q4 Report', 2024, 195000);
        ");

        var compiler = new SqliteCompiler();

        // ────────────────────────────────────────────────────────────────────
        // 1. SqlResult — Access compiled SQL and parameters
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 1. SqlResult — Inspect compiled SQL and its parameters");

        var query = Sql.From<Report>()
            .Where(r => r.Year == 2024)
            .And(r => r.Revenue > 160000m)
            .OrderByDescending(r => r.Revenue);

        // Build() materializes the AST into SQL + parameters dictionary
        SqlResult result = query.Build(compiler);

        Console.WriteLine($"    SQL: {result.Sql}");
        Console.WriteLine($"    Parameters:");
        foreach (var param in result.Parameters)
        {
            Console.WriteLine($"      {param.Key} = {param.Value}");
        }

        // ────────────────────────────────────────────────────────────────────
        // 2. GetFingerprint() — Identificar estructura de query sin valores
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 2. GetFingerprint() — Hash identifier for query structure");

        var q1 = Sql.From<Report>().Where(r => r.Year == 2024).Limit(10);
        var q2 = Sql.From<Report>().Where(r => r.Year == 2025).Limit(10);  // Same structure, different value
        var q3 = Sql.From<Report>().Where(r => r.Year == 2024).Limit(20);  // Same structure, different limit

        string fp1 = q1.GetFingerprint();
        string fp2 = q2.GetFingerprint(); 
        string fp3 = q3.GetFingerprint();

        Console.WriteLine($"    Fingerprint q1 (Year=2024, Limit=10): {fp1[..16]}...");
        Console.WriteLine($"    Fingerprint q2 (Year=2025, Limit=10): {fp2[..16]}...");
        Console.WriteLine($"    Fingerprint q3 (Year=2024, Limit=20): {fp3[..16]}...");

        // Note: fingerprints reflect structural shape (node types), not parameter values
        Console.WriteLine($"    ¿q1 == q2 (mismo shape, distinto valor)? {fp1 == fp2}");
        Console.WriteLine($"    ¿q1 == q3 (distinto Limit)? {fp1 == fp3}");

        // Practical use: query-level cache keys without exposing parameter values
        Console.WriteLine("\n    Practical use: Cache Key based on fingerprint + param values");
        var cacheKey = $"{fp1}|{2024}|{10}";
        Console.WriteLine($"    CacheKey: {cacheKey[..40]}...");

        // ────────────────────────────────────────────────────────────────────
        // 3. GetContract() — Verifiable query contract
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 3. GetContract() — Contract of query tables and columns");

        var contractQuery = Sql.From<Report>()
            .Select("id", "title", "revenue")
            .Where(r => r.Year == 2024);

        var contract = contractQuery.GetContract();
        Console.WriteLine($"    Fingerprint: {contract.Fingerprint[..16]}...");
        Console.WriteLine($"    Tables: [{string.Join(", ", contract.Tables)}]");
        Console.WriteLine($"    Columns: [{string.Join(", ", contract.Columns)}]");

        // Practical use: schema-based validation in CI/CD
        Console.WriteLine("\n    Practical use: Validate query contracts when deploying migrations.");

        // ────────────────────────────────────────────────────────────────────
        // 4. ProjectTo<T, TResult> — Type projection without duplicating query
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 4. ProjectTo<T, TResult> — Reuse query with different result type");

        var baseReportQuery = Sql.From<Report>()
            .Where(r => r.Year == 2024)
            .OrderByDescending(r => r.Revenue);

        // ProjectTo transfers all AST nodes to a new query with a different result type
        // Useful when you want to map to a DTO/flat class without repeating the WHERE/ORDER BY
        var summaryQuery = baseReportQuery.ProjectTo<Report, ReportSummary>();
        var summaries = await connection.QueryAsync<ReportSummary>(summaryQuery);

        Console.WriteLine($"    ProjectTo<Report, ReportSummary> — {summaries.Count()} records:");
        foreach (var s in summaries.Take(2))
        {
            Console.WriteLine($"      Title: {s.Title}, Revenue: {s.Revenue}");
        }

        // ────────────────────────────────────────────────────────────────────
        // 5. ToResultAsync — Result wrapped in Result<T>
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 5. ToResultAsync — Result<IReadOnlyList<T>> for functional error handling");

        var resultAsync = await Sql.From<Report>()
            .Where(r => r.Year == 2024)
            .ToResultAsync(connection);

        if (resultAsync.IsSuccess)
        {
            Console.WriteLine($"    Success: {resultAsync.Value!.Count} reportes obtenidos.");
        }
        else
        {
            Console.WriteLine($"    Error: {resultAsync.Error!.Description}");
        }

        // ────────────────────────────────────────────────────────────────────
        // 6. ToPagedListAsync — Pagination as Result<IPagedList<T>>
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 6. ToPagedListAsync — Result<IPagedList<T>> unificado");

        var pagedResult = await Sql.From<Report>()
            .Where(r => r.Year == 2024)
            .OrderByDescending(r => r.Revenue)
            .ToPagedListAsync(connection, pageNumber: 1, pageSize: 2);

        if (pagedResult.IsSuccess)
        {
            var page = pagedResult.Value!;
            Console.WriteLine($"    Page {page.Page}/{page.TotalPages}, Items: {page.Count}, Total: {page.TotalCount}");
        }

        // ────────────────────────────────────────────────────────────────────
        // 7. WithTag — Query tagging for diagnostics
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 7. WithTag — Query tagging for OpenTelemetry traceability");

        var taggedQuery = Sql.From<Report>()
            .Where(r => r.Year == 2024)
            .WithTag("revenue-report-fetch");

        Console.WriteLine($"    Tag asignado: {taggedQuery.Tag}");
        Console.WriteLine("    The tag appears as an activity attribute in OpenTelemetry traces.");

        // ────────────────────────────────────────────────────────────────────
        // 8. Sql.Raw(FormattableString) — Raw query with safe interpolation
        // ────────────────────────────────────────────────────────────────────
        Console.WriteLine("\n[+] 8. Sql.Raw — Raw SQL with safe interpolation (never concatenation)");

        int year = 2024;
        decimal threshold = 170000m;

        // Sql.Raw extracts interpolated values as @p0, @p1 — no string concatenation
        var rawSafeQuery = Sql.Raw($"SELECT * FROM reports WHERE year = {year} AND revenue > {threshold}");
        var rawResult = rawSafeQuery.Build(new SqliteCompiler());

        Console.WriteLine($"    SQL raw seguro: {rawResult.Sql}");
        Console.WriteLine($"    Parameters: {string.Join(", ", rawResult.Parameters.Select(p => $"{p.Key}={p.Value}"))}");

        var highRevReports = await connection.QueryAsync<Report>(rawSafeQuery);
        Console.WriteLine($"    Reports with revenue > {threshold}: {highRevReports.Count()}");
    }
}






