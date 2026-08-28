// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.Sqlite;
using EricksonLopez.SqlBuilder.Testing.Domain;
using EricksonLopez.SqlBuilder.Testing.Seeders;
using Microsoft.Data.Sqlite;

namespace EricksonLopez.SqlBuilder.Testing.Infrastructure;

/// <summary>
/// SQLite in-memory fixture. Does NOT require Docker.
/// Uses a named shared-cache connection string so all connections
/// within the fixture share the same in-memory database.
///
/// Behavior:
///   • Database is created in-memory per fixture instance
///   • Schema is created once at startup
///   • Data is seeded once at startup
///   • All tests share the same data (read tests should not modify)
///   • For mutation tests: use transactions rolled back after each test
/// </summary>
public sealed class SqliteFixture : DatabaseFixture
{
    // Shared cache allows multiple connections to see the same data
    private const string DbName = "sqlbuilder_test";
    private static readonly string SharedConnectionString = $"Data Source={DbName};Mode=Memory;Cache=Shared;";

    // Keep a "root" connection alive so the in-memory DB persists for the fixture lifetime
    private SqliteConnection? _rootConnection;

    public override string ConnectionString => SharedConnectionString;
    public override string EngineName => "SQLite";

    public override IDbConnection CreateConnection()
    {
        var conn = new SqliteConnection(SharedConnectionString);
        conn.Open();
        // Enable foreign keys and WAL for every connection
        conn.Execute("PRAGMA foreign_keys = ON;");
        conn.Execute("PRAGMA journal_mode = WAL;");
        return conn;
    }

    public override ISqlCompiler CreateCompiler() => new SqliteCompiler();

