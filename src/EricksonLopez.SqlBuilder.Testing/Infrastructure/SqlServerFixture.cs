// Copyright © Erickson Lopez. MIT License.
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.SqlServer;
using EricksonLopez.SqlBuilder.Testing.Seeders;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace EricksonLopez.SqlBuilder.Testing.Infrastructure;

/// <summary>
/// SQL Server fixture using Testcontainers.
/// Starts a mcr.microsoft.com/mssql/server:2022-latest container.
///
/// Requirements:
///   • Docker Desktop with ≥4GB RAM (SQL Server requirement)
///   • NuGet: Testcontainers.MsSql
///   • ACCEPT_EULA=Y is set automatically by the builder
/// </summary>
public sealed class SqlServerFixture : DatabaseFixture
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Test@1234!")
        .WithCleanUp(true)
        .Build();

    public override string ConnectionString => _container.GetConnectionString();
    public override string EngineName => "SQL Server";

    public override IDbConnection CreateConnection()
    {
        var conn = new SqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    public override ISqlCompiler CreateCompiler() => new SqlServerCompiler();

    protected override async Task StartContainerAsync()
    {
        await _container.StartAsync();

        DapperExtensions.RegisterCompiler<SqlConnection>(() => new SqlServerCompiler());
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    protected override async Task StopContainerAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }

    protected override async Task InitializeSchemaAsync(System.Data.Common.DbConnection connection)
    {
        // SQL Server requires GO-separated batches — we split manually
        var ddl = @"
IF OBJECT_ID('dbo.order_items', 'U') IS NOT NULL DROP TABLE dbo.order_items;
IF OBJECT_ID('dbo.invoices',    'U') IS NOT NULL DROP TABLE dbo.invoices;
IF OBJECT_ID('dbo.payments',    'U') IS NOT NULL DROP TABLE dbo.payments;
IF OBJECT_ID('dbo.orders',      'U') IS NOT NULL DROP TABLE dbo.orders;
IF OBJECT_ID('dbo.products',    'U') IS NOT NULL DROP TABLE dbo.products;
IF OBJECT_ID('dbo.categories',  'U') IS NOT NULL DROP TABLE dbo.categories;
IF OBJECT_ID('dbo.customers',   'U') IS NOT NULL DROP TABLE dbo.customers;
IF OBJECT_ID('dbo.audit_logs',  'U') IS NOT NULL DROP TABLE dbo.audit_logs;
IF OBJECT_ID('dbo.user_roles',  'U') IS NOT NULL DROP TABLE dbo.user_roles;
IF OBJECT_ID('dbo.roles',       'U') IS NOT NULL DROP TABLE dbo.roles;
IF OBJECT_ID('dbo.users',       'U') IS NOT NULL DROP TABLE dbo.users;
IF OBJECT_ID('dbo.test_users',  'U') IS NOT NULL DROP TABLE dbo.test_users;";

        await connection.ExecuteAsync(ddl);

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.customers (
    id         INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    name       NVARCHAR(255) NOT NULL,
    email      NVARCHAR(320) NOT NULL CONSTRAINT uq_cust_email UNIQUE,
    phone      NVARCHAR(50),
    tax_id     NVARCHAR(50),
    is_active  BIT           NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at DATETIME2
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.test_users (
    id         INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    name       NVARCHAR(255) NOT NULL,
    email      NVARCHAR(320),
    age        INT           NOT NULL,
    is_active  BIT           NOT NULL DEFAULT 1,
    created_at DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.categories (
    id                 INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    name               NVARCHAR(200) NOT NULL,
    slug               NVARCHAR(200) NOT NULL CONSTRAINT uq_cat_slug UNIQUE,
    parent_category_id INT           REFERENCES dbo.categories(id) ON DELETE NO ACTION,
    is_active          BIT           NOT NULL DEFAULT 1,
    sort_order         INT           NOT NULL DEFAULT 0
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.products (
    id          INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    category_id INT           NOT NULL REFERENCES dbo.categories(id) ON DELETE NO ACTION,
    name        NVARCHAR(400) NOT NULL,
    sku         NVARCHAR(100) NOT NULL CONSTRAINT uq_prod_sku UNIQUE,
    description NVARCHAR(MAX),
    price       DECIMAL(18,4) NOT NULL DEFAULT 0,
    cost_price  DECIMAL(18,4) NOT NULL DEFAULT 0,
    stock       INT           NOT NULL DEFAULT 0,
    min_stock   INT           NOT NULL DEFAULT 0,
    is_active   BIT           NOT NULL DEFAULT 1,
    created_at  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    updated_at  DATETIME2
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.orders (
    id              INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    customer_id     INT           NOT NULL REFERENCES dbo.customers(id) ON DELETE NO ACTION,
    status          NVARCHAR(20)  NOT NULL DEFAULT N'pending',
    notes           NVARCHAR(MAX),
    total_amount    DECIMAL(18,4) NOT NULL DEFAULT 0,
    tax_amount      DECIMAL(18,4) NOT NULL DEFAULT 0,
    discount_amount DECIMAL(18,4) NOT NULL DEFAULT 0,
    currency        NCHAR(3)      NOT NULL DEFAULT N'USD',
    created_at      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    confirmed_at    DATETIME2,
    shipped_at      DATETIME2,
    delivered_at    DATETIME2,
    is_deleted      BIT           NOT NULL DEFAULT 0,
    deleted_at      DATETIME2
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.order_items (
    id               INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    order_id         INT           NOT NULL REFERENCES dbo.orders(id) ON DELETE CASCADE,
    product_id       INT           NOT NULL REFERENCES dbo.products(id) ON DELETE NO ACTION,
    quantity         INT           NOT NULL DEFAULT 1,
    unit_price       DECIMAL(18,4) NOT NULL DEFAULT 0,
    discount_percent DECIMAL(5,2)  NOT NULL DEFAULT 0,
    total_price      DECIMAL(18,4) NOT NULL DEFAULT 0,
    notes            NVARCHAR(MAX)
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.invoices (
    id              INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    order_id        INT           NOT NULL REFERENCES dbo.orders(id) ON DELETE NO ACTION,
    invoice_number  NVARCHAR(100) NOT NULL CONSTRAINT uq_inv_num UNIQUE,
    status          NVARCHAR(20)  NOT NULL DEFAULT N'draft',
    subtotal_amount DECIMAL(18,4) NOT NULL DEFAULT 0,
    tax_amount      DECIMAL(18,4) NOT NULL DEFAULT 0,
    total_amount    DECIMAL(18,4) NOT NULL DEFAULT 0,
    paid_amount     DECIMAL(18,4) NOT NULL DEFAULT 0,
    currency        NCHAR(3)      NOT NULL DEFAULT N'USD',
    issued_at       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    due_at          DATETIME2 NOT NULL,
    paid_at         DATETIME2,
    notes           NVARCHAR(MAX)
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.payments (
    id               INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    invoice_id       INT           NOT NULL REFERENCES dbo.invoices(id) ON DELETE NO ACTION,
    amount           DECIMAL(18,4) NOT NULL,
    method           NVARCHAR(30)  NOT NULL,
    status           NVARCHAR(20)  NOT NULL DEFAULT N'pending',
    transaction_ref  NVARCHAR(200),
    gateway_response NVARCHAR(MAX),
    paid_at          DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    refunded_at      DATETIME2,
    refunded_amount  DECIMAL(18,4)
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.users (
    id                    INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    username              NVARCHAR(100)  NOT NULL CONSTRAINT uq_usr_name UNIQUE,
    email                 NVARCHAR(320)  NOT NULL CONSTRAINT uq_usr_email UNIQUE,
    password_hash         NVARCHAR(500)  NOT NULL,
    first_name            NVARCHAR(100),
    last_name             NVARCHAR(100),
    is_active             BIT            NOT NULL DEFAULT 1,
    email_verified        BIT            NOT NULL DEFAULT 0,
    created_at            DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    last_login_at         DATETIME2,
    locked_until          DATETIME2,
    failed_login_attempts INT            NOT NULL DEFAULT 0
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.roles (
    id          INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    name        NVARCHAR(100)  NOT NULL CONSTRAINT uq_role_name UNIQUE,
    description NVARCHAR(500),
    permissions NVARCHAR(MAX)  NOT NULL DEFAULT N'[]',
    is_system   BIT            NOT NULL DEFAULT 0,
    created_at  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.user_roles (
    user_id             INT            NOT NULL REFERENCES dbo.users(id) ON DELETE CASCADE,
    role_id             INT            NOT NULL REFERENCES dbo.roles(id) ON DELETE CASCADE,
    assigned_at         DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    assigned_by_user_id INT            REFERENCES dbo.users(id) ON DELETE NO ACTION,
    CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id)
);");

        await connection.ExecuteAsync(@"
CREATE TABLE dbo.audit_logs (
    id             BIGINT         NOT NULL IDENTITY(1,1) PRIMARY KEY,
    entity_name    NVARCHAR(100)  NOT NULL,
    entity_id      NVARCHAR(100)  NOT NULL,
    action         NVARCHAR(10)   NOT NULL,
    old_values     NVARCHAR(MAX),
    new_values     NVARCHAR(MAX),
    changed_fields NVARCHAR(MAX),
    user_id        INT            REFERENCES dbo.users(id) ON DELETE SET NULL,
    ip_address     NVARCHAR(45),
    user_agent     NVARCHAR(500),
    timestamp      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    correlation_id NVARCHAR(200)
);");
    }

    protected override async Task SeedCoreDataAsync(System.Data.Common.DbConnection connection)
    {
        await using var tx = await connection.BeginTransactionAsync();
        foreach (var cat in TestDataSeeder.Categories())
        {
            await connection.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM dbo.categories WHERE slug = @Slug)
                BEGIN
                    SET IDENTITY_INSERT dbo.categories ON;
                    INSERT INTO dbo.categories (id, name, slug, parent_category_id, sort_order, is_active)
                    VALUES (@Id, @Name, @Slug, @ParentCategoryId, @SortOrder, @IsActive);
                    SET IDENTITY_INSERT dbo.categories OFF;
                END",
                new { cat.Id, cat.Name, cat.Slug, cat.ParentCategoryId, cat.SortOrder, cat.IsActive }, tx);
        }
        await tx.CommitAsync();
    }

    protected override async Task SeedTestDataAsync(System.Data.Common.DbConnection connection)
    {
        var dataset = Data;
        await using var tx = await connection.BeginTransactionAsync();

        foreach (var c in dataset.Customers)
        {
            await connection.ExecuteAsync(
                @"IF NOT EXISTS (SELECT 1 FROM dbo.customers WHERE email = @Email)
                  INSERT INTO dbo.customers (name, email, phone, tax_id, is_active, created_at)
                  VALUES (@Name, @Email, @Phone, @TaxId, @IsActive, @CreatedAt)",
                new { c.Name, c.Email, c.Phone, c.TaxId, c.IsActive, c.CreatedAt }, tx);
        }

        foreach (var p in dataset.Products)
        {
            await connection.ExecuteAsync(
                @"IF NOT EXISTS (SELECT 1 FROM dbo.products WHERE sku = @Sku)
                  INSERT INTO dbo.products (category_id, name, sku, description, price, cost_price, stock, min_stock, is_active, created_at)
                  VALUES (@CategoryId, @Name, @Sku, @Description, @Price, @CostPrice, @Stock, @MinStock, @IsActive, @CreatedAt)",
                new { p.CategoryId, p.Name, p.Sku, p.Description, p.Price, p.CostPrice, p.Stock, p.MinStock, p.IsActive, p.CreatedAt }, tx);
        }

        // Orders
        var customerIds = (await connection.QueryAsync<int>("SELECT id FROM dbo.customers", transaction: tx)).ToArray();
        var rng = new Random(42);
        foreach (var o in dataset.Orders.Take(100)) // Limit for CI
        {
            var customerId = customerIds.Length > 0 ? customerIds[rng.Next(customerIds.Length)] : 1;
            await connection.ExecuteAsync(
                @"INSERT INTO dbo.orders (customer_id, status, total_amount, tax_amount, discount_amount, currency, created_at, is_deleted)
                  VALUES (@CustomerId, @Status, @TotalAmount, @TaxAmount, @DiscountAmount, @Currency, @CreatedAt, @IsDeleted)",
                new { CustomerId = customerId, o.Status, o.TotalAmount, o.TaxAmount, o.DiscountAmount, o.Currency, o.CreatedAt, o.IsDeleted }, tx);
        }

        await tx.CommitAsync();
    }
}




