// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.PostgreSql;
using EricksonLopez.SqlBuilder.Testing.Domain;
using EricksonLopez.SqlBuilder.Testing.Seeders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace EricksonLopez.SqlBuilder.Testing.Infrastructure;

/// <summary>
/// PostgreSQL fixture using Testcontainers.
/// Starts a postgres:16-alpine container for each test collection.
///
/// Requirements:
///   • Docker Desktop or Docker Engine running
///   • NuGet: Testcontainers.PostgreSql
///
/// Container specs:
///   • Image:    postgres:16-alpine (smallest, fastest)
///   • Database: sqlbuilder_test
///   • Username: test
///   • Password: Test@1234!
/// </summary>
public sealed class PostgreSqlFixture : DatabaseFixture
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("sqlbuilder_test")
        .WithUsername("test")
        .WithPassword("Test@1234!")
        .WithCleanUp(true)
        .Build();

    public override string ConnectionString => _container.GetConnectionString();
    public override string EngineName => "PostgreSQL";

    public override IDbConnection CreateConnection()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    public override ISqlCompiler CreateCompiler() => new PostgreSqlCompiler();

    protected override async Task StartContainerAsync()
    {
        await _container.StartAsync();

        // Register compiler for NpgsqlConnection
        DapperExtensions.RegisterCompiler<NpgsqlConnection>(() => new PostgreSqlCompiler());
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    protected override async Task StopContainerAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    protected override async Task InitializeSchemaAsync(System.Data.Common.DbConnection connection)
    {
        await connection.ExecuteAsync(@"
            -- Extensions
            CREATE EXTENSION IF NOT EXISTS ""pgcrypto"";

            -- Customers
            CREATE TABLE IF NOT EXISTS customers (
                id         INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name       TEXT NOT NULL,
                email      TEXT NOT NULL UNIQUE,
                phone      TEXT,
                tax_id     TEXT,
                is_active  BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ
            );

            -- test_users
            CREATE TABLE IF NOT EXISTS test_users (
                id         INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name       TEXT NOT NULL,
                email      TEXT,
                age        INTEGER NOT NULL,
                is_active  BOOLEAN NOT NULL DEFAULT TRUE,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            -- Categories
            CREATE TABLE IF NOT EXISTS categories (
                id                 INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name               TEXT NOT NULL,
                slug               TEXT NOT NULL UNIQUE,
                parent_category_id INTEGER REFERENCES categories(id) ON DELETE SET NULL,
                is_active          BOOLEAN NOT NULL DEFAULT TRUE,
                sort_order         INTEGER NOT NULL DEFAULT 0
            );

            -- Products
            CREATE TABLE IF NOT EXISTS products (
                id          INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                category_id INTEGER NOT NULL REFERENCES categories(id) ON DELETE RESTRICT,
                name        TEXT NOT NULL,
                sku         TEXT NOT NULL UNIQUE,
                description TEXT,
                price       NUMERIC(18,4) NOT NULL CHECK (price >= 0),
                cost_price  NUMERIC(18,4) NOT NULL DEFAULT 0,
                stock       INTEGER NOT NULL DEFAULT 0,
                min_stock   INTEGER NOT NULL DEFAULT 0,
                is_active   BOOLEAN NOT NULL DEFAULT TRUE,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at  TIMESTAMPTZ
            );

            -- Orders
            CREATE TABLE IF NOT EXISTS orders (
                id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                customer_id     INTEGER NOT NULL REFERENCES customers(id) ON DELETE RESTRICT,
                status          TEXT NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','confirmed','shipped','delivered','cancelled')),
                notes           TEXT,
                total_amount    NUMERIC(18,4) NOT NULL DEFAULT 0,
                tax_amount      NUMERIC(18,4) NOT NULL DEFAULT 0,
                discount_amount NUMERIC(18,4) NOT NULL DEFAULT 0,
                currency        CHAR(3) NOT NULL DEFAULT 'USD',
                created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                confirmed_at    TIMESTAMPTZ,
                shipped_at      TIMESTAMPTZ,
                delivered_at    TIMESTAMPTZ,
                is_deleted      BOOLEAN NOT NULL DEFAULT FALSE,
                deleted_at      TIMESTAMPTZ
            );

            -- Order Items
            CREATE TABLE IF NOT EXISTS order_items (
                id               INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                order_id         INTEGER NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
                product_id       INTEGER NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
                quantity         INTEGER NOT NULL CHECK (quantity > 0),
                unit_price       NUMERIC(18,4) NOT NULL DEFAULT 0,
                discount_percent NUMERIC(5,2)  NOT NULL DEFAULT 0,
                total_price      NUMERIC(18,4) NOT NULL DEFAULT 0,
                notes            TEXT
            );

            -- Users
            CREATE TABLE IF NOT EXISTS users (
                id                    INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                username              TEXT NOT NULL UNIQUE,
                email                 TEXT NOT NULL UNIQUE,
                password_hash         TEXT NOT NULL,
                first_name            TEXT,
                last_name             TEXT,
                is_active             BOOLEAN NOT NULL DEFAULT TRUE,
                email_verified        BOOLEAN NOT NULL DEFAULT FALSE,
                created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                last_login_at         TIMESTAMPTZ,
                locked_until          TIMESTAMPTZ,
                failed_login_attempts INTEGER NOT NULL DEFAULT 0
            );

            -- Roles
            CREATE TABLE IF NOT EXISTS roles (
                id          INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                name        TEXT NOT NULL UNIQUE,
                description TEXT,
                permissions JSONB NOT NULL DEFAULT '[]'::jsonb,
                is_system   BOOLEAN NOT NULL DEFAULT FALSE,
                created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            -- User Roles
            CREATE TABLE IF NOT EXISTS user_roles (
                user_id             INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
                role_id             INTEGER NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
                assigned_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                assigned_by_user_id INTEGER REFERENCES users(id) ON DELETE SET NULL,
                PRIMARY KEY (user_id, role_id)
            );

            -- Invoices
            CREATE TABLE IF NOT EXISTS invoices (
                id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                order_id        INTEGER NOT NULL REFERENCES orders(id) ON DELETE RESTRICT,
                invoice_number  TEXT NOT NULL UNIQUE,
                status          TEXT NOT NULL DEFAULT 'draft' CHECK (status IN ('draft','issued','paid','overdue','cancelled')),
                subtotal_amount NUMERIC(18,4) NOT NULL DEFAULT 0,
                tax_amount      NUMERIC(18,4) NOT NULL DEFAULT 0,
                total_amount    NUMERIC(18,4) NOT NULL DEFAULT 0,
                paid_amount     NUMERIC(18,4) NOT NULL DEFAULT 0,
                currency        CHAR(3) NOT NULL DEFAULT 'USD',
                issued_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                due_at          TIMESTAMPTZ NOT NULL,
                paid_at         TIMESTAMPTZ,
                notes           TEXT
            );

            -- Payments
            CREATE TABLE IF NOT EXISTS payments (
                id               INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                invoice_id       INTEGER NOT NULL REFERENCES invoices(id) ON DELETE RESTRICT,
                amount           NUMERIC(18,4) NOT NULL,
                method           TEXT NOT NULL,
                status           TEXT NOT NULL DEFAULT 'pending',
                transaction_ref  TEXT,
                gateway_response TEXT,
                paid_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                refunded_at      TIMESTAMPTZ,
                refunded_amount  NUMERIC(18,4)
            );

            -- Audit Logs
            CREATE TABLE IF NOT EXISTS audit_logs (
                id             BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                entity_name    TEXT NOT NULL,
                entity_id      TEXT NOT NULL,
                action         TEXT NOT NULL CHECK (action IN ('INSERT','UPDATE','DELETE')),
                old_values     JSONB,
                new_values     JSONB,
                changed_fields JSONB,
                user_id        INTEGER REFERENCES users(id) ON DELETE SET NULL,
                ip_address     TEXT,
                user_agent     TEXT,
                timestamp      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                correlation_id TEXT
            );

            -- Indexes
            CREATE INDEX IF NOT EXISTS idx_customers_email    ON customers(email);
            CREATE INDEX IF NOT EXISTS idx_products_sku       ON products(sku);
            CREATE INDEX IF NOT EXISTS idx_orders_customer_id ON orders(customer_id);
            CREATE INDEX IF NOT EXISTS idx_orders_status      ON orders(status);
            CREATE INDEX IF NOT EXISTS idx_audit_entity       ON audit_logs(entity_name, entity_id);
        ");
    }

    protected override async Task SeedCoreDataAsync(System.Data.Common.DbConnection connection)
    {
        await connection.ExecuteAsync(@"
            INSERT INTO categories (name, slug, parent_category_id, sort_order)
            VALUES
                ('Electronics','electronics',NULL,1),
                ('Clothing & Apparel','clothing',NULL,2),
                ('Home & Garden','home-garden',NULL,3),
                ('Books & Media','books-media',NULL,4),
                ('Laptops','laptops',1,1),
                ('Smartphones','smartphones',1,2),
                ('Accessories','accessories',1,3),
                ('Mens Clothing','mens-clothing',2,1),
                ('Womens Clothing','womens-clothing',2,2),
                ('Gaming Laptops','gaming-laptops',5,1)
            ON CONFLICT (slug) DO NOTHING;
        ");
    }

    protected override async Task SeedTestDataAsync(System.Data.Common.DbConnection connection)
    {
        var dataset = Data;

        await using var tx = await connection.BeginTransactionAsync();

        // Bulk insert customers using unnest (PostgreSQL-specific for performance)
        var customerIds     = dataset.Customers.Select(c => c.Id).ToArray();
        var customerNames   = dataset.Customers.Select(c => c.Name).ToArray();
        var customerEmails  = dataset.Customers.Select(c => c.Email).ToArray();
        var customerPhones  = dataset.Customers.Select(c => (object?)c.Phone).ToArray();
        var customerActive  = dataset.Customers.Select(c => c.IsActive).ToArray();
        var customerCreated = dataset.Customers.Select(c => c.CreatedAt).ToArray();

        // Use parameterized batch inserts (simple, portable approach)
        // For maximum PG performance, use COPY via NpgsqlBinaryImporter (not in scope here)
        foreach (var c in dataset.Customers)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO customers (name, email, phone, tax_id, is_active, created_at)
                  VALUES (@Name, @Email, @Phone, @TaxId, @IsActive, @CreatedAt)
                  ON CONFLICT (email) DO NOTHING",
                new { c.Name, c.Email, c.Phone, c.TaxId, c.IsActive, c.CreatedAt },
                tx);
        }

        // Fetch actual customer IDs after insert (SERIAL vs IDENTITY generates new IDs)
        var actualCustomerIds = (await connection.QueryAsync<int>(
            "SELECT id FROM customers ORDER BY id", transaction: tx)).ToArray();

        // Products
        foreach (var p in dataset.Products)
        {
            await connection.ExecuteAsync(
                @"INSERT INTO products (category_id, name, sku, description, price, cost_price, stock, min_stock, is_active, created_at)
                  VALUES (@CategoryId, @Name, @Sku, @Description, @Price, @CostPrice, @Stock, @MinStock, @IsActive, @CreatedAt)
                  ON CONFLICT (sku) DO NOTHING",
                new { p.CategoryId, p.Name, p.Sku, p.Description, p.Price, p.CostPrice, p.Stock, p.MinStock, p.IsActive, p.CreatedAt },
                tx);
        }

        // Orders (with remapped customer IDs to actual DB IDs)
        var customerIdMap = actualCustomerIds.Select((id, i) => (i + 1, id))
            .ToDictionary(x => x.Item1, x => x.id);

        foreach (var o in dataset.Orders)
        {
            var actualCustomerId = customerIdMap.TryGetValue(o.CustomerId, out var mapped) ? mapped : o.CustomerId;
            await connection.ExecuteAsync(
                @"INSERT INTO orders (customer_id, status, notes, total_amount, tax_amount, discount_amount, currency, created_at, is_deleted, deleted_at)
                  VALUES (@CustomerId, @Status, @Notes, @TotalAmount, @TaxAmount, @DiscountAmount, @Currency, @CreatedAt, @IsDeleted, @DeletedAt)",
                new { CustomerId = actualCustomerId, o.Status, o.Notes, o.TotalAmount, o.TaxAmount,
                      o.DiscountAmount, o.Currency, o.CreatedAt, o.IsDeleted, o.DeletedAt },
                tx);
        }

        // OrderItems — fetch actual order and product IDs
        var actualOrderIds   = (await connection.QueryAsync<int>("SELECT id FROM orders ORDER BY id", transaction: tx)).ToArray();
        var actualProductIds = (await connection.QueryAsync<int>("SELECT id FROM products ORDER BY id", transaction: tx)).ToArray();

        if (actualOrderIds.Length > 0 && actualProductIds.Length > 0)
        {
            var rng = new Random(42);
            foreach (var oi in dataset.OrderItems.Take(1000)) // Limit to 1000 for CI speed
            {
                var orderId   = actualOrderIds[rng.Next(actualOrderIds.Length)];
                var productId = actualProductIds[rng.Next(actualProductIds.Length)];
                await connection.ExecuteAsync(
                    @"INSERT INTO order_items (order_id, product_id, quantity, unit_price, discount_percent, total_price)
                      VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice, @DiscountPercent, @TotalPrice)",
                    new { OrderId = orderId, ProductId = productId, oi.Quantity, oi.UnitPrice, oi.DiscountPercent, oi.TotalPrice },
                    tx);
            }
        }

        await tx.CommitAsync();
    }
}




