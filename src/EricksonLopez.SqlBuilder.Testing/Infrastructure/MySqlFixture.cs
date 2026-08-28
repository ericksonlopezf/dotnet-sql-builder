// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EricksonLopez.SqlBuilder.Abstractions;
using EricksonLopez.SqlBuilder.Dapper;
using EricksonLopez.SqlBuilder.MySql;
using EricksonLopez.SqlBuilder.Testing.Seeders;
using MySqlConnector;
using Testcontainers.MySql;

namespace EricksonLopez.SqlBuilder.Testing.Infrastructure;

/// <summary>
/// MySQL 8.0 test fixture backed by Testcontainers.
/// Provides a real MySQL container for integration tests.
///
/// Behavior:
///   • Docker container is started once per fixture instance (class-level, via IClassFixture)
///   • Schema is created once at startup from inline DDL
///   • Data is seeded once at startup via TestDataSeeder
///   • For mutation tests: use transactions rolled back after each test
///
/// Testcontainers image: mysql:8.0.36
/// Default credentials: root / Password=sqlbuilder_test_pw
/// </summary>
public sealed class MySqlFixture : DatabaseFixture
{
    // ─── Container configuration ──────────────────────────────────────────────

    private const string Image       = "mysql:8.0.36";
    private const string Database    = "sqlbuilder_test";
    private const string Username    = "sqlbuilder";
    private const string Password    = "S3cure@SqlB!";

    private MySqlContainer? _container;

    // ─── DatabaseFixture implementation ───────────────────────────────────────

    public override string ConnectionString =>
        _container?.GetConnectionString()
            ?? throw new InvalidOperationException("MySQL container has not been started.");

    public override string EngineName => "MySQL";

    public override IDbConnection CreateConnection()
    {
        var conn = new MySqlConnection(ConnectionString);
        conn.Open();
        return conn;
    }

    public override ISqlCompiler CreateCompiler() => new MySqlCompiler();

    // ─── Container lifecycle ──────────────────────────────────────────────────

