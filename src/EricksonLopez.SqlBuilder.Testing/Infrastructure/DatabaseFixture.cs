// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Testing.Domain;
using EricksonLopez.SqlBuilder.Testing.Seeders;
using Xunit;

namespace EricksonLopez.SqlBuilder.Testing.Infrastructure;

/// <summary>
/// Abstract base class for all database test fixtures.
/// Implements IAsyncLifetime for proper xUnit async setup/teardown.
///
/// Each engine-specific fixture inherits this class and provides:
///   1. Container lifecycle (start/stop)
///   2. Connection factory
///   3. Compiler factory
///   4. Schema initialization (DDL)
///   5. Data seeding
/// </summary>
public abstract class DatabaseFixture : IAsyncLifetime
{
    private static readonly StandardDataset _sharedDataset = TestDataSeeder.Generate();

    /// <summary>
    /// The standard dataset shared across all tests in the fixture.
    /// Generated once, used by all tests for read operations.
    /// </summary>
    public StandardDataset Data => _sharedDataset;

    /// <summary>
    /// Creates a new open database connection for a test.
    /// Callers are responsible for disposing the connection.
    /// </summary>
    public abstract IDbConnection CreateConnection();

    /// <summary>
    /// Returns the SQL compiler for this database engine.
    /// Compilers are typically stateless and can be reused.
    /// </summary>
    public abstract ISqlCompiler CreateCompiler();

    /// <summary>
    /// Returns the connection string for this fixture.
    /// Used by Dapper RegisterCompiler and diagnostic tools.
    /// </summary>
    public abstract string ConnectionString { get; }

    /// <summary>
    /// Engine identifier for diagnostics (e.g. "PostgreSQL", "SQLite").
    /// </summary>
    public abstract string EngineName { get; }

    /// <summary>
    /// Called by xUnit before any test in the fixture runs.
    /// Starts the container (if applicable), creates the schema, and seeds data.
    /// </summary>
    public async Task InitializeAsync()
    {
        await StartContainerAsync();
        await using var conn = CreateConnection() as System.Data.Common.DbConnection
            ?? throw new InvalidOperationException("Connection must be a DbConnection");

        if (conn.State != ConnectionState.Open)
        {
            await conn.OpenAsync();
        }
        await InitializeSchemaAsync(conn);
        await SeedCoreDataAsync(conn);
        await SeedTestDataAsync(conn);
    }

    /// <summary>
    /// Called by xUnit after all tests in the fixture have run.
    /// Stops and disposes the container.
    /// </summary>
    public async Task DisposeAsync()
    {
        await StopContainerAsync();
    }

    // ─── Override Points ──────────────────────────────────────────────────────

    /// <summary>
    /// Starts the Docker container. Override for Testcontainers-based fixtures.
    /// Base implementation is a no-op (for SQLite in-memory).
    /// </summary>
    protected virtual Task StartContainerAsync() => Task.CompletedTask;

    /// <summary>
    /// Stops the Docker container. Override for Testcontainers-based fixtures.
    /// </summary>
    protected virtual Task StopContainerAsync() => Task.CompletedTask;

    /// <summary>
    /// Executes the DDL script to create the schema.
    /// Each engine has its own DDL dialect.
    /// </summary>
    protected abstract Task InitializeSchemaAsync(System.Data.Common.DbConnection connection);

    /// <summary>
    /// Seeds reference data: roles and categories.
    /// These are required for FK constraints before seeding transactional data.
    /// </summary>
    protected abstract Task SeedCoreDataAsync(System.Data.Common.DbConnection connection);

    /// <summary>
    /// Seeds the full test dataset: customers, products, orders, order items, etc.
    /// Uses the TestDataSeeder-generated StandardDataset.
    /// </summary>
    protected abstract Task SeedTestDataAsync(System.Data.Common.DbConnection connection);

    // ─── Helper: Batch Insert ─────────────────────────────────────────────────

    /// <summary>
    /// Executes a command with retry for transient failures (e.g. container startup).
    /// </summary>
    protected static async Task ExecuteWithRetryAsync(
        Func<Task> action,
        int maxRetries = 3,
        TimeSpan? delay = null)
    {
        delay ??= TimeSpan.FromSeconds(2);
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch when (attempt < maxRetries)
            {
                await Task.Delay(delay.Value);
            }
        }
    }

    /// <summary>
    /// Reads a DDL file from the embedded resources or disk.
    /// </summary>
    protected static string ReadDdlFile(string filename)
    {
        // Try to read from the DDL directory relative to test assembly
        var assembly = typeof(DatabaseFixture).Assembly;
        var resourceName = $"EricksonLopez.SqlBuilder.Testing.Infrastructure.DDL.{filename}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        // Fallback: look in the file system (dev mode)
        var basePath = AppContext.BaseDirectory;
        var ddlPath = Path.Combine(basePath, "Infrastructure", "DDL", filename);
        if (File.Exists(ddlPath))
        {
            return File.ReadAllText(ddlPath);
        }

        // Last resort: walk up directories
        var dir = new DirectoryInfo(basePath);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "Infrastructure", "DDL", filename);
            if (File.Exists(candidate))
            {
                return File.ReadAllText(candidate);
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"DDL file '{filename}' not found. " +
            "Ensure it's included as EmbeddedResource or present in the output directory.");
    }
}



