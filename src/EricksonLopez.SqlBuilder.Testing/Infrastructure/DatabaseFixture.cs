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
/// Provides an abstract base class for database test fixtures.
/// </summary>
/// <remarks>
/// Implements <see cref="IAsyncLifetime"/> for xUnit setup and teardown, managing container lifecycle, connection factories, and data seeding.
/// </remarks>
public abstract class DatabaseFixture : IAsyncLifetime
{
    private static readonly StandardDataset _sharedDataset = TestDataSeeder.Generate();

    /// <summary>
    /// Gets the standard dataset shared across all tests in the fixture.
    /// </summary>
    public StandardDataset Data => _sharedDataset;

    /// <summary>
    /// Creates a new open database connection for a test.
    /// </summary>
    /// <returns>A new <see cref="IDbConnection"/> instance.</returns>
    public abstract IDbConnection CreateConnection();

    /// <summary>
    /// Returns the SQL compiler for this database engine.
    /// </summary>
    /// <returns>An <see cref="ISqlCompiler"/> instance configured for the database dialect.</returns>
    public abstract ISqlCompiler CreateCompiler();

    /// <summary>
    /// Gets the connection string for this fixture.
    /// </summary>
    public abstract string ConnectionString { get; }

    /// <summary>
    /// Gets the engine identifier for diagnostics and compiler registration.
    /// </summary>
    public abstract string EngineName { get; }

    /// <summary>
    /// Starts the container if applicable, initializes the schema, and seeds test data.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    /// <exception cref="InvalidOperationException">The connection created by <see cref="CreateConnection"/> is not a <see cref="System.Data.Common.DbConnection"/></exception>
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
    /// Stops and disposes the container.
    /// </summary>
    /// <returns>A task representing the asynchronous disposal operation.</returns>
    public async Task DisposeAsync()
    {
        await StopContainerAsync();
    }

    // ─── Override Points ──────────────────────────────────────────────────────

    /// <summary>
    /// Starts the database container asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous start operation.</returns>
    protected virtual Task StartContainerAsync() => Task.CompletedTask;

    /// <summary>
    /// Stops the database container asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous stop operation.</returns>
    protected virtual Task StopContainerAsync() => Task.CompletedTask;

    /// <summary>
    /// Executes the DDL script to create the schema.
    /// </summary>
    /// <param name="connection">The database connection on which DDL commands are executed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task InitializeSchemaAsync(System.Data.Common.DbConnection connection);

    /// <summary>
    /// Seeds reference data including roles and categories.
    /// </summary>
    /// <param name="connection">The database connection used to seed reference data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task SeedCoreDataAsync(System.Data.Common.DbConnection connection);

    /// <summary>
    /// Seeds the full test dataset using the standard dataset.
    /// </summary>
    /// <param name="connection">The database connection used to seed test data.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected abstract Task SeedTestDataAsync(System.Data.Common.DbConnection connection);

    // ─── Helper: Batch Insert ─────────────────────────────────────────────────

    /// <summary>
    /// Executes an asynchronous operation with retry logic for transient failures.
    /// </summary>
    /// <param name="action">The asynchronous operation to execute.</param>
    /// <param name="maxRetries">The maximum number of retry attempts.</param>
    /// <param name="delay">The optional delay between retry attempts; defaults to two seconds when <see langword="null"/>.</param>
    /// <returns>A task representing the asynchronous retry operation.</returns>
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
    /// Reads a DDL script file from embedded resources or disk.
    /// </summary>
    /// <param name="filename">The filename of the DDL script to read.</param>
    /// <returns>The content of the DDL script file.</returns>
    /// <exception cref="FileNotFoundException">The specified DDL script file could not be found</exception>
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