    protected override async Task StartContainerAsync()
    {
        _container = new MySqlBuilder()
            .WithImage(Image)
            .WithDatabase(Database)
            .WithUsername(Username)
            .WithPassword(Password)
            // Performance: skip sync binlog writes (test-only!)
            .WithEnvironment("MYSQL_INITDB_SKIP_TZINFO", "1")
            .Build();

        await _container.StartAsync();

        // Register compiler globally for MySqlConnection
        DapperExtensions.RegisterCompiler<MySqlConnection>(() => new MySqlCompiler());
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    protected override async Task StopContainerAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
            _container = null;
        }
    }

    // ─── Schema initialization ────────────────────────────────────────────────

    /// <summary>
    /// Creates the full schema for MySQL 8.0.
    /// Uses MySQL-specific syntax: backtick identifiers, TINYINT(1) for booleans,
    /// ENGINE=InnoDB, utf8mb4 charset, JSON columns.
    /// </summary>
    protected override async Task InitializeSchemaAsync(System.Data.Common.DbConnection connection)
    {
        // Enable multiple statement execution by using individual ExecuteAsync calls
        var ddlStatements = GetMySqlDdl();
        foreach (var statement in ddlStatements)
        {
            if (!string.IsNullOrWhiteSpace(statement))
            {
                await connection.ExecuteAsync(statement);
            }
        }
    }

    private static IEnumerable<string> GetMySqlDdl() =>
    [
        // ─── categories ───────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `categories` (
            `id`                 INT            NOT NULL AUTO_INCREMENT,
            `name`               VARCHAR(150)   NOT NULL,
            `slug`               VARCHAR(150)   NOT NULL,
            `parent_category_id` INT                NULL,
            `is_active`          TINYINT(1)     NOT NULL DEFAULT 1,
            `sort_order`         INT            NOT NULL DEFAULT 0,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uq_categories_slug` (`slug`),
            CONSTRAINT `fk_cat_parent` FOREIGN KEY (`parent_category_id`)
                REFERENCES `categories` (`id`) ON DELETE SET NULL
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── test_users ───────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `test_users` (
            `id`         INT            NOT NULL AUTO_INCREMENT,
            `name`       VARCHAR(250)   NOT NULL,
            `email`      VARCHAR(320)       NULL,
            `age`        INT            NOT NULL,
            `is_active`  TINYINT(1)     NOT NULL DEFAULT 1,
            `created_at` DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            PRIMARY KEY (`id`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── customers ────────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `customers` (
            `id`         INT            NOT NULL AUTO_INCREMENT,
            `name`       VARCHAR(250)   NOT NULL,
            `email`      VARCHAR(320)   NOT NULL,
            `phone`      VARCHAR(30)        NULL,
            `tax_id`     VARCHAR(50)        NULL,
            `is_active`  TINYINT(1)     NOT NULL DEFAULT 1,
            `created_at` DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `updated_at` DATETIME(3)        NULL,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uq_customers_email` (`email`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── products ─────────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `products` (
            `id`          INT            NOT NULL AUTO_INCREMENT,
            `category_id` INT            NOT NULL,
            `name`        VARCHAR(250)   NOT NULL,
            `sku`         VARCHAR(100)   NOT NULL,
            `description` TEXT               NULL,
            `price`       DECIMAL(18,4)  NOT NULL,
            `cost_price`  DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `stock`       INT            NOT NULL DEFAULT 0,
            `min_stock`   INT            NOT NULL DEFAULT 0,
            `is_active`   TINYINT(1)     NOT NULL DEFAULT 1,
            `created_at`  DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `updated_at`  DATETIME(3)        NULL,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uq_products_sku` (`sku`),
            CONSTRAINT `fk_products_category`
                FOREIGN KEY (`category_id`) REFERENCES `categories`(`id`) ON DELETE RESTRICT
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── orders ───────────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `orders` (
            `id`              INT            NOT NULL AUTO_INCREMENT,
            `customer_id`     INT            NOT NULL,
            `status`          VARCHAR(20)    NOT NULL DEFAULT 'pending',
            `notes`           TEXT               NULL,
            `total_amount`    DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `tax_amount`      DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `discount_amount` DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `currency`        CHAR(3)        NOT NULL DEFAULT 'USD',
            `created_at`      DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `confirmed_at`    DATETIME(3)        NULL,
            `shipped_at`      DATETIME(3)        NULL,
            `delivered_at`    DATETIME(3)        NULL,
            `is_deleted`      TINYINT(1)     NOT NULL DEFAULT 0,
            `deleted_at`      DATETIME(3)        NULL,
            PRIMARY KEY (`id`),
            CONSTRAINT `fk_orders_customer`
                FOREIGN KEY (`customer_id`) REFERENCES `customers`(`id`) ON DELETE RESTRICT,
            CONSTRAINT `chk_order_status`
                CHECK (`status` IN ('pending','confirmed','shipped','delivered','cancelled'))
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── order_items ──────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `order_items` (
            `id`               INT            NOT NULL AUTO_INCREMENT,
            `order_id`         INT            NOT NULL,
            `product_id`       INT            NOT NULL,
            `quantity`         INT            NOT NULL,
            `unit_price`       DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `discount_percent` DECIMAL(5,2)   NOT NULL DEFAULT 0.00,
            `total_price`      DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `notes`            TEXT               NULL,
            PRIMARY KEY (`id`),
            CONSTRAINT `fk_items_order`   FOREIGN KEY (`order_id`)   REFERENCES `orders`(`id`)   ON DELETE CASCADE,
            CONSTRAINT `fk_items_product` FOREIGN KEY (`product_id`) REFERENCES `products`(`id`) ON DELETE RESTRICT,
            CONSTRAINT `chk_qty` CHECK (`quantity` > 0)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── users ────────────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `users` (
            `id`                    INT          NOT NULL AUTO_INCREMENT,
            `username`              VARCHAR(100) NOT NULL,
            `email`                 VARCHAR(320) NOT NULL,
            `password_hash`         VARCHAR(250) NOT NULL,
            `first_name`            VARCHAR(100)     NULL,
            `last_name`             VARCHAR(100)     NULL,
            `is_active`             TINYINT(1)   NOT NULL DEFAULT 1,
            `email_verified`        TINYINT(1)   NOT NULL DEFAULT 0,
            `created_at`            DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `last_login_at`         DATETIME(3)      NULL,
            `locked_until`          DATETIME(3)      NULL,
            `failed_login_attempts` INT          NOT NULL DEFAULT 0,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uq_users_username` (`username`),
            UNIQUE KEY `uq_users_email`    (`email`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── roles ────────────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `roles` (
            `id`          INT          NOT NULL AUTO_INCREMENT,
            `name`        VARCHAR(100) NOT NULL,
            `description` TEXT             NULL,
            `permissions` JSON         NOT NULL,
            `is_system`   TINYINT(1)   NOT NULL DEFAULT 0,
            `created_at`  DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            PRIMARY KEY (`id`),
            UNIQUE KEY `uq_roles_name` (`name`)
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── user_roles ───────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `user_roles` (
            `user_id`             INT          NOT NULL,
            `role_id`             INT          NOT NULL,
            `assigned_at`         DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `assigned_by_user_id` INT              NULL,
            PRIMARY KEY (`user_id`, `role_id`),
            CONSTRAINT `fk_ur_user` FOREIGN KEY (`user_id`) REFERENCES `users`(`id`) ON DELETE CASCADE,
            CONSTRAINT `fk_ur_role` FOREIGN KEY (`role_id`) REFERENCES `roles`(`id`) ON DELETE CASCADE
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── invoices ─────────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `invoices` (
            `id`              INT            NOT NULL AUTO_INCREMENT,
            `order_id`        INT            NOT NULL,
            `invoice_number`  VARCHAR(50)    NOT NULL,
            `status`          VARCHAR(20)    NOT NULL DEFAULT 'draft',
            `subtotal_amount` DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `tax_amount`      DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `total_amount`    DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `paid_amount`     DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
            `currency`        CHAR(3)        NOT NULL DEFAULT 'USD',
            `issued_at`       DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `due_at`          DATETIME(3)    NOT NULL,
            `paid_at`         DATETIME(3)        NULL,
            `notes`           TEXT               NULL,
            PRIMARY KEY (`id`),
            UNIQUE KEY `uq_invoice_number` (`invoice_number`),
            CONSTRAINT `fk_invoices_order` FOREIGN KEY (`order_id`) REFERENCES `orders`(`id`) ON DELETE RESTRICT
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── payments ─────────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `payments` (
            `id`               INT            NOT NULL AUTO_INCREMENT,
            `invoice_id`       INT            NOT NULL,
            `amount`           DECIMAL(18,4)  NOT NULL,
            `method`           VARCHAR(30)    NOT NULL,
            `status`           VARCHAR(20)    NOT NULL DEFAULT 'pending',
            `transaction_ref`  VARCHAR(200)       NULL,
            `gateway_response` TEXT               NULL,
            `paid_at`          DATETIME(3)    NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `refunded_at`      DATETIME(3)        NULL,
            `refunded_amount`  DECIMAL(18,4)      NULL,
            PRIMARY KEY (`id`),
            CONSTRAINT `fk_payments_invoice` FOREIGN KEY (`invoice_id`) REFERENCES `invoices`(`id`) ON DELETE RESTRICT
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """,

        // ─── audit_logs ───────────────────────────────────────────────────────
        """
        CREATE TABLE IF NOT EXISTS `audit_logs` (
            `id`             BIGINT       NOT NULL AUTO_INCREMENT,
            `entity_name`    VARCHAR(100) NOT NULL,
            `entity_id`      VARCHAR(100) NOT NULL,
            `action`         VARCHAR(10)  NOT NULL,
            `old_values`     JSON             NULL,
            `new_values`     JSON             NULL,
            `changed_fields` JSON             NULL,
            `user_id`        INT              NULL,
            `ip_address`     VARCHAR(45)      NULL,
            `user_agent`     VARCHAR(500)     NULL,
            `timestamp`      DATETIME(3)  NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
            `correlation_id` VARCHAR(50)      NULL,
            PRIMARY KEY (`id`),
            CONSTRAINT `chk_audit_action` CHECK (`action` IN ('INSERT','UPDATE','DELETE'))
        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
        """
    ];

    // ─── Data seeding ─────────────────────────────────────────────────────────

    protected override async Task SeedCoreDataAsync(System.Data.Common.DbConnection connection)
    {
        foreach (var cat in TestDataSeeder.Categories())
        {
            await connection.ExecuteAsync(
                """
                INSERT IGNORE INTO `categories` (`id`,`name`,`slug`,`parent_category_id`,`is_active`,`sort_order`)
                VALUES (@Id, @Name, @Slug, @ParentCategoryId, @IsActive, @SortOrder)
                """,
                new { cat.Id, cat.Name, cat.Slug, cat.ParentCategoryId, IsActive = cat.IsActive ? 1 : 0, cat.SortOrder });
        }
    }

    protected override async Task SeedTestDataAsync(System.Data.Common.DbConnection connection)
    {
        var dataset = Data;

        await using var tx = await connection.BeginTransactionAsync();

        // Customers
        foreach (var c in dataset.Customers)
        {
            await connection.ExecuteAsync(
                """
                INSERT IGNORE INTO `customers` (`id`,`name`,`email`,`phone`,`tax_id`,`is_active`,`created_at`,`updated_at`)
                VALUES (@Id, @Name, @Email, @Phone, @TaxId, @IsActive, @CreatedAt, @UpdatedAt)
                """,
                new { c.Id, c.Name, c.Email, c.Phone, c.TaxId, IsActive = c.IsActive ? 1 : 0,
                      c.CreatedAt, c.UpdatedAt },
                tx);
        }

        // Products
        foreach (var p in dataset.Products)
        {
            await connection.ExecuteAsync(
                """
                INSERT IGNORE INTO `products` (`id`,`category_id`,`name`,`sku`,`description`,`price`,`cost_price`,`stock`,`min_stock`,`is_active`,`created_at`)
                VALUES (@Id, @CategoryId, @Name, @Sku, @Description, @Price, @CostPrice, @Stock, @MinStock, @IsActive, @CreatedAt)
                """,
                new { p.Id, p.CategoryId, p.Name, p.Sku, p.Description, p.Price, p.CostPrice, p.Stock, p.MinStock,
                      IsActive = p.IsActive ? 1 : 0, p.CreatedAt },
                tx);
        }

        // Orders
        foreach (var o in dataset.Orders)
        {
            await connection.ExecuteAsync(
                """
                INSERT IGNORE INTO `orders` (`id`,`customer_id`,`status`,`notes`,`total_amount`,`tax_amount`,`discount_amount`,`currency`,`created_at`,`is_deleted`,`deleted_at`)
                VALUES (@Id, @CustomerId, @Status, @Notes, @TotalAmount, @TaxAmount, @DiscountAmount, @Currency, @CreatedAt, @IsDeleted, @DeletedAt)
                """,
                new { o.Id, o.CustomerId, o.Status, o.Notes, o.TotalAmount, o.TaxAmount, o.DiscountAmount,
                      o.Currency, o.CreatedAt, IsDeleted = o.IsDeleted ? 1 : 0, o.DeletedAt },
                tx);
        }

        // OrderItems
        foreach (var oi in dataset.OrderItems)
        {
            await connection.ExecuteAsync(
                """
                INSERT IGNORE INTO `order_items` (`id`,`order_id`,`product_id`,`quantity`,`unit_price`,`discount_percent`,`total_price`)
                VALUES (@Id, @OrderId, @ProductId, @Quantity, @UnitPrice, @DiscountPercent, @TotalPrice)
                """,
                new { oi.Id, oi.OrderId, oi.ProductId, oi.Quantity, oi.UnitPrice, oi.DiscountPercent, oi.TotalPrice },
                tx);
        }

        await tx.CommitAsync();
    }
}




