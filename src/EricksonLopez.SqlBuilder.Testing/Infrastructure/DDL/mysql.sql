-- =============================================================================
-- EricksonLopez.SqlBuilder — MySQL 8.0+ DDL
-- Version: 1.0.0 | Target: MySQL 8.0+ / MariaDB 10.5+
-- =============================================================================
-- Notes:
--   • ENGINE=InnoDB for ACID transactions and foreign key support
--   • DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci for full Unicode
--   • AUTO_INCREMENT for identity columns (MySQL classic approach)
--   • DATETIME(6) for microsecond precision (UTC_TIMESTAMP(6) for UTC)
--   • JSON native type (MySQL 5.7.8+, 8.0 has JSON_TABLE, JSON_OVERLAPS, etc.)
--   • TINYINT(1) for boolean values (MySQL has no native BOOLEAN, but alias works)
--   • VARCHAR with length limits (unlike PostgreSQL TEXT, MySQL optimizes VARCHAR)
--   • GENERATED COLUMNS for computed fields (virtual/stored)
--   • No partitioned audit_logs here (use RANGE PARTITION BY YEAR/MONTH in prod)
-- =============================================================================

-- ─── Drop existing tables ─────────────────────────────────────────────────────
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS `audit_logs`;
DROP TABLE IF EXISTS `user_roles`;
DROP TABLE IF EXISTS `payments`;
DROP TABLE IF EXISTS `invoices`;
DROP TABLE IF EXISTS `order_items`;
DROP TABLE IF EXISTS `orders`;
DROP TABLE IF EXISTS `products`;
DROP TABLE IF EXISTS `categories`;
DROP TABLE IF EXISTS `addresses`;
DROP TABLE IF EXISTS `customers`;
DROP TABLE IF EXISTS `roles`;
DROP TABLE IF EXISTS `users`;
SET FOREIGN_KEY_CHECKS = 1;

