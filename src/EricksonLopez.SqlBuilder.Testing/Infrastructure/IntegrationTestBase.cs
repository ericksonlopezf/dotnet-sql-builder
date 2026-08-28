// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Dapper;

namespace EricksonLopez.SqlBuilder.Testing.Infrastructure;

/// <summary>
/// Provides a reusable base for integration tests that require a live database connection.
/// </summary>
/// <remarks>
/// Subclasses supply the connection factory and compiler, and define database initialization
/// and cleanup hooks. The <see cref="Connection"/> property is initialized before each test
/// via <see cref="InitializeAsync"/> and disposed after each test via <see cref="DisposeAsync"/>.
/// </remarks>
public abstract class IntegrationTestBase
{
    /// <summary>Gets the active database connection for use within a test.</summary>
    protected IDbConnection Connection { get; private set; } = null!;
    /// <summary>Gets the SQL compiler associated with the target database dialect.</summary>
    protected abstract ISqlCompiler Compiler { get; }
    
    /// <summary>
    /// Creates and returns an open database connection for the target database.
    /// </summary>
    /// <returns>A task that resolves to the open database connection.</returns>
    protected abstract Task<IDbConnection> CreateConnectionAsync();
    /// <summary>Populates or migrates the test database schema before each test run.</summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    protected abstract Task InitializeDatabaseAsync();
    /// <summary>Removes test data or reverts any changes made during the test.</summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    protected abstract Task CleanupDatabaseAsync();

    /// <summary>Initializes the test environment by creating the database connection and preparing the schema.</summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        Connection = await CreateConnectionAsync();
        
        // Register the compiler globally for DapperExtensions if not already registered for this connection type
        var connType = Connection.GetType();
        // Uses reflection strictly for the test setup to register the compiler type
        typeof(DapperExtensions)
            .GetMethod("RegisterCompiler")!
            .MakeGenericMethod(connType)
            .Invoke(null, new object[] { new Func<ISqlCompiler>(() => Compiler) });

        await InitializeDatabaseAsync();
    }

    /// <summary>Disposes the database connection and cleans up after the test.</summary>
    /// <returns>A task representing the asynchronous disposal operation.</returns>
    public async Task DisposeAsync()
    {
        await CleanupDatabaseAsync();
        Connection?.Dispose();
    }
}