    protected override async Task StartContainerAsync()
    {
        // No container needed — open the root connection to keep in-memory DB alive
        _rootConnection = new SqliteConnection(SharedConnectionString);
        await _rootConnection.OpenAsync();
        _rootConnection.Execute("PRAGMA foreign_keys = ON;");

        // Register compiler globally for SqliteConnection
        DapperExtensions.RegisterCompiler<SqliteConnection>(() => new SqliteCompiler());
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    protected override Task StopContainerAsync()
    {
        _rootConnection?.Close();
        _rootConnection?.Dispose();
        _rootConnection = null;
        return Task.CompletedTask;
    }

    protected override async Task InitializeSchemaAsync(System.Data.Common.DbConnection connection)
    {
        // SQLite DDL: create all tables
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS ""customers"" (
                ""id""         INTEGER PRIMARY KEY,
                ""name""       TEXT NOT NULL,
                ""email""      TEXT NOT NULL UNIQUE,
                ""phone""      TEXT,
                ""tax_id""     TEXT,
                ""is_active""  INTEGER NOT NULL DEFAULT 1,
                ""created_at"" TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                ""updated_at"" TEXT,
                CHECK (""is_active"" IN (0, 1))
            );

            CREATE TABLE IF NOT EXISTS ""categories"" (
                ""id""                 INTEGER PRIMARY KEY,
                ""name""               TEXT NOT NULL,
                ""slug""               TEXT NOT NULL UNIQUE,
                ""parent_category_id"" INTEGER REFERENCES ""categories""(""id"") ON DELETE SET NULL,
                ""is_active""          INTEGER NOT NULL DEFAULT 1,
                ""sort_order""         INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS ""products"" (
                ""id""          INTEGER PRIMARY KEY,
                ""category_id"" INTEGER NOT NULL REFERENCES ""categories""(""id"") ON DELETE RESTRICT,
                ""name""        TEXT NOT NULL,
                ""sku""         TEXT NOT NULL UNIQUE,
                ""description"" TEXT,
                ""price""       REAL NOT NULL CHECK (""price"" >= 0),
                ""cost_price""  REAL NOT NULL DEFAULT 0.0,
                ""stock""       INTEGER NOT NULL DEFAULT 0,
                ""min_stock""   INTEGER NOT NULL DEFAULT 0,
                ""is_active""   INTEGER NOT NULL DEFAULT 1,
                ""created_at""  TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                ""updated_at""  TEXT
            );

            CREATE TABLE IF NOT EXISTS ""orders"" (
                ""id""              INTEGER PRIMARY KEY,
                ""customer_id""     INTEGER NOT NULL REFERENCES ""customers""(""id"") ON DELETE RESTRICT,
                ""status""          TEXT NOT NULL DEFAULT 'pending' CHECK (""status"" IN ('pending','confirmed','shipped','delivered','cancelled')),
                ""notes""           TEXT,
                ""total_amount""    REAL NOT NULL DEFAULT 0,
                ""tax_amount""      REAL NOT NULL DEFAULT 0,
                ""discount_amount"" REAL NOT NULL DEFAULT 0,
                ""currency""        TEXT NOT NULL DEFAULT 'USD',
                ""created_at""      TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                ""confirmed_at""    TEXT,
                ""shipped_at""      TEXT,
                ""delivered_at""    TEXT,
                ""is_deleted""      INTEGER NOT NULL DEFAULT 0,
                ""deleted_at""      TEXT
            );

            CREATE TABLE IF NOT EXISTS ""order_items"" (
                ""id""               INTEGER PRIMARY KEY,
                ""order_id""         INTEGER NOT NULL REFERENCES ""orders""(""id"") ON DELETE CASCADE,
                ""product_id""       INTEGER NOT NULL REFERENCES ""products""(""id"") ON DELETE RESTRICT,
                ""quantity""         INTEGER NOT NULL CHECK (""quantity"" > 0),
                ""unit_price""       REAL NOT NULL DEFAULT 0,
                ""discount_percent"" REAL NOT NULL DEFAULT 0,
                ""total_price""      REAL NOT NULL DEFAULT 0,
                ""notes""            TEXT
            );

            CREATE TABLE IF NOT EXISTS ""users"" (
                ""id""                    INTEGER PRIMARY KEY,
                ""username""              TEXT NOT NULL UNIQUE,
                ""email""                 TEXT NOT NULL UNIQUE,
                ""password_hash""         TEXT NOT NULL,
                ""first_name""            TEXT,
                ""last_name""             TEXT,
                ""is_active""             INTEGER NOT NULL DEFAULT 1,
                ""email_verified""        INTEGER NOT NULL DEFAULT 0,
                ""created_at""            TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                ""last_login_at""         TEXT,
                ""locked_until""          TEXT,
                ""failed_login_attempts"" INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS ""test_users"" (
                ""id""         INTEGER PRIMARY KEY,
                ""name""       TEXT NOT NULL,
                ""email""      TEXT,
                ""age""        INTEGER NOT NULL,
                ""is_active""  INTEGER NOT NULL DEFAULT 1,
                ""created_at"" TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
            );

            CREATE TABLE IF NOT EXISTS ""roles"" (
                ""id""          INTEGER PRIMARY KEY,
                ""name""        TEXT NOT NULL UNIQUE,
                ""description"" TEXT,
                ""permissions"" TEXT NOT NULL DEFAULT '[]',
                ""is_system""   INTEGER NOT NULL DEFAULT 0,
                ""created_at""  TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
            );

            CREATE TABLE IF NOT EXISTS ""user_roles"" (
                ""user_id""             INTEGER NOT NULL REFERENCES ""users""(""id"") ON DELETE CASCADE,
                ""role_id""             INTEGER NOT NULL REFERENCES ""roles""(""id"") ON DELETE CASCADE,
                ""assigned_at""         TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                ""assigned_by_user_id"" INTEGER,
                PRIMARY KEY (""user_id"", ""role_id"")
            );

            CREATE TABLE IF NOT EXISTS ""invoices"" (
                ""id""              INTEGER PRIMARY KEY,
                ""order_id""        INTEGER NOT NULL REFERENCES ""orders""(""id"") ON DELETE RESTRICT,
                ""invoice_number""  TEXT NOT NULL UNIQUE,
                ""status""          TEXT NOT NULL DEFAULT 'draft',
                ""subtotal_amount"" REAL NOT NULL DEFAULT 0,
                ""tax_amount""      REAL NOT NULL DEFAULT 0,
                ""total_amount""    REAL NOT NULL DEFAULT 0,
                ""paid_amount""     REAL NOT NULL DEFAULT 0,
                ""currency""        TEXT NOT NULL DEFAULT 'USD',
                ""issued_at""       TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                ""due_at""          TEXT NOT NULL,
                ""paid_at""         TEXT,
                ""notes""           TEXT
            );

            CREATE TABLE IF NOT EXISTS ""payments"" (
                ""id""               INTEGER PRIMARY KEY,
                ""invoice_id""       INTEGER NOT NULL REFERENCES ""invoices""(""id"") ON DELETE RESTRICT,
                ""amount""           REAL NOT NULL,
                ""method""           TEXT NOT NULL,
                ""status""           TEXT NOT NULL DEFAULT 'pending',
                ""transaction_ref""  TEXT,
                ""gateway_response"" TEXT,
                ""paid_at""          TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                ""refunded_at""      TEXT,
                ""refunded_amount""  REAL
            );

            CREATE TABLE IF NOT EXISTS ""audit_logs"" (
                ""id""             INTEGER PRIMARY KEY,
                ""entity_name""    TEXT NOT NULL,
                ""entity_id""      TEXT NOT NULL,
                ""action""         TEXT NOT NULL CHECK (""action"" IN ('INSERT','UPDATE','DELETE')),
                ""old_values""     TEXT,
                ""new_values""     TEXT,
                ""changed_fields"" TEXT,
                ""user_id""        INTEGER,
                ""ip_address""     TEXT,
                ""user_agent""     TEXT,
                ""timestamp""      TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
                ""correlation_id"" TEXT
            );
        ");
    }

    protected override async Task SeedCoreDataAsync(System.Data.Common.DbConnection connection)
    {
        // Seed categories first (referenced by products)
        foreach (var cat in TestDataSeeder.Categories())
        {
            await connection.ExecuteAsync(
                @"INSERT OR IGNORE INTO ""categories"" (""id"",""name"",""slug"",""parent_category_id"",""is_active"",""sort_order"")
                  VALUES (@Id, @Name, @Slug, @ParentCategoryId, @IsActive, @SortOrder)",
                new { cat.Id, cat.Name, cat.Slug, cat.ParentCategoryId, IsActive = cat.IsActive ? 1 : 0, cat.SortOrder });
        }
    }

    protected override async Task SeedTestDataAsync(System.Data.Common.DbConnection connection)
    {
        var dataset = Data;

        // Batch insert with transactions for performance
        await using var tx = await connection.BeginTransactionAsync();

        // Customers
        foreach (var c in dataset.Customers)
        {
            await connection.ExecuteAsync(
                @"INSERT OR IGNORE INTO ""customers"" (""id"",""name"",""email"",""phone"",""tax_id"",""is_active"",""created_at"",""updated_at"")
                  VALUES (@Id, @Name, @Email, @Phone, @TaxId, @IsActive, @CreatedAt, @UpdatedAt)",
                new { c.Id, c.Name, c.Email, c.Phone, c.TaxId, IsActive = c.IsActive ? 1 : 0,
                      CreatedAt = c.CreatedAt.ToString("O"), UpdatedAt = c.UpdatedAt?.ToString("O") },
                tx);
        }

        // Products
        foreach (var p in dataset.Products)
        {
            await connection.ExecuteAsync(
                @"INSERT OR IGNORE INTO ""products"" (""id"",""category_id"",""name"",""sku"",""description"",""price"",""cost_price"",""stock"",""min_stock"",""is_active"",""created_at"")
                  VALUES (@Id, @CategoryId, @Name, @Sku, @Description, @Price, @CostPrice, @Stock, @MinStock, @IsActive, @CreatedAt)",
                new { p.Id, p.CategoryId, p.Name, p.Sku, p.Description, p.Price, p.CostPrice, p.Stock, p.MinStock,
                      IsActive = p.IsActive ? 1 : 0, CreatedAt = p.CreatedAt.ToString("O") },
                tx);
        }

        // Orders
        foreach (var o in dataset.Orders)
        {
            await connection.ExecuteAsync(
                @"INSERT OR IGNORE INTO ""orders"" (""id"",""customer_id"",""status"",""notes"",""total_amount"",""tax_amount"",""discount_amount"",""currency"",""created_at"",""is_deleted"",""deleted_at"")
                  VALUES (@Id, @CustomerId, @Status, @Notes, @TotalAmount, @TaxAmount, @DiscountAmount, @Currency, @CreatedAt, @IsDeleted, @DeletedAt)",
                new { o.Id, o.CustomerId, o.Status, o.Notes, o.TotalAmount, o.TaxAmount, o.DiscountAmount,
                      o.Currency, CreatedAt = o.CreatedAt.ToString("O"), IsDeleted = o.IsDeleted ? 1 : 0,
                      DeletedAt = o.DeletedAt?.ToString("O") },
                tx);
        }

        // OrderItems
        foreach (var oi in dataset.OrderItems)
        {
            await connection.ExecuteAsync(
                @"INSERT OR IGNORE INTO ""order_items"" (""id"",""order_id"",""product_id"",""quantity"",""unit_price"",""discount_percent"",""total_price"")
                  VALUES (@Id, @OrderId, @ProductId, @Quantity, @UnitPrice, @DiscountPercent, @TotalPrice)",
                new { oi.Id, oi.OrderId, oi.ProductId, oi.Quantity, oi.UnitPrice, oi.DiscountPercent, oi.TotalPrice },
                tx);
        }

        await tx.CommitAsync();
    }
}