-- ─── Customers ────────────────────────────────────────────────────────────────
CREATE TABLE `customers` (
    `id`         INT            NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `name`       VARCHAR(255)   NOT NULL,
    `email`      VARCHAR(320)   NOT NULL,
    `phone`      VARCHAR(50),
    `tax_id`     VARCHAR(50),
    `is_active`  TINYINT(1)     NOT NULL DEFAULT 1,
    `created_at` DATETIME(6)    NOT NULL DEFAULT UTC_TIMESTAMP(6),
    `updated_at` DATETIME(6),
    UNIQUE KEY `uq_customers_email` (`email`),
    INDEX `idx_customers_is_active` (`is_active`),
    INDEX `idx_customers_created_at` (`created_at` DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Business and individual customers';

-- ─── Addresses ────────────────────────────────────────────────────────────────
CREATE TABLE `addresses` (
    `id`           INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `customer_id`  INT          NOT NULL,
    `street`       VARCHAR(500) NOT NULL,
    `city`         VARCHAR(100) NOT NULL,
    `state`        VARCHAR(100) NOT NULL,
    `country`      VARCHAR(100) NOT NULL DEFAULT 'US',
    `postal_code`  VARCHAR(20)  NOT NULL,
    `address_type` VARCHAR(20)  NOT NULL DEFAULT 'shipping',
    `is_default`   TINYINT(1)   NOT NULL DEFAULT 0,
    INDEX `idx_addresses_customer_id` (`customer_id`),
    CONSTRAINT `fk_addresses_customer` FOREIGN KEY (`customer_id`) REFERENCES `customers`(`id`) ON DELETE CASCADE,
    CONSTRAINT `chk_address_type` CHECK (`address_type` IN ('shipping','billing'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Categories ───────────────────────────────────────────────────────────────
CREATE TABLE `categories` (
    `id`                 INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `name`               VARCHAR(200) NOT NULL,
    `slug`               VARCHAR(200) NOT NULL,
    `parent_category_id` INT,
    `is_active`          TINYINT(1)   NOT NULL DEFAULT 1,
    `sort_order`         INT          NOT NULL DEFAULT 0,
    UNIQUE KEY `uq_categories_slug` (`slug`),
    INDEX `idx_categories_parent_id` (`parent_category_id`),
    CONSTRAINT `fk_categories_parent` FOREIGN KEY (`parent_category_id`) REFERENCES `categories`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Products ─────────────────────────────────────────────────────────────────
CREATE TABLE `products` (
    `id`          INT            NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `category_id` INT            NOT NULL,
    `name`        VARCHAR(400)   NOT NULL,
    `sku`         VARCHAR(100)   NOT NULL,
    `description` TEXT,
    `price`       DECIMAL(18,4)  NOT NULL,
    `cost_price`  DECIMAL(18,4)  NOT NULL DEFAULT 0.0000,
    `stock`       INT            NOT NULL DEFAULT 0,
    `min_stock`   INT            NOT NULL DEFAULT 0,
    `is_active`   TINYINT(1)     NOT NULL DEFAULT 1,
    `created_at`  DATETIME(6)    NOT NULL DEFAULT UTC_TIMESTAMP(6),
    `updated_at`  DATETIME(6),
    -- MySQL GENERATED COLUMN: computed margin percentage
    `margin_percent` DECIMAL(10,4) GENERATED ALWAYS AS (
        CASE WHEN `price` > 0 THEN ((`price` - `cost_price`) / `price`) * 100 ELSE 0 END
    ) VIRTUAL,
    UNIQUE KEY `uq_products_sku` (`sku`),
    INDEX `idx_products_category_id` (`category_id`),
    INDEX `idx_products_price` (`price`),
    INDEX `idx_products_is_active` (`is_active`),
    CONSTRAINT `fk_products_category` FOREIGN KEY (`category_id`) REFERENCES `categories`(`id`) ON DELETE RESTRICT,
    CONSTRAINT `chk_product_price` CHECK (`price` >= 0),
    CONSTRAINT `chk_cost_price` CHECK (`cost_price` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Orders ───────────────────────────────────────────────────────────────────
CREATE TABLE `orders` (
    `id`              INT           NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `customer_id`     INT           NOT NULL,
    `status`          VARCHAR(20)   NOT NULL DEFAULT 'pending',
    `notes`           TEXT,
    `total_amount`    DECIMAL(18,4) NOT NULL,
    `tax_amount`      DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
    `discount_amount` DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
    `currency`        CHAR(3)       NOT NULL DEFAULT 'USD',
    `created_at`      DATETIME(6)   NOT NULL DEFAULT UTC_TIMESTAMP(6),
    `confirmed_at`    DATETIME(6),
    `shipped_at`      DATETIME(6),
    `delivered_at`    DATETIME(6),
    `is_deleted`      TINYINT(1)    NOT NULL DEFAULT 0,
    `deleted_at`      DATETIME(6),
    INDEX `idx_orders_customer_id` (`customer_id`),
    INDEX `idx_orders_status` (`status`),
    INDEX `idx_orders_created_at` (`created_at` DESC),
    INDEX `idx_orders_not_deleted` (`customer_id`, `created_at` DESC),
    CONSTRAINT `fk_orders_customer` FOREIGN KEY (`customer_id`) REFERENCES `customers`(`id`) ON DELETE RESTRICT,
    CONSTRAINT `chk_order_status` CHECK (`status` IN ('pending','confirmed','shipped','delivered','cancelled')),
    CONSTRAINT `chk_order_total` CHECK (`total_amount` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Order Items ──────────────────────────────────────────────────────────────
CREATE TABLE `order_items` (
    `id`               INT           NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `order_id`         INT           NOT NULL,
    `product_id`       INT           NOT NULL,
    `quantity`         INT           NOT NULL,
    `unit_price`       DECIMAL(18,4) NOT NULL,
    `discount_percent` DECIMAL(5,2)  NOT NULL DEFAULT 0.00,
    `total_price`      DECIMAL(18,4) NOT NULL,
    `notes`            TEXT,
    INDEX `idx_order_items_order_id` (`order_id`),
    INDEX `idx_order_items_product_id` (`product_id`),
    CONSTRAINT `fk_order_items_order`   FOREIGN KEY (`order_id`)   REFERENCES `orders`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_order_items_product` FOREIGN KEY (`product_id`) REFERENCES `products`(`id`) ON DELETE RESTRICT,
    CONSTRAINT `chk_oi_qty`   CHECK (`quantity` > 0),
    CONSTRAINT `chk_oi_price` CHECK (`unit_price` >= 0),
    CONSTRAINT `chk_oi_disc`  CHECK (`discount_percent` BETWEEN 0 AND 100),
    CONSTRAINT `chk_oi_total` CHECK (`total_price` >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Invoices ─────────────────────────────────────────────────────────────────
CREATE TABLE `invoices` (
    `id`              INT           NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `order_id`        INT           NOT NULL,
    `invoice_number`  VARCHAR(100)  NOT NULL,
    `status`          VARCHAR(20)   NOT NULL DEFAULT 'draft',
    `subtotal_amount` DECIMAL(18,4) NOT NULL,
    `tax_amount`      DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
    `total_amount`    DECIMAL(18,4) NOT NULL,
    `paid_amount`     DECIMAL(18,4) NOT NULL DEFAULT 0.0000,
    `currency`        CHAR(3)       NOT NULL DEFAULT 'USD',
    `issued_at`       DATETIME(6)   NOT NULL DEFAULT UTC_TIMESTAMP(6),
    `due_at`          DATETIME(6)   NOT NULL,
    `paid_at`         DATETIME(6),
    `notes`           TEXT,
    UNIQUE KEY `uq_invoices_number` (`invoice_number`),
    INDEX `idx_invoices_order_id` (`order_id`),
    INDEX `idx_invoices_status` (`status`),
    INDEX `idx_invoices_due_at` (`due_at`),
    CONSTRAINT `fk_invoices_order` FOREIGN KEY (`order_id`) REFERENCES `orders`(`id`) ON DELETE RESTRICT,
    CONSTRAINT `chk_inv_status` CHECK (`status` IN ('draft','issued','paid','overdue','cancelled'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Payments ─────────────────────────────────────────────────────────────────
CREATE TABLE `payments` (
    `id`               INT           NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `invoice_id`       INT           NOT NULL,
    `amount`           DECIMAL(18,4) NOT NULL,
    `method`           VARCHAR(30)   NOT NULL,
    `status`           VARCHAR(20)   NOT NULL DEFAULT 'pending',
    `transaction_ref`  VARCHAR(200),
    `gateway_response` TEXT,
    `paid_at`          DATETIME(6)   NOT NULL DEFAULT UTC_TIMESTAMP(6),
    `refunded_at`      DATETIME(6),
    `refunded_amount`  DECIMAL(18,4),
    INDEX `idx_payments_invoice_id` (`invoice_id`),
    INDEX `idx_payments_status` (`status`),
    INDEX `idx_payments_transaction_ref` (`transaction_ref`),
    CONSTRAINT `fk_payments_invoice` FOREIGN KEY (`invoice_id`) REFERENCES `invoices`(`id`) ON DELETE RESTRICT,
    CONSTRAINT `chk_pay_amount` CHECK (`amount` > 0),
    CONSTRAINT `chk_pay_status` CHECK (`status` IN ('pending','completed','failed','refunded')),
    CONSTRAINT `chk_pay_method` CHECK (`method` IN ('credit_card','bank_transfer','paypal','stripe','cash','check'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Users ────────────────────────────────────────────────────────────────────
CREATE TABLE `users` (
    `id`                    INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `username`              VARCHAR(100) NOT NULL,
    `email`                 VARCHAR(320) NOT NULL,
    `password_hash`         VARCHAR(500) NOT NULL,
    `first_name`            VARCHAR(100),
    `last_name`             VARCHAR(100),
    `is_active`             TINYINT(1)   NOT NULL DEFAULT 1,
    `email_verified`        TINYINT(1)   NOT NULL DEFAULT 0,
    `created_at`            DATETIME(6)  NOT NULL DEFAULT UTC_TIMESTAMP(6),
    `last_login_at`         DATETIME(6),
    `locked_until`          DATETIME(6),
    `failed_login_attempts` INT          NOT NULL DEFAULT 0,
    -- MySQL 5.7+ generated column: full name
    `full_name` VARCHAR(201) GENERATED ALWAYS AS (
        CASE
            WHEN `first_name` IS NOT NULL AND `last_name` IS NOT NULL
                THEN CONCAT(`first_name`, ' ', `last_name`)
            WHEN `first_name` IS NOT NULL THEN `first_name`
            ELSE `username`
        END
    ) VIRTUAL,
    UNIQUE KEY `uq_users_username` (`username`),
    UNIQUE KEY `uq_users_email` (`email`),
    INDEX `idx_users_is_active` (`is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Roles ────────────────────────────────────────────────────────────────────
CREATE TABLE `roles` (
    `id`          INT         NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `name`        VARCHAR(100) NOT NULL,
    `description` VARCHAR(500),
    `permissions` JSON        NOT NULL,
    `is_system`   TINYINT(1)  NOT NULL DEFAULT 0,
    `created_at`  DATETIME(6) NOT NULL DEFAULT UTC_TIMESTAMP(6),
    UNIQUE KEY `uq_roles_name` (`name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── User Roles ───────────────────────────────────────────────────────────────
CREATE TABLE `user_roles` (
    `user_id`             INT        NOT NULL,
    `role_id`             INT        NOT NULL,
    `assigned_at`         DATETIME(6) NOT NULL DEFAULT UTC_TIMESTAMP(6),
    `assigned_by_user_id` INT,
    PRIMARY KEY (`user_id`, `role_id`),
    INDEX `idx_user_roles_user_id` (`user_id`),
    INDEX `idx_user_roles_role_id` (`role_id`),
    CONSTRAINT `fk_ur_user`        FOREIGN KEY (`user_id`) REFERENCES `users`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_ur_role`        FOREIGN KEY (`role_id`) REFERENCES `roles`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_ur_assigned_by` FOREIGN KEY (`assigned_by_user_id`) REFERENCES `users`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Audit Logs ───────────────────────────────────────────────────────────────
CREATE TABLE `audit_logs` (
    `id`             BIGINT       NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `entity_name`    VARCHAR(100) NOT NULL,
    `entity_id`      VARCHAR(100) NOT NULL,
    `action`         VARCHAR(10)  NOT NULL,
    `old_values`     JSON,
    `new_values`     JSON,
    `changed_fields` JSON,
    `user_id`        INT,
    `ip_address`     VARCHAR(45),
    `user_agent`     VARCHAR(500),
    `timestamp`      DATETIME(6)  NOT NULL DEFAULT UTC_TIMESTAMP(6),
    `correlation_id` VARCHAR(200),
    INDEX `idx_audit_entity`    (`entity_name`, `entity_id`),
    INDEX `idx_audit_timestamp` (`timestamp` DESC),
    INDEX `idx_audit_user_id`   (`user_id`),
    INDEX `idx_audit_action`    (`action`),
    CONSTRAINT `chk_audit_action` CHECK (`action` IN ('INSERT','UPDATE','DELETE'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ─── Seed Data ────────────────────────────────────────────────────────────────
INSERT INTO `roles` (`name`, `description`, `permissions`, `is_system`) VALUES
    ('admin',   'Full system administrator', JSON_ARRAY('*'), 1),
    ('manager', 'Can manage orders, products, customers', JSON_ARRAY('orders:*','products:*','customers:*'), 1),
    ('viewer',  'Read-only access', JSON_ARRAY('orders:read','products:read','customers:read'), 1),
    ('support', 'Customer support role', JSON_ARRAY('orders:read','customers:*'), 0);

INSERT INTO `categories` (`name`, `slug`, `parent_category_id`, `sort_order`) VALUES
    ('Electronics',        'electronics',    NULL, 1),
    ('Clothing & Apparel', 'clothing',       NULL, 2),
    ('Home & Garden',      'home-garden',    NULL, 3),
    ('Books & Media',      'books-media',    NULL, 4),
    ('Laptops',            'laptops',        1,    1),
    ('Smartphones',        'smartphones',    1,    2),
    ('Accessories',        'accessories',    1,    3),
    ("Men's Clothing",     'mens-clothing',  2,    1),
    ("Women's Clothing",   'womens-clothing',2,    2),
    ('Gaming Laptops',     'gaming-laptops', 5,    1);
